// -----------------------------------------------------------------------
// <copyright file="Client.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;
using Scrum.Model.Base;

namespace Scrum.Model
{


	public class Client : BaseEntity<int, Client>
	{

		[Required]
		[StringLength(60, MinimumLength = 1)]
		public string Name { get; set; }

		[StringLength(512)]
		public string Description { get; set; }

	}
}
