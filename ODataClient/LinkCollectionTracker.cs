// -----------------------------------------------------------------------
// <copyright file="LinkCollectionTracker.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using PD.Base.EntityRepository.Api.Base;

namespace PD.Base.EntityRepository.ODataClient
{

	/// <summary>
	/// Tracks changes to one-to-many collection in an entity within an <see cref="EditRepository{TEntity}"/>.
	/// </summary>
	internal class LinkCollectionTracker
	{

		private readonly EntityTracker _parentEntityTracker;
		private readonly string _sourcePropertyName;
		private readonly IEnumerable _collection;
		private bool _collectionChanged;

		/// <summary>
		/// If not <c>null</c>, this collection contains the values that correspond to an unmodified collection.
		/// </summary>
		private object[] _unmodifiedCollection;

		internal LinkCollectionTracker(EntityTracker parentEntityTracker, string sourcePropertyName, IEnumerable collection)
		{
			Contract.Requires<ArgumentNullException>(parentEntityTracker != null);
			Contract.Requires<ArgumentException>(Check.NotNullOrWhiteSpace(sourcePropertyName));
			Contract.Requires<ArgumentNullException>(collection != null);

			_parentEntityTracker = parentEntityTracker;
			_sourcePropertyName = sourcePropertyName;
			_collection = collection;

			INotifyCollectionChanged incc = _collection as INotifyCollectionChanged;
			if (incc != null)
			{
				incc.CollectionChanged += OnCollectionChanged;
			}
		}

		internal string SourcePropertyName
		{
			get { return _sourcePropertyName; }
		}

		private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
		{
			if (! EntityTracker.ChangeTrackingDisabled)
			{
				_collectionChanged = true;

				if (notifyCollectionChangedEventArgs.Action == NotifyCollectionChangedAction.Add)
				{
					foreach (object o in notifyCollectionChangedEventArgs.NewItems)
					{
						// If o (each of the new items) are already attached, this will do nothing.
						// If o is new, this will mark it as added, and will also add any connected objects
						// that aren't attached.
						_parentEntityTracker.ODataClient.AddEntityGraph(o, null, _parentEntityTracker.Entity, _sourcePropertyName);
					}
				}
			}
		}

		/// <summary>
		/// This method is called to enable value change tracking of the link collection, and support
		/// Revert()ing to the unmodified state.
		/// </summary>
		internal void CaptureUnmodifiedState()
		{
			_unmodifiedCollection = _collection.Cast<object>().ToArray();
			_collectionChanged = false;
		}

		internal bool IsLinkCollectionEqualToUnmodified()
		{
			// Shortcut if collection implements INotifyCollectionChanged
			if ((_collection is INotifyCollectionChanged)
			    && ! _collectionChanged)
			{
				return true;
			}

			if (_unmodifiedCollection == null)
			{
				// This can happen when the parent entity was Added and CaptureUnmodifiedState() has not been called.
				// If there are any elements in collection, it's not equal.
				return ! _collection.GetEnumerator().MoveNext();
			}

			// REVIEW: Is SequenceEqual good enough?  Might need to compare the contents of collections...
			return _collection.Cast<object>().SequenceEqual(_unmodifiedCollection);
		}

		internal void RevertLinksToUnmodified()
		{
		    MethodInfo clearMethod = _collection.GetType().GetMethod("Clear");
		    MethodInfo addMethod = _collection.GetType().GetMethod("Add");

		    if (clearMethod == null || addMethod == null)
		    {
		        throw new InvalidOperationException(string.Format("Can't Revert a link collection that doesn't implement Clear and Add.  Collection type is '{0}'", _collection.GetType()));
		    }

            if (_unmodifiedCollection == null) {
                throw new InvalidOperationException("Can't Revert because the unmodified links were not captured.");
            }

		    clearMethod.Invoke(_collection, null);

			foreach (object o in _unmodifiedCollection)
			{
			    addMethod.Invoke(_collection, new[] {o});
			}

			_collectionChanged = false;
		}

		internal void ReportChanges(ODataClient oDataClient)
		{
			if (! IsLinkCollectionEqualToUnmodified())
			{
				// Check whether each item in _collection is in the _unmodifiedCollection
				HashSet<object> previousLinks = _unmodifiedCollection == null ? new HashSet<object>() : new HashSet<object>(_unmodifiedCollection);

				// Remove everything in previousLinks that is in _collection.
				// The remaining set was removed.
				foreach (object o in _collection)
				{
					if (! previousLinks.Remove(o))
					{
						// New or existing link; add it if it doesn't exist
						if (null == oDataClient.DataServiceContext.GetLinkDescriptor(_parentEntityTracker.Entity, _sourcePropertyName, o))
						{
							// If o is already being tracked, this won't do anything.
							// This is necessary if the collection doesn't implement INotifyCollectionChanged, and o is new.
							oDataClient.AddEntityGraph(o, null, _parentEntityTracker.Entity, _sourcePropertyName);

							oDataClient.DataServiceContext.AddLink(_parentEntityTracker.Entity, _sourcePropertyName, o);
						}
					}
				}
				foreach (var previousLink in previousLinks)
				{
					oDataClient.DataServiceContext.DeleteLink(_parentEntityTracker.Entity, _sourcePropertyName, previousLink);
				}
			}
		}

	}
}
