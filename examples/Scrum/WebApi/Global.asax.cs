// -----------------------------------------------------------------------
// <copyright file="Global.asax.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using EntityRepository.ODataServer.Ioc;
using SimpleInjector;
using SimpleInjector.Integration.WebApi;
using System.Web.Http;
using System.Web.Mvc;

namespace Scrum.WebApi
{
	// Note: For instructions on enabling IIS6 or IIS7 classic mode, 
	// visit http://go.microsoft.com/?LinkId=9394801
	public class MvcApplication : System.Web.HttpApplication
	{

		protected void Application_Start()
		{
			// ASP.NET MVC setup
			AreaRegistration.RegisterAllAreas();
			FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
			//MvcRouteConfig.RegisterRoutes(RouteTable.Routes);

			// DI config
			var container = new Container();
			container.Options.AllowOverridingRegistrations = false;
			container.RegisterModules(new ODataServiceModule(), new AppModule());

			// Web API config
			GlobalConfiguration.Configuration.DependencyResolver = new SimpleInjectorWebApiDependencyResolver(container);
			GlobalConfiguration.Configure(WebApiConfig.Register);
		}

	}
}
