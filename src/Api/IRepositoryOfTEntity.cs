// -----------------------------------------------------------------------
// <copyright file="IRepositoryOfTEntity.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;

namespace EntityRepository.Api
{
	/// <summary>
	/// Generic repository base interface, providing shared members between <see cref="IEditRepository{TEntity}"/> and
	/// <see cref="IReadOnlyRepository{TEntity}"/>.
	/// </summary>
	/// <typeparam name="TEntity"></typeparam>
	[ContractClass(typeof(RepositoryContract<>))]
	public interface IRepository<TEntity> : IRepository, IQueryable<TEntity>
	{

		/// <summary>
		/// The set of locally cached entities.
		/// </summary>
		/// <remarks>
		/// This collection consists of entities that have been previously queried, or have been added via <see cref="Attach"/>.
		/// <para>
		/// This collection can be queried directly when appropriate, to avoid the performance overhead of querying the backing repository.
		/// </para>
		/// </remarks>
		// TODO: Write and use IReadOnlySet, or IReadOnlyObservableSet
		ICollection<TEntity> Local { get; }

		/// <summary>
		/// Provides a query for all entities in this repository.  The repository itself cannot be queried for all items.
		/// </summary>
		IQueryRequest<TEntity> All { get; }

		/// <summary>
		/// Stores an entity in the local cache.
		/// </summary>
		/// <param name="entity">The <typeparamref name="TEntity"/> to store.</param>
		/// <returns>
		/// The attached entity.  This may not be the same object as <paramref name="entity"/>; an equal <typeparamref name="TEntity"/> may be returned.
		/// </returns>
		TEntity Attach(TEntity entity);

		/// <summary>
		/// Removes an entity from the local cache.
		/// </summary>
		/// <param name="entity">The <typeparamref name="TEntity"/> to remove.</param>
		/// <returns>
		/// <c>true</c> if <paramref name="entity"/> was detached; <c>false</c> if <c>entity</c> was not contained in the local cache.
		/// </returns>
		bool Detach(TEntity entity);

#pragma warning disable 0419
		/// <summary>
		/// Creates and returns an <see cref="IRequest"/> that will, when invoked, load the property specified by <paramref name="propertyExpression"/>
		/// on object <paramref name="entity"/>.
		/// </summary>
		/// <typeparam name="TProperty">The property type.</typeparam>
		/// <param name="entity">An entity with a property that is a reference to another entity, which needs to be loaded.</param>
		/// <param name="propertyExpression">An expression indicating the property to be loaded.</param>
		/// <returns>An <see cref="IRequest"/>, which can be invoked asynchronously using <see cref="DataContext.InvokeAsync"/></returns>
		IRequest LoadReference<TProperty>(TEntity entity, Expression<Func<TEntity, TProperty>> propertyExpression) where TProperty : class;
#pragma warning restore 0419

	}


	[ContractClassFor(typeof(IRepository<>))]
	internal abstract class RepositoryContract<TEntity> : IRepository<TEntity>
	{
		#region IRepository<TEntity> Members

		public ICollection<TEntity> Local
		{
			get
			{
				Contract.Ensures(Contract.Result<ICollection<TEntity>>() != null);

				throw new NotImplementedException();
			}
		}

		public IQueryRequest<TEntity> All
		{
			get
			{
				Contract.Ensures(Contract.Result<IQueryRequest<TEntity>>() != null);

				throw new NotImplementedException();
			}
		}

		public TEntity Attach(TEntity entity)
		{
			Contract.Requires<ArgumentNullException>(entity != null);
			Contract.Ensures(Contract.Result<TEntity>() != null);

			throw new NotImplementedException();
		}

		public bool Detach(TEntity entity)
		{
			Contract.Requires<ArgumentNullException>(entity != null);

			throw new NotImplementedException();
		}

		public IRequest LoadReference<TProperty>(TEntity entity, Expression<Func<TEntity, TProperty>> propertyExpression) where TProperty : class
		{
			Contract.Requires<ArgumentNullException>(entity != null);
			Contract.Requires<ArgumentNullException>(propertyExpression != null);
			Contract.Ensures(Contract.Result<IRequest>() != null);

			throw new NotImplementedException();
		}

		#endregion

		#region Members provide by other contract classes

		public abstract string Name { get; }
		public abstract void ClearLocal();
		public abstract Type ElementType { get; }
		public abstract IEnumerable<Type> EntityTypes { get; }

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public abstract IEnumerator<TEntity> GetEnumerator();

		public abstract Expression Expression { get; }
		public abstract IQueryProvider Provider { get; }

		#endregion
	}
}
