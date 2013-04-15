// -----------------------------------------------------------------------
// <copyright file="SimpleDb.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------


using System;
using System.Data.Entity;
using Simple.Model;

namespace Simple.Web
{

	public class SimpleDbContext : DbContext
	{
		static SimpleDbContext()
		{
			Database.SetInitializer<SimpleDbContext>(new SimpleDbDatabaseInitializer());
		}

		public DbSet<EqualityTestRecord> EqualityTestRecords { get; set; }

		protected override void OnModelCreating(DbModelBuilder modelBuilder)
		{
			modelBuilder.Entity<EqualityTestRecord>().Property(r => r.EqualityTestRecordID).HasColumnName("EqualityTestRecordId");
		}

	}
}