// -----------------------------------------------------------------------
// <copyright file="ScrumDbValidation.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Data.Entity.Validation;
using System.Linq;
using Scrum.Model;
using Xunit;
using Xunit.Extensions;

namespace Scrum.Dal.IntegrationTests
{


	public class ScrumDbValidation : ScrumDbIntegrationTestBase
	{

		public ScrumDbValidation()
		{
			EnsureIntegrationDatabaseExists();
		}

		[Fact, AutoRollback]
		public void BasicVerificationOfScrumDb()
		{
			using (ScrumDb scrumDb = new ScrumDb())
			{
				try
				{
					User user = new User { UserName = Guid.NewGuid().ToString(), Email = "test@domain.com" };

					scrumDb.Users.Add(user);
					scrumDb.SaveChanges();
				}
				catch (DbEntityValidationException entityValidationException)
				{
					var entityValidationResult = entityValidationException.EntityValidationErrors.First();
					var validationError = entityValidationResult.ValidationErrors.First();
					throw;
				}
			}
		}


	}
}
