// -----------------------------------------------------------------------
// <copyright file="UserGroup.cs" company="Adap.tv">
// Copyright (c) 2015 Adap.tv.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using Scrum.Model.Base;

namespace Scrum.Model
{
    public class UserGroup : BaseEntity<int, UserGroup>
    {

        private ICollection<User> _users;

        public ICollection<User> Users
        {
            get { return EnsureCollectionProperty(ref _users); }
        }

    }
}
