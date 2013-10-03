// -----------------------------------------------------------------------
// <copyright file="ProjectArea.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using PD.Base.PortableUtil.Model;
using Scrum.Model.Base;

namespace Scrum.Model
{


	public class ProjectArea : BaseEntity<int, ProjectArea>
	{
		#region Fields

		private EntityRef<Project, int> _project;
		private ICollection<User> _owners;

		#endregion

		[StringLength(512)]
		public string Description { get; set; }

		[Required]
		[StringLength(100, MinimumLength = 2)]
		public string Name { get; set; }

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

		public ICollection<User> Owners
		{
			get { return EnsureCollectionProperty(ref _owners); }
			set { SetCollectionProperty(ref _owners, value); }
		}

	}
}
