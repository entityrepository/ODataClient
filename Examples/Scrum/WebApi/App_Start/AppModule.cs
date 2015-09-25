// -----------------------------------------------------------------------
// <copyright file="AppModule.cs" company="EntityRepository Contributors" year="2013">
// This software is part of the EntityRepository library
// Copyright © 2012 EntityRepository Contributors
// http://entityrepository.codeplex.org/
// </copyright>
// -----------------------------------------------------------------------

using EntityRepository.ODataServer.EF;
using EntityRepository.ODataServer.Ioc;
using EntityRepository.ODataServer.Model;
using Scrum.Dal;
using SimpleInjector;
using SimpleInjector.Integration.WebApi;

namespace Scrum.WebApi
{
	/// <summary>
	/// Handles configuration of application specific types in AutoFac.
	/// </summary>
	public class AppModule : IModule
	{

		public void RegisterServices(Container container)
		{
			// Required: How to instantiate the DbContext; and share it amongst objects participating in a single request.
			var webApiRequestLifestyle = new WebApiRequestLifestyle(true);
			var hybridLifestyle = Lifestyle.CreateHybrid(() => webApiRequestLifestyle.GetCurrentScope(container) == null, Lifestyle.Transient, webApiRequestLifestyle);
			container.Register(() =>
			                   {
				                   var db = new ScrumDb();
				                   db.AttachDbEnums();
				                   return db;
			                   },
			                   hybridLifestyle);
			container.RegisterLazy<ScrumDb>();

            // Required: Register global datamodel metadata
            var metadataRegistration = hybridLifestyle.CreateRegistration<DbContextMetadata<ScrumDb>>(container);
            container.AddRegistration(typeof(IContainerMetadata<ScrumDb>), metadataRegistration);
            container.AddRegistration(typeof(IContainerMetadata), metadataRegistration);

			// Query validation settings could be specified here
			//container.RegisterInstance(new ODataValidationSettings
			//{
			//	MaxExpansionDepth = 15,
			//	MaxTop = 200
			//}); //.Named<ODataValidationSettings>("Edit");  TODO: Figure out how to separate ODataValidationSettings for Edit controllers vs ReadOnly controllers
		}

	}
}
