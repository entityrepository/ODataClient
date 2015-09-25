// -----------------------------------------------------------------------
// <copyright file="EditRepository.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data.Services.Client;
using System.Diagnostics;
using System.Linq;
using PD.Base.EntityRepository.Api;

namespace PD.Base.EntityRepository.ODataClient
{
	/// <summary>
	/// The <see cref="IEditRepository{TEntity}"/> implementation for <see cref="ODataClient"/>.
	/// </summary>
	/// <typeparam name="TEntity">Entity type for this edit repository.</typeparam>
	internal class EditRepository<TEntity> : BaseRepository<TEntity>, IEditRepository<TEntity>
		where TEntity : class
	{

		private readonly Dictionary<TEntity, EntityTracker> _entityTrackers = new Dictionary<TEntity, EntityTracker>();

		internal EditRepository(ODataClient odataClient, EntitySetInfo entitySetInfo)
			: base(odataClient, entitySetInfo)
		{}

		#region BaseRepository<TEntity>

		public override ICollection<TEntity> Local
		{
			get { return _entityTrackers.Keys; }
		}

		internal override TEntity ProcessQueryResult(TEntity entity)
		{
			return AddToLocalCache(entity, EntityState.Unmodified);
		}

		internal override bool IsEditable
		{
			get { return true; }
		}

		internal override TEntity AddToLocalCache(TEntity entity, EntityState entityState)
		{
			lock (this)
			{
				EntityTracker entityTracker;
				if (! _entityTrackers.TryGetValue(entity, out entityTracker))
				{
					// entity is not being tracked; start tracking it
					entityTracker = new EntityTracker(ODataClient, entity);
					_entityTrackers.Add(entity, entityTracker);
				}

				if (entityState == EntityState.Unmodified)
				{
					// Start change tracking, and enable Revert()
					entityTracker.CaptureUnmodifiedState();
					foreach (var linkCollectionTracker in entityTracker.LinkCollectionTrackers)
					{
						if (linkCollectionTracker != null)
						{
							linkCollectionTracker.CaptureUnmodifiedState();
						}
					}
				}

				return (TEntity) entityTracker.Entity;
			}
		}

		internal override bool RemoveFromLocalCache(TEntity entity)
		{
			lock (this)
			{
				return _entityTrackers.Remove(entity);
			}
		}

		#endregion

		#region BaseRepository

		internal override EntityTracker GetEntityTracker(object entity)
		{
			TEntity typedEntity = entity as TEntity;
			if (typedEntity == null)
			{
				throw new ArgumentException(string.Format("Entity {0} is type {1} : not compatible with repository {2}", entity, entity.GetType().FullName, this));
			}

			EntityTracker entityTracker = null;
			_entityTrackers.TryGetValue(typedEntity, out entityTracker);
			return entityTracker;
		}

		internal override LinkCollectionTracker GetLinkCollectionTracker(object entity, string linkCollectionPropertyName)
		{
			EntityTracker entityTracker = GetEntityTracker(entity);
			if (entityTracker == null)
			{
				return null;
			}
			return entityTracker.GetLinkCollectionTracker(linkCollectionPropertyName);
		}

		internal override void ReportChanges()
		{
			lock (this)
			{
				foreach (var entityTracker in _entityTrackers.Values)
				{
					entityTracker.ReportChanges(this);
				}
			}
		}

		internal override void ClearChanges()
		{
			lock (this)
			{
				foreach (var entityTracker in _entityTrackers.Values)
				{
					entityTracker.CaptureUnmodifiedState();
					foreach (var linkCollectionTracker in entityTracker.LinkCollectionTrackers)
					{
						linkCollectionTracker.CaptureUnmodifiedState();
					}
				}
			}
		}

		/// <summary>
		/// Clears all entities from this repository's local cache, from change-tracking, and from the DataServiceContext cache.
		/// </summary>
		public override void ClearLocal()
		{
			lock (this)
			{
				foreach (TEntity cachedEntity in _entityTrackers.Keys)
				{
					DataServiceContext.Detach(cachedEntity);
				}
				_entityTrackers.Clear();

#if DEBUG
				// In Debug builds, verify that there are no TEntity objects remaining
				Debug.Assert(! DataServiceContext.Entities.Any(ed => ed.Entity is TEntity),
				             "There should be no remaining " + typeof(TEntity).FullName + " objects in DataServiceContext after ClearLocal() completes.");
#endif
			}
		}

		#endregion

		#region IEditRepository<TEntity> Members

		public TEntity Add(TEntity entity)
		{
			return (TEntity) ODataClient.AddEntityGraph(entity, this);
		}

		public bool Delete(TEntity entity)
		{
			return ODataClient.DeleteEntityFromGraph(entity, this);
		}

		public TEntity Attach(TEntity entity, EntityState entityState = EntityState.Unmodified)
		{
			// NOTE: Attach does not break apart related objects - it is lower level than Add or Delete
			switch (entityState)
			{
				case EntityState.Added:
					DataServiceContext.AddObject(Name, entity);
					return AddToLocalCache(entity, EntityState.Added);

				case EntityState.Deleted:
					if (DataServiceContext.GetEntityDescriptor(entity) == null)
					{
						DataServiceContext.AttachTo(Name, entity);
					}
					DataServiceContext.DeleteObject(entity);
					RemoveFromLocalCache(entity);
					break;

				case EntityState.Modified:
					if (DataServiceContext.GetEntityDescriptor(entity) == null)
					{
						DataServiceContext.AttachTo(Name, entity);
					}
					DataServiceContext.UpdateObject(entity);
					return AddToLocalCache(entity, EntityState.Modified);

				case EntityState.Unmodified:
					if (DataServiceContext.GetEntityDescriptor(entity) == null)
					{
						DataServiceContext.AttachTo(Name, entity);
					}
					else
					{
						DataServiceContext.ChangeState(entity, EntityStates.Unchanged);
					}
					return AddToLocalCache(entity, EntityState.Unmodified);

				default:
					throw new InvalidOperationException("Attach() cannot be called with " + entityState);
			}
			return entity;
		}

		public EntityState Revert(TEntity entity)
		{
			EntityTracker entityTracker;
			if (_entityTrackers.TryGetValue(entity, out entityTracker))
			{
				entityTracker.RevertEntityToUnmodified(this);
				return EntityState.Unmodified;
			}
			else
			{
				return EntityState.Detached;
			}
		}

		public EntityState GetEntityState(TEntity entity)
		{
			// Check the EntityDescriptor - tracks value property changes
			EntityDescriptor entityDescriptor = DataServiceContext.GetEntityDescriptor(entity);
			if (entityDescriptor == null)
			{
				return EntityState.Detached;
			}

			switch (entityDescriptor.State)
			{
				case EntityStates.Added:
					return EntityState.Added;
				case EntityStates.Deleted:
					return EntityState.Deleted;
				case EntityStates.Detached:
					return EntityState.Detached;
				case EntityStates.Modified:
				case EntityStates.Unchanged:
					EntityTracker entityTracker;
					if (_entityTrackers.TryGetValue(entity, out entityTracker))
					{
						return entityTracker.AreStructuralPropertiesUnmodified()
						       && entityTracker.AreNavigationPropertiesUnmodified()
						       && entityTracker.AreLinkCollectionsUnmodified()
							       ? EntityState.Unmodified
							       : EntityState.Modified;
					}
					break;
				default:
					throw new InvalidOperationException("Unexpected EntityDescriptor.State: " + entityDescriptor.State);
			}

			// REVIEW: This only runs if there is no EntityTracker for entity, which should not be the case...
			// Check LinkDescriptors - tracks reference property changes
			var changedLinks = DataServiceContext.Links.Where(linkDescriptor => Object.ReferenceEquals(entity, linkDescriptor.Source) &&
			                                                                    ((linkDescriptor.State == EntityStates.Added) || (linkDescriptor.State == EntityStates.Deleted)
			                                                                                                                  || (linkDescriptor.State == EntityStates.Modified)));
			return changedLinks.Any() ? EntityState.Modified : EntityState.Unmodified;
		}

		#endregion
	}
}
