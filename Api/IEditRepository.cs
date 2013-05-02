// -----------------------------------------------------------------------
// <copyright file="IEditRepository.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;

namespace PD.Base.EntityRepository.Api
{
	/// <summary>
	/// An <c>IEditRepository</c> represents a queryable and editable collection of all persisted instances of the specified <typeparamref name="TEntity"/> type.  
	/// <c>IEditRepository</c> objects are obtained from an <see cref="IDataContextImpl"/> using the <see cref="IDataContextImpl.Edit{TEntity}"/> method.
	/// </summary>
	/// <typeparam name="TEntity">The entity type for this repository.</typeparam>
	/// <remarks>
	/// The abstraction includes a remote repository containing all entities; and a local working set (the <em>context</em>) containing entities that have been 
	/// queried from the remote repository, or that will be added to the remote repository, and which may have been modified or deleted. 
	/// </remarks>
	[ContractClass(typeof(EditRepositoryContract<>))]
	public interface IEditRepository<TEntity> : IRepository<TEntity>
		where TEntity : class
	{

		/// <summary>
		/// Adds the given <paramref name="entity"/> and connected entities to the context in the <see cref="EntityState.Added"/> state such that it will
		/// be added to the remote repository when <see cref="IDataContextImpl.SaveChanges"/> is called.
		/// </summary>
		/// <param name="entity">The entity to add.</param>
		/// <returns>
		/// The entity.
		/// </returns>
		/// <remarks>
		/// Note that if <paramref name="entity"/> is already in the context in some other state it will have its state set
		/// to Added.  <c>Add()</c> is a no-op if the entity is already in the context in the <see cref="EntityState.Added"/> state.
		/// Connected entities that are not already in the context will be added to the context with state = Added; connected
		/// entities that are already in the context will be not be modified; links to connected entities that do not already exist will
		/// be added.
		/// </remarks>
		TEntity Add(TEntity entity);

		/// <summary>
		/// Sets the state for the given <paramref name="entity"/> to <see cref="EntityState.Deleted"/>, such that it will be
		/// deleted from the remote repository when <see cref="IDataContextImpl.SaveChanges"/> is called.
		/// </summary>
		/// <param name="entity">The entity to delete.</param>
		/// <returns><c>true</c> if <paramref name="entity"/> is already in the context and is eligible for deletion.  <c>false</c> if 
		/// <paramref name="entity"/> is not current in the context and may not be eligible for deletion.</returns>
		/// <remarks>
		/// If <paramref name="entity"/> is not in the context, it will be attached to the context and marked for deletion.
		/// Connected entities that are not already in the context will not be added to the context; connected
		/// entities that are already in the context will be not be modified; links to connected entities that are in the context
		/// will be deleted.
		/// </remarks>
		bool Delete(TEntity entity);

		/// <summary>
		/// Attaches the given <paramref name="entity"/> to the context in the specified <paramref name="entityState"/>.
		/// </summary>
		/// <param name="entity">The entity to attach.</param>
		/// <param name="entityState">The <see cref="EntityState"/> value to associate with <paramref name="entity"/>.</param>
		/// <returns>
		/// The attached entity.  This may not be the same object as <paramref name="entity"/>.
		/// </returns>
		/// <remarks>
		/// Attach does not break apart related objects - it is lower level than <see cref="Add"/> or <see cref="Delete"/>.
		/// <para>
		/// Note that entities that are already in the context in some other state will have their state set to <paramref name="entityState"/>.
		/// <c>Attach</c> is a no-op if the entity is already in the context in the specified state.
		/// </para>
		/// </remarks>
		TEntity Attach(TEntity entity, EntityState entityState = EntityState.Unmodified);

		/// <summary>
		/// Undo all changes to <paramref name="entity"/> so that it matches the state that it was when last returned from the remote repository.
		/// </summary>
		/// <param name="entity">The entity to revert</param>
		/// <returns>The state of the reverted entity after it is reverted.</returns>.
		/// <remarks>
		/// If the entity state for <paramref name="entity"/> is <see cref="EntityState.Unmodified"/> or <see cref="EntityState.Detached"/>, this method does nothing
		/// and returns the same <see cref="EntityState"/>.
		/// 
		/// If the entity state for <paramref name="entity"/> is <see cref="EntityState.Added"/>, this method removes <c>entity</c> 
		/// from the context and returns <see cref="EntityState.Detached"/>.
		/// 
		/// If the entity state for <paramref name="entity"/> is <see cref="EntityState.Modified"/> or <see cref="EntityState.Deleted"/>, this method changes <c>entity</c>'s property values 
		/// back to how they were returned from the remote repository, and returns <see cref="EntityState.Unmodified"/>.
		/// </remarks>
		EntityState Revert(TEntity entity);

		/// <summary>
		/// Returns this repository's <see cref="EntityState"/> for <paramref name="entity"/>.
		/// </summary>
		/// <param name="entity">An entity.</param>
		/// <returns>The current <see cref="EntityState"/> for <paramref name="entity"/>.</returns>
		EntityState GetEntityState(TEntity entity);

	}


	[ContractClassFor(typeof(IEditRepository<>))]
	internal abstract class EditRepositoryContract<TEntity> : IEditRepository<TEntity>
		where TEntity : class
	{
		#region IEditRepository<TEntity> Members

		public IReadOnlyRepository<TEntity> ReadOnly
		{
			get
			{
				Contract.Ensures(Contract.Result<IReadOnlyRepository<TEntity>>() != null);

				throw new NotImplementedException();
			}
		}

		public TEntity Add(TEntity entity)
		{
			Contract.Requires<ArgumentNullException>(entity != null);
			Contract.Ensures(Contract.Result<TEntity>() != null);

			throw new NotImplementedException();
		}

		public bool Delete(TEntity entity)
		{
			Contract.Requires<ArgumentNullException>(entity != null);

			throw new NotImplementedException();
		}

		public TEntity Attach(TEntity entity, EntityState entityState = EntityState.Unmodified)
		{
			Contract.Requires<ArgumentNullException>(entity != null);
			Contract.Ensures(Contract.Result<TEntity>() != null);

			throw new NotImplementedException();
		}

		public EntityState Revert(TEntity entity)
		{
			Contract.Requires<ArgumentNullException>(entity != null);

			throw new NotImplementedException();
		}

		public EntityState GetEntityState(TEntity entity)
		{
			throw new NotImplementedException();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion

		#region Members provide by other contract classes

		public abstract string Name { get; }
		public abstract void ClearLocal();
		public abstract Type ElementType { get; }
		public abstract IEnumerable<Type> EntityTypes { get; }

		public abstract IEnumerator<TEntity> GetEnumerator();

		public abstract Expression Expression { get; }
		public abstract IQueryProvider Provider { get; }

		public abstract ICollection<TEntity> Local { get; }
		public abstract IQueryRequest<TEntity> All { get; }
		public abstract TEntity Attach(TEntity entity);
		public abstract bool Detach(TEntity entity);
		public abstract IRequest LoadReference<TProperty>(TEntity entity, Expression<Func<TEntity, TProperty>> propertyExpression) where TProperty : class;

		#endregion
	}
}
