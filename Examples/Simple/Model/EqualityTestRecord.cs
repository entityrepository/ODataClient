// -----------------------------------------------------------------------
// <copyright file="EqualityTestRecord.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------


using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;

namespace Simple.Model
{

	/// <summary>
	/// A test entity class that allows us to test whether identity equality or value equality work correctly with Microsoft's odata classes.
	/// The current theory is that identity equality is required, and object values should be excluded.
	/// </summary>
	public class EqualityTestRecord : IEquatable<EqualityTestRecord>, INotifyPropertyChanged
	{
		/// <summary>
		/// REVIEW: Unfortunately <c>Id</c> is not possible due to bad code in <c>Microsoft.Data.Services.Client</c>.
		/// </summary>
		public short EqualityTestRecordID { get; set; }

		/// <summary>
		/// Just an additional value on the type, that affects value equality.
		/// </summary>
		public string Payload { get; set; }

		/// <summary>
		/// What does equality mean for this object?
		/// </summary>
		/// <remarks>
		/// This property is <c>[NotMapped]</c> b/c data services doesn't support enums.  EF 5.0 on .NET 4.5 does; but data services doesn't.
		/// Instead, <see cref="EqualitySemanticId"/> was added to work with data services.
		/// </remarks>
		[NotMapped]
		public EqualitySemantics EqualitySemantic { get; set; }

		public byte EqualitySemanticId
		{
			get { return (byte) EqualitySemantic; }
			set { EqualitySemantic = (EqualitySemantics) value; }
		}

		#region INotifyPropertyChange

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion

		#region IEquatable<EqualityTestRecord>

		public bool Equals(EqualityTestRecord other)
		{
			if (other == null)
			{
				return false;
			}

			switch (EqualitySemantic)
			{
				case EqualitySemantics.IdentityOnly:
					return this.EqualityTestRecordID == other.EqualityTestRecordID;

				case EqualitySemantics.ValuesOnly:
					return string.Equals(this.Payload, other.Payload, StringComparison.Ordinal);

				case EqualitySemantics.IdentityAndValues:
					return (this.EqualityTestRecordID == other.EqualityTestRecordID)
						&& string.Equals(this.Payload, other.Payload, StringComparison.Ordinal);

				default:
					throw new InvalidOperationException("EqualitySemantic must be set to a valid value before equality methods will work.");
			}
		}

		#endregion

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(obj, null))
			{
				return false;
			}
			else if (ReferenceEquals(obj, this))
			{
				return true;
			}
			return Equals(obj as EqualityTestRecord);
		}

		public override int GetHashCode()
		{
			switch (EqualitySemantic)
			{
				case EqualitySemantics.IdentityOnly:
					return EqualityTestRecordID.GetHashCode();

				case EqualitySemantics.ValuesOnly:
					return Payload == null ? 0 : Payload.GetHashCode();

				case EqualitySemantics.IdentityAndValues:
					return EqualityTestRecordID.GetHashCode() * 127
						^ (Payload == null ? 0 : Payload.GetHashCode());

				default:
					throw new InvalidOperationException("EqualitySemantic must be set to a valid value before equality methods can work.");
			}
		}

	}
}