// -----------------------------------------------------------------------
// <copyright file="TestSaveChanges.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using PD.Base.EntityRepository.Api;
using Simple.Model;
using Xunit;

namespace Simple.IntegrationTests
{

	public class TestSaveChanges : SimpleClientBaseTest
	{

		[Fact]
		public void TestIdsAndEqualityBeforeAndAfterSaving2Items()
		{
			Client.Clear();

			// Attach referenced object
			Client.EqualitySemantics.Attach(EqualitySemantics.IdentityOnly);

			var record1 = new EqualityTestRecord { EqualitySemantic = EqualitySemantics.IdentityOnly, Payload = "record1" };
			var record2 = new EqualityTestRecord { EqualitySemantic = EqualitySemantics.IdentityOnly, Payload = "record2" };
			Assert.Equal(EntityState.Detached, Client.EqualityTestRecords.GetEntityState(record1));
			Assert.Equal(EntityState.Detached, Client.EqualityTestRecords.GetEntityState(record2));

			// record1 and record2 should NOT be equal before they're added.
			// If they are, Client.EqualityTestRecords.Add(record2) will throw: System.InvalidOperationException : The context is already tracking the entity.
			Assert.NotEqual(record1, record2);

			Client.EqualityTestRecords.Add(record1);
			Client.EqualityTestRecords.Add(record2);
			Assert.Equal(0, record1.EqualityTestRecordID);
			Assert.Equal(0, record2.EqualityTestRecordID);
			Assert.Equal(EntityState.Added, Client.EqualityTestRecords.GetEntityState(record1));
			Assert.Equal(EntityState.Added, Client.EqualityTestRecords.GetEntityState(record2));

			Assert.True(Client.SaveChanges().Wait(TestTimeout));

			Assert.NotEqual(0, record1.EqualityTestRecordID);
			Assert.NotEqual(0, record2.EqualityTestRecordID);
			Assert.Equal(EntityState.Unmodified, Client.EqualityTestRecords.GetEntityState(record1));
			Assert.Equal(EntityState.Unmodified, Client.EqualityTestRecords.GetEntityState(record2));

			// record1 and record2 should NOT be equal after they're added.
			Assert.NotEqual(record1, record2);

			// Cleanup: Delete record1 and record2 to return the database to how it was before
			Client.EqualityTestRecords.Delete(record1);
			Client.EqualityTestRecords.Delete(record2);
			Assert.Equal(EntityState.Deleted, Client.EqualityTestRecords.GetEntityState(record1));
			Assert.Equal(EntityState.Deleted, Client.EqualityTestRecords.GetEntityState(record2));
			Assert.True(Client.SaveChanges().Wait(TestTimeout));

			Assert.Equal(EntityState.Detached, Client.EqualityTestRecords.GetEntityState(record1));
			Assert.Equal(EntityState.Detached, Client.EqualityTestRecords.GetEntityState(record2));
		}

	}
}
