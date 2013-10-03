// -----------------------------------------------------------------------
// <copyright file="Sprint.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.ComponentModel.DataAnnotations;
using Scrum.Model.Base;

namespace Scrum.Model
{


	public class Sprint : BaseEntity<int, Sprint>
	{

		[StringLength(64)]
		public string Name { get; set; }

		public DateTime? StartDate { get; set; }

		public DateTime? EndDate { get; set; }

	}
}
