// -----------------------------------------------------------------------
// <copyright file="ODataClient.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Services.Client;
using System.Data.Services.Common;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net;
using System.Reflection;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Data.Edm;
using Microsoft.Data.Edm.Csdl;
using Microsoft.Data.Edm.Validation;
using PD.Base.EntityRepository.Api;
using PD.Base.EntityRepository.Api.Exceptions;

namespace PD.Base.EntityRepository.ODataClient
{
	/// <summary>
	/// An <see cref="IDataContext"/> implementation that wraps a WCF Data Services client.
	/// </summary>
	// Named repositories are necessary b/c we could have multiple tables backed by the same type
	// Also TODO: Preview items
	// TODO: Delayed load (resolve a referenced object that wasn't fetched initially)
	public class ODataClient : IDataContextImpl, IDisposable
	{

		/// <summary>
		/// The service base URI, plus a trailing slash.
		/// </summary>
		private readonly Uri _baseUriWithSlash;

		/// <summary>
		/// WCF Data Services client, with some customization.
		/// </summary>
		private readonly CustomDataServiceContext _dataServiceContext;

		/// <summary>
		/// Collection of edit repositories - only modified within a lock.
		/// </summary>
		private readonly Dictionary<string, Tuple<Type, IRepository>> _editRepositories = new Dictionary<string, Tuple<Type, IRepository>>();

		/// <summary>
		/// Collection of readonly repositories - only modified within a lock.
		/// </summary>
		private readonly Dictionary<string, Tuple<Type, IRepository>> _readOnlyRepositories = new Dictionary<string, Tuple<Type, IRepository>>();

		/// <summary>
		/// The assemblies containing the entity types
		/// </summary>
		private readonly ISet<Assembly> _entityAssemblies;

		/// <summary>
		/// All the namespaces in the odata service metadata.
		/// </summary>
		private readonly ISet<string> _entityMetadataNamespaces = new HashSet<string>();

		/// <summary>
		/// Map of all entitySet names to types - not modified after initialization.
		/// </summary>
		private readonly Dictionary<string, Type> _entitySetTypes = new Dictionary<string, Type>();

		/// <summary>
		/// All the namespaces containing the entity types in this service.
		/// </summary>
		private readonly ISet<string> _entityTypeNamespaces;

		/// <summary>
		/// Task tracking asynchronous initialization.
		/// </summary>
		private readonly Task _initializeTask;

		/// <summary>Cache for <see cref="ResolveNameFromType"/>.</summary>
		private readonly Dictionary<Type, string> _nameFromTypeCache = new Dictionary<Type, string>();

		/// <summary>Cache for <see cref="ResolveTypeFromName"/>.</summary>
		private readonly Dictionary<string, Type> _typeFromNameCache = new Dictionary<string, Type>();

		/// <summary>
		/// Entity data model for the OData web service - not modified after initialization.
		/// </summary>
		private IEdmModel _edmModel;

		/// <summary>
		/// Creates and begins initialization of this <see cref="ODataClient"/>
		/// </summary>
		/// <param name="serviceRoot">The <see cref="Uri"/> to the OData service.</param>
		/// <param name="entityAssemblies">The set of assemblies containing the entity types used in this service.</param>
		/// <param name="entityTypeNamespaces">The set of namespaces containing the entity types used in this service.</param>
		public ODataClient(Uri serviceRoot, IEnumerable<Assembly> entityAssemblies, IEnumerable<string> entityTypeNamespaces)
		{
			Contract.Requires<ArgumentNullException>(serviceRoot != null);

			_entityAssemblies = new HashSet<Assembly>(entityAssemblies);
			_entityTypeNamespaces = new HashSet<string>(entityTypeNamespaces);
			_dataServiceContext = new CustomDataServiceContext(serviceRoot, this);
			_initializeTask = BeginInitializeTask();

			// Build _baseUriWithSlash
			UriBuilder uriBuilder = new UriBuilder(_dataServiceContext.BaseUri);
			if ((uriBuilder.Path.Length > 0) && ! uriBuilder.Path.EndsWith("/"))
			{
				uriBuilder.Path += "/";
			}
			_baseUriWithSlash = uriBuilder.Uri;
		}

