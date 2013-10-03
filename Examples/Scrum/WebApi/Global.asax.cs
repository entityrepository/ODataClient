// -----------------------------------------------------------------------
// <copyright file="Global.asax.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;
using EntityRepository.ODataServer.Autofac;

namespace Scrum.WebApi
{
	// Note: For instructions on enabling IIS6 or IIS7 classic mode, 
	// visit http://go.microsoft.com/?LinkId=9394801
	public class MvcApplication : System.Web.HttpApplication
	{

		protected void Application_Start()
		{
			AreaRegistration.RegisterAllAreas();

			AutofacConfiguration.Configure(GlobalConfiguration.Configuration, new AutofacAppModule());

			WebApiConfig.Register(GlobalConfiguration.Configuration);
			FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
			//MvcRouteConfig.RegisterRoutes(RouteTable.Routes);
		}

	}
}
