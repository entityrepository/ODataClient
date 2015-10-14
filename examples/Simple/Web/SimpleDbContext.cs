// -----------------------------------------------------------------------
// <copyright file="SimpleDbContext.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Data.Entity;
using Simple.Model;

namespace Simple.Web
{

	public class SimpleDbContext : DbContext
	{

		static SimpleDbContext()
		{
			Database.SetInitializer(new SimpleDbDatabaseInitializer());
		}

		public DbSet<EqualityTestRecord> EqualityTestRecords { get; set; }

		// Implied:
		//public DbSet<EqualitySemantics> EqualitySemantics { get; set; }

		protected override void OnModelCreating(DbModelBuilder modelBuilder)
		{
			modelBuilder.Entity<EqualityTestRecord>().Property(r => r.EqualityTestRecordID).HasColumnName("EqualityTestRecordId");
		}

	}
}
