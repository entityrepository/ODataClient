// -----------------------------------------------------------------------
// <copyright file="EntityRef.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Diagnostics.Contracts;

namespace Scrum.Model.Base
{


	public struct EntityRef<TEntity, TKey> where TEntity : class
	{

		// Whether the entity reference is set.
		// The entity value.
		private TEntity _entity;
		private TKey _foreignKey;
		// Function to obtain the key of an entity
		private Func<TEntity, TKey> _funcEntityToKey;
		private bool _isSet;

		public EntityRef(Func<TEntity, TKey> funcEntityToKey)
		{
			Contract.Requires<ArgumentNullException>(funcEntityToKey != null);

			_funcEntityToKey = funcEntityToKey;
			_isSet = false;
			_foreignKey = default(TKey);
			_entity = null;
		}

		public TKey ForeignKey
		{
			get { return _foreignKey; }
			set { _foreignKey = value; }
		}

		public TEntity Entity
		{
			get
			{
				if (Object.ReferenceEquals(_entity, null))
				{
					if (_isSet)
					{
						LoadEntity();
					}
					else
					{
						return null;
					}
				}
				return _entity;
			}
			set
			{
				if (Object.ReferenceEquals(value, null))
				{
					Clear();
				}
				else
				{
					_entity = value;
					_foreignKey = GetKeyOfEntity(value);
					_isSet = true;
				}
			}
		}

		public void Clear()
		{
			_isSet = false;
			_foreignKey = default(TKey);
			_entity = null;
		}

		public TKey GetKeyOfEntity(TEntity entity)
		{
			Contract.Requires<ArgumentNullException>(entity != null);

			return _funcEntityToKey(entity);
		}

		private void LoadEntity()
		{
			throw new NotImplementedException();
		}

	}
}
