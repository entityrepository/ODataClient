// -----------------------------------------------------------------------
// <copyright file="IQueryRequest.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Linq;

namespace EntityRepository.Api
{
	/// <summary>
	/// A query request.
	/// </summary>
	/// <typeparam name="TEntity"></typeparam>
	public interface IQueryRequest<out TEntity> : IRequest, IQueryable<TEntity>
	{

	}
}
