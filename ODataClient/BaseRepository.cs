// -----------------------------------------------------------------------
// <copyright file="BaseRepository.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------


using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Services.Client;
using System.Linq;
using System.Linq.Expressions;
using PD.Base.EntityRepository.Api;

namespace PD.Base.EntityRepository.ODataClient
{
	/// <summary>
	/// Common repository functionality between edit and readonly repositories.
	/// </summary>
	internal abstract class BaseRepository<TEntity> : IRepository<TEntity>
	{

		private readonly ODataClient _odataClient;
		private readonly ODataClientQuery<TEntity> _baseQuery;

 
		internal BaseRepository(ODataClient odataClient, string entitySetName)
		{
			Name = entitySetName;
			_odataClient = odataClient;
			_baseQuery = new ODataClientQuery<TEntity>(odataClient.DataServiceContext, this);
		}

		internal ODataClient ODataClient
		{
			get { return _odataClient; }
		}

		protected DataServiceContext DataServiceContext
		{
			get { return _odataClient.DataServiceContext; }
		}

		#region IRepository

		public string Name { get; private set; }

		public abstract void ClearLocal();

		#endregion

		#region IRepository<TEntity>

		public abstract ReadOnlyObservableCollection<TEntity> Local { get; }

		public IQueryRequest<TEntity> All
		{
			get { return _baseQuery.Clone(); }
		}

		public TEntity Attach(TEntity entity)
		{
			DataServiceContext.AttachTo(Name, entity);
			return entity;
		}

		public IRequest LoadReference<TProperty>(TEntity entity, Expression<Func<TEntity, TProperty>> propertyExpression) where TProperty : class
		{
			throw new NotImplementedException();
		}

		#endregion

		#region IQueryable<TEntity>

		public IEnumerator<TEntity> GetEnumerator()
		{
			throw new InvalidOperationException("The repository cannot be enumerated without a query - use IRepository.All to enumerate all entities.");
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public Expression Expression
		{
			get { return _baseQuery.Expression; }
		}

		public Type ElementType
		{
			get { return _baseQuery.ElementType; }
		}

		public IQueryProvider Provider
		{
			get { return _baseQuery.Provider; }
		}

		#endregion

	}
}