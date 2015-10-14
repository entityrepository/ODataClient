// -----------------------------------------------------------------------
// <copyright file="odata.svc.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Data.Entity;
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
			config.DataServiceBehavior.IncludeAssociationLinksInResponse = true;
			config.DataServiceBehavior.MaxProtocolVersion = DataServiceProtocolVersion.V3;

			// Skip database initialization so we can use the EF 6.0 version of the database with EF 5.0
			Database.SetInitializer<ScrumDb>(null);
		}

		protected override ScrumDb CreateDataSource()
		{
			ScrumDb db = new ScrumDb();
			db.AttachDbEnums();

			// This is needed b/c ID values are passed back from the client, which may reference entities that aren't yet loaded.
			db.Configuration.ValidateOnSaveEnabled = false;
			return db;
		}

		protected override void OnStartProcessingRequest(ProcessRequestArgs args)
		{
			base.OnStartProcessingRequest(args);
		}

		protected override void HandleException(HandleExceptionArgs args)
		{
			base.HandleException(args);
		}

	}
}
