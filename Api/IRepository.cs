// -----------------------------------------------------------------------
// <copyright file="IRepository.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using PD.Base.EntityRepository.Api.Base;

namespace PD.Base.EntityRepository.Api
{
	/// <summary>
	/// Exposes common functionality between edit and read-only repositories.  This class is not generic so that all
	/// repository instances can be treated the same way with respect to this interface.
	/// </summary>
	[ContractClass(typeof(RepositoryContract))]
	public interface IRepository
	{

		/// <summary>
		/// Returns the name of this repository - also known as the entity set name.
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Returns the base entity <see cref="Type"/> for this repository.
		/// </summary>
		Type ElementType { get; }

		/// <summary>
		/// Returns the collection of entity types (which all must equal or subclass <see cref="ElementType"/>) held in this repository.
		/// </summary>
		IEnumerable<Type> EntityTypes { get; }

		/// <summary>
		/// Clear all locally cached data.
		/// </summary>
		void ClearLocal();

	}


	[ContractClassFor(typeof(IRepository))]
	internal abstract class RepositoryContract : IRepository
	{
		#region IRepository Members

		public string Name
		{
			get
			{
				Contract.Ensures(Check.NotNullOrWhiteSpace(Contract.Result<string>()));

				throw new NotImplementedException();
			}
		}

		public void ClearLocal()
		{
			throw new NotImplementedException();
		}

		public Type ElementType
		{
			get
			{
				Contract.Ensures(null != Contract.Result<Type>());

				throw new NotImplementedException();
			}
		}

		public IEnumerable<Type> EntityTypes
		{
			get
			{
				Contract.Ensures(null != Contract.Result<IEnumerable<Type>>());
				Contract.Ensures(Contract.Result<IEnumerable<Type>>().Any());

				throw new NotImplementedException();
			}
		}

		#endregion
	}
}
