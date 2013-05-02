// -----------------------------------------------------------------------
// <copyright file="ScrumClient.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Reflection;
using PD.Base.EntityRepository.Api;
using PD.Base.EntityRepository.ODataClient;
using Scrum.Model;

namespace Scrum.Silverlight
{

	/// <summary>
	/// Client class representing the IDataContext in a more friendly manner (property repositories instead of a lookup)
	/// This is a model for writing a client DataContext.
	/// </summary>
	public class ScrumClient : DataContext
	{
		// TODO: Support configuration of service URL
		public const string DefaultUrl = "odata.svc";

		private static readonly Type s_modelType = typeof(Project);
		private static readonly Assembly[] s_entityAssemblies = new[] { s_modelType.Assembly };
		private static readonly string[] s_entityNamespaces = new[] { s_modelType.Namespace };

		public ScrumClient(string scrumServiceUrl)
			: base(new ODataClient(new Uri(scrumServiceUrl, UriKind.Relative), s_entityAssemblies, s_entityNamespaces))
		{}

		public ScrumClient()
			: this(DefaultUrl)
		{}

		public IEditRepository<Project> Projects { get; private set; }
		public IEditRepository<ProjectArea> ProjectAreas { get; private set; }
		public IEditRepository<WorkItem> WorkItems { get; private set; }

		protected override void InitializeRepositoryProperties()
		{
			Projects = EditRepositoryFor(() => Projects);
			ProjectAreas = EditRepositoryFor(() => ProjectAreas);
			WorkItems = EditRepositoryFor(() => WorkItems);
		}
	}

}
