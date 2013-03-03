// -----------------------------------------------------------------------
// <copyright file="odata.svc.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Data.Services;
using System.Data.Services.Common;
using System.ServiceModel;
using Scrum.Dal;

namespace Scrum.Web
{

	[ServiceBehavior(IncludeExceptionDetailInFaults = true)]
	public class ScrumDataService : DataService<ScrumDb>
	{

		// This method is called only once to initialize service-wide policies.
		public static void InitializeService(DataServiceConfiguration config)
		{
			// Set rules to indicate which entity sets and service operations are visible, updatable, etc.
			config.SetEntitySetAccessRule("Users", EntitySetRights.AllRead);
			config.SetEntitySetAccessRule("*", EntitySetRights.All);
			config.SetEntitySetPageSize("*", 50);

#if DEBUG
			config.UseVerboseErrors = true;
#endif

			config.DataServiceBehavior.MaxProtocolVersion = DataServiceProtocolVersion.V3;
		}

	}
}
