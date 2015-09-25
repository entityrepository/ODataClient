// -----------------------------------------------------------------------
// <copyright file="SimpleClientBaseTest.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Xunit;

namespace Simple.IntegrationTests
{

	public abstract class SimpleClientBaseTest
	{

		// TODO: Shorten the timeout for real testing to 4000 or so
		internal const int TestTimeout = 600000; // For debugging, this is 10m

		private readonly SimpleClient _client;

		protected SimpleClientBaseTest()
		{
			_client = new SimpleClient(SimpleClient.TestServiceUrl);

			// REVIEW: We should require that this is called before any repository properties are accessed.  But that's somewhat difficult to do,
			// so for now, we have to wait for initialization to complete.
			Assert.True(_client.InitializeTask.Wait(TestTimeout));
		}

		protected SimpleClient Client
		{
			get { return _client; }
		}

	}
}
