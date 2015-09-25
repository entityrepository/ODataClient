// -----------------------------------------------------------------------
// <copyright file="EntityState.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace PD.Base.EntityRepository.Api
{
	/// <summary>
	/// Tracks the state of an entity within an <see cref="IEditRepository{TEntity}"/>.
	/// </summary>
	public enum EntityState
	{

		/// <summary>
		/// Default value - its presence generally indicates an error.
		/// </summary>
		Uninitialized,

		/// <summary>
		/// The entity is not stored in the <see cref="IEditRepository{TEntity}"/>.
		/// </summary>
		Detached,

		/// <summary>
		/// The entity is stored in the <see cref="IEditRepository{TEntity}"/>, and it has not been modified.
		/// </summary>
		Unmodified,

		/// <summary>
		/// The entity is stored in the <see cref="IEditRepository{TEntity}"/>, and it is marked as added.
		/// </summary>
		Added,

		/// <summary>
		/// The entity is stored in the <see cref="IEditRepository{TEntity}"/>, and it is marked as modified.
		/// </summary>
		Modified,

		/// <summary>
		/// The entity is stored in the <see cref="IEditRepository{TEntity}"/>, and it has been marked as deleted.
		/// </summary>
		Deleted

	}
}
