// -----------------------------------------------------------------------
// <copyright file="EqualitySemantics.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------


using System.ComponentModel;
using System.Runtime.CompilerServices;
using PD.Base.PortableUtil.Enum;

namespace Simple.Model
{
	/// <summary>
	/// The possible values for what equality means.
	/// </summary>
	public sealed class EqualitySemantics : NamedDbEnum<byte, EqualitySemantics>
	{

		public static readonly EqualitySemantics Undefined = new EqualitySemantics(0, "Undefined");

		/// <summary>Use only identity (IDs) to determine equality.</summary>
		public static readonly EqualitySemantics IdentityOnly = new EqualitySemantics(1, "Identity only");

		/// <summary>Use only property and field values to determine equality.</summary>
		public static readonly EqualitySemantics ValuesOnly = new EqualitySemantics(2, "Values only");

		/// <summary>Use both identity and values to determine equality.</summary>
		public static readonly EqualitySemantics IdentityAndValues = new EqualitySemantics(3, "Identity and values");

		private EqualitySemantics(byte id, string name)
			: base(id, name)
		{}

		/// <summary>
		/// Used for remoting.
		/// </summary>
		public EqualitySemantics()
		{}

	}
}