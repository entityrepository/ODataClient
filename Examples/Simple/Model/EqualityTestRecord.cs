// -----------------------------------------------------------------------
// <copyright file="EqualityTestRecord.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------


using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Simple.Model
{

	/// <summary>
	/// A test entity class that allows us to test whether identity equality or value equality work correctly with Microsoft's odata classes.
	/// The current theory is that identity equality is required, and object values should be excluded.
	/// </summary>
	public class EqualityTestRecord : IEquatable<EqualityTestRecord>, INotifyPropertyChanged
	{
		// Ensures that once a hash code is returned for an instance, it is never changed.
		private int _cachedEntityHashCode;

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
		public EqualitySemantics EqualitySemantic { get; set; }

		#region INotifyPropertyChanged

#pragma warning disable 67 // This property is used by Fody.PropertyChanged
		public event PropertyChangedEventHandler PropertyChanged;
#pragma warning restore 67

		#endregion

		#region IEquatable<EqualityTestRecord>

		public bool Equals(EqualityTestRecord other)
		{
			if (ReferenceEquals(this, other))
			{
				return true;
			}
			if (other == null)
			{
				return false;
			}

			if (EqualitySemantic == EqualitySemantics.IdentityOnly)
			{
				if ((EqualityTestRecordID == 0) &&
				    (other.EqualityTestRecordID == 0))
				{
					// Not the same instance, but neither entity has been stored in the DB yet.
					return false;
				}
				return this.EqualityTestRecordID == other.EqualityTestRecordID;
			}
			else if (EqualitySemantic == EqualitySemantics.ValuesOnly)
			{
				return string.Equals(this.Payload, other.Payload, StringComparison.Ordinal);
			}
			else if (EqualitySemantic == EqualitySemantics.IdentityAndValues)
			{
				return (this.EqualityTestRecordID == other.EqualityTestRecordID)
				       && string.Equals(this.Payload, other.Payload, StringComparison.Ordinal);
			}
			else
			{
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
// ReSharper disable NonReadonlyFieldInGetHashCode
			if (_cachedEntityHashCode != 0)
			{
				return _cachedEntityHashCode;
			}

			lock (this)
			{
				if (EqualitySemantic == EqualitySemantics.IdentityOnly)
				{
					_cachedEntityHashCode = EqualityTestRecordID > 0 ? EqualityTestRecordID.GetHashCode() : 0;
				}
				else if (EqualitySemantic == EqualitySemantics.ValuesOnly)
				{
					_cachedEntityHashCode = Payload == null ? 0 : Payload.GetHashCode();
				}
				else if (EqualitySemantic == EqualitySemantics.IdentityAndValues)
				{
					_cachedEntityHashCode = EqualityTestRecordID.GetHashCode() * 127
					       ^ (Payload == null ? 0 : Payload.GetHashCode());
				}

				if (_cachedEntityHashCode == 0)
				{
					_cachedEntityHashCode = RuntimeHelpers.GetHashCode(this);
				}
			}
			return _cachedEntityHashCode;
// ReSharper restore NonReadonlyFieldInGetHashCode
		}

	}
}