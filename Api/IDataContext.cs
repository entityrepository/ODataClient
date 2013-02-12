// -----------------------------------------------------------------------
// <copyright file="IDataContext.cs" company="PrecisionDemand">
// Copyright (c) 2012 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Linq;
using System.Threading.Tasks;

namespace PD.Base.EntityRepository.Api
{
#pragma warning disable 1574
	/// <summary>
	/// Interface defining top-level access to a combined repository and unit-of-work pattern.  <c>IDataContext</c> supports query operations
	/// and CRUD operations over multiple entity types.  The entity types that are exposed, and the types of operations supported for each
	/// entity type are dictated by the implementation.
	/// </summary>
	/// <remarks>
	/// Entity objects will support joining with other entities in the same <c>IDataContext</c>, extension (via <c>Include</c>), and projection
	/// (via <see cref="Queryable.Select"/>).  The functionality may be limited by the implementation.
	/// 
	/// This interface is to be implemented both by database access layers (via EntityFramework), and by web service clients (via OData or equivalent).
	/// All operations that could result in a perceptible delay (all operations that access the network apply) are implemented as asynchronous
	/// operations using TPL classes - though it is possible to circumvent the asynchrony by enumerating <see cref="IQueryable"/> results.
	/// 
	/// Implementations of this interface must be thread-safe.
	/// </remarks>
#pragma warning restore 1574
	public interface IDataContext
	{
		/// <summary>
		/// Returns a task that signals when initialization of the <see cref="IDataContext"/> is complete.
		/// </summary>
		Task InitializeTask { get; }

		/// <summary>
		/// Returns the <see cref="IEditRepository{TEntity}"/> for querying and editing objects of type <typeparamref name="TEntity"/>, or a subclass of 
		/// <c>TEntity</c>.  Entities queried through an <see cref="IEditRepository{TEntity}"/> will be change-tracked.
		/// </summary>
		/// <typeparam name="TEntity">The entity type to edit.</typeparam>
		/// <param name="entitySetName">The name of the entity-set.  This is analogous to a database table name.</param>
		/// <returns>The <see cref="IEditRepository{TEntity}"/> associated with this <c>IDataContext</c> and <paramref name="entitySetName"/>, for querying 
		/// and editing <typeparamref name="TEntity"/> objects.</returns>
		IEditRepository<TEntity> Edit<TEntity>(string entitySetName) where TEntity : class;

		/// <summary>
		/// Returns the <see cref="IReadOnlyRepository{TEntity}"/> for querying objects of type <typeparamref name="TEntity"/>, or a subclass of 
		/// <c>TEntity</c>.  Entities queried through an <see cref="IReadOnlyRepository{TEntity}"/> will not be change-tracked; and they do not support
		/// writing back to the remote repository.
		/// </summary>
		/// <typeparam name="TEntity">The entity type to query.</typeparam>
		/// <param name="entitySetName">The name of the entity-set.  This is analogous to a database table name.</param>
		/// <returns>The <see cref="IReadOnlyRepository{TEntity}"/> associated with this <c>IDataContext</c> and <paramref name="entitySetName"/>, for querying
		/// <typeparamref name="TEntity"/> objects.</returns>
		/// <remarks>
		/// If <typeparamref name="TEntity"/> implements <see cref="IFreezable"/>, all entities will be frozen before they are returned from this repository.
		/// </remarks>
		IReadOnlyRepository<TEntity> ReadOnly<TEntity>(string entitySetName) where TEntity : class;

		/// <summary>
		/// Provides asynchronous execution of one or more queries from the remote repository.  When the task is successfully completed, all <see cref="IQueryable"/> parameters
		/// will contain results that can be enumerated.
		/// </summary>
		/// <param name="queries">The set of queries to execute.</param>
		/// <returns>A TPL <see cref="Task"/> that manages execution and completion of the specified queries.</returns>
		/// <remarks>
		/// TODO: Add support for per-query exceptions in the case of failures.
		/// </remarks>
		Task QueryAsync(params IQueryable[] queries);

		/// <summary>
		/// Provides an asynchronous batch save of all modified entities in all <see cref="IEditRepository{TEntity}"/>s in this <see cref="IDataContext"/>.
		/// </summary>
		/// <returns>A TPL <see cref="Task"/> that manages execution and completion of the batch save operation.</returns>
		/// <remarks>
		/// Upon completion, all previously modified entities will be updated to their current state, and <see cref="IEditRepository{TEntity}.GetEntityState"/> for each
		/// object will return <see cref="EntityState.Unmodified"/>.
		/// 
		/// If an entity that is about to be saved implements <see cref="IValidatable"/>, <see cref="IValidatable.Validate"/> will be called before the entity is saved.
		/// 
		/// TODO: Add support for per-entity-change exceptions in the case of failures.
		/// </remarks>
		Task SaveChanges();

		/// <summary>
		/// Changes all modified entities in all <see cref="IEditRepository{TEntity}"/>s in this <see cref="IDataContext"/> back to the state they were in
		/// when last returned from the remote repository.  In other words, all changes are erased.
		/// </summary>
		void RevertChanges();

		/// <summary>
		/// Clears the local cache for all entity types, so that the <see cref="IDataContext"/> can start new. 
		/// </summary>
		void Clear();
	}
}
