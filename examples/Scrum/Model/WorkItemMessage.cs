﻿// -----------------------------------------------------------------------
// <copyright file="WorkItemMessage.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.ComponentModel.DataAnnotations;

using Scrum.Model.Base;

namespace Scrum.Model
{


	public class WorkItemMessage : BaseEntity<long, WorkItemMessage>
	{

		private EntityRef<WorkItem, int> _workItem = new EntityRef<WorkItem, int>(workItem => workItem.ID);
		private EntityRef<User, int> _author = new EntityRef<User, int>(user => user.ID);

		public WorkItemMessage(WorkItem workItem, User author)
		{
			WorkItem = workItem;
			Author = author;
			Created = DateTime.Now;
		}

		/// <summary>
		/// Useful for deserialization.
		/// </summary>
		public WorkItemMessage()
		{}

		public WorkItem WorkItem
		{
			get { return _workItem.Entity; }
			set { _workItem.Entity = value; }
		}
		public int WorkItemId
		{
			get { return _workItem.ForeignKey; }
			set { _workItem.ForeignKey = value; }
		}

		public User Author
		{
			get { return _author.Entity; }
			set { _author.Entity = value; }
		}
		public int AuthorId
		{
			get { return _author.ForeignKey; }
			set { _author.ForeignKey = value; }
		}

		[Required]
		public string Message { get; set; }

		[Required]
		public DateTime Created { get; set; }

		public DateTime? LastUpdated { get; set; }

	}
}
