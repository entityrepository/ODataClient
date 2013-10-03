﻿// -----------------------------------------------------------------------
// <copyright file="WorkItemTimeLog.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using PD.Base.PortableUtil.Model;
using Scrum.Model.Base;
using System;

namespace Scrum.Model
{


	public class WorkItemTimeLog : BaseEntity<long, WorkItemTimeLog>
	{

		private EntityRef<WorkItem, int> _workItem = new EntityRef<WorkItem, int>(workItem => workItem.ID);
		private EntityRef<User, int> _worker = new EntityRef<User, int>(user => user.ID);

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

		public User Worker
		{
			get { return _worker.Entity; }
			set { _worker.Entity = value; }
		}
		public int WorkerId
		{
			get { return _worker.ForeignKey; }
			set { _worker.ForeignKey = value; }
		}

		public TimeSpan TimeWorked { get; set; }

		public DateTime? StartDateTime { get; set; }
		public DateTime? EndDateTime { get; set; }

		public string Comments { get; set; }

	}
}
