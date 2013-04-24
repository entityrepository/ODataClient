// -----------------------------------------------------------------------
// <copyright file="ReadOnlyRepository.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------


using System;
using PD.Base.EntityRepository.Api;
using PD.Base.PortableUtil.Model;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace PD.Base.EntityRepository.ODataClient
{

	/// <summary>
	/// The <see cref="IReadOnlyRepository{TEntity}"/> implementation for <see cref="ODataClient"/>.
	/// </summary>
	/// <typeparam name="TEntity">Entity type for this readonly repository.</typeparam>
	internal class ReadOnlyRepository<TEntity> : BaseRepository<TEntity>, IReadOnlyRepository<TEntity>
		where TEntity : class
	{

		private readonly ObservableCollection<TEntity> _localCollection;
		private readonly ReadOnlyObservableCollection<TEntity> _readOnlyLocalCollection;

		internal ReadOnlyRepository(ODataClient odataClient, string entitySetName)
			: base(odataClient, entitySetName)
		{
			_localCollection = new ObservableCollection<TEntity>();
			_readOnlyLocalCollection = new ReadOnlyObservableCollection<TEntity>(_localCollection);
		}

		#region BaseRepository<TEntity>

		internal override TEntity[] ProcessQueryResults(IEnumerable<TEntity> entities)
		{
			TEntity[] array = entities.ToArray();
			for (int i = 0; i < array.Length; ++i)
			{
				TEntity e = array[i];
				IFreezable freezable = e as IFreezable;
				if (freezable != null)
				{
					freezable.Freeze();
				}

				lock (this)
				{
					// TODO: Support deduping by Id
					_localCollection.Add(e);
				}
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
				_localCollection.Clear();
			}
		}

		#endregion		 

	}
}