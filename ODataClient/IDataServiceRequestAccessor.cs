// -----------------------------------------------------------------------
// <copyright file="IDataServiceRequestAccessor.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Data.Services.Client;

namespace PD.Base.EntityRepository.ODataClient
{
	/// <summary>
	/// Supports converting an object to a <see cref="DataServiceRequest"/>.
	/// </summary>
	internal interface IDataServiceRequestAccessor
	{
		DataServiceRequest GetDataServiceRequest();
	}
}
