// // -----------------------------------------------------------------------
// <copyright file="ODataEntryExtensions.cs" company="Adap.tv">
// Copyright (c) 2014 Adap.tv.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------


using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

// ReSharper disable once CheckNamespace
namespace Microsoft.Data.OData
{

	/// <summary>
	/// Extension methods for <see cref="ODataEntry"/>.
	/// </summary>
	public static class ODataEntryExtensions
	{
#if SILVERLIGHT
		private static readonly Dictionary<string, string[]> s_typeIgnoreProperties = new Dictionary<string, string[]>(StringComparer.Ordinal);
#else
		private static readonly ConcurrentDictionary<string, string[]> s_typeIgnoreProperties = new ConcurrentDictionary<string, string[]>(StringComparer.Ordinal);
#endif
		/// <summary>
		/// Removes any properties with the <c>[IgnoreDataMember]</c> attribute.
		/// </summary>
		/// <param name="entry"></param>
		public static void RemoveIgnoreDataMemberProperties(this ODataEntry entry)
		{
			var ignoreProperties = GetIgnorePropertiesForType(entry.TypeName);
			if (ignoreProperties.Length > 0)
			{
				var properties = entry.Properties as List<ODataProperty>;
				if (properties == null)
				{
					properties = new List<ODataProperty>(entry.Properties);
				}
				properties.RemoveAll(oDataProperty => ignoreProperties.Contains(oDataProperty.Name, StringComparer.Ordinal));
				entry.Properties = properties;
			}
		}

		private static string[] GetIgnorePropertiesForType(string typeName)
		{
#if SILVERLIGHT
			lock(typeof(ODataEntryExtensions))
			{
#endif
			string[] ignoreProperties;
			if (s_typeIgnoreProperties.TryGetValue(typeName, out ignoreProperties))
			{
				return ignoreProperties;
			}

			Type type = null;
			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				type = assembly.GetType(typeName);
				if (type != null)
				{
					break;
				}
			}
			var listIgnoreProperties = new List<string>();
			if (type != null)
			{
				foreach (var propertyInfo in type.GetProperties())
				{
					object[] ignoreAttributes = propertyInfo.GetCustomAttributes(typeof(IgnoreDataMemberAttribute), true);
					if (ignoreAttributes.Length > 0)
					{
						listIgnoreProperties.Add(propertyInfo.Name);
					}
				}
			}

			ignoreProperties = listIgnoreProperties.ToArray();
#if SILVERLIGHT
			s_typeIgnoreProperties.Add(typeName, ignoreProperties);
			return ignoreProperties;
			}
#else
			s_typeIgnoreProperties.TryAdd(typeName, ignoreProperties);
			return ignoreProperties;
#endif
		}

	}

}