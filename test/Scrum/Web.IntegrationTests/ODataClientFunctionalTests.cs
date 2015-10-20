// -----------------------------------------------------------------------
// <copyright file="ODataClientFunctionalTests.cs" company="Adap.tv">
// Copyright (c) 2015 Adap.tv.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;
using System.Threading.Tasks;
using EntityRepository.Api;
using EntityRepository.ODataClient;
using Scrum.Model;
using Scrum.Model.Base;
using Xunit;

namespace Scrum.Web.IntegrationTests
{


    public class ODataClientFunctionalTests : IDisposable
    {

        private readonly ScrumClient _client;

        public ODataClientFunctionalTests()
        {
            _client = new ScrumClient();

            // REVIEW: We should require that this is called before any repository properties are accessed.  But that's somewhat difficult to do,
            // so for now, we have to wait for initialization to complete.
            Assert.True(_client.InitializeTask.Wait(ScrumClient.TestTimeout));
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        [Fact]
        public void VerifyDbEnumsLoaded()
        {
            const short statusId = 2;
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
                                                          {
                                                              Assert.False(task.IsFaulted);
                                                              Console.WriteLine(query.First());
                                                          });
            completion.Wait(ScrumClient.TestTimeout);
        }

        [Fact]
        public void TestSimpleAllAsync()
        {
            // No select or anything - just use the whole set
            var query = _client.WorkItems.All;
            Task completion = _client.InvokeAsync(query)
                                     .ContinueWith(
                                                   task =>
                                                   {
                                                       Assert.True(task.IsCompleted);
                                                       Console.WriteLine(query.First());
                                                   });
            Assert.True(completion.Wait(ScrumClient.TestTimeout));
            Assert.NotEmpty(query);
        }

        [Fact]
        public void TestAsynchronousMultiInclude()
        {
            var query =
                _client.WorkItems.Include(wi => wi.Areas)
                       .Include(wi => wi.TimeLog)
                       .Include(wi => wi.Subscribers)
                       .Include(wi => wi.AssignedTo)
                       .Include(wi => wi.Status)
                       .Include(wi => wi.Priority);
            WorkItem[] workItems = null;
            var queryCompletion = _client.InvokeAsync(query);
            var completion = queryCompletion.ContinueWith(
                                                          task =>
                                                          {
                                                              Assert.True(task.IsCompleted);
                                                              workItems = query.ToArray();
                                                          });
            completion.Wait(ScrumClient.TestTimeout);

            Assert.NotNull(workItems);
            Assert.NotEmpty(workItems);
            Assert.True(workItems.Any(wi => wi.Areas.Any()));
            Assert.True(workItems.Any(wi => wi.TimeLog.Any()));
            Assert.True(workItems.Any(wi => wi.Subscribers.Any()));
            Assert.True(workItems.Any(wi => wi.AssignedTo.Any()));
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
            Assert.True(completion.Wait(ScrumClient.TestTimeout));

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
            _client.Clear().Wait(ScrumClient.TestTimeout);

            // Fetch a work item, and all ProjectAreas
            var workItemQuery = _client.WorkItems.Take(1);
            _client.InvokeAsync(workItemQuery).Wait(ScrumClient.TestTimeout);
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
            Assert.Equal(EntityState.Unmodified, _client.WorkItems.GetEntityState(workItem));

            // Make the change again
            workItem.TimeEstimate = new TimeSpan(0, 90, 0);
            Assert.Equal(EntityState.Modified, _client.WorkItems.GetEntityState(workItem));

            // Revert the change
            _client.WorkItems.Revert(workItem);
            Assert.Equal(EntityState.Unmodified, _client.WorkItems.GetEntityState(workItem));

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

            _client.Clear().Wait(ScrumClient.TestTimeout);

            // Fetch a work item, and project
            var workItemQuery = _client.WorkItems.Include(wi => wi.Project).Take(1);
            // Query for all Projects
            var allProjectsQuery = _client.Projects.All;

            _client.InvokeAsync(workItemQuery, allProjectsQuery).Wait(ScrumClient.TestTimeout);
            WorkItem workItem = workItemQuery.First();
            Assert.Equal(EntityState.Unmodified, _client.WorkItems.GetEntityState(workItem));

            int initialProjectID = workItem.Project.ID;


            // Make changes

            // Don't change the value - set the Project to the same
            workItem.Project = _client.Projects.Local.Single(project => project.ID == initialProjectID);
            Assert.Equal(EntityState.Unmodified, _client.WorkItems.GetEntityState(workItem));

            // Change the Project
            workItem.Project = _client.Projects.Local.First(project => project.ID != initialProjectID);
            Assert.Equal(EntityState.Modified, _client.WorkItems.GetEntityState(workItem));

            // Change back
            workItem.Project = _client.Projects.Local.Single(project => project.ID == initialProjectID);
            Assert.Equal(EntityState.Unmodified, _client.WorkItems.GetEntityState(workItem));

            // Change the Project
            workItem.Project = _client.Projects.Local.First(project => project.ID != initialProjectID);
            Assert.Equal(EntityState.Modified, _client.WorkItems.GetEntityState(workItem));

            // Revert
            _client.WorkItems.Revert(workItem);
            Assert.Equal(EntityState.Unmodified, _client.WorkItems.GetEntityState(workItem));
            Assert.Equal(initialProjectID, workItem.Project.ID);

            // Cleanup
            _client.WorkItems.ClearLocal();
        }

