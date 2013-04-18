// -----------------------------------------------------------------------
// <copyright file="EntityTypeInfo.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace PD.Base.EntityRepository.ODataClient
{

	/// <summary>
	/// Holds type information on an Entity type.
	/// </summary>
	internal class EntityTypeInfo
	{

		private readonly Type _type;

		/// <summary>
		/// The set of properties on <see cref="_type"/> with <c>[NotMapped]</c> attribute.
		/// Such properties should not be serialized.
		/// </summary>
		private readonly PropertyInfo[] _dontSerializeProperties;

		internal EntityTypeInfo(Type type)
		{
			Contract.Requires<ArgumentNullException>(type != null);

			_type = type;

			// Initialize DontSerializeProperties
			_dontSerializeProperties = type.GetProperties().Where(p => p.GetCustomAttributes(typeof(IgnoreDataMemberAttribute), true).Length > 0).ToArray();
		}

		internal PropertyInfo[] DontSerializeProperties
		{
			get { return _dontSerializeProperties; }
		}

		public Type EntityType
		{
			get { return _type; }
		}
	}
}