// -----------------------------------------------------------------------
// <copyright file="IBaseRepository.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace PD.Base.EntityRepository.Api
{
	/// <summary>
	/// Common functionality between edit and read-only repositories
	/// </summary>
	public interface IBaseRepository
	{
		/// <summary>
		/// Clear all locally cached data.
		/// </summary>
		void ClearLocal();
	}
}
