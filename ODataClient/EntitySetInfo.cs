// -----------------------------------------------------------------------
// <copyright file="EntitySetInfo.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.OData.Edm;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace PD.Base.EntityRepository.ODataClient
{
	/// <summary>
	/// Holds metadata for an Entity Set.
	/// </summary>
	/// <remarks>
	/// This class is immutable - it is unchanged after initialization.
	/// </remarks>
	internal class EntitySetInfo
	{

		internal EntitySetInfo(IEdmModel edmModel, IEdmEntitySet edmEntitySet, ITypeResolver typeResolver)
		{
			Contract.Assert(edmModel != null);
			Contract.Assert(edmEntitySet != null);
			Contract.Assert(typeResolver != null);

			Name = edmEntitySet.Name;
			ElementType = new EntityTypeInfo(edmModel, edmEntitySet.Type as IEdmEntityType, typeResolver);
			var entityTypes = new List<EntityTypeInfo>(3) { ElementType };

			// Create an EntityTypeInfo for any derived types in the model
			foreach (var edmDerivedType in edmModel.FindAllDerivedTypes(edmEntitySet.Type as IEdmEntityType).OfType<IEdmEntityType>())
			{
				entityTypes.Add(new EntityTypeInfo(edmModel, edmDerivedType, typeResolver));
			}

			// Connect any derived types with their base class
			for (int i = 1; i < entityTypes.Count; ++i)
			{
				var baseEdmEntityType = entityTypes[i].EdmEntityType.BaseEntityType();
				if (baseEdmEntityType != null)
				{
					var baseEntityTypeInfo = entityTypes.First(entityTypeInfo => entityTypeInfo.EdmEntityType == baseEdmEntityType);
					if (baseEntityTypeInfo != null)
					{
						entityTypes[i].BaseTypeInfo = baseEntityTypeInfo;
					}
				}
			}

			EntityTypes = entityTypes;
		}

		internal string Name { get; private set; }

		internal EntityTypeInfo ElementType { get; private set; }

		internal IEnumerable<EntityTypeInfo> EntityTypes { get; private set; }

	}
}
