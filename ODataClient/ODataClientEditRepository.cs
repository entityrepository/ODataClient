// -----------------------------------------------------------------------
// <copyright file="ODataClientEditRepository.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.ObjectModel;
using System.Data.Services.Client;
using PD.Base.EntityRepository.Api;

namespace PD.Base.EntityRepository.ODataClient
{
	/// <summary>
	/// The <see cref="IEditRepository{TEntity}"/> implementation for <see cref="ODataClient"/>.
	/// </summary>
	/// <typeparam name="TEntity">Entity type for this edit repository.</typeparam>
	internal class ODataClientEditRepository<TEntity> : BaseRepository<TEntity>, IEditRepository<TEntity>
		where TEntity : class
	{

		private readonly DataServiceCollection<TEntity> _dataServiceCollection;
		private readonly ReadOnlyObservableCollection<TEntity> _readOnlyLocalCollection; 

		internal ODataClientEditRepository(ODataClient odataClient, string entitySetName)
			: base(odataClient, entitySetName)
		{
			// Provides change-tracking of INotifyPropertyChanged objects
			// BUG: Currently creates a StackOverflowException... do our own change tracking?
			_dataServiceCollection = new DataServiceCollection<TEntity>(DataServiceContext);
			_readOnlyLocalCollection = new ReadOnlyObservableCollection<TEntity>(_dataServiceCollection);
		}

		public override ReadOnlyObservableCollection<TEntity> Local
		{
			get { return _readOnlyLocalCollection; }
		}

		#region IEditRepository<TEntity> Members

		public IReadOnlyRepository<TEntity> ReadOnly
		{
			get { return ODataClient.ReadOnly<TEntity>(Name); }
		}

		public TEntity Add(TEntity entity)
		{
			DataServiceContext.AddObject(Name, entity);
			_dataServiceCollection.Add(entity);
			return entity;
		}

		public bool Delete(TEntity entity)
		{
			try
			{
				DataServiceContext.DeleteObject(entity);
				return true;
			}
			catch (InvalidOperationException)
			{ // Not in the context
				return false;
			}
		}

		public TEntity Attach(TEntity entity, EntityState entityState = EntityState.Unmodified)
		{
			switch (entityState)
			{
				case EntityState.Added:
					DataServiceContext.AddObject(Name, entity);
					break;

				case EntityState.Deleted:
					DataServiceContext.AttachTo(Name, entity);
					DataServiceContext.DeleteObject(entity);
					break;

				case EntityState.Modified:
					DataServiceContext.AttachTo(Name, entity);
					DataServiceContext.UpdateObject(entity);
					break;

				case EntityState.Unmodified:
					DataServiceContext.AttachTo(Name, entity);
					break;

				default:
					throw new InvalidOperationException("Attach() cannot be called with " + entityState);
			}
			return entity;
		}

		public EntityState Revert(TEntity entity)
		{
			throw new NotImplementedException("Need to implement Revert(TEntity)");
		}

		public EntityState? GetEntityState(TEntity entity)
		{
			EntityDescriptor entityDescriptor = DataServiceContext.GetEntityDescriptor(entity);
			if (entityDescriptor == null)
			{
				return null;
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
					return EntityState.Modified;
				case EntityStates.Unchanged:
					return EntityState.Unmodified;
				default:
					return null;
			}
		}

		#endregion

		#region IRepository members

		public override void ClearLocal()
		{
			_dataServiceCollection.Clear(true);
		}

		#endregion

	}
}
