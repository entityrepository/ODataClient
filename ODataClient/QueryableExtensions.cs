// -----------------------------------------------------------------------
// <copyright file="QueryableExtensions.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Data.Services.Client;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;

namespace PD.Base.EntityRepository.ODataClient
{
	/// <summary>
	/// Extension methods for <see cref="IQueryable"/>.
	/// </summary>
	public static class QueryableExtensions
	{

		private static ODataClientQuery<TEntity> ConvertQueryableToODataClientQuery<TEntity>(IQueryable queryable)
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
		/// Adds support for <c>Include</c> to <see cref="ODataClient"/> queries.
		/// </summary>
		/// <typeparam name="TEntity"></typeparam>
		/// <typeparam name="TProperty"></typeparam>
		/// <param name="source">An <see cref="IQueryable"/> that resolves to a <see cref="DataServiceQuery"/>.</param>
		/// <param name="navigationProperty">An expression indicating the property to include</param>
		/// <returns></returns>
		public static IQueryable<TEntity> Include<TEntity, TProperty>(
			this IQueryable<TEntity> source, Expression<Func<TEntity, TProperty>> navigationProperty)
		{
			Contract.Requires<ArgumentNullException>(navigationProperty != null);

			ODataClientQuery<TEntity> clientQuery = ConvertQueryableToODataClientQuery<TEntity>(source);
			DataServiceQuery<TEntity> dataServiceQuery = clientQuery.GetDataServiceQuery();
			return new ODataClientQuery<TEntity>(dataServiceQuery.Expand(navigationProperty));
		}

		#endregion
	}
}
