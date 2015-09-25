// -----------------------------------------------------------------------
// <copyright file="SimpleDbDatabaseInitializer.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Data.Entity;
using Simple.Model;

namespace Simple.Web
{

	public class SimpleDbDatabaseInitializer : DropCreateDatabaseIfModelChanges<SimpleDbContext>
	{

		/// <summary>
		/// Adds 2 identical values with the specified equality semantics.
		/// </summary>
		/// <param name="db"></param>
		/// <param name="equalitySemantic"></param>
		private void AddSeedValuesFor(SimpleDbContext db, EqualitySemantics equalitySemantic)
		{
			for (int i = 0; i < 2; i++)
			{
				db.EqualityTestRecords.Add(new EqualityTestRecord
				                           {
					                           EqualitySemantic = equalitySemantic,
					                           Payload = equalitySemantic.ToString()
				                           });
			}
		}

		protected override void Seed(SimpleDbContext db)
		{
			AddSeedValuesFor(db, EqualitySemantics.IdentityOnly);
			AddSeedValuesFor(db, EqualitySemantics.ValuesOnly);
			AddSeedValuesFor(db, EqualitySemantics.IdentityAndValues);
		}

	}
}
