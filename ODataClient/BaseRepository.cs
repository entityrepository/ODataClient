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
		// The parent ODataClient
		private readonly ODataClient _odataClient;
		// The query that returns all items in the repository
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

		/// <summary>
		/// When entities come in off the wire when returned from a request, they are added to the local collection by calling this
		/// method.
		///
		/// In addition, the repository may freeze or enable change tracking on the entities.
		/// </summary>
		/// <param name="entities">The set of entities to be added to the local collection.</param>
		/// <returns>The same logical set of entities as <paramref name="entities"/>, though some
		/// of the entities may be replaced due to deduplication.</returns>
		internal abstract TEntity[] ProcessQueryResults(IEnumerable<TEntity> entities);

		#region IRepository

		public string Name { get; private set; }

		public abstract void ClearLocal();

		public Type EntityType
		{
			get { return typeof(TEntity); }
		}

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