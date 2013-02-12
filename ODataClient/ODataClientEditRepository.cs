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
	internal class ODataClientEditRepository<TEntity> : ODataClientBaseQueryable<TEntity>, IBaseRepository, IEditRepository<TEntity>
		where TEntity : class
	{
		private readonly DataServiceCollection<TEntity> _dataServiceCollection;

		internal ODataClientEditRepository(DataServiceContext dataServiceContext, string entitySetName)
			: base(dataServiceContext, entitySetName)
		{
			// Provides change-tracking of INotifyPropertyChanged objects
			_dataServiceCollection = new DataServiceCollection<TEntity>(_dataServiceContext);

			//_dataServiceContext.ReadingEntity += DataServiceContext_ReadingEntity;
		}

		#region IEditRepository<TEntity> Members

		public TEntity Add(TEntity entity)
		{
			_dataServiceContext.AddObject(_entitySetName, entity);
			Local.Add(entity);
			return entity;
		}

		public bool Delete(TEntity entity)
		{
			try
			{
				_dataServiceContext.DeleteObject(entity);
				return true;
			}
			catch (InvalidOperationException)
			{ // Not in the context
				return false;
			}
		}

		public ObservableCollection<TEntity> Local
		{
			get { return _dataServiceCollection; }
		}

		public TEntity Attach(TEntity entity, EntityState entityState = EntityState.Unmodified)
		{
			switch (entityState)
			{
				case EntityState.Added:
					_dataServiceContext.AddObject(_entitySetName, entity);
					break;

				case EntityState.Deleted:
					_dataServiceContext.AttachTo(_entitySetName, entity);
					_dataServiceContext.DeleteObject(entity);
					break;

				case EntityState.Modified:
					_dataServiceContext.AttachTo(_entitySetName, entity);
					_dataServiceContext.UpdateObject(entity);
					break;

				case EntityState.Unmodified:
					_dataServiceContext.AttachTo(_entitySetName, entity);
					break;

				default:
					throw new InvalidOperationException("Attach() cannot be called with " + entityState);
			}
			return entity;
		}

		public EntityState Revert(TEntity entity)
		{
			throw new NotImplementedException("Need to impelemnt Revert(TEntity)");
		}

		public EntityState? GetEntityState(TEntity entity)
		{
			EntityDescriptor entityDescriptor = _dataServiceContext.GetEntityDescriptor(entity);
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

		#region IBaseRepository members

		public void ClearLocal()
		{
			_dataServiceCollection.Clear(true);
		}

		#endregion

		/// <summary>
		/// When an entity is read off the wire, check to see if it belongs in this repository.  If it does, include it in
		/// the DataServiceCollection that performs the change tracking.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		// TODO: This doesn't work when reading JSON
		private void DataServiceContext_ReadingEntity(object sender, ReadingWritingEntityEventArgs e)
		{
			TEntity entity = e.Entity as TEntity;
			if ((entity != null)
			    && _entitySetBaseUri.IsBaseOf(e.BaseUri))
			{
				// Merge the entity into the tracked collection
				_dataServiceCollection.Load(entity);
			}
		}
	}
}
