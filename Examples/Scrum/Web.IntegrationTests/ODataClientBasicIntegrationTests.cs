// -----------------------------------------------------------------------
// <copyright file="ODataClientBasicIntegrationTests.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;
using System.Threading.Tasks;
using PD.Base.EntityRepository.Api;
using PD.Base.EntityRepository.ODataClient;
using PD.Base.PortableUtil.Enum;
using Scrum.Model;
using Xunit;

namespace Scrum.Web.IntegrationTests
{


	public class ODataClientBasicIntegrationTests
	{

		private const string c_odataTestServiceUrl = "http://localhost:42200/odata.svc/";

		// TODO: Shorten the timeout for real testing to 4000 or so
		internal const int TestTimeout = 600000; // For debugging, this is 10m

		private readonly ScrumClient _client;

		public ODataClientBasicIntegrationTests()
		{
			_client = new ScrumClient(c_odataTestServiceUrl);

			// REVIEW: We should require that this is called before any repository properties are accessed.  But that's somewhat difficult to do,
			// so for now, we have to wait for initialization to complete.
			Assert.True(_client.InitializeTask.Wait(TestTimeout));
		}

		[Fact]
		public void VerifyDbEnumsLoaded()
		{
			short statusId = 2;
			Assert.Same(DbEnumManager.LookupById<short, Status>(statusId), _client.Status.Local.Single(status => status.ID == statusId));
			
			Assert.Equal(Priority.Unknown, _client.Priority.Local.Single(priority => priority.ID == 0));
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
			completion.Wait(TestTimeout);
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
			completion.Wait(TestTimeout);
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
			completion.Wait(TestTimeout);

			Assert.NotNull(workItem);
			Assert.NotEmpty(workItem.Areas);
			Assert.NotEmpty(workItem.TimeLog);
			Assert.NotEmpty(workItem.Subscribers);
			Assert.NotEmpty(workItem.AssignedTo);
		}

		[Fact]
		public void TestAsynchronousNestedInclude()
		{
			var query = _client.WorkItems.Include(wi => wi.Areas.Include(a => a.Owners))
							   .Include(wi => wi.TimeLog.Include(tl => tl.Worker))
			                   .Include(wi => wi.Project.Include(p => p.Areas.Include(a => a.Owners)).Include(p => p.Versions))
			                   .Include(wi => wi.Subscribers)
			                   .Include(wi => wi.AssignedTo);
			WorkItem workItem = null;
			var queryCompletion = _client.InvokeAsync(query);
			var completion = queryCompletion.ContinueWith(
												   task =>
												   {
													   Assert.True(task.IsCompleted);
													   workItem = query.First();
												   });
			completion.Wait(TestTimeout);

			Assert.NotNull(workItem);
			Assert.True(workItem.Areas.Any(a => a.Owners.Any()));
			Assert.NotEmpty(workItem.TimeLog);
			Assert.True(workItem.TimeLog.All(tl => tl.Worker != null));
			Assert.True(workItem.Project.Areas.Any(a => a.Owners.Any()));
			Assert.NotEmpty(workItem.Project.Versions);
			Assert.NotEmpty(workItem.Subscribers);
			Assert.NotEmpty(workItem.AssignedTo);
		}

		[Fact]
		public void BadIncludeExpressionThrows()
		{
			// The expression doesn't include a property selector, so it should throw
			Assert.Throws<InvalidOperationException>(() => _client.WorkItems.Include<WorkItem, ProjectArea>(wi => new ProjectArea()));
		}

		[Fact]
		public void TestPropertyChangeTracking()
		{
			_client.Clear().Wait(TestTimeout);

			// Fetch a work item, and all ProjectAreas
			var workItemQuery = _client.WorkItems.Take(1);
			_client.InvokeAsync(workItemQuery).Wait(TestTimeout);
			WorkItem workItem = workItemQuery.First();
			Assert.Equal(EntityState.Unmodified, _client.WorkItems.GetEntityState(workItem));

			// Change a property to the same value, should result in no change
			TimeSpan? previousTimeEstimate = workItem.TimeEstimate;
			workItem.TimeEstimate = previousTimeEstimate;
			Assert.Equal(EntityState.Unmodified, _client.WorkItems.GetEntityState(workItem));
	
			// Change a property, should result in a tracked change
			workItem.TimeEstimate = new TimeSpan(0, 90, 0);
			Assert.Equal(EntityState.Modified, _client.WorkItems.GetEntityState(workItem));

			// Change it back - what happens?
			workItem.TimeEstimate = previousTimeEstimate;
			Assert.Equal(EntityState.Modified, _client.WorkItems.GetEntityState(workItem));
			// TODO: It would be ideal to perform a diff between the original values and current value, and not
			// track a change if there is no change in values or relationships.

			// TODO: Implement ODataClient Revert
			//// Revert the change
			//_client.WorkItems.Revert(workItem);
			//Assert.Equal(EntityState.Unmodified, _client.WorkItems.GetEntityState(workItem));

			// Make the change again
			workItem.TimeEstimate = new TimeSpan(0, 90, 0);
			Assert.Equal(EntityState.Modified, _client.WorkItems.GetEntityState(workItem));

			// Should reset state to unmodified, not that doing so is a good idea...
			_client.WorkItems.ClearLocal();
			_client.WorkItems.Attach(workItem);
			Assert.Equal(EntityState.Unmodified, _client.WorkItems.GetEntityState(workItem));
		}

