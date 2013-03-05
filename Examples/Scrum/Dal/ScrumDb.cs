// -----------------------------------------------------------------------
// <copyright file="ScrumDb.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using Scrum.Model;

namespace Scrum.Dal
{


	public class ScrumDb : DbContext
	{

		public const string DatabaseName = "Scrum";

		public ScrumDb()
			: base(DatabaseName)
		{}

		public DbSet<ProjectArea> ProjectAreas { get; set; }
		public DbSet<ProjectVersion> ProjectVersions { get; set; }
		public DbSet<Project> Projects { get; set; }
		public DbSet<Sprint> Sprints { get; set; }
		public DbSet<User> Users { get; set; }
		public DbSet<WorkItemMessage> WorkItemMessages { get; set; }
		public DbSet<WorkItemTimeLog> WorkItemTimeLog { get; set; }
		public DbSet<WorkItem> WorkItems { get; set; }

		public DbSet<Priority> Priority { get; set; }
		public DbSet<Status> Status { get; set; }

		protected override void OnModelCreating(DbModelBuilder modelBuilder)
		{
			modelBuilder.Entity<WorkItemTimeLog>().ToTable("WorkItemTimeLog");

			var workItemConfig = modelBuilder.Entity<WorkItem>();
			workItemConfig.HasMany(workItem => workItem.AssignedTo).WithMany()
			              .Map(manyToManyConfig => manyToManyConfig.ToTable("WorkItemAssignedTo"));
			workItemConfig.HasMany(workItem => workItem.Subscribers).WithMany()
			              .Map(manyToManyConfig => manyToManyConfig.ToTable("WorkItemSubscribers"));
			workItemConfig.HasMany(workItem => workItem.AffectsVersions).WithMany()
			              .Map(manyToManyConfig => manyToManyConfig.ToTable("WorkItemAffectsVersions"));
			workItemConfig.HasMany(workItem => workItem.FixVersions).WithMany()
			              .Map(manyToManyConfig => manyToManyConfig.ToTable("WorkItemFixVersions"));
			workItemConfig.HasMany(workItem => workItem.Areas).WithMany()
			              .Map(manyToManyConfig => manyToManyConfig.ToTable("WorkItemAreas"));

			modelBuilder.Entity<Project>().HasMany(project => project.Owners).WithMany()
			            .Map(manyToManyConfig => manyToManyConfig.ToTable("ProjectOwners"));

			modelBuilder.Entity<ProjectArea>().HasMany(projectArea => projectArea.Owners).WithMany()
			            .Map(manyToManyConfig => manyToManyConfig.ToTable("ProjectAreaOwners"));

			// For the DbEnum subclasses, turn off autoincrement so IDs of 0 (or flags) can work
			modelBuilder.Entity<Priority>().Property(priority => priority.ID).HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
			modelBuilder.Entity<Status>().Property(status => status.ID).HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);

			// TODO: Iterate over all entity types and set the correct default schema
			// TODO: Iterate over all entity types and set correct key names + mappings
		}

	}


}
