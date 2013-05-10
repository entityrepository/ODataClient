// -----------------------------------------------------------------------
// <copyright file="ODataClientBugTests.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------


using System;
using System.Linq;
using PD.Base.EntityRepository.ODataClient;
using Scrum.Model;
using Xunit;

namespace Scrum.Web.IntegrationTests
{

	public class ODataClientBugTests : IDisposable
	{

		private readonly ScrumClient _client;

		public ODataClientBugTests()
		{
			_client = new ScrumClient();
			Assert.True(_client.InitializeTask.Wait(ScrumClient.TestTimeout));
		}

		public void Dispose()
		{
			_client.Dispose();
		}

		/// <summary>
		/// Ensures that change tracking doesn't cause problems when the client re-queries for the same object.
		/// Previously LinkCollectionTracker was throwing an exception during the deserialization of a second query.
		/// </summary>
		[Fact]
		public void TestSimpleQueryFollowedByLargerQueriesForSameRootObjects()
		{
			var simpleQuery = _client.WorkItems.Take(1).Include(wi => wi.Project.Include(p => p.Versions));
			Assert.True(_client.InvokeAsync(simpleQuery).Wait(ScrumClient.TestTimeout));
			WorkItem workItem = simpleQuery.Single();
			Assert.Empty(workItem.Areas);
			Assert.Empty(workItem.AssignedTo);
			Assert.NotNull(workItem.Project);
			Assert.Empty(workItem.Project.Areas);

			var extensiveQuery = _client.WorkItems.Where(wi => wi.ID == workItem.ID)
			                            .Include(wi => wi.Areas.Include(a => a.Owners))
			                            .Include(wi => wi.TimeLog.Include(tl => tl.Worker))
			                            .Include(wi => wi.Project.Include(p => p.Areas.Include(a => a.Owners)).Include(p => p.Versions))
			                            .Include(wi => wi.Subscribers)
			                            .Include(wi => wi.AssignedTo);
			Assert.True(_client.InvokeAsync(extensiveQuery).Wait(ScrumClient.TestTimeout));

			// The previous workItem is modified by running extensiveQuery, so this is not necessary:
			//workItem = extensiveQuery.Single();

			Assert.NotEmpty(workItem.Areas);
			Assert.NotEmpty(workItem.AssignedTo);
			Assert.NotEmpty(workItem.Project.Areas);
			Assert.Equal(3, workItem.Project.Areas.Count);
			Assert.Equal(4, workItem.Project.Versions.Count);

			// Run the extensive query again...
			Assert.True(_client.InvokeAsync(extensiveQuery).Wait(ScrumClient.TestTimeout));
			Assert.Equal(3, workItem.Project.Areas.Count);
			Assert.Equal(4, workItem.Project.Versions.Count);

			// Verify that no changes have been made.
			Assert.Equal(0, _client.ReportChanges(null, null));
		}

	}
}