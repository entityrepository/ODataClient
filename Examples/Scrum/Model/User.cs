// -----------------------------------------------------------------------
// <copyright file="User.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;
using Scrum.Model.Base;

namespace Scrum.Model
{


	public class User : BaseEntity<int, User>
	{

		[Required]
		[DataType(DataType.EmailAddress)]
		public string Email { get; set; }

		[Required]
		[StringLength(40, MinimumLength = 3)]
		public string UserName { get; set; }

	}
}