		internal DataServiceContext DataServiceContext
		{
			get { return _dataServiceContext; }
		}

		/// <summary>
		/// Returns a task that completes when initialization is complete.
		/// </summary>
		public Task InitializeTask
		{
			get { return _initializeTask; }
		}

		#region IDisposable Members

		public void Dispose()
		{
			// TODO
		}

		#endregion

		public IEditRepository<TEntity> Edit<TEntity>(string entitySetName) where TEntity : class
		{
			EnsureInitializationCompleted();
			Type entityType = typeof(TEntity);

			lock (this)
			{
				Tuple<Type, IRepository> editRepoRecord;
				if (_editRepositories.TryGetValue(entitySetName, out editRepoRecord))
				{
					if (editRepoRecord.Item1 == entityType)
					{
						return (IEditRepository<TEntity>) editRepoRecord.Item2;
					}
					else
					{
						throw new InvalidOperationException(string.Format("EntitySet '{0}' is type {1} ; not {2}.", entitySetName, editRepoRecord.Item1, entityType));
					}
				}

				Type metadataEntityType;
				if (! _entitySetTypes.TryGetValue(entitySetName, out metadataEntityType))
				{
					// TODO: Create a placeholder/mock IEditRepository
					throw new ArgumentException(string.Format("No entity set found in {0} named {1}", _dataServiceContext.BaseUri, entitySetName));
				}
				if (metadataEntityType != entityType)
				{
					throw new InvalidOperationException(string.Format("EntitySet '{0}' is type {1} ; not {2}.", entitySetName, _entitySetTypes[entitySetName], entityType));
				}

				ODataClientEditRepository<TEntity> editRepository = new ODataClientEditRepository<TEntity>(this, entitySetName);
				_editRepositories.Add(entitySetName, new Tuple<Type, IRepository>(entityType, editRepository));
				return editRepository;
			}

		}

		public IReadOnlyRepository<TEntity> ReadOnly<TEntity>(string entitySetName) where TEntity : class
		{
			return null;
			// TODO:
			throw new NotImplementedException();
		}

		public Task<ReadOnlyCollection<IRequest>> InvokeAsync(params IRequest[] requests)
		{
			// Convert the queries into DataServiceRequests
			ODataClientRequest[] internalRequests = requests.Cast<ODataClientRequest>().ToArray();
			DataServiceRequest[] dataServiceRequests = internalRequests.Select(internalRequest => internalRequest.SendingRequest()).ToArray();
			Task<DataServiceResponse> taskResponse =
				Task.Factory.FromAsync<DataServiceRequest[], DataServiceResponse>((reqs, callback, state) => _dataServiceContext.BeginExecuteBatch(callback, state, reqs),
				                                                                  _dataServiceContext.EndExecuteBatch,
				                                                                  dataServiceRequests,
																				  internalRequests);
			return taskResponse.ContinueWith<ReadOnlyCollection<IRequest>>(ProcessBatchResponse);
		}

		public Task SaveChanges()
		{
			throw new NotImplementedException();
		}

		public void RevertChanges()
		{
			throw new NotImplementedException();
		}

		public void Clear()
		{
			lock (this)
			{
				foreach (var editRepository in _editRepositories.Values.Select(tuple => tuple.Item2))
				{
					editRepository.ClearLocal();
				}
				foreach (var readOnlyRepository in _readOnlyRepositories.Values.Select(tuple => tuple.Item2))
				{
					readOnlyRepository.ClearLocal();
				}

				// Detach remaining entities
				object[] entities = _dataServiceContext.Entities.Select(entityDescriptor => entityDescriptor.Entity).ToArray();
				Array.ForEach(entities, entity => _dataServiceContext.Detach(entity));
				Contract.Assert(_dataServiceContext.Entities.Count == 0);
				Contract.Assert(_dataServiceContext.Links.Count == 0);
			}
		}

