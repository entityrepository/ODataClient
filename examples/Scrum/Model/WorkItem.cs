﻿// -----------------------------------------------------------------------
// <copyright file="WorkItem.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

using Scrum.Model.Base;

namespace Scrum.Model
{


	public class WorkItem : BaseEntity<int, WorkItem>
	{
		#region Fields

		private EntityRef<Project, int> _project;
		private OptionalEntityRef<WorkItem, int> _parent;
		private EntityRef<Priority, short> _priority;
		private EntityRef<Status, short> _status;
		private EntityRef<User, int> _creator;
		private OptionalEntityRef<User, int> _resolver;
		private OptionalEntityRef<User, int> _closer;
		private OptionalEntityRef<Client, int> _client;

		private ICollection<ProjectVersion> _affectsVersions;
		private ICollection<ProjectArea> _areas;
		private ICollection<User> _assignedTo;
		private ICollection<WorkItemPropertyChange> _changeHistory;
		private ICollection<ProjectVersion> _fixVersions;
		private ICollection<WorkItemMessage> _messages;
		private ICollection<User> _subscribers;
		private ICollection<WorkItemTimeLog> _timeLog;

		#endregion

		public WorkItem(Project project, User creator, Priority priority)
		{
			Project = project;
			Creator = creator;
			// By default the Creator is added as a Subscriber to the WorkItem.
			Subscribers.Add(creator);
			Priority = priority;
			Status = Status.Open;
			Created = DateTime.Now;
		}

		/// <summary>
		/// Used for deserialization.
		/// </summary>
		public WorkItem()
		{}

		public Project Project
		{
			get { return _project.Entity; }
			set { _project.Entity = value; }
		}
		public int ProjectId
		{
			get { return _project.ForeignKey; }
			set { _project.ForeignKey = value; }
		}

		public WorkItem Parent
		{
			get { return _parent.Entity; }
			set { _parent.Entity = value; }
		}
		public int? ParentId
		{
			get { return _parent.ForeignKey; }
			set { _parent.ForeignKey = value; }
		}

		[Required, StringLength(256, MinimumLength = 2)]
		public string Title { get; set; }

		public ICollection<ProjectArea> Areas
		{
			get { return EnsureCollectionProperty(ref _areas); }
		}

		//public Priority Priority { get; set; }
		//public Status Status { get; set; }

		public Priority Priority
		{
			get { return _priority.Entity; }
			set { _priority.Entity = value; }
		}
		public short PriorityId
		{
			get { return _priority.ForeignKey; }
			set { _priority.ForeignKey = value; }
		}

		public Status Status
		{
			get { return _status.Entity; }
			set { _status.Entity = value; }
		}
		public short StatusId
		{
			get { return _status.ForeignKey; }
			set { _status.ForeignKey = value; }
		}

		public ICollection<ProjectVersion> AffectsVersions
		{
			get { return EnsureCollectionProperty(ref _affectsVersions); }
		}

		public ICollection<ProjectVersion> FixVersions
		{
			get { return EnsureCollectionProperty(ref _fixVersions); }
		}

		public User Creator
		{
			get { return _creator.Entity; }
			set { _creator.Entity = value; }
		}
		public int CreatorId
		{
			get { return _creator.ForeignKey; }
			set { _creator.ForeignKey = value; }
		}

		public User Resolver
		{
			get { return _resolver.Entity; }
			set { _resolver.Entity = value; }
		}
		public int? ResolverId
		{
			get { return _resolver.ForeignKey; }
			set { _resolver.ForeignKey = value; }
		}

		public User Closer
		{
			get { return _closer.Entity; }
			set { _closer.Entity = value; }
		}
		public int? CloserId
		{
			get { return _closer.ForeignKey; }
			set { _closer.ForeignKey = value; }
		}

		public ICollection<User> AssignedTo
		{
			get { return EnsureCollectionProperty(ref _assignedTo); }
		}

		public ICollection<User> Subscribers
		{
			get { return EnsureCollectionProperty(ref _subscribers); }
		}

		public Client Client
		{
			get { return _client.Entity; }
			set { _client.Entity = value; }
		}
		public int? ClientId
		{
			get { return _client.ForeignKey; }
			set { _client.ForeignKey = value; }
		}

		public string Description { get; set; }

		public TimeSpan? TimeEstimate { get; set; }

		public DateTime Created { get; set; }

		/// <summary>
		/// This fields tests the exclusion of properties from serialization and the server representation.
		/// </summary>
		[IgnoreDataMember]
#if !SILVERLIGHT
		[NotMapped]
#endif
		public DateTime CreatedDate
		{
			get { return Created.Date; }
			set { Created = value.Date + Created.TimeOfDay; }
		}

		public DateTime? Due { get; set; }

		public ICollection<WorkItemMessage> Messages
		{
			get { return EnsureCollectionProperty(ref _messages); }
		}

		public ICollection<WorkItemPropertyChange> ChangeHistory
		{
			get { return EnsureCollectionProperty(ref _changeHistory); }
		}

		public ICollection<WorkItemTimeLog> TimeLog
		{
			get { return EnsureCollectionProperty(ref _timeLog); }
		}

	}
}
