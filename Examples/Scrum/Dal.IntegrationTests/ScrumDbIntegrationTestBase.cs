// -----------------------------------------------------------------------
// <copyright file="ScrumDbIntegrationTestBase.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Data.Entity;

namespace Scrum.Dal.IntegrationTests
{


	/// <summary>
	/// Common functionality for integration tests that use a <see cref="ScrumDb"/>.
	/// </summary>
	public abstract class ScrumDbIntegrationTestBase
	{

		protected ScrumDbIntegrationTestBase()
		{
			Database.SetInitializer(new ScrumDbTestDatabaseInitializer());
		}

		public void EnsureIntegrationDatabaseExists()
		{
			using (ScrumDb scrumDb = new ScrumDb())
			{
				scrumDb.Database.Initialize(false);
			}
		}

	}
}
