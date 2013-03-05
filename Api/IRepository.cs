// -----------------------------------------------------------------------
// <copyright file="IRepository.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Diagnostics.Contracts;
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
		/// Clear all locally cached data.
		/// </summary>
		void ClearLocal();

		/// <summary>
		/// Returns the entity <see cref="Type"/> for this repository.
		/// </summary>
		Type EntityType { get; }

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

		public Type EntityType
		{
			get
			{
				Contract.Ensures(null != Contract.Result<Type>());

				throw new NotImplementedException();
			}
		}

		#endregion
	}
}
