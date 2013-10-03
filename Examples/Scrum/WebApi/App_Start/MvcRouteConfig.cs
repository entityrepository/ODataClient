// -----------------------------------------------------------------------
// <copyright file="MvcRouteConfig.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Web.Mvc;
using System.Web.Routing;

namespace Scrum.WebApi
{
	public class MvcRouteConfig
	{

		public static void RegisterRoutes(RouteCollection routes)
		{
			routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

			routes.MapRoute(
			                name: "Default",
			                url: "{controller}/{action}/{id}",
			                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
				);
		}

	}
}
