// -----------------------------------------------------------------------
// <copyright file="ScrumClient.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------


using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PD.Base.PortableUtil.Reflection;
using PD.Base.EntityRepository.Api;
using PD.Base.EntityRepository.ODataClient;
using PD.Base.PortableUtil.Enum;
using Scrum.Model;

namespace Scrum.Web.IntegrationTests
{

	/// <summary>
	/// Client class representing the IDataContext in a more friendly manner (property repositories instead of a lookup)
	/// This is a model for writing a client DataContext.
	/// </summary>
	internal class ScrumClient : DataContext
	{

		public ScrumClient(string scrumServiceUrl)
			: base(new ODataClient(new Uri(scrumServiceUrl), typeof(Project)), DataContextExtensions.SynchronousPreLoadDbEnums)
		{}

		public IEditRepository<Project> Projects { get; private set; }
		public IEditRepository<ProjectArea> ProjectAreas { get; private set; }
		public IEditRepository<ProjectVersion> ProjectVersions { get; private set; }
		public IEditRepository<WorkItem> WorkItems { get; private set; }
		public IEditRepository<Sprint> Sprints { get; private set; }
		public IEditRepository<WorkItemMessage> WorkItemMessages { get; private set; }
		public IEditRepository<WorkItemPropertyChange> WorkItemPropertyChanges { get; private set; }
		public IEditRepository<WorkItemTimeLog> WorkItemTimeLog { get; private set; }

		public IReadOnlyRepository<Priority> Priority { get; private set; }
		public IReadOnlyRepository<Status> Status { get; private set; }

	}

}