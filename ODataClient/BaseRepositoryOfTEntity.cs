// -----------------------------------------------------------------------
// <copyright file="BaseRepositoryOfTEntity.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.OData.Client;
using System.Linq;
using System.Linq.Expressions;
using PD.Base.EntityRepository.Api;

namespace PD.Base.EntityRepository.ODataClient
{
	/// <summary>
	/// Common generically-typed repository functionality between edit and readonly repositories.
	/// </summary>
	internal abstract class BaseRepository<TEntity> : BaseRepository, IRepository<TEntity>
		where TEntity : class
	{

		// The query that returns all items in the repository
		private readonly ODataClientQuery<TEntity> _baseQuery;

		internal BaseRepository(ODataClient odataClient, EntitySetInfo entitySetInfo)
			: base(odataClient, entitySetInfo)
		{
			_baseQuery = new ODataClientQuery<TEntity>(odataClient.DataServiceContext, this);
		}

		#region IRepository<TEntity>

		public abstract ICollection<TEntity> Local { get; }

		public IQueryRequest<TEntity> All
		{
			get { return _baseQuery.Clone(); }
		}

		public TEntity Attach(TEntity entity)
		{
			lock (this)
			{
				EntityDescriptor ed = DataServiceContext.GetEntityDescriptor(entity);
				if (ed == null)
				{
					DataServiceContext.AttachTo(Name, entity);
					return entity;
				}
				else
				{
					return (TEntity) ed.Entity;
				}
			}
		}

		public bool Detach(TEntity entity)
		{
			return DataServiceContext.Detach(entity);
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

		public IQueryProvider Provider
		{
			get { return _baseQuery.Provider; }
		}

		#endregion

		internal abstract TEntity ProcessQueryResult(TEntity entity);

		internal override object ProcessQueryResult(object entity)
		{
			// Convert the untyped entity to a call to typed ProcessQueryResult(TEntity)
			if (entity == null)
			{
				return null;
			}
			TEntity typedEntity = entity as TEntity;
			if (typedEntity == null)
			{
				// tracer.Error("Error processing entity {0}; not of TEntity type {1}.", entity, typeof(TEntity));
				return null;
			}
			return ProcessQueryResult(typedEntity);
		}

		/// <summary>
		/// Adds <paramref name="entity"/> to the local cache, with tracking if applicable.  Does <em>not</em> add the entity
		/// to the <c>DataServiceContext</c>.
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="entityState">The <see cref="EntityState"/> for <paramref name="entity"/>.</param>
		/// <returns></returns>
		internal abstract TEntity AddToLocalCache(TEntity entity, EntityState entityState);

		/// <summary>
		/// Removes <paramref name="entity"/> from the local cache, and stops change-tracking of <paramref name="entity"/> if applicable.
		/// Does <em>not</em> remove <c>entity</c> from the <c>DataServiceContext</c>.
		/// </summary>
		/// <param name="entity"></param>
		/// <returns></returns>
		internal abstract bool RemoveFromLocalCache(TEntity entity);

		internal override object AddToLocal(object entity, EntityState entityState)
		{
			TEntity typedEntity = entity as TEntity;
			if (typedEntity == null)
			{
				throw new ArgumentException(string.Format("Entity {0} is type {1} : not compatible with repository {2}", entity, entity.GetType().FullName, this));
			}

			return AddToLocalCache(typedEntity, entityState);
		}

		internal override bool RemoveFromLocal(object entity)
		{
			TEntity typedEntity = entity as TEntity;
			if (typedEntity == null)
			{
				throw new ArgumentException(string.Format("Entity {0} is type {1} : not compatible with repository {2}", entity, entity.GetType().FullName, this));
			}

			return RemoveFromLocalCache(typedEntity);
		}

	}
}
