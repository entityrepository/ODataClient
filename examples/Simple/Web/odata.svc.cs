// -----------------------------------------------------------------------
// <copyright file="odata.svc.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Data.Services;
using System.Data.Services.Common;
using System.ServiceModel;

namespace Simple.Web
{
	[ServiceBehavior(IncludeExceptionDetailInFaults = true)]
	public class SimpleDataService : DataService<SimpleDbContext>
	{

		// This method is called only once to initialize service-wide policies.
		public static void InitializeService(DataServiceConfiguration config)
		{
			// Set rules to indicate which entity sets and service operations are visible, updatable, etc.
			config.SetEntitySetAccessRule("*", EntitySetRights.All);
			config.SetEntitySetPageSize("*", 50);

			config.DataServiceBehavior.MaxProtocolVersion = DataServiceProtocolVersion.V3;

			config.UseVerboseErrors = true;
		}

	}
}
