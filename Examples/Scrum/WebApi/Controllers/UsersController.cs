// -----------------------------------------------------------------------
// <copyright file="ProjectsController.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Linq;
using System.Web.Http;
using System.Web.Http.OData;
using Scrum.Dal;
using Scrum.Model;

namespace Scrum.WebApi.Controllers
{
	public class UsersController : EntitySetController<User, int>
	{

		private ScrumDb _db;

		protected ScrumDb Db
		{
			get
			{
				if (_db == null)
				{
					_db = new ScrumDb();
				}
				return _db;
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (_db != null)
				{
					_db.Dispose();
				}
			}
			base.Dispose(disposing);
		}

		[Queryable]
		public override IQueryable<User> Get()
		{
			return Db.Users;
		}

		protected override User GetEntityByKey(int key)
		{
			return Db.Users.Find(key);
		}
	}
}
