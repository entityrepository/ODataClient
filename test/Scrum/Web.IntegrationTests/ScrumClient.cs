// -----------------------------------------------------------------------
// <copyright file="ScrumClient.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using EntityRepository.Api;
using EntityRepository.ODataClient;
using Scrum.Model;

namespace Scrum.Web.IntegrationTests
{

	/// <summary>
	/// Client class representing the IDataContext in a more friendly manner (property repositories instead of a lookup)
	/// This is a model for writing a client DataContext.
	/// </summary>
	internal class ScrumClient : DataContext
	{

		private const string c_odataTestServiceUrl = "http://localhost:42201/odata/";

		// TODO: Shorten the timeout for real testing to 4000 or so
		internal const int TestTimeout = 600000; // For debugging, this is 10m

		public ScrumClient()
			: this(c_odataTestServiceUrl)
		{}

		public ScrumClient(string scrumServiceUrl)
			: base(new ODataClient(new Uri(scrumServiceUrl), typeof(Project)), null /* DataContextExtensions.SynchronousPreLoadDbEnums*/)
		{}

		public IEditRepository<Project> Projects { get; private set; }
		public IEditRepository<ProjectArea> ProjectAreas { get; private set; }
		public IEditRepository<ProjectVersion> ProjectVersions { get; private set; }
		public IEditRepository<WorkItem> WorkItems { get; private set; }
		public IEditRepository<Sprint> Sprints { get; private set; }
		public IEditRepository<WorkItemMessage> WorkItemMessages { get; private set; }
		public IEditRepository<WorkItemPropertyChange> WorkItemPropertyChanges { get; private set; }
		public IEditRepository<WorkItemTimeLog> WorkItemTimeLog { get; private set; }
		public IEditRepository<User> Users { get; private set; }
        public IEditRepository<UserGroup> UserGroups { get; private set; } 

		public IReadOnlyRepository<Priority> Priority { get; private set; }
		public IReadOnlyRepository<Status> Status { get; private set; }

	}

}
