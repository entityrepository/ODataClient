// -----------------------------------------------------------------------
// <copyright file="EditRepository.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Services.Client;
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

		//private readonly DataServiceCollection<TEntity> _dataServiceCollection;
		private readonly ReadOnlyObservableCollection<TEntity> _readOnlyLocalCollection;

		internal EditRepository(ODataClient odataClient, string entitySetName)
			: base(odataClient, entitySetName)
		{
			// TODO: Either change DbEnum<T, TId> to be DbEnum<TId, T>, or change it to be DbEnum<T> (with extension methods), 
			// TODO: or write my own replacement for DataServiceCollection to implement change-tracking.
			// Reason: System.Data.Services.Client.BindingEntityInfo.IsDataServiceCollection(typeof(T<T>), ...) causes a StackOverflowException
			// calling System.Data.Services.Client.BindingEntityInfo.IsEntityType(typeof(T<T>)) and recursing forever.

			// Provides change-tracking of INotifyPropertyChanged objects
			//_dataServiceCollection = new DataServiceCollection<TEntity>(DataServiceContext);

			_readOnlyLocalCollection = new ReadOnlyObservableCollection<TEntity>(new ObservableCollection<TEntity>()); //_dataServiceCollection);
		}

		#region BaseRepository<TEntity>

		internal override TEntity[] ProcessQueryResults(IEnumerable<TEntity> entities)
		{
			TEntity[] array = entities.ToArray();

			lock (this)
			{
				// TODO: Support deduping by Id, if not done by DataServiceCollection?
				//_dataServiceCollection.Load(array);
			}
			return array;
		}

		public override ReadOnlyObservableCollection<TEntity> Local
		{
			get { return _readOnlyLocalCollection; }
		}

		public override void ClearLocal()
		{
			lock (this)
			{
				//_dataServiceCollection.Clear(true);
			}
		}

		#endregion

		#region IEditRepository<TEntity> Members

		public TEntity Add(TEntity entity)
		{
			DataServiceContext.AddObject(Name, entity);
			//_dataServiceCollection.Add(entity);
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
			// Check the EntityDescriptor - tracks value property changes
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
					break;
				default:
					return null;
			}

			// Check LinkDescriptors - tracks reference property changes
			var changedLinks = DataServiceContext.Links.Where(linkDescriptor => Object.ReferenceEquals(entity, linkDescriptor.Source) &&
			                                                                    ((linkDescriptor.State == EntityStates.Added) || (linkDescriptor.State == EntityStates.Deleted)
			                                                                                                                  || (linkDescriptor.State == EntityStates.Modified)));
			return changedLinks.Any() ? EntityState.Modified : EntityState.Unmodified;
		}

		#endregion

	}
}
