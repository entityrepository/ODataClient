// -----------------------------------------------------------------------
// <copyright file="BaseEntity.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.Contracts;

namespace Scrum.Model.Base
{


	/// <summary>
	/// Shared entity functionality.
	/// </summary>
	public abstract class BaseEntity : INotifyPropertyChanged
	{
		#region INotifyPropertyChanged Members

#pragma warning disable 0067
		/// <summary>
		/// INotifyPropertyChanged interface is implemented by Fody.
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;
#pragma warning restore 0067

		#endregion

		protected ICollection<T> EnsureCollectionProperty<T>(ref ICollection<T> collectionField)
		{
			Contract.Ensures(Contract.Result<ICollection<T>>() != null);

			if (collectionField == null)
			{
//#if SILVERLIGHT
				collectionField = new ObservableCollection<T>();
//#else
//				collectionField = new List<T>();
//#endif
			}

			return collectionField;
		}

		protected void SetCollectionProperty<T>(ref ICollection<T> collectionField, ICollection<T> value)
		{
			if (value == null)
			{
				collectionField = null;
			}
			else
			{
//#if SILVERLIGHT
				if (value is ObservableCollection<T>)
				{
					collectionField = value;
				}
				else
				{
					collectionField = new ObservableCollection<T>(value);
				}
//#else
//				collectionField = value;
//#endif
			}
		}

	}
}
