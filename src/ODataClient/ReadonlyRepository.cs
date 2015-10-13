// -----------------------------------------------------------------------
// <copyright file="ReadonlyRepository.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using EntityRepository.Api;

namespace EntityRepository.ODataClient
{

	/// <summary>
	/// The <see cref="IReadOnlyRepository{TEntity}"/> implementation for <see cref="ODataClient"/>.
	/// </summary>
	/// <typeparam name="TEntity">Entity type for this readonly repository.</typeparam>
	internal class ReadOnlyRepository<TEntity> : BaseRepository<TEntity>, IReadOnlyRepository<TEntity>
		where TEntity : class
	{

		private readonly Dictionary<TEntity, TEntity> _localCache;
        private Action<TEntity> _onLoadObjectFromRepository;

        /// <summary>
        /// Create a readonly repository.
        /// </summary>
        /// <param name="odataClient">The client</param>
        /// <param name="entitySetInfo">The entity set</param>
        /// <param name="onLoadObjectFromRepository">Useful to implement, eg, IFreezable.  May be null.</param>
        internal ReadOnlyRepository(ODataClient odataClient, EntitySetInfo entitySetInfo, Action<TEntity> onLoadObjectFromRepository)
			: base(odataClient, entitySetInfo)
		{
			_localCache = new Dictionary<TEntity, TEntity>();
            _onLoadObjectFromRepository = onLoadObjectFromRepository;
		}

		#region BaseRepository<TEntity>

		public override ICollection<TEntity> Local
		{
			get { return _localCache.Values; }
		}

		internal override TEntity ProcessQueryResult(TEntity entity)
		{
			if (_onLoadObjectFromRepository != null)
			{
                _onLoadObjectFromRepository(entity);
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
				Debug.Assert(! DataServiceContext.Entities.Any(ed => ed.Entity is TEntity),
				             "There should be no remaining " + typeof(TEntity).FullName + " objects in DataServiceContext after ClearLocal() completes.");
#endif
			}
		}

		#endregion
	}
}
