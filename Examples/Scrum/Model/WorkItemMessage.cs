// -----------------------------------------------------------------------
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

		[Required]
		public WorkItem WorkItem { get; set; }
		public int WorkItemID { get; set; }

		[Required]
		public User Author { get; set; }

		[Required]
		public string Message { get; set; }

		[Required]
		public DateTime Created { get; set; }

		public DateTime? LastUpdated { get; set; }

	}
}
