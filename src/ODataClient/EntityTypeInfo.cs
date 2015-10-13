// -----------------------------------------------------------------------
// <copyright file="EntityTypeInfo.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Microsoft.Data.Edm;

namespace EntityRepository.ODataClient
{

	/// <summary>
	/// Holds type information on an Entity type.
	/// </summary>
	internal class EntityTypeInfo
	{

		private readonly Type _type;
		private readonly IEdmEntityType _edmEntityType;

		/// <summary>
		/// The set of properties on <see cref="_type"/> with <c>[IgnoreDataMember]</c> attribute.
		/// Such properties should not be serialized.
		/// </summary>
		private readonly string[] _dontSerializeProperties;

		/// <summary> Structural properties. </summary>
		private readonly PropertyInfo[] _structuralProperties;

		/// <summary> Reference properties. </summary>
		private readonly PropertyInfo[] _navigationProperties;

		/// <summary> One-to-many reference properties, which require creating links. </summary>
		private readonly PropertyInfo[] _collectionProperties;

		/// <summary> <see cref="ValidationAttribute"/>s for properties on this class. </summary>
		private readonly PropertyValidationInfo[] _propertyValidationInfo;

		internal EntityTypeInfo(IEdmModel edmModel, IEdmEntityType edmEntityType, ITypeResolver typeResolver)
		{
			Contract.Assert(edmModel != null);
			Contract.Assert(edmEntityType != null);
			Contract.Assert(typeResolver != null);

			_edmEntityType = edmEntityType;
			string edmTypeName = edmEntityType.FullName();
			_type = typeResolver.ResolveTypeFromName(edmTypeName);

			// Initialize DontSerializeProperties
			_dontSerializeProperties = _type.GetProperties().Where(p => p.GetCustomAttributes(typeof(IgnoreDataMemberAttribute), true).Length > 0).Select(p => p.Name).ToArray();

			//edmEntityType.DeclaredKey;
			//edmEntityType.BaseEntityType();
			var structuralProperties = new List<PropertyInfo>();
			foreach (var edmStructuralProperty in edmEntityType.StructuralProperties())
			{
				if (! _dontSerializeProperties.Contains(edmStructuralProperty.Name))
				{
					structuralProperties.Add(_type.GetProperty(edmStructuralProperty.Name));
				}
			}
			_structuralProperties = structuralProperties.ToArray();

			var navigationProperties = new List<PropertyInfo>();
			var linkProperties = new List<PropertyInfo>();
			foreach (var edmNavigationProperty in edmEntityType.NavigationProperties())
			{
				if (! _dontSerializeProperties.Contains(edmNavigationProperty.Name))
				{
					if (edmNavigationProperty.Type.IsCollection())
					{
						linkProperties.Add(_type.GetProperty(edmNavigationProperty.Name));
					}
					else
					{
						navigationProperties.Add(_type.GetProperty(edmNavigationProperty.Name));
					}
				}
			}
			_navigationProperties = navigationProperties.ToArray();
			_collectionProperties = linkProperties.ToArray();

			// Reflect for ValidationAttributes on all properties
			var validationInfo = new List<PropertyValidationInfo>();
			InitValidationInfo(validationInfo, _structuralProperties, PropertyCategory.Structural);
			InitValidationInfo(validationInfo, _navigationProperties, PropertyCategory.Navigation);
			InitValidationInfo(validationInfo, _collectionProperties, PropertyCategory.Collection);
			_propertyValidationInfo = validationInfo.ToArray();
		}

		private void InitValidationInfo(List<PropertyValidationInfo> validationInfo, IEnumerable<PropertyInfo> properties, PropertyCategory category)
		{
			foreach (PropertyInfo property in properties)
			{
				object[] propertyValidationAttrs = property.GetCustomAttributes(typeof(ValidationAttribute), true);
				if (propertyValidationAttrs.Length > 0)
				{
					ValidationAttribute[] attrs = new ValidationAttribute[propertyValidationAttrs.Length];
					Array.Copy(propertyValidationAttrs, attrs, propertyValidationAttrs.Length);
					validationInfo.Add(new PropertyValidationInfo(property, category, attrs));
				}
			}
		}

		/// <summary> Properties that shouldn't be serialized. </summary>
		internal string[] DontSerializeProperties
		{
			get { return _dontSerializeProperties; }
		}

		/// <summary> Structural properties. </summary>
		internal PropertyInfo[] StructuralProperties
		{
			get { return _structuralProperties; }
		}

		/// <summary> Reference properties. </summary>
		internal PropertyInfo[] NavigationProperties
		{
			get { return _navigationProperties; }
		}

		/// <summary> One-to-many reference properties, which require creating links. </summary>
		internal PropertyInfo[] CollectionProperties
		{
			get { return _collectionProperties; }
		}

		/// <summary> The CLR entity type managed by this instance. </summary>
		public Type EntityType
		{
			get { return _type; }
		}

		/// <summary> The EDM entity type managed by this instance. </summary>
		public IEdmEntityType EdmEntityType
		{
			get { return _edmEntityType; }
		}

		/// <summary>
		/// If set, specifies a base <see cref="EntityTypeInfo"/> type that this type inherits from.
		/// </summary>
		internal EntityTypeInfo BaseTypeInfo { get; set; }

		internal PropertyValidationInfo[] PropertyValidation
		{
			get { return _propertyValidationInfo; }
		}


		internal enum PropertyCategory : byte
		{

			/// <summary> Primitive values, doesn't reference another type. </summary>
			Structural,

			/// <summary> 1-to-1 property </summary>
			Navigation,

			/// <summary> 1-to-many property </summary>
			Collection

		}


		internal struct PropertyValidationInfo
		{

			private readonly PropertyInfo _property;
			private readonly PropertyCategory _category;
			private readonly ValidationAttribute[] _validationAttributes;

			internal PropertyValidationInfo(PropertyInfo property, PropertyCategory category, ValidationAttribute[] validationAttributes)
			{
				_property = property;
				_category = category;
				_validationAttributes = validationAttributes;
			}

			public PropertyInfo Property
			{
				get { return _property; }
			}

			public PropertyCategory Category
			{
				get { return _category; }
			}

			public ValidationAttribute[] ValidationAttributes
			{
				get { return _validationAttributes; }
			}

		}

	}
}
