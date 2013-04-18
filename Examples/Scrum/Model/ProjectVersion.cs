// -----------------------------------------------------------------------
// <copyright file="ProjectVersion.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.ComponentModel.DataAnnotations;
using Scrum.Model.Base;

namespace Scrum.Model
{


	public class ProjectVersion : BaseEntity<int, ProjectVersion>
	{

		public Project Project { get; set; }

		[Required, StringLength(20, MinimumLength = 1)]
		public string Name { get; set; }

		public string Description { get; set; }

		public DateTime? ReleaseDate { get; set; }

		public bool IsReleased { get; set; }

	}
}
