// -----------------------------------------------------------------------
// <copyright file="EntityTracker.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------


using System;
using System.Collections;
using System.ComponentModel;
using System.Data.Services.Client;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;

namespace PD.Base.EntityRepository.ODataClient
{
	/// <summary>
	/// Tracks changes to an entity within an <see cref="EditRepository{TEntity}"/>.
	/// </summary>
	// TODO: It's likely that perf could be significantly better if using code generation instead of reflection...
	// TODO: Evaluate perf.
	internal class EntityTracker
	{

		[ThreadStatic]
		private static bool s_changeTrackingDisabled;

		private readonly ODataClient _oDataClient;
		private readonly object _entity;
		private readonly EntityTypeInfo _entityTypeInfo;
		private bool _propertyChanged;

		/// <summary>
		/// If not <c>null</c>, this array contains the structural property values that correspond to an unmodified entity.
		/// </summary>
		private object[] _unmodifiedStructuralPropertyValues;
		/// <summary>
		/// If not <c>null</c>, this array contains the non-collection navigation property values that correspond to an unmodified entity.
		/// </summary>
		private object[] _unmodifiedNavigationPropertyValues;

		/// <summary>
		/// Tracks the link collections connected to <see cref="_entity"/>.
		/// </summary>
		private readonly LinkCollectionTracker[] _linkCollectionTrackers;

		/// <summary>
		/// Used to disable change tracking when entities are being deserialized.
		/// </summary>
		internal static bool ChangeTrackingDisabled
		{
			get { return s_changeTrackingDisabled; }
			set { s_changeTrackingDisabled = value; }
		}

		internal EntityTracker(ODataClient oDataClient, object entity)
		{
			Contract.Requires<ArgumentNullException>(oDataClient != null);
			Contract.Requires<ArgumentNullException>(entity != null);

			_oDataClient = oDataClient;
			_entity = entity;
			_entityTypeInfo = oDataClient.GetEntityTypeInfoFor(entity.GetType());

			_linkCollectionTrackers = new LinkCollectionTracker[_entityTypeInfo.LinkProperties.Length];
			for (int i = 0; i < _entityTypeInfo.LinkProperties.Length; ++i)
			{
				PropertyInfo property = _entityTypeInfo.LinkProperties[i];
				IEnumerable collection = (IEnumerable) property.GetValue(_entity, null);
				if (collection != null)
				{
					_linkCollectionTrackers[i] = new LinkCollectionTracker(this, property.Name, collection);
				}
			}

			INotifyPropertyChanged inpc = _entity as INotifyPropertyChanged;
			if (inpc != null)
			{
				// Use change tracking for more efficiency (possibly)
				inpc.PropertyChanged += OnPropertyChanged;
			}
		}

		internal ODataClient ODataClient
		{ get { return _oDataClient; } }

		internal LinkCollectionTracker[] LinkCollectionTrackers
		{ get { return _linkCollectionTrackers; } }

		internal LinkCollectionTracker GetLinkCollectionTracker(string linkCollectionPropertyName)
		{
			return _linkCollectionTrackers.FirstOrDefault(t => t != null && t.SourcePropertyName.Equals(linkCollectionPropertyName));
		}

		/// <summary>
		/// This method is called to start per-property-value change tracking of the entity,
		/// and support Revert()ing to the same state.
		/// </summary>
		internal void CaptureUnmodifiedState()
		{
			int countStructuralProperties = _entityTypeInfo.StructuralProperties.Length;
			_unmodifiedStructuralPropertyValues = new object[countStructuralProperties];
			for (int i = 0; i < countStructuralProperties; ++i)
			{
				PropertyInfo property = _entityTypeInfo.StructuralProperties[i];
				_unmodifiedStructuralPropertyValues[i] = property.GetValue(_entity, null);
			}

			int countNavigationProperties = _entityTypeInfo.NavigationProperties.Length;
			_unmodifiedNavigationPropertyValues = new object[countNavigationProperties];
			for (int i = 0; i < countNavigationProperties; ++i)
			{
				PropertyInfo property = _entityTypeInfo.NavigationProperties[i];
				_unmodifiedNavigationPropertyValues[i] = property.GetValue(_entity, null);
			}

			_propertyChanged = false;
		}

		/// <summary>
		/// Returns the entity being tracked.
		/// </summary>
		internal object Entity
		{
			get { return _entity; }
		}

		private void OnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
		{
			if (! ChangeTrackingDisabled)
			{
				// Mark the entity as updated, even though it might be changed to the same value as it started.
				// When SaveChanges() is called, ReportChanges() will be called to determine whether the entity is actually changed.
				_propertyChanged = true;
				_oDataClient.DataServiceContext.UpdateObject(_entity);

				// If this is a navigation property, ensure that the referenced entity is added to the correct repository.
				string propertyName = propertyChangedEventArgs.PropertyName;
				PropertyInfo navigationProperty = _entityTypeInfo.NavigationProperties.FirstOrDefault(p => p.Name.Equals(propertyName, StringComparison.Ordinal));
				if (navigationProperty != null)
				{
					object referencedEntity = navigationProperty.GetValue(_entity, null);
					if (referencedEntity != null)
					{
						_oDataClient.AddEntityGraph(referencedEntity);
					}
				}
			}
		}

		internal bool AreStructuralPropertiesUnmodified()
		{
			if ((_entity is INotifyPropertyChanged)
			    && ! _propertyChanged)
			{
				// Shortcut exit - if not INPC events were raised, nothing should be changed.
				return true;
			}
			if (_unmodifiedStructuralPropertyValues == null)
			{
				// It's not possible to detect changes, so return <c>true</c>
				return true;
			}

			for (int i = 0; i < _unmodifiedStructuralPropertyValues.Length; ++i)
			{
				PropertyInfo property = _entityTypeInfo.StructuralProperties[i];
				if (! Equals(_unmodifiedStructuralPropertyValues[i], property.GetValue(_entity, null)))
				{
					return false;
				}
			}
			return true;
		}

