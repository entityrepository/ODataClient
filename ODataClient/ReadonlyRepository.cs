// -----------------------------------------------------------------------
// <copyright file="ReadOnlyRepository.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------


using System.Diagnostics;
using System.Linq;
using PD.Base.EntityRepository.Api;
using PD.Base.PortableUtil.Model;
using System.Collections.Generic;

namespace PD.Base.EntityRepository.ODataClient
{

	/// <summary>
	/// The <see cref="IReadOnlyRepository{TEntity}"/> implementation for <see cref="ODataClient"/>.
	/// </summary>
	/// <typeparam name="TEntity">Entity type for this readonly repository.</typeparam>
	internal class ReadOnlyRepository<TEntity> : BaseRepository<TEntity>, IReadOnlyRepository<TEntity>
		where TEntity : class
	{

		private readonly Dictionary<TEntity, TEntity> _localCache;
 
		internal ReadOnlyRepository(ODataClient odataClient, EntitySetInfo entitySetInfo)
			: base(odataClient, entitySetInfo)
		{
			_localCache = new Dictionary<TEntity, TEntity>();
		}

		#region BaseRepository<TEntity>

		public override ICollection<TEntity> Local
		{
			get { return _localCache.Values; }
		}

		internal override TEntity ProcessQueryResult(TEntity entity)
		{
			IFreezable freezable = entity as IFreezable;
			if (freezable != null)
			{
				freezable.Freeze();
			}

			return AddToLocalCache(entity, EntityState.Unmodified);
		}

		internal override bool IsEditable
		{
			get { return false; }
		}

		internal override TEntity AddToLocalCache(TEntity entity, EntityState entityState)
		{
			lock (this)
			{
				// Dedup entity and equal entities
				TEntity existingEqual;
				if (_localCache.TryGetValue(entity, out existingEqual))
				{
					return existingEqual;
				}
				else
				{
					_localCache.Add(entity, entity);
					return entity;
				}
			}
		}

		internal override bool RemoveFromLocalCache(TEntity entity)
		{
			lock (this)
			{
				return _localCache.Remove(entity);
			}
		}

		#endregion
		#region BaseRepository

		/// <summary>
		/// Clears all entities from this repository's local cache, and from the DataServiceContext cache.
		/// </summary>
		public override void ClearLocal()
		{
			lock (this)
			{
				foreach (TEntity cachedEntity in _localCache.Keys)
				{
					DataServiceContext.Detach(cachedEntity);
				}
				_localCache.Clear();

#if DEBUG
				// In Debug builds, verify that there are no TEntity objects remaining
				Debug.Assert(! DataServiceContext.Entities.Any(ed => ed.Entity is TEntity), "There should be no remaining " + typeof(TEntity).FullName + " objects in DataServiceContext after ClearLocal() completes.");
#endif
			}
		}

		#endregion

	}
}