		private Task BeginInitializeTask()
		{
			// Start an async request to the odata $metadata URL
			HttpWebRequest webRequest = WebRequest.CreateHttp(_dataServiceContext.GetMetadataUri());
			webRequest.Headers["DataServiceVersion"] = "3.0";
			return Task.Factory.FromAsync<WebResponse>(webRequest.BeginGetResponse, webRequest.EndGetResponse, null)
			           .ContinueWith(InitializeFromMetadata);
		}

		private void InitializeFromMetadata(Task<WebResponse> responseTask)
		{
			if (responseTask.IsFaulted)
			{
				throw new InitializationException("Could not initialize from metadata " + _dataServiceContext.GetMetadataUri(), responseTask.GetException());
			}

			using (WebResponse response = responseTask.Result)
			{
				using (XmlReader xmlReader = XmlReader.Create(response.GetResponseStream()))
				{
					IEnumerable<EdmError> edmErrors;
					if (! EdmxReader.TryParse(xmlReader, out _edmModel, out edmErrors))
					{
						throw new InitializationException("Error parsing OData schema from " + response.ResponseUri + " : " + string.Join("; ", edmErrors));
					}

					foreach (IEdmEntityContainer entityContainer in _edmModel.EntityContainers())
					{
						_entityMetadataNamespaces.Add(entityContainer.Namespace);
						foreach (var entitySet in entityContainer.EntitySets())
						{
							string elementTypeName = entitySet.ElementType.FullName();
							Type elementType = ResolveTypeFromName(elementTypeName);
							if (elementType != null)
							{
								_entitySetTypes.Add(entitySet.Name, elementType);
							}
						}
					}

					_dataServiceContext.Format.LoadServiceModel = () => _edmModel;
					// REVIEW: In case of difficult-to-understand errors, it can be useful to comment out this .UseJson() line
					// Reason: The Atom code path seems to have more testing and better error messages.
					_dataServiceContext.Format.UseJson();
				}
			}
		}

		/// <summary>
		/// Call to ensure that initialization has completed.
		/// </summary>
		private void EnsureInitializationCompleted()
		{
			if (! InitializeTask.IsCompleted)
			{
				// REVIEW: Make the timeout a ctor parameter?
				InitializeTask.Wait(); //60000);
			}

			if (InitializeTask.Status != TaskStatus.RanToCompletion)
			{
				throw new InitializationException("ODataClient initialization did not complete successfully.", InitializeTask.GetException());
			}
		}

		/// <summary>
		/// Convert an OData service type name to a <see cref="Type"/>; namespaces may not match up.
		/// </summary>
		/// <param name="typeName"></param>
		/// <returns></returns>
		protected Type ResolveTypeFromName(string typeName)
		{
			// Try a cache lookup
			Type resolvedType = null;
			lock (this)
			{
				if (_typeFromNameCache.TryGetValue(typeName, out resolvedType))
				{
					// Cache hit
					return resolvedType;
				}
			}

			// Determine the partialTypeName by stripping off any prefixes that match
			int longestPrefixMatch = 0;
			foreach (string prefix in _entityMetadataNamespaces)
			{
				if (typeName.StartsWith(prefix) && (prefix.Length > longestPrefixMatch))
				{
					longestPrefixMatch = prefix.Length;
				}
			}
			string partialTypeName = typeName.Substring(longestPrefixMatch);

			// Try resolving the last segment of the typeName from all the specified namespaces + assemblies
			foreach (string namespacePrefix in _entityTypeNamespaces)
			{
				string tryTypeName = namespacePrefix + partialTypeName;
				foreach (Assembly assembly in _entityAssemblies)
				{
					resolvedType = assembly.GetType(tryTypeName);
					if (resolvedType != null)
					{
						break;
					}
				}
				if (resolvedType != null)
				{
					break;
				}
			}

			// Another possibility - typeName is a real type
			if (resolvedType == null)
			{
				resolvedType = Type.GetType(typeName);
			}

			if (resolvedType != null)
			{
				lock (this)
				{ // Cache this work
					_nameFromTypeCache[resolvedType] = typeName;
					_typeFromNameCache[typeName] = resolvedType;
				}
			}

			return resolvedType;
		}

