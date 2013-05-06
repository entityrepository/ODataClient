// -----------------------------------------------------------------------
// <copyright file="BaseRepository.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------


using System;
using System.Collections.Generic;
using System.Data.Services.Client;
using System.Linq;
using PD.Base.EntityRepository.Api;

namespace PD.Base.EntityRepository.ODataClient
{

	/// <summary>
	/// Common repository functionality between edit and readonly repositories.
	/// </summary>
	internal abstract class BaseRepository : IRepository
	{

		// The parent ODataClient
		private readonly ODataClient _odataClient;
		// Metadata holder for the EntitySet connected to this repository
		private readonly EntitySetInfo _entitySetInfo;

		internal BaseRepository(ODataClient odataClient, EntitySetInfo entitySetInfo)
		{
			_odataClient = odataClient;
			_entitySetInfo = entitySetInfo;
		}

		internal ODataClient ODataClient
		{
			get { return _odataClient; }
		}

		internal EntitySetInfo EntitySetInfo
		{
			get { return _entitySetInfo; }
		}

		internal EntityTypeInfo ElementTypeInfo
		{
			get { return _entitySetInfo.ElementType; }
		}

		internal DataServiceContext DataServiceContext
		{
			get { return _odataClient.DataServiceContext; }
		}

		/// <summary>
		///  When entities come in off the wire when returned from a request, they are added to the local collection by calling this
		///  method.
		/// 
		///  In addition, the repository may freeze or enable change tracking on the entity.
		/// </summary>
		/// <param name="entity"></param>
		/// <returns>The processed entity.</returns>
		internal abstract object ProcessQueryResult(object entity);

		/// <summary>
		/// Returns <c>true</c> if the contents of this repository can be edited.
		/// </summary>
		internal abstract bool IsEditable { get; }

		/// <summary>
		/// Adds <paramref name="entity"/> to the local cache, with tracking if applicable.  Does <em>not</em> add the entity
		/// to the <c>DataServiceContext</c>.
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="entityState">The <see cref="EntityState"/> for <paramref name="entity"/>.</param>
		/// <returns></returns>
		internal abstract object AddToLocal(object entity, EntityState entityState);

		/// <summary>
		/// Removes <paramref name="entity"/> from the local cache, and stops change-tracking of <paramref name="entity"/> if applicable.
		/// Does <em>not</em> remove <c>entity</c> from the <c>DataServiceContext</c>.
		/// </summary>
		/// <param name="entity"></param>
		/// <returns></returns>
		internal abstract bool RemoveFromLocal(object entity);

		/// <summary>
		/// If applicable, subclasses should override this method to return the <see cref="EntityTracker"/> that is
		/// tracking <paramref name="entity"/>.
		/// </summary>
		/// <param name="entity"></param>
		/// <returns></returns>
		internal virtual EntityTracker GetEntityTracker(object entity)
		{
			return null;
		}

		/// <summary>
		/// If applicable, subclasses should override this method to return the <see cref="LinkCollectionTracker"/> that is
		/// tracking the specified linke collection property on <paramref name="entity"/>.
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="linkCollectionPropertyName">The name of the property that corresponds to a collection of links.</param>
		/// <returns></returns>
		internal virtual LinkCollectionTracker GetLinkCollectionTracker(object entity, string linkCollectionPropertyName)
		{
			return null;
		}

		/// <summary>
		/// Iterates over all the change-tracked entities and records their change state.
		/// </summary>
		internal virtual void ReportChanges()
		{}

		/// <summary>
		/// Clears the change state for all entities that are change-tracked in this repository.
		/// </summary>
		internal virtual void ClearChanges()
		{}

		#region IRepository

		public string Name
		{
			get { return _entitySetInfo.Name; }
		}

		public Type ElementType
		{
			get { return _entitySetInfo.ElementType.EntityType; }
		}

		public IEnumerable<Type> EntityTypes
		{
			get { return _entitySetInfo.EntityTypes.Select(eti => eti.EntityType); }
		}

		public abstract void ClearLocal();

		#endregion

		public override string ToString()
		{
			return string.Format("{0}<{1}> {2}", IsEditable ? "EditRepository" : "ReadOnlyRepository", ElementType.FullName, Name);
		}

	}
}