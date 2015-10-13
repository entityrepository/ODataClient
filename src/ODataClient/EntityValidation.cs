// -----------------------------------------------------------------------
// <copyright file="EntityValidation.cs" company="AOL">
// Copyright (c) 2015 AOL Platforms.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;

namespace EntityRepository.ODataClient
{

	/// <summary>
	/// Container class for entity validation data
	/// </summary>
	public static class EntityValidation
	{

		/// <summary>
		/// A key for <see cref="ValidationContext.Items"/>, indicating whether the entity being validated was added to the data context or not.
		/// </summary>
		public const string IsAddedKey = "IsAdded";

		/// <summary>
		/// A key for <see cref="ValidationContext.Items"/>, indicating whether the entity being validated was modified or not.
		/// </summary>
		public const string IsModifiedKey = "IsModified";

	}
}
