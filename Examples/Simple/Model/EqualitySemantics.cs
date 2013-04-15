// -----------------------------------------------------------------------
// <copyright file="EqualitySemantics.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------


using System;

namespace Simple.Model
{
	/// <summary>
	/// The possible values for what equality means.
	/// </summary>
	public enum EqualitySemantics : byte
	{

		Undefined = 0,
		/// <summary>Use only identity (IDs) to determine equality.</summary>
		IdentityOnly = 1,
		/// <summary>Use only property and field values to determine equality.</summary>
		ValuesOnly = 2,
		/// <summary>Use both identity and values to determine equality.</summary>
		IdentityAndValues = 3

	}
}