// -----------------------------------------------------------------------
// <copyright file="QueryableExtensions.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data.Services.Client;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace PD.Base.EntityRepository.ODataClient
{
	/// <summary>
	/// Extension methods for <see cref="IQueryable"/>.
	/// </summary>
	public static class QueryableExtensions
	{

		private static ODataClientQuery<TEntity> ConvertQueryableToODataClientQuery<TEntity>(IQueryable queryable)
			where TEntity : class
		{
			Contract.Requires<ArgumentNullException>(queryable != null);
			Contract.Ensures(Contract.Result<ODataClientQuery<TEntity>>() != null);

			ODataClientQuery<TEntity> odataClientQuery = queryable as ODataClientQuery<TEntity>;
			if (odataClientQuery == null)
			{
				BaseRepository<TEntity> repository = queryable as BaseRepository<TEntity>;
				if (repository != null)
				{
					odataClientQuery = (ODataClientQuery<TEntity>) repository.All;
				}
			}

			if (odataClientQuery == null)
			{
				throw new ArgumentException(
					string.Format("Query {0} could not be converted to a ODataClientQuery<{1}>", queryable, typeof(TEntity).FullName));
			}
			return odataClientQuery;
		}

		#region IQueryable extensions

		/// <summary>
		/// Provides support for <c>Include</c> when building <see cref="ODataClient"/> queries.  Calls to any of the <c>Include</c>
		/// methods are translated into <see cref="DataServiceQuery{TElement}.Expand"/> calls.
		/// </summary>
		/// <typeparam name="TEntity"></typeparam>
		/// <typeparam name="TProperty"></typeparam>
		/// <param name="source">An <see cref="IQueryable"/> that resolves to a <see cref="DataServiceQuery"/>.</param>
		/// <param name="navigationProperty">An expression indicating the property to include</param>
		/// <returns></returns>
		public static IQueryable<TEntity> Include<TEntity, TProperty>(this IQueryable<TEntity> source, Expression<Func<TEntity, TProperty>> navigationProperty)
			where TEntity : class
		{
			Contract.Requires<ArgumentNullException>(navigationProperty != null);

			// Determine the expand paths from the expression tree
			IEnumerable<StringBuilder> sbExpandPaths = CreateExpandPathsForTree(navigationProperty);

			ODataClientQuery<TEntity> clientQuery = ConvertQueryableToODataClientQuery<TEntity>(source);
			DataServiceQuery<TEntity> dataServiceQuery = clientQuery.GetDataServiceQuery();

			// Add all of the Expand paths
			foreach (StringBuilder sbExpandPath in sbExpandPaths)
			{
				dataServiceQuery = dataServiceQuery.Expand(sbExpandPath.ToString());
			}

			// Wrap with an ODataClientQuery
			return new ODataClientQuery<TEntity>(dataServiceQuery);
		}

		/// <summary>
		/// Provides support for nested <c>Include</c> when building <see cref="ODataClient"/> queries.  This method should not be
		/// called directly, but can be used in query expressions.
		/// </summary>
		/// <typeparam name="TEntity"></typeparam>
		/// <typeparam name="TProperty"></typeparam>
		/// <param name="source"></param>
		/// <param name="navigationProperty"></param>
		/// <returns></returns>
		public static IEnumerable<TEntity> Include<TEntity, TProperty>(this IEnumerable<TEntity> source, Expression<Func<TEntity, TProperty>> navigationProperty)
			where TEntity : class
		{
			throw new NotImplementedException("Should only be used within a query expression; should not be called.");
		}

		/// <summary>
		/// Provides support for nested <c>Include</c> when building <see cref="ODataClient"/> queries.  This method should not be
		/// called directly, but can be used in query expressions.
		/// </summary>
		/// <typeparam name="TEntity"></typeparam>
		/// <typeparam name="TProperty"></typeparam>
		/// <param name="source"></param>
		/// <param name="navigationProperty"></param>
		/// <returns></returns>
		// TODO: Consider moving BaseEntity to Base.EntityRepository.Api, then requiring TEntity : BaseEntity - to make this more selective
		public static TEntity Include<TEntity, TProperty>(this TEntity source, Expression<Func<TEntity, TProperty>> navigationProperty)
			where TEntity : class, new()
		{
			throw new NotImplementedException("Should only be used within a query expression; should not be called.");
		}

		/// <summary>
		/// A recursive method that determines the paths to pass to <see cref="DataServiceQuery{TElement}.Expand"/>.
		/// </summary>
		/// <param name="navigationProperty">An expression tree containing navigation properties and chained calls to <c>Include</c></param>
		/// <returns>A collection of expand paths to include in the query.</returns>
		private static IEnumerable<StringBuilder> CreateExpandPathsForTree(LambdaExpression navigationProperty)
		{
			Contract.Requires<ArgumentNullException>(navigationProperty != null);

			while (navigationProperty.CanReduce)
			{
				navigationProperty = (LambdaExpression) navigationProperty.Reduce();
			}
			List<StringBuilder> expandPaths = new List<StringBuilder>();

			// Iterate over linked calls to .Include() and/or property selectors
			// Recurse for nested includes - ie recurse for the navigationProperty argument of Include(, LambdaExpression)
			MemberInfo property = null;
			Expression nextExpression = navigationProperty.Body;
			while (nextExpression != null)
			{
				switch (nextExpression.NodeType)
				{
					case ExpressionType.Call:
						// Must be a call to one of the static Include methods
						MethodCallExpression callExpression = (MethodCallExpression) nextExpression;
						MethodInfo method = callExpression.Method;
						if ((method.Name != "Include")
						    || (method.DeclaringType != typeof(QueryableExtensions)))
						{
							throw new InvalidOperationException("Invalid method call within .Include() : " + callExpression);
						}
						nextExpression = callExpression.Arguments[0];
						Expression navPropertyArg = callExpression.Arguments[1];
						// Lambda expressions seem to always be wrapped in these - presumably to support closures
						if (navPropertyArg.NodeType == ExpressionType.Quote)
						{
							navPropertyArg = ((UnaryExpression) navPropertyArg).Operand;
						}
						Contract.Assert(navPropertyArg is LambdaExpression);

						// Recurse
						IEnumerable<StringBuilder> childPaths = CreateExpandPathsForTree((LambdaExpression) navPropertyArg);
						expandPaths.AddRange(childPaths);
						break;

					case ExpressionType.MemberAccess:
						// Property selector
						MemberExpression memberExpression = (MemberExpression) nextExpression;
						property = memberExpression.Member;
						nextExpression = null;
						break;

					default:
						throw new InvalidOperationException(
							string.Format("ExpressionType {0} not supported in .Include() expressions.  Expression '{1}' could not be processed within full expression '{2}'.",
							              nextExpression.NodeType,
							              nextExpression,
							              navigationProperty));
				}
			}

			if (property == null)
			{
				throw new ArgumentException("One property selector must be present in a .Include() expression: " + navigationProperty);
			}
			if (expandPaths.Any())
			{
				// Prefix this property name before the nested includes
				foreach (var sb in expandPaths)
				{
#if SILVERLIGHT
					sb.Insert(0, "/");
#else
					sb.Insert(0, '/');
#endif
					sb.Insert(0, property.Name);
				}
			}
			else
			{ // No nested includes
				expandPaths.Add(new StringBuilder(property.Name));
			}
			return expandPaths;
		}

		#endregion
	}
}
