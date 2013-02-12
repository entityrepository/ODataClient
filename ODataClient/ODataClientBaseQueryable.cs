// -----------------------------------------------------------------------
// <copyright file="ODataClientBaseQueryable.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Services.Client;
using System.Linq;
using System.Linq.Expressions;

namespace PD.Base.EntityRepository.ODataClient
{
	/// <summary>
	/// Provides shared functionality for <see cref="ODataClientEditRepository{TEntity}"/> and <see cref="ODataClientReadOnlyRepository{TEntity}"/>. 
	/// </summary>
	/// <typeparam name="TEntity"></typeparam>
	public abstract class ODataClientBaseQueryable<TEntity> : IDataServiceRequestAccessor, IQueryable<TEntity>
		where TEntity : class
	{
		protected readonly DataServiceContext _dataServiceContext;
		protected readonly DataServiceQuery<TEntity> _dataServiceQuery;
		protected readonly Type _elementType;
		protected readonly Uri _entitySetBaseUri;
		protected readonly string _entitySetName;

		internal ODataClientBaseQueryable(DataServiceContext dataServiceContext, string entitySetName)
		{
			_elementType = typeof(TEntity);
			_dataServiceContext = dataServiceContext;
			_entitySetName = entitySetName;
			_dataServiceQuery = _dataServiceContext.CreateQuery<TEntity>(_entitySetName);
			_entitySetBaseUri = _dataServiceQuery.RequestUri;
		}

		#region IDataServiceRequestAccessor Members

		DataServiceRequest IDataServiceRequestAccessor.GetDataServiceRequest()
		{
			return GetDataServiceQuery();
		}

		#endregion

		#region IQueryable<TEntity> Members

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public IEnumerator<TEntity> GetEnumerator()
		{
			return ((IEnumerable<TEntity>) GetDataServiceQuery()).GetEnumerator();
		}

		public Expression Expression
		{
			get { return GetDataServiceQuery().Expression; }
		}

		public Type ElementType
		{
			get { return _elementType; }
		}

		public IQueryProvider Provider
		{
			get { return GetDataServiceQuery().Provider; }
		}

		#endregion

		internal DataServiceQuery<TEntity> GetDataServiceQuery()
		{
			return _dataServiceQuery;
		}
	}
}
