// -----------------------------------------------------------------------
// <copyright file="ScrumDb.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Data.Entity.Validation;
using System.Diagnostics;
using Scrum.Model;

namespace Scrum.Dal
{


	public class ScrumDb : DbContext
	{

		public const string DatabaseName = "Scrum";

		public ScrumDb()
			: base(DatabaseName)
		{
			Configuration.AutoDetectChangesEnabled = false;
			Configuration.LazyLoadingEnabled = false;
			Configuration.ProxyCreationEnabled = false;

			// By default, this is on, but should be disabled for data services server context.
			Configuration.ValidateOnSaveEnabled = true;

			AttachDbEnums();
		}

		private void AttachDbEnums()
		{
			if (! Database.Exists())
			{
				// Skip this when the database doesn't exist.
				return;
			}

			foreach (var priority in Scrum.Model.Priority.All)
			{
				Priority.Attach(priority);
			}
			foreach (var status in Scrum.Model.Status.All)
			{
				Status.Attach(status);
			}
		}

		protected override DbEntityValidationResult ValidateEntity(DbEntityEntry entityEntry, IDictionary<object, object> items)
		{
			// TODO: We could do our custom validation here, and turn validation back on...
			DbEntityValidationResult validationResult = base.ValidateEntity(entityEntry, items);
			if (!validationResult.IsValid)
			{
				foreach (var validationError in validationResult.ValidationErrors)
				{
					Debug.WriteLine("  Validation error on {0} : {1}", validationError.PropertyName, validationError.ErrorMessage);
				}
			}
			return validationResult;
		}

		public override int SaveChanges()
		{
			ChangeTracker.DetectChanges();
			return base.SaveChanges();
		}

		public DbSet<Client> Clients { get; set; }
		public DbSet<Project> Projects { get; set; }
		public DbSet<ProjectArea> ProjectAreas { get; set; }
		public DbSet<ProjectVersion> ProjectVersions { get; set; }
		public DbSet<Sprint> Sprints { get; set; }
		public DbSet<User> Users { get; set; }
		public DbSet<WorkItem> WorkItems { get; set; }
		public DbSet<WorkItemMessage> WorkItemMessages { get; set; }
		public DbSet<WorkItemPropertyChange> WorkItemPropertyChanges { get; set; }
		public DbSet<WorkItemTimeLog> WorkItemTimeLog { get; set; }

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

			modelBuilder.Entity<WorkItemMessage>().HasRequired(m => m.Author).WithMany().WillCascadeOnDelete(false);

			modelBuilder.Entity<WorkItemPropertyChange>().HasRequired(c => c.Author).WithMany().WillCascadeOnDelete(false);
			modelBuilder.Entity<WorkItemPropertyChange>().HasRequired(c => c.WorkItem).WithMany();

			modelBuilder.Entity<WorkItemTimeLog>().HasRequired(l => l.Worker).WithMany().WillCascadeOnDelete(false);

			// For the DbEnum subclasses, turn off autoincrement so IDs of 0 (or flags) can work
			modelBuilder.Entity<Priority>().Property(priority => priority.ID).HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
			modelBuilder.Entity<Status>().Property(status => status.ID).HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);

			// Required, since there's no way to selectively disable many-to-many cascade delete.
			modelBuilder.Conventions.Remove<ManyToManyCascadeDeleteConvention>();

			// TODO: Iterate over all entity types and set the correct default schema
			// TODO: Iterate over all entity types and set correct key names + mappings
		}

	}


}
