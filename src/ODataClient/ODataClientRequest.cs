// -----------------------------------------------------------------------
// <copyright file="ODataClientRequest.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Data.Services.Client;
using PD.Base.EntityRepository.Api;

namespace PD.Base.EntityRepository.ODataClient
{
	/// <summary>
	/// Base class for everything that executes an OData request, or part of a batch request.
	/// </summary>
	internal abstract class ODataClientRequest : IRequest
	{

		protected ODataClientRequest()
		{}

		/// <summary>
		/// Copy constructor.
		/// </summary>
		/// <param name="copy"></param>
		protected ODataClientRequest(ODataClientRequest copy)
		{
			_requestState = copy._requestState;
			Exception = copy.Exception;
		}

		private RequestState _requestState = RequestState.NotSent;

		#region IRequest

		public bool IsCompleted
		{
			get { return (_requestState == RequestState.Completed) || (_requestState == RequestState.CompletedWithError); }
		}

		public bool IsFaulted
		{
			get { return _requestState == RequestState.CompletedWithError; }
		}

		public bool IsCompletedSuccessfully
		{
			get { return _requestState == RequestState.Completed; }
		}

		public Exception Exception { get; private set; }

		#endregion

		internal virtual DataServiceRequest SendingRequest()
		{
			_requestState = RequestState.Sending;

			// Subclasses need to return a real DataServiceRequest
			return null;
		}

		/// <summary>
		/// In batch operations, this method is called to determine if this <see cref="ODataClientRequest"/>
		/// matches the specified <see cref="OperationResponse"/>.
		/// </summary>
		/// <param name="operationResponse"></param>
		/// <returns></returns>
		internal abstract bool IsRequestFor(OperationResponse operationResponse);

		/// <summary>
		/// Failed with an exception before a response could be received.
		/// </summary>
		/// <param name="exception"></param>
		internal virtual void Failed(Exception exception)
		{
			_requestState = RequestState.CompletedWithError;
			Exception = exception;
		}

		internal virtual void HandleResponse(ODataClient client, OperationResponse operationResponse)
		{
			_requestState = RequestState.Completed;
			if (operationResponse.Error != null)
			{
			    if (operationResponse.StatusCode != 404) 
                {
                    _requestState = RequestState.CompletedWithError;
                    Exception = operationResponse.Error;   
			    }
			} 
			else if (operationResponse.StatusCode < 200 || operationResponse.StatusCode > 250)
			{
				_requestState = RequestState.CompletedWithError;
			}
		}


		private enum RequestState : byte
		{

			NotSent,
			Sending,
			Completed,
			CompletedWithError

		}

	}
}