		[Fact]
		public void TestOneToOneRelationshipChangeTracking()
		{
			// Setup

			_client.Clear().Wait(TestTimeout);

			// Fetch a work item, and project
			var workItemQuery = _client.WorkItems.Include(wi => wi.Project).Take(1);
			// Query for all Projects
			var allProjectsQuery = _client.Projects.All;
	
			_client.InvokeAsync(workItemQuery, allProjectsQuery).Wait(TestTimeout);
			WorkItem workItem = workItemQuery.First();
			Assert.Equal(EntityState.Unmodified, _client.WorkItems.GetEntityState(workItem));

			int initialProjectID = workItem.Project.ID;


			// Making changes

			// Don't change the value - set the Project to the same
			workItem.Project = _client.Projects.Local.Single(project => project.ID == initialProjectID);
			Assert.Equal(EntityState.Unmodified, _client.WorkItems.GetEntityState(workItem));

			// Change the Project
			workItem.Project = _client.Projects.Local.First(project => project.ID != initialProjectID);
			Assert.Equal(EntityState.Modified, _client.WorkItems.GetEntityState(workItem));

			// Change back
			// TODO: This should probably work, but doesn't
			//workItem.Project = _client.Projects.Local.Single(project => project.ID == initialProjectID);
			//Assert.Equal(EntityState.Unmodified, _client.WorkItems.GetEntityState(workItem));

			// Cleanup
			_client.WorkItems.ClearLocal();
		}

		[Fact(Skip = "TODO: One-to-many change tracking needs work.")]
		public void TestOneToManyRelationshipChangeTracking()
		{
			_client.Clear().Wait(TestTimeout);

			// Fetch a work item, and all ProjectAreas
			var workItemQuery = _client.WorkItems.Include(wi => wi.Project).Include(wi => wi.Areas).Take(1);
			_client.InvokeAsync(workItemQuery).Wait(TestTimeout);
			WorkItem workItem = workItemQuery.First();
			Assert.Equal(EntityState.Unmodified, _client.WorkItems.GetEntityState(workItem));

			var allAreasInProjectQuery = _client.ProjectAreas.Where(area => area.Project.ID == workItem.Project.ID);
			_client.InvokeAsync(allAreasInProjectQuery).Wait(TestTimeout);
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
		}

		/// <summary>
		/// Exercises create, update, and delete operations. These should write through to the integration database,
		/// but the delete should remove the data that is added, so if everything runs successfully there are
		/// effectively no changes to the test database.
		/// </summary>
		[Fact(Skip = "SaveChanges() not fully working.")]
		public void TestCreateUpdateDelete()
		{
			_client.Clear().Wait(TestTimeout);

			// Create a new work item in the INFRA/Logging, assigned to joe

			// Query for associated entities
			var userQuery = _client.Users.Where(user => user.UserName == "joe");
			var projectQuery = _client.Projects.Where(p => p.Key == "INFRA").Include(p => p.Areas).Include(p => p.Versions);
			_client.InvokeAsync(userQuery, projectQuery).Wait(TestTimeout);
			User joeUser = userQuery.Single();
			Project infraProject = projectQuery.Single();
			ProjectArea loggingArea = infraProject.Areas.Single(area => area.Name == "Logging");
			var affectsVersions = infraProject.Versions.Where(version => version.IsReleased);

			// Create the new workitem
			WorkItem workItem = new WorkItem()
			                    {
				                    Project = infraProject,
				                    Areas = { loggingArea },
				                    Creator = joeUser,
									Created = DateTime.Now,
				                    AssignedTo = { joeUser },
				                    Title = "Logger isn't logging",
				                    Description = "I think this is because it's not configured in app.config."
			                    };
			// Create an associated message
			WorkItemMessage message = new WorkItemMessage()
									  {
										  Author = joeUser,
										  Created = DateTime.Now,
										  WorkItem = workItem,
										  Message = "Yep, I verified that it's not configured correctly."
									  };
			workItem.Messages.Add(message);

			_client.WorkItems.Add(workItem);

			// Verify that SaveChanges works
			Assert.Equal(0, workItem.ID); // New
			Assert.Equal(EntityState.Added, _client.WorkItems.GetEntityState(workItem));
			Assert.Equal(EntityState.Added, _client.WorkItemMessages.GetEntityState(message));
			_client.SaveChanges().Wait(TestTimeout);
			Assert.True(workItem.ID > 1); // First WorkItem in db initializer should be ID 1, so adding this should always create a larger ID
			Assert.Equal(EntityState.Unmodified, _client.WorkItems.GetEntityState(workItem));

			// TODO: Verify response - are IDs now present?
			// TODO: Verify update works
			// TODO: Verify delete works

			_client.Clear().Wait(TestTimeout);
		}

		// TODO tests:
		// CUD on included objects
		// Verify client-side validation of [Required] and [StringLength] attributes before SaveChanges() calls the server

		// Change references
		// Add mock repository objects when the server doesn't have them
		// Read-only objects
		// Cache read-only enum tables, then connect/include them on the client
		// Delayed load, single items and collections
		// References with ID and reference - like dbenum
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

	}
}
