// -----------------------------------------------------------------------
// <copyright file="IReadOnlyRepository.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace PD.Base.EntityRepository.Api
{
	/// <summary>
	/// A repository that provides read-only access to entity objects of type <typeparamref name="TEntity"/>.
	/// </summary>
	/// <typeparam name="TEntity">The entity type for this repository.</typeparam>
	/// <remarks>
	///  If <typeparamref name="TEntity"/> implements <c>IFreezable</c>, all entities will be frozen before they are returned from this repository.
	/// 
	/// The main difference between <c>IReadOnlyRepository</c> and <see cref="IEditRepository{TEntity}"/> is change-tracking - to query for entities 
	/// and not support editing them, query through an <c>IReadOnlyRepository</c>.  To include support for change-tracking, and saving back to
	/// the remote repository (writing changes back to the web service or database), query from an <see cref="IEditRepository{TEntity}"/>,
	/// write changes to the remote repository via <see cref="IDataContextImpl.SaveChanges"/>. 
	/// </remarks>
	public interface IReadOnlyRepository<TEntity> : IRepository<TEntity>
		where TEntity : class
	{

	}

}
