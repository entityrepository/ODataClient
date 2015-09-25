// -----------------------------------------------------------------------
// <copyright file="FilterConfig.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Web.Mvc;

namespace Scrum.WebApi
{
	public class FilterConfig
	{

		public static void RegisterGlobalFilters(GlobalFilterCollection filters)
		{
			filters.Add(new HandleErrorAttribute());
		}

	}
}
