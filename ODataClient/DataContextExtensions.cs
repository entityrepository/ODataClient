// -----------------------------------------------------------------------
// <copyright file="DataContextExtensions.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections;
using System.Threading.Tasks;
using PD.Base.EntityRepository.Api;
using PD.Base.PortableUtil.Enum;
using PD.Base.PortableUtil.Reflection;

namespace PD.Base.EntityRepository.ODataClient
{
	/// <summary>
	/// Extension methods for <see cref="DataContext"/>.
	/// </summary>
	public static class DataContextExtensions
	{

		/// <summary>
		/// Pre-loads the <see cref="DbEnum{TId,T}"/>-derived entities in <paramref name="dataContext"/>.
		/// </summary>
		/// <param name="dataContext"></param>
		/// <returns></returns>
		public static void SynchronousPreLoadDbEnums(this DataContext dataContext)
		{
			AsyncPreLoadDbEnums(dataContext).Wait();
		}

		/// <summary>
		/// Pre-loads the <see cref="DbEnum{TId,T}"/>-derived entities in <paramref name="dataContext"/>.
		/// </summary>
		/// <param name="dataContext"></param>
		/// <returns></returns>
		public static Task AsyncPreLoadDbEnums(this DataContext dataContext)
		{
			Action<IEnumerable> dbEnumTypeInitializer =
				enumerable =>
				{
					try
					{
						foreach (dynamic dbEnumValue in enumerable)
						{
							DbEnumManager.RegisterDbEnumValue(dbEnumValue);
						}
					}
					catch (Exception) // ex)
					{
						// TODO: Add logging support
						// Log(ex, "While preloading DbEnums");
					}
				};
			return PreLoadEntitiesDerivedFrom(dataContext, typeof(DbEnum<,>), dbEnumTypeInitializer);
		}

		/// <summary>
		/// Pre-loads the entities types in <paramref name="dataContext"/> that are derived from <paramref name="baseClass"/>.
		/// </summary>
		/// <param name="dataContext"></param>
		/// <param name="baseClass"></param>
		/// <param name="perResultInitializer">An optional initializer method that is called once per entity set.</param>
		/// <returns></returns>
		public static Task PreLoadEntitiesDerivedFrom(this DataContext dataContext, Type baseClass, Action<IEnumerable> perResultInitializer = null)
		{
			return DataContext.PreLoad(dataContext, (type) => type.IsSubclassOf(baseClass) || type.IsDerivedFromGenericType(baseClass), perResultInitializer);
		}

	}
}
