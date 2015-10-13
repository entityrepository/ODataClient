// -----------------------------------------------------------------------
// <copyright file="ITypeResolver.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace EntityRepository.ODataClient
{
	internal interface ITypeResolver
	{

		/// <summary>
		/// Converts a metadata type name to a real type.  In OData, the typeName often does
		/// not contain the real namespace for the type.
		/// </summary>
		/// <param name="typeName"></param>
		/// <returns></returns>
		Type ResolveTypeFromName(string typeName);

	}
}