		/// <summary>
		/// Convert a <paramref name="clientType"/> to an odata service type name.
		/// </summary>
		/// <param name="clientType"></param>
		/// <returns></returns>
		protected string ResolveNameFromType(Type clientType)
		{
			// Try a cache lookup
			string odataTypeName = null;
			lock (this)
			{
				if (_nameFromTypeCache.TryGetValue(clientType, out odataTypeName))
				{
					// Cache hit
					return odataTypeName;
				}
			}

			// Determine the partialTypeName by stripping off any prefixes that match
			string clientTypeName = clientType.FullName;
			int longestPrefixMatch = 0;
			foreach (string prefix in _entityTypeNamespaces)
			{
				if (clientTypeName.StartsWith(prefix) && (prefix.Length > longestPrefixMatch))
				{
					longestPrefixMatch = prefix.Length;
				}
			}
			string partialTypeName = clientTypeName.Substring(longestPrefixMatch);

			// Since we're building a name, there's nothing to match - just use the first entity container namespace
			string metadataNamespace = _entityMetadataNamespaces.FirstOrDefault();
			odataTypeName = metadataNamespace == null ? partialTypeName : metadataNamespace + partialTypeName;

			lock (this)
			{ // Cache this work
				_nameFromTypeCache[clientType] = odataTypeName;
				_typeFromNameCache[odataTypeName] = clientType;
			}

			return odataTypeName;
		}

		protected Uri ResolveEntitySet(string entitySetName)
		{
			// TODO: Validate the entitySetName?
			Uri entitySetUri;
			if (!Uri.TryCreate(_baseUriWithSlash, entitySetName, out entitySetUri))
			{
				throw new ArgumentException("Unable to resolve entity set Uri for " + entitySetName);
			}
			return entitySetUri;
		}

		private ReadOnlyCollection<IRequest> ProcessBatchResponse(Task<DataServiceResponse> responseTask)
		{
			// Extract the requests from the state property
			ODataClientRequest[] internalRequests = (ODataClientRequest[]) responseTask.AsyncState;

			DataServiceResponse batchResponse = responseTask.Result;

			try
			{
				// Check for failures that affect all requests in the batch
				if (!batchResponse.IsBatchResponse)
				{
					throw new CommunicationException("OData communications error - expected batch response, but received non-batch  " + batchResponse);
				}
				if ((batchResponse.BatchStatusCode < 200)
				    || (batchResponse.BatchStatusCode > 299))
				{
					throw new CommunicationException("OData communications error - batch call returned batch status code " + batchResponse.BatchStatusCode);
				}
			}
			catch (CommunicationException comEx)
			{
				foreach (ODataClientRequest oDataClientRequest in internalRequests)
				{
					oDataClientRequest.CommunicationsFailure(comEx);
				}
			}

			foreach (OperationResponse operationResponse in batchResponse)
			{
				ODataClientRequest request = internalRequests.Single(req => req.IsRequestFor(operationResponse));
				request.HandleResponse(operationResponse);
			}
			return new ReadOnlyCollection<IRequest>(internalRequests);
		}

		#region Nested type: CustomDataServiceContext

		/// <summary>
		/// Custom subclass of <see cref="DataServiceContext"/>, to allow modification of default behavior.
		/// </summary>
		internal class CustomDataServiceContext : DataServiceContext
		{

			public CustomDataServiceContext(Uri serviceRoot, ODataClient client)
				: base(serviceRoot, DataServiceProtocolVersion.V3)
			{
				this.ResolveName = client.ResolveNameFromType;
				this.ResolveType = client.ResolveTypeFromName;
				this.ResolveEntitySet = client.ResolveEntitySet;

				this.AddAndUpdateResponsePreference = DataServiceResponsePreference.IncludeContent;
				this.MergeOption = MergeOption.PreserveChanges;

				this.IgnoreMissingProperties = false;
			}

		}

		#endregion
	}
}
