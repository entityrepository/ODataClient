// -----------------------------------------------------------------------
// <copyright file="User.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

using Scrum.Model.Base;

namespace Scrum.Model
{


	public class User : BaseEntity<int, User>
	{

	    private RequiredEntityRef<UserGroup, int?> _group;
        
        [Required]
		[DataType(DataType.EmailAddress)]
		public string Email { get; set; }

		[Required]
		[StringLength(40, MinimumLength = 3)]
		public string UserName { get; set; }

	    public UserGroup Group
	    {
            get { return _group.Entity; }
            set { _group.Entity = value; }
	    }

        [IgnoreDataMember]
	    public int? GroupId
	    {
            get { return _group.ForeignKey; }
            set { _group.ForeignKey = value; }
	    }
	}
}