		internal bool AreSingleLinksUnmodified()
		{
			if ((_entity is INotifyPropertyChanged)
				&& !_propertyChanged)
			{
				// Shortcut exit - if not INPC events were raised, nothing should be changed.
				return true;
			}
			if (_unmodifiedNavigationPropertyValues == null)
			{
				// It's not possible to detect changes, so return <c>true</c>
				return true;
			}

			for (int i = 0; i < _unmodifiedNavigationPropertyValues.Length; ++i)
			{
				PropertyInfo property = _entityTypeInfo.NavigationProperties[i];
				if (! Equals(_unmodifiedNavigationPropertyValues[i], property.GetValue(_entity, null)))
				{
					return false;
				}
			}
			return true;
		}

		internal bool AreLinkCollectionsUnmodified()
		{
			foreach (LinkCollectionTracker linkCollectionTracker in _linkCollectionTrackers)
			{
				if ((linkCollectionTracker != null)
					&& !linkCollectionTracker.IsLinkCollectionEqualToUnmodified())
				{
					return false;
				}
			}

			return true;
		}

		internal void RevertEntityToUnmodified(BaseRepository repository)
		{
			if ((_entity is INotifyPropertyChanged)
				&& ! _propertyChanged)
			{
				// Shortcut exit - if not INPC events were raised, nothing should be changed.
				return;
			}
			if ((_unmodifiedStructuralPropertyValues == null)
				|| (_unmodifiedNavigationPropertyValues == null))
			{
				// It's not possible to revert
				throw new InvalidOperationException("Can't Revert because the entity's unmodified state was not captured.");
			}

			// Restore the unmodified property values
			for (int i = 0; i < _unmodifiedStructuralPropertyValues.Length; ++i)
			{
				PropertyInfo property = _entityTypeInfo.StructuralProperties[i];
				property.SetValue(_entity, _unmodifiedStructuralPropertyValues[i], null);
			}

			for (int i = 0; i < _unmodifiedNavigationPropertyValues.Length; ++i)
			{
				PropertyInfo property = _entityTypeInfo.NavigationProperties[i];
				// Check each property value before setting it - only set properties that need to change
				object currentValue = property.GetValue(_entity, null);
				if (! Equals(currentValue, _unmodifiedNavigationPropertyValues[i]))
				{
					object revertToValue = _unmodifiedNavigationPropertyValues[i];
					property.SetValue(_entity, revertToValue, null);
					repository.DataServiceContext.SetLink(_entity, property.Name, revertToValue);
				}
			}

			// Set the state to unmodified in the DataServiceContext
			repository.DataServiceContext.ChangeState(_entity, EntityStates.Unchanged);

			// Set link collections, if any, to unmodified
			foreach (var linkCollectionTracker in _linkCollectionTrackers)
			{
				if (linkCollectionTracker != null)
				{
					linkCollectionTracker.RevertLinksToUnmodified();
				}
			}

			_propertyChanged = false;
		}

		/// <summary>
		/// Determine if the object is changed; and set the DataServiceContext state accordingly.
		/// </summary>
		/// <param name="repository"></param>
		internal void ReportChanges(BaseRepository repository)
		{
			EntityDescriptor ed = repository.DataServiceContext.GetEntityDescriptor(_entity);
			Debug.Assert(ed != null, "No EntityDescriptor for " + _entity);

			if ((ed.State == EntityStates.Unchanged) || (ed.State == EntityStates.Modified))
			{
				if (_propertyChanged || !(_entity is INotifyPropertyChanged))
				{
					// Only run this block if there's a chance a property has changed,
					// and if changed values will affect whether the entity is saved.
					if (AreStructuralPropertiesUnmodified())
					{ // Record the entity as Unmodified
						if (ed.State != EntityStates.Unchanged)
						{
							repository.DataServiceContext.ChangeState(_entity, EntityStates.Unchanged);
						}
					}
					else if (ed.State == EntityStates.Unchanged)
					{ // Record the entity as Modified
						repository.DataServiceContext.UpdateObject(_entity);
					}

					// Set links for navigation properties that have changed
					for (int i = 0; i < _unmodifiedNavigationPropertyValues.Length; ++i)
					{
						PropertyInfo property = _entityTypeInfo.NavigationProperties[i];
						// Check each property value before setting it - only set properties that need to change
						object currentValue = property.GetValue(_entity, null);
						if (! Equals(currentValue, _unmodifiedNavigationPropertyValues[i]))
						{
							repository.DataServiceContext.SetLink(_entity, property.Name, currentValue);
						}
					}
				}
			}
			else if (ed.State == EntityStates.Added)
			{
				// Set links for all navigation properties
				foreach (PropertyInfo navProperty in _entityTypeInfo.NavigationProperties)
				{
					object currentValue = navProperty.GetValue(_entity, null);
					if (currentValue != null)
					{
						// This may be unecessary, but if currentValue is already being tracked it will return immediately.
						// This is necessary if _entity doesn't implement INotifyPropertyChanged, and currentValue is new.
						_oDataClient.AddEntityGraph(currentValue);
						repository.DataServiceContext.SetLink(_entity, navProperty.Name, currentValue);
					}
				}
			}

			// Delegate reporting of link collection changes
			foreach (var linkCollectionTracker in _linkCollectionTrackers)
			{
				if (linkCollectionTracker != null)
				{
					linkCollectionTracker.ReportChanges(repository.ODataClient);
				}
			}
		}
	}
}