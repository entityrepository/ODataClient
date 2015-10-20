// -----------------------------------------------------------------------
// <copyright file="EqualitySemantics.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

//using PD.Base.PortableUtil.Enum;

using System.Runtime.Serialization;

namespace Simple.Model
{
	/// <summary>
	/// The possible values for what equality means.
	/// </summary>
	[DataContract]
	public sealed class EqualitySemantics 
	{

		public static readonly EqualitySemantics Undefined = new EqualitySemantics(0, "Undefined");

		/// <summary>Use only identity (IDs) to determine equality.</summary>
		public static readonly EqualitySemantics IdentityOnly = new EqualitySemantics(1, "Identity only");

		/// <summary>Use only property and field values to determine equality.</summary>
		public static readonly EqualitySemantics ValuesOnly = new EqualitySemantics(2, "Values only");

		/// <summary>Use both identity and values to determine equality.</summary>
		public static readonly EqualitySemantics IdentityAndValues = new EqualitySemantics(3, "Identity and values");

	    private EqualitySemantics(byte id, string name)
	    {
            ID = id;
            Name = name;
	    }

        [DataMember]
        public byte ID { get; set; }

        [DataMember]
        public string Name { get; set; }

		/// <summary>
		/// Used for remoting.
		/// </summary>
		public EqualitySemantics()
		{}

	}
}
