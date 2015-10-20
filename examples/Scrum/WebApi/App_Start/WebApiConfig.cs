// -----------------------------------------------------------------------
// <copyright file="WebApiConfig.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using EntityRepository.ODataServer;
using EntityRepository.ODataServer.EF;
using EntityRepository.ODataServer.Model;
//using PD.Base.PortableUtil.Enum;
//using PD.Base.PortableUtil.Reflection;
using EntityRepository.ODataServer.Util;
using Scrum.Dal;
using System;
using System.Web.Http;
using Scrum.Model.Base;

namespace Scrum.WebApi
{
	public static class WebApiConfig
	{

		internal const string ODataRoute = "odata";

		public static void Register(HttpConfiguration config)
		{
			// Pull the container metadata from the DI service
			var containerMetadata = EntityRepository.ODataServer.Util.WebExtensions.Resolve<IContainerMetadata<ScrumDb>>(config.DependencyResolver);

			// Configure OData controllers
			var oDataServerConfigurer = new ODataServerConfigurer(config);

			// Just to prove that regular controller classes can be added when customization is needed
			//oDataServerConfigurer.AddEntitySetController("Projects", typeof(Project), typeof(ProjectsController));
			//oDataServerConfigurer.AddEntitySetController("Users", typeof(User), typeof(UsersController));

			oDataServerConfigurer.AddStandardEntitySetControllers(DbSetControllerSelector);
			oDataServerConfigurer.ConfigureODataRoutes(config.Routes, "ODataRoute", ODataRoute, GlobalConfiguration.DefaultServer);
		}

		/// <summary>
		/// For each entity, entity key, and DbContext type combination, determine the type of the controller
		/// to create for the entity set.
		/// </summary>
		/// <param name="entityType"></param>
		/// <param name="keyTypes"></param>
		/// <param name="dbContextType"></param>
		/// <returns></returns>
		private static Type DbSetControllerSelector(Type entityType, Type[] keyTypes, Type dbContextType)
		{
			if (keyTypes.Length != 1)
			{
				throw new ArgumentException("No default controller exists that supports multiple keys.");
			}

			if (entityType.IsDerivedFromGenericType(typeof(NamedDbEnum<,>)))
			{
				// DbEnum -> ReadOnlyDbSetController
				return typeof(ReadOnlyDbSetController<,,>).MakeGenericType(entityType, keyTypes[0], dbContextType);
			}
			else
			{
				// Everything else -> EditDbSetController
				return typeof(EditDbSetController<,,>).MakeGenericType(entityType, keyTypes[0], dbContextType);
			}
		}

	}
}
