// -----------------------------------------------------------------------
// <copyright file="InitializationException.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace PD.Base.EntityRepository.Api.Exceptions
{
	/// <summary>
	/// Thrown when the entity repository implementation fails to initialize.
	/// </summary>
	public class InitializationException : Exception
	{

		/// <summary>
		/// Creates an <see cref="InitializationException"/>.
		/// </summary>
		/// <param name="message"></param>
		public InitializationException(string message)
			: base(message)
		{}

		/// <summary>
		/// Creates an <see cref="InitializationException"/>.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="innerException"></param>
		public InitializationException(string message, Exception innerException)
			: base(message, innerException)
		{}

	}
}
