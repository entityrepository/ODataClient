// -----------------------------------------------------------------------
// <copyright file="DataServiceLinkGraph.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------


using System;
using System.Collections.Generic;
using System.Data.Services.Client;
using System.Diagnostics.Contracts;
using System.Linq;

namespace PD.Base.EntityRepository.ODataClient
{
	/// <summary>
	/// Implements tracing a link graph starting from a collection of entities, following all links from those entities held in a <see cref="DataServiceContext"/>.
	/// </summary>
	public class DataServiceLinkGraph
	{

		private readonly DataServiceContext _dataServiceContext;
		private readonly IEnumerable<object> _seedEntities;

		private readonly HashSet<object> _entities = new HashSet<object>();
		private readonly HashSet<LinkDescriptor> _links = new HashSet<LinkDescriptor>();

		/// <summary>
		/// Creates a new <see cref="DataServiceLinkGraph"/>, initialized from the specified parameters.
		/// </summary>
		/// <param name="dataServiceContext">A <see cref="DataServiceContext"/> containing the entities and links to walk.</param>
		/// <param name="seedEntities">A set of seed entities that are held in <see cref="DataServiceContext.Entities"/>, to start <see cref="WalkGraph"/> from.</param>
		public DataServiceLinkGraph(DataServiceContext dataServiceContext, IEnumerable<object> seedEntities)
		{
			Contract.Requires<ArgumentNullException>(dataServiceContext != null);
			Contract.Requires<ArgumentNullException>(seedEntities != null);

			_dataServiceContext = dataServiceContext;
			_seedEntities = seedEntities;
		}

		/// <summary>
		/// Walk the graph of links to find all objects and links starting from the seed entities.
		/// </summary>
		public void WalkGraph()
		{
			_entities.Clear();
			_links.Clear();

			Queue<object> frontier = new Queue<object>(_seedEntities);
			while (frontier.Count > 0)
			{
				object entity = frontier.Dequeue();
				if ((entity == null)
					|| _entities.Contains(entity))
				{	// Skip it
					continue;
				}

				var entitysLinks = _dataServiceContext.Links.Where(link => ReferenceEquals(link.Source, entity)).ToList();
				_links.UnionWith(entitysLinks);
				_entities.Add(entity);
				foreach (object relatedEntity in entitysLinks.Select(link => link.Target))
				{
					frontier.Enqueue(relatedEntity);
				}
			}
		}

		/// <summary>
		/// After calling <see cref="WalkGraph"/>, this property holds the set of entities found in the entity link graph.
		/// </summary>
		public ISet<object> Entities
		{
			get { return _entities; }
		}

		/// <summary>
		/// After calling <see cref="WalkGraph"/>, this property holds the set of links found in the entity link graph.
		/// </summary>
		public ISet<LinkDescriptor> Links
		{
			get { return _links; }
		}
	}
}