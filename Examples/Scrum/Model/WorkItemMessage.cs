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


	public class WorkItemMessage : BaseEntity
	{

		public long ID { get; set; }

		public int WorkItemID { get; set; }

		[Required]
		public WorkItem WorkItem { get; set; }

		[Required]
		public User Author { get; set; }

		public DateTime Created { get; set; }
		public DateTime? LastUpdated { get; set; }

	}
}