        [Fact]
        public void TestOneToManyRelationshipChangeTracking()
        {
            _client.Clear().Wait(ScrumClient.TestTimeout);

            // Fetch a work item, and all ProjectAreas
            var workItemQuery = _client.WorkItems.Include(wi => wi.Project).Include(wi => wi.Areas).Take(1);
            _client.InvokeAsync(workItemQuery).Wait(ScrumClient.TestTimeout);
            WorkItem workItem = workItemQuery.First();
            Assert.Equal(EntityState.Unmodified, _client.WorkItems.GetEntityState(workItem));

            var allAreasInProjectQuery = _client.ProjectAreas.Where(area => area.Project.ID == workItem.Project.ID);
            _client.InvokeAsync(allAreasInProjectQuery).Wait(ScrumClient.TestTimeout);
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
        [Fact]
        public void TestCreateUpdateDelete()
        {
            _client.Clear().Wait(ScrumClient.TestTimeout);

            // Create a new work item in the INFRA/Logging, assigned to joe

            // Query for associated entities
            var userQuery = _client.Users.Where(user => user.UserName == "joe");
            var userQuery2 = _client.Users.Where(user => user.UserName == "gail");
            var projectQuery = _client.Projects.Where(p => p.Key == "INFRA").Include(p => p.Areas).Include(p => p.Versions);
            _client.InvokeAsync(userQuery, userQuery2, projectQuery).Wait(ScrumClient.TestTimeout);
            User joeUser = userQuery.Single();
            User gailUser = userQuery2.Single();
            Project infraProject = projectQuery.Single();
            ProjectArea loggingArea = infraProject.Areas.Single(area => area.Name == "Logging");
            var affectsVersions = infraProject.Versions.Where(version => version.IsReleased);

            // Create the new workitem
            WorkItem workItem = new WorkItem(infraProject, joeUser, Priority.High)
                                {
                                    Project = infraProject,
                                    Areas = { loggingArea },
                                    AssignedTo = { joeUser },
                                    Title = "Logger isn't logging",
                                    Description = "I think this is because it's not configured in app.config.",
                                    AffectsVersions = { affectsVersions.First() }
                                };
            // Create an associated message
            WorkItemMessage message = new WorkItemMessage(workItem, joeUser)
                                      {
                                          Message = "Yep, I verified that it's not configured correctly."
                                      };
            workItem.Messages.Add(message);

            // This adds the whole tree of related items and links
            _client.WorkItems.Add(workItem);

            // Verify that SaveChanges works
            Assert.Equal(0, workItem.ID); // New
            Assert.Equal(0, message.ID); // New
            Assert.Equal(EntityState.Added, _client.WorkItems.GetEntityState(workItem));
            Assert.Equal(EntityState.Added, _client.WorkItemMessages.GetEntityState(message));
            Assert.Equal(9, _client.ReportChanges(null, null)); // 2 entities and 7 links
            Assert.True(_client.SaveChanges().Wait(ScrumClient.TestTimeout));
            Assert.True(workItem.ID > 1); // First WorkItem in db initializer should be ID 1, so adding this should always create a larger ID
            Assert.True(message.ID >= 1);
            Assert.Equal(EntityState.Unmodified, _client.WorkItems.GetEntityState(workItem));
            Assert.Equal(EntityState.Unmodified, _client.WorkItemMessages.GetEntityState(message));

            // Verify update works
            workItem.Description += Environment.NewLine + "Please add a better bug description";
            workItem.Due = DateTime.Now.AddDays(5);
            Assert.Equal(EntityState.Modified, _client.WorkItems.GetEntityState(workItem));
			Assert.Equal(1, _client.ReportChanges(null, null)); // 1 entity
			Assert.True(_client.SaveChanges().Wait(ScrumClient.TestTimeout));
            Assert.Equal(EntityState.Unmodified, _client.WorkItems.GetEntityState(workItem));

            // Adds a link to an existing item
            workItem.Subscribers.Add(gailUser);
            Assert.Equal(EntityState.Modified, _client.WorkItems.GetEntityState(workItem));
			Assert.Equal(1, _client.ReportChanges(null, null)); // 1 link
			Assert.True(_client.SaveChanges().Wait(ScrumClient.TestTimeout));
            Assert.Equal(EntityState.Unmodified, _client.WorkItems.GetEntityState(workItem));
			Assert.Equal(0, _client.ReportChanges(null, null));

            // Verify adding child objects works
            WorkItemMessage message2 = new WorkItemMessage(workItem, gailUser)
                                       {
                                           Created = DateTime.Now.AddHours(1),
                                           Message = "This message was written in the future."
                                       };
            workItem.Messages.Add(message2);
            Assert.Equal(EntityState.Modified, _client.WorkItems.GetEntityState(workItem));
            // Since WorkItem.Messages implement INotifyCollectionChanged, message2 should be added to the repository as soon as it's added to workItem.Messages,
            // since workItem is being tracked:
            Assert.Equal(EntityState.Added, _client.WorkItemMessages.GetEntityState(message2));
            Assert.True(_client.SaveChanges().Wait(ScrumClient.TestTimeout));
            Assert.True(message2.ID >= 1);
            Assert.Equal(EntityState.Unmodified, _client.WorkItems.GetEntityState(workItem));
            Assert.Equal(EntityState.Unmodified, _client.WorkItemMessages.GetEntityState(message2));

            // Clear all the local caches
            Assert.Contains(message, _client.WorkItemMessages.Local);
            Assert.True(_client.Clear().Wait(ScrumClient.TestTimeout));
            Assert.DoesNotContain(message, _client.WorkItemMessages.Local);
            Assert.Equal(EntityState.Detached, _client.WorkItems.GetEntityState(workItem));

            // Query for this WorkItem and connected objects, check that everything is there
            var query = _client.WorkItems.Where(wi => wi.ID == workItem.ID)
                               .Include(wi => wi.Areas)
                               .Include(wi => wi.Subscribers)
                               .Include(wi => wi.Project.Include(p => p.Areas).Include(p => p.Versions))
                               .Include(wi => wi.Messages.Include(m => m.Author).Include(m => m.WorkItem));
            Assert.True(_client.InvokeAsync(query).Wait(ScrumClient.TestTimeout));
            WorkItem workItemQueried = query.Single();
            Assert.Equal(2, workItemQueried.Messages.Count);
            WorkItemMessage messageQueried = workItemQueried.Messages.ElementAt(1);
            Assert.Equal(message2, messageQueried);
            Assert.Same(workItemQueried, messageQueried.WorkItem);
            Assert.Equal(workItem, messageQueried.WorkItem);
            Assert.NotSame(workItem, messageQueried.WorkItem);
            Assert.Contains(gailUser, workItemQueried.Subscribers);
            Assert.DoesNotContain(joeUser, workItemQueried.Subscribers);
            Assert.Contains(message, _client.WorkItemMessages.Local);
            Assert.Contains(message2, _client.WorkItemMessages.Local);

            // TODO: Delete is currently commented out.  Not quite working... it's tricky due to DB reference constraints.
            //// Verify delete works
            //// TODO: Cascade delete should be somehow described/implemented in the model.  I should be able to delete the WorkItem,
            //// and have the WorkItemMessages deleted, but not the users, subscribers, etc (though the links should be deleted).
            //// For now, I have to manually delete each entity.
            //Assert.True(_client.WorkItems.Delete(workItemQueried));
            //foreach (var workItemMessage in workItemQueried.Messages)
            //{
            //	Assert.True(_client.WorkItemMessages.Delete(workItemMessage));				
            //}
            //// REVIEW: There should be a cascade delete of the messages, but that may not be the case.
            //Assert.True(_client.SaveChanges().Wait(ScrumClient.TestTimeout));

            //// Try running the same query again
            //Assert.True(_client.InvokeAsync(query).Wait(ScrumClient.TestTimeout));
            //workItemQueried = query.SingleOrDefault();
            //Assert.Null(workItemQueried);

            //// REVIEW: Not sure what this will do
            //Assert.DoesNotContain(message, _client.WorkItemMessages.Local);

            _client.Clear().Wait(ScrumClient.TestTimeout);
        }

        [Fact]
        public void TestStructuralPropertyValidation()
        {
            // Setup

            _client.Clear().Wait(ScrumClient.TestTimeout);

            // Fetch a work item, and its project
            var workItemQuery = _client.WorkItems.Include(wi => wi.Project).Include(wi => wi.Areas).Include(wi => wi.Creator).Take(1);
            _client.InvokeAsync(workItemQuery).Wait(ScrumClient.TestTimeout);
            WorkItem workItem = workItemQuery.First();
            Assert.Equal(EntityState.Unmodified, _client.WorkItems.GetEntityState(workItem));

            // Test

            // Clear a [Required] structural property
            workItem.Creator = null;
            Assert.Equal(EntityState.Modified, _client.WorkItems.GetEntityState(workItem));
            Assert.Throws<AggregateException>(() => _client.SaveChanges().Wait(ScrumClient.TestTimeout));
            // Setup
            _client.WorkItems.Revert(workItem);

            // Change the project key
            // Alphabetic upper-cased keys required by the [RegularExpression] attr
            workItem.Project.Key = "1";
            Assert.Equal(EntityState.Unmodified, _client.WorkItems.GetEntityState(workItem));
            Assert.Equal(EntityState.Modified, _client.Projects.GetEntityState(workItem.Project));
            Assert.ThrowsAsync<AggregateException>(() => _client.SaveChanges()).Wait();
        }

        [Fact]
        public void TestWhereSingleIdNotFound()
        {
            _client.Clear().Wait(ScrumClient.TestTimeout);

            var workItemQuery = _client.WorkItems.Where(wi => wi.ID == 1305230952);
            _client.InvokeAsync(workItemQuery).Wait(ScrumClient.TestTimeout);
            Assert.False(workItemQuery.Any());
        }

        [Fact]
        public void TestCreateWithSingleItemLinkToNewObject()
        {
            // Setup
            _client.Clear().Wait(ScrumClient.TestTimeout);

            // Create a new project
            var project = new Project()
                          {
                              Key = "TESTPROJECTKEY",
                              Name = "Test Project",
                              Description = "This is a test project"
                          };

            // Create a new ProjectArea
            var projectArea = new ProjectArea()
                              {
                                  Project = project,
                                  Description = "Test project area",
                                  Name = "Test Project Area"
                              };

            _client.ProjectAreas.Add(projectArea);
            Assert.True(_client.SaveChanges().Wait(ScrumClient.TestTimeout));

        }

        public void TestReferencePropertyValidationForExistingEntities()
        {
            // Setup
            _client.Clear().Wait(ScrumClient.TestTimeout);
            // Fetch a work item, and its project
            var workItemQuery = _client.WorkItems.Include(wi => wi.Project).Include(wi => wi.Areas).Take(1);
            _client.InvokeAsync(workItemQuery).Wait(ScrumClient.TestTimeout);
            WorkItem workItem = workItemQuery.First();
            Assert.Equal(EntityState.Unmodified, _client.WorkItems.GetEntityState(workItem));

            // Test #1
            // Clear the project from the WorkItem - should fail, as it's [Required]
            workItem.Project = null;
            Assert.Equal(EntityState.Modified, _client.WorkItems.GetEntityState(workItem));
            Assert.ThrowsAsync<AggregateException>(() => _client.SaveChanges()).Wait();
        }

        public void TestReferencePropertyValidationForNewEntities()
        {
            // Setup
            var projectQuery = _client.Projects.Where(p => p.Key == "INFRA").Include(p => p.Areas);
            var userQuery = _client.Users.Where(u => u.UserName == "joe");
            Assert.True(_client.InvokeAsync(projectQuery, userQuery).Wait(ScrumClient.TestTimeout));
            Project infraProject = projectQuery.Single();
            User joeUser = userQuery.Single();

            // Test
            // Set no properties
            WorkItem workItem = new WorkItem();
            Assert.ThrowsAsync<AggregateException>(() => _client.SaveChanges()).Wait();

            // Set a few, but not all, required properties
            workItem.Project = infraProject;
            workItem.Creator = joeUser;
            workItem.Created = DateTime.Now;
            workItem.Title = "Test workitem - should be deleted";
            Assert.ThrowsAsync<AggregateException>(() => _client.SaveChanges()).Wait();

            // Set the remaining required properties
            workItem.Priority = Priority.Optional;
            workItem.Status = Status.Unknown;
            Assert.True(_client.SaveChanges().Wait(ScrumClient.TestTimeout));

            // Cleanup - delete the workItem just added
            _client.WorkItems.Delete(workItem);
            Assert.True(_client.SaveChanges().Wait(ScrumClient.TestTimeout));
        }

        public void TestThatSaveDoesntNullOutReferencePropertiesNotInObjectGraph()
        {
            // Setup
            _client.Clear().Wait(ScrumClient.TestTimeout);
            // Fetch a work item, without any connected objects
            var workItemQuery = _client.WorkItems.Take(1);
            _client.InvokeAsync(workItemQuery).Wait(ScrumClient.TestTimeout);
            WorkItem workItem = workItemQuery.First();
            Assert.Equal(EntityState.Unmodified, _client.WorkItems.GetEntityState(workItem));
            string previousTitle = workItem.Title;
            Assert.Null(workItem.Project);
            Assert.Null(workItem.Creator);
            Assert.Empty(workItem.Areas);

            // Test: Change the title
            workItem.Title = "Unit test title";
            Assert.Equal(EntityState.Modified, _client.WorkItems.GetEntityState(workItem));
            Assert.True(_client.SaveChanges().Wait(ScrumClient.TestTimeout));

            // Re-query with connected objects, to ensure that they're all there
            workItemQuery = _client.WorkItems.Include(wi => wi.Project).Include(wi => wi.Areas).Include(wi => wi.Creator).Take(1);
            _client.InvokeAsync(workItemQuery).Wait(ScrumClient.TestTimeout);
            Assert.NotNull(workItem.Project);
            Assert.NotNull(workItem.Creator);
            Assert.NotEmpty(workItem.Areas);

            // Cleanup: Change the title back
            workItem.Title = previousTitle;
            Assert.True(_client.SaveChanges().Wait(ScrumClient.TestTimeout));
        }

        [Fact]
        public void TestCreateDerivedType()
        {
            // Setup
            _client.Clear().Wait(ScrumClient.TestTimeout);

            // Create a new project
            var project = new Project()
                          {
                              Key = "TESTPROJECTKEY",
                              Name = "Test Project",
                              Description = "This is a test project"
                          };

            // Create a new ExtendedProjectArea
            string tag = new Guid().ToString();
            var workItem = new ExtendedProjectArea
                           {
                               Project = project,
                               Name = "Test Project Name",
                               Description = "Test project area",
                               Tag = tag
                           };

            _client.ProjectAreas.Add(workItem);
            Assert.True(_client.SaveChanges().Wait(ScrumClient.TestTimeout));

            // Clear the client, then read the last item from the database
            _client.Clear().Wait(ScrumClient.TestTimeout);

            var projectAreaQuery = _client.ProjectAreas.All;
            _client.InvokeAsync(projectAreaQuery).Wait(ScrumClient.TestTimeout);

            var lastItem = projectAreaQuery.Last();
            Assert.Equal(typeof(ExtendedProjectArea), lastItem.GetType());
            Assert.Equal(tag, ((ExtendedProjectArea) lastItem).Tag);
        }

        [Fact]
        public void TestCreateDeleteSingleReferenceLink()
        {
            // Setup
            _client.Clear().Wait(ScrumClient.TestTimeout);

            // Find a user
            var userQuery = _client.Users.All;
            Assert.True(_client.InvokeAsync(userQuery).Wait(ScrumClient.TestTimeout));
            var sprint = userQuery.First();

            // Create a new UserGroup
            var userGroup = new UserGroup();
            userGroup.Users.Add(sprint);

            _client.UserGroups.Add(userGroup);
            Assert.True(_client.SaveChanges().Wait(ScrumClient.TestTimeout));

            sprint.Group = userGroup;
            Assert.True(_client.SaveChanges().Wait(ScrumClient.TestTimeout));

            // Now delete the group
            _client.UserGroups.Delete(userGroup);
            Assert.True(_client.SaveChanges().Wait(ScrumClient.TestTimeout));
        }

        // TODO tests:
        // CUD on included objects
        // Verify client-side validation of [Required] and [StringLength] attributes before SaveChanges() calls the server

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
