// -----------------------------------------------------------------------
// <copyright file="WorkItem.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using PD.Base.PortableUtil.Enum;
using Scrum.Model.Base;

namespace Scrum.Model
{


	public class WorkItem : BaseEntity<int, WorkItem>
	{
		#region Fields

		private ICollection<ProjectVersion> _affectsVersions;
		private ICollection<ProjectArea> _areas;
		private ICollection<User> _assignedTo;
		private ICollection<WorkItemPropertyChange> _changeHistory;
		private ICollection<ProjectVersion> _fixVersions;
		private ICollection<WorkItemMessage> _messages;
		// REVIEW: Perhaps the KeyFunc should be managed by an EntityManager? Or be moved to a base class.
		private DbEnumRef<Priority, short> _priority = new DbEnumRef<Priority, short>();
		private DbEnumRef<Status, short> _status = new DbEnumRef<Status, short>();
		private EntityRef<User, int> _creator = new EntityRef<User, int>(user => user.ID);
		private ICollection<User> _subscribers;
		private ICollection<WorkItemTimeLog> _timeLog;

		#endregion

		public WorkItem(Project project, User creator, Priority priority)
		{
			Project = project;
			Creator = creator;
			Priority = priority;
			Status = Status.Open;
			Created = DateTime.Now;
		}

		/// <summary>
		/// Used for deserialization.
		/// </summary>
		public WorkItem()
		{}

		[Required]
		public Project Project { get; set; }

		public WorkItem Parent { get; set; }

		[Required, StringLength(256, MinimumLength = 2)]
		public string Title { get; set; }

		public virtual ICollection<ProjectArea> Areas
		{
			get { return EnsureCollectionProperty(ref _areas); }
			set { SetCollectionProperty(ref _areas, value); }
		}

		//public Priority Priority { get; set; }
		//public Status Status { get; set; }

		[Required]
		public Priority Priority
		{
			get { return _priority.Value; }
			set { _priority.Value = value; }
		}

		//public short PriorityId
		//{
		//	get { return _priority.ForeignKey; }
		//	set { _priority.ForeignKey = value; }
		//}

		[Required]
		public Status Status
		{
			get { return _status.Value; }
			set { _status.Value = value; }
		}
		public short StatusId
		{
			get { return _status.Id; }
			set { _status.Id = value; }
		}

		public virtual ICollection<ProjectVersion> AffectsVersions
		{
			get { return EnsureCollectionProperty(ref _affectsVersions); }
			set { SetCollectionProperty(ref _affectsVersions, value); }
		}

		public virtual ICollection<ProjectVersion> FixVersions
		{
			get { return EnsureCollectionProperty(ref _fixVersions); }
			set { SetCollectionProperty(ref _fixVersions, value); }
		}

		[Required]
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

		public User Resolver { get; set; }
		public User Closer { get; set; }

		public virtual ICollection<User> AssignedTo
		{
			get { return EnsureCollectionProperty(ref _assignedTo); }
			set { SetCollectionProperty(ref _assignedTo, value); }
		}

		public virtual ICollection<User> Subscribers
		{
			get { return EnsureCollectionProperty(ref _subscribers); }
			set { SetCollectionProperty(ref _subscribers, value); }
		}

		public Client Client { get; set; }

		public string Description { get; set; }

		public TimeSpan? TimeEstimate { get; set; }

		[Required]
		public DateTime Created { get; set; }

		public DateTime? Due { get; set; }

		public virtual ICollection<WorkItemMessage> Messages
		{
			get { return EnsureCollectionProperty(ref _messages); }
			set { SetCollectionProperty(ref _messages, value); }
		}

		public virtual ICollection<WorkItemPropertyChange> ChangeHistory
		{
			get { return EnsureCollectionProperty(ref _changeHistory); }
			set { SetCollectionProperty(ref _changeHistory, value); }
		}

		public virtual ICollection<WorkItemTimeLog> TimeLog
		{
			get { return EnsureCollectionProperty(ref _timeLog); }
			set { SetCollectionProperty(ref _timeLog, value); }
		}

	}
}
