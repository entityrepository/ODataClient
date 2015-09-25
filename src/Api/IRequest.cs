// -----------------------------------------------------------------------
// <copyright file="IRequest.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace PD.Base.EntityRepository.Api
{
#pragma warning disable 0419
	/// <summary>
	/// Base interface for requests issued via <see cref="DataContext.InvokeAsync"/>
	/// </summary>
#pragma warning restore 0419
	public interface IRequest
	{

		/// <summary>
		/// Whether the request is completed.  If <c>false</c> is returned,
		/// the request is either processing or it has not been started.
		/// </summary>
		bool IsCompleted { get; }

		/// <summary>
		/// Whether an exception occurred when the request was processing.  <c>IsFaulted</c>
		/// will only return <c>true</c> when <see cref="IsCompleted"/> is <c>true</c>.
		/// </summary>
		bool IsFaulted { get; }

		/// <summary>
		/// Whether the request has completed successfully.
		/// </summary>
		bool IsCompletedSuccessfully { get; }

		/// <summary>
		/// If <see cref="IsFaulted"/> is <c>true</c>, this property will
		/// return a non-<c>null</c> <see cref="Exception"/> describing the cause of the fault.
		/// </summary>
		Exception Exception { get; }

	}
}
