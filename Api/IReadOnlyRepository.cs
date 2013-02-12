// -----------------------------------------------------------------------
// <copyright file="IReadOnlyRepository.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.ObjectModel;
using System.Linq;

namespace PD.Base.EntityRepository.Api
{
	/// <summary>
	/// A repository that provides read-only access to entity objects of type <typeparamref name="TEntity"/>.
	/// </summary>
	/// <typeparam name="TEntity">The entity type for this repository.</typeparam>
	/// <remarks>
	///  If <typeparamref name="TEntity"/> implements <see cref="IFreezable"/>, all entities will be frozen before they are returned from this repository.
	/// 
	/// The main difference between <c>IReadOnlyRepository</c> and <see cref="IEditRepository{TEntity}"/> is change-tracking - to query for entities 
	/// and not support editing them, query through an <c>IReadOnlyRepository</c>.  To include support for change-tracking, and saving back to
	/// the remote repository (writing changes back to the web service or database), query from an <see cref="IEditRepository{TEntity}"/>,
	/// write changes to the remote repository via <see cref="IDataContext.SaveChanges"/>. 
	/// </remarks>
	public interface IReadOnlyRepository<TEntity> : IBaseRepository, IQueryable<TEntity>
		where TEntity : class
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
		ReadOnlyObservableCollection<TEntity> Local { get; }

		/// <summary>
		/// Stores an entity in the local cache.
		/// </summary>
		/// <param name="entity">The <typeparamref name="TEntity"/> to store.</param>
		void Attach(TEntity entity);
	}
}
