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
		internal static DataServiceQuery<TEntity> ConvertQueryableToDataServiceQuery<TEntity>(IQueryable queryable)
		{
			Contract.Ensures(Contract.Result<DataServiceQuery<TEntity>>() != null);

			DataServiceQuery<TEntity> dataServiceQuery = queryable as DataServiceQuery<TEntity>;
			if (dataServiceQuery == null)
			{
				IDataServiceRequestAccessor accessor = queryable as IDataServiceRequestAccessor;
				if (accessor != null)
				{
					DataServiceRequest request = accessor.GetDataServiceRequest();
					dataServiceQuery = request as DataServiceQuery<TEntity>;
				}
			}
			if (dataServiceQuery == null)
			{
				throw new ArgumentException(
					string.Format("Query {0} could not be converted to a DataServiceQuery<{1}>", queryable, typeof(TEntity).FullName));
			}
			return dataServiceQuery;
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

			DataServiceQuery<TEntity> dataServiceQuery = ConvertQueryableToDataServiceQuery<TEntity>(source);
			return dataServiceQuery.Expand(navigationProperty);
		}

		#endregion
	}
}
