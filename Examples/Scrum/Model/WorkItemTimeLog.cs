// -----------------------------------------------------------------------
// <copyright file="WorkItemTimeLog.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.ComponentModel.DataAnnotations;
using Scrum.Model.Base;

namespace Scrum.Model
{


	public class WorkItemTimeLog : BaseEntity<long, WorkItemTimeLog>
	{

		[Required]
		public WorkItem WorkItem { get; set; }

		[Required]
		public User Worker { get; set; }

		public TimeSpan TimeWorked { get; set; }

		public DateTime? StartTime { get; set; }
		public DateTime? EndTime { get; set; }

		public string Comments { get; set; }

	}
}
