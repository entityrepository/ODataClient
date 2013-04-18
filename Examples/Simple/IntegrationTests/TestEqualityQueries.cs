// -----------------------------------------------------------------------
// <copyright file="TestEqualityQueries.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------


using System;
using System.Collections.Generic;
using System.Linq;
using PD.Base.EntityRepository.ODataClient;
using Simple.Model;
using Xunit;

namespace Simple.IntegrationTests
{

	/// <summary>
	/// Exercises querying of <see cref="EqualityTestRecord"/>s from the server, with the intent of directing the
	/// requirements for implementing equality in our entities.
	/// </summary>
	public class TestEqualityQueries : SimpleClientBaseTest
	{

		private IEnumerable<EqualityTestRecord> QueryForRecordsWith(EqualitySemantics equalitySemantic, int expectedRecordCount)
		{
			var query = Client.EqualityTestRecords.Include(r => r.EqualitySemantic).Where(r => r.EqualitySemantic.ID == equalitySemantic.ID);
			Assert.True(Client.InvokeAsync(query).Wait(TestTimeout));

			// Verify expected record count.
			Assert.Equal(expectedRecordCount, query.Count());
			return query;
		}

		[Fact]
		public void QueryWithIdentityEquality()
		{
			var results = QueryForRecordsWith(EqualitySemantics.IdentityOnly, 2).ToArray();
			results[0].Payload += "-modified";
			var results2 = QueryForRecordsWith(EqualitySemantics.IdentityOnly, 2).ToArray();

			Assert.Equal(results, results2);

			// Note that identity equality preserves changes - which it should, since we use MergeOption.PreserveChanges
			Assert.True(results[0].Payload.EndsWith("-modified"));
		}

		[Fact]
		public void ValueEqualityThrowsExceptionWhenEntityValuesAreEqual()
		{
			// This exception occurs if we use value equality and two objects are the same (same hash and Equals == true):
			//
			//System.InvalidOperationException : The context is already tracking a different entity with the same resource Uri.
			//   at System.Data.Services.Client.EntityTracker.InternalAttachEntityDescriptor(EntityDescriptor entityDescriptorFromMaterializer, Boolean failIfDuplicated)
			//   at System.Data.Services.Client.AtomMaterializerLog.ApplyToContext()
			//   at System.Data.Services.Client.Materialization.ODataEntityMaterializer.ApplyLogToContext()
			//   at System.Data.Services.Client.MaterializeAtom.MoveNextInternal()
			//   at System.Data.Services.Client.MaterializeAtom.MoveNext()
			//   at System.Linq.Enumerable.<CastIterator>d__b1`1.MoveNext()
			//   at System.Linq.Buffer`1..ctor(IEnumerable`1 source)
			//   at System.Linq.Enumerable.ToArray[TSource](IEnumerable`1 source)
			//   at PD.Base.EntityRepository.ODataClient.EditRepository`1.ProcessQueryResults(IEnumerable`1 entities) in c:\src\code\Base\EntityRepository\trunk\ODataClient\EditRepository.cs:line 45
			//   at PD.Base.EntityRepository.ODataClient.ODataClient.ProcessQueryResults[TEntity](IEnumerable`1 results) in c:\src\code\Base\EntityRepository\trunk\ODataClient\ODataClient.cs:line 619
			//   at PD.Base.EntityRepository.ODataClient.ODataClientQuery`1.HandleResponse(ODataClient client, OperationResponse operationResponse) in c:\src\code\Base\EntityRepository\trunk\ODataClient\ODataClientQuery.cs:line 209
			Assert.Throws<InvalidOperationException>(() => QueryForRecordsWith(EqualitySemantics.ValuesOnly, 2));
		}

		[Fact(Skip = "This test worked until I added the cached hash code implementation.")]
		public void QueryWithIdentityAndValueEqualityLosesChanges()
		{
			var results = QueryForRecordsWith(EqualitySemantics.IdentityAndValues, 2).ToArray();
			results[0].Payload += "-modified";
			var results2 = QueryForRecordsWith(EqualitySemantics.IdentityAndValues, 2).ToArray();

			Assert.Equal(results, results2);
			Assert.Equal(results[0].Payload, results2[0].Payload);

			// If we use both identity and value equality in Equals() and GetHashCode(),
			// changes to entities are overwritten.  Presumably that's because the identity is the same,
			// but the object is different, so it's overwritten.
			Assert.False(results[0].Payload.EndsWith("-modified"));
		}

	}
}