// -----------------------------------------------------------------------
// <copyright file="ODataClientBasicIntegrationTests.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using PD.Base.EntityRepository.Api;
using PD.Base.EntityRepository.ODataClient;
using Scrum.Model;
using Xunit;

namespace Scrum.Web.IntegrationTests
{


	public class ODataClientBasicIntegrationTests
	{

		private const string c_odataTestServiceUrl = "http://localhost:42200/odata.svc/";

		// TODO: Shorten the timeout for real testing to 4000 or so
		private const int c_odataTestTimeout = 600000; // For debugging, this is 10m

		private readonly ScrumClient _client;

		public ODataClientBasicIntegrationTests()
		{
			_client = new ScrumClient(c_odataTestServiceUrl);

			// REVIEW: We should require that this is called before any repository properties are accessed.  But that's somewhat difficult to do,
			// so for now, we have to wait for initialization to complete.
			_client.EnsureInitializationCompleted();
		}

		[Fact]
		public void TestSimpleSelectAsync()
		{
			// Select all
			IQueryable<WorkItem> query = _client.WorkItems.Select(workItem => workItem);
			var queryCompletion = _client.InvokeAsync(query);
			var completion = queryCompletion.ContinueWith(
			                                       task =>
			                                       { // TODO: Error propagation doesn't work
				                                       Assert.False(task.IsFaulted);
				                                       Console.WriteLine(query.First());
			                                       });
			completion.Wait(c_odataTestTimeout);
		}

		[Fact]
		public void TestSimpleAllAsync()
		{
			// No select or anything - just use the whole set
			var query = _client.WorkItems.All;
			Task completion = _client.InvokeAsync(query)
			                         .ContinueWith(
			                                       task =>
			                                       { // TODO: Error propagation doesn't work
				                                       Assert.True(task.IsCompleted);
				                                       Console.WriteLine(query.First());
			                                       });
			completion.Wait(c_odataTestTimeout);
		}

		[Fact]
		public void TestAsynchronousMultiInclude()
		{
			var query = _client.WorkItems.Include(wi => wi.Areas).Include(wi => wi.TimeLog).Include(wi => wi.Subscribers).Include(wi => wi.AssignedTo);
			WorkItem workItem = null;
			var queryCompletion = _client.InvokeAsync(query);
			var completion = queryCompletion.ContinueWith(
			                                       task =>
			                                       {
				                                       Assert.True(task.IsCompleted);
				                                       workItem = query.First();
			                                       });
			completion.Wait(c_odataTestTimeout);

			Assert.NotNull(workItem);
			Assert.NotEmpty(workItem.Areas);
			Assert.NotEmpty(workItem.TimeLog);
			Assert.NotEmpty(workItem.Subscribers);
			Assert.NotEmpty(workItem.AssignedTo);
		}

		[Fact]
		public void TestChangeTracking()
		{
			// Fetch a work item, and all ProjectAreas
			var workItemQuery = _client.WorkItems.Include(wi => wi.Project).Include(wi => wi.Areas).Take(1);
			_client.InvokeAsync(workItemQuery).Wait();
			WorkItem workItem = workItemQuery.First();
			Assert.Equal(EntityState.Unmodified, _client.WorkItems.GetEntityState(workItem));

			var allAreasInProjectQuery = _client.ProjectAreas.Where(area => area.Project.ID == workItem.Project.ID);
			_client.InvokeAsync(allAreasInProjectQuery).Wait();
			ProjectArea[] allAreasInProject = allAreasInProjectQuery.ToArray();

			// Remove all current Areas from the workitem
			workItem.Areas.Clear();
			Assert.Equal(EntityState.Modified, _client.WorkItems.GetEntityState(workItem));

			// Should reset state to unmodified
			_client.WorkItems.Attach(workItem);
			Assert.Equal(EntityState.Unmodified, _client.WorkItems.GetEntityState(workItem));

			ProjectArea sourceControlArea = allAreasInProject.Single(area => area.Name.Equals("Source Control"));
			workItem.Areas.Add(sourceControlArea);
			Assert.Equal(EntityState.Modified, _client.WorkItems.GetEntityState(workItem));

			_client.Clear();
		}

		// TODO tests:
		// Change tracking
		// Create-Update-Delete
		// Change references
		// Add mock repository objects when the server doesn't have them
		// Read-only objects
		// Cache read-only enum tables, then connect/include them on the client
		// Delayed load, single items and collections
		// Nested include
		// References with ID and reference - like dbenum
		// CUD on included objects
		// Check SQL for all of the above
		// Test revert
		// Test subclassed entities
		// IncludeTotalCount

		//[Fact]
		//public void TestDeferredPropertyLoading()
		//{
		//	var workItem = _client.WorkItems.First();
		//	Console.WriteLine(workItem);

		//	// TODO: Make async, then abstract
		//	Assert.Null(workItem.Project);
		//	QueryOperationResponse queryOpResponse = _client.WcfDataServiceContext.LoadProperty(workItem, "Project");
		//	Assert.NotNull(workItem.Project);

		//	Assert.Null(workItem.Status);
		//	queryOpResponse = _client.WcfDataServiceContext.LoadProperty(workItem, "Status");
		//	Assert.NotNull(workItem.Status);

		//	Assert.Null(workItem.Priority);
		//	queryOpResponse = _client.WcfDataServiceContext.LoadProperty(workItem, "Priority");
		//	Assert.NotNull(workItem.Priority);
		//}

		#region Nested type: ScrumClient

		/// <summary>
		/// Client class representing the IDataContext in a more friendly manner (property repositories instead of a lookup)
		/// This is a model for writing a client DataContext.
		/// </summary>
		internal class ScrumClient : DataContext
		{

			private static readonly Type s_modelType = typeof(Project);
			private static readonly Assembly[] s_entityAssemblies = new[] { s_modelType.Assembly };
			private static readonly string[] s_entityNamespaces = new[] { s_modelType.Namespace };

			public ScrumClient(string scrumServiceUrl)
				: base(new ODataClient(new Uri(scrumServiceUrl), s_entityAssemblies, s_entityNamespaces))
			{
				//WcfDataServiceContext = ((ODataClient) base._dataContextImpl).WcfDataServiceContext;
			}

			// TODO: Remove this after initial development
			// Only used for trying stuff out...
			//public DataServiceContext WcfDataServiceContext { get; private set; }


			public IEditRepository<Project> Projects { get; protected set; }
			public IEditRepository<ProjectArea> ProjectAreas { get; protected set; }
			public IEditRepository<WorkItem> WorkItems { get; protected set; }

		}

		#endregion
	}
}
