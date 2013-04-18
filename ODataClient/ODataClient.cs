﻿// -----------------------------------------------------------------------
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
using System.Xml.Linq;
using Microsoft.Data.Edm;
using Microsoft.Data.Edm.Csdl;
using Microsoft.Data.Edm.Validation;
using PD.Base.EntityRepository.Api;
using PD.Base.EntityRepository.Api.Exceptions;
using PD.Base.PortableUtil.Model;

namespace PD.Base.EntityRepository.ODataClient
{
	/// <summary>
	/// An <see cref="IDataContextImpl"/> implementation that wraps a WCF Data Services client.
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
		/// Map of entity type to repository - not modified after initialization.  This ensures that only a single repository is used per type.
		/// </summary>
		private readonly Dictionary<Type, IRepository> _cacheRepositoryForType = new Dictionary<Type, IRepository>();

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
			_dataServiceContext.WritingEntity += OnWritingEntity;
			_dataServiceContext.ReadingEntity += OnReadingEntity;

			// Build _baseUriWithSlash
			UriBuilder uriBuilder = new UriBuilder(_dataServiceContext.BaseUri);
			if ((uriBuilder.Path.Length > 0) && ! uriBuilder.Path.EndsWith("/"))
			{
				uriBuilder.Path += "/";
			}
			_baseUriWithSlash = uriBuilder.Uri;

			_initializeTask = BeginInitializeTask();
		}

		/// <summary>
		/// Creates and begins initialization of this <see cref="ODataClient"/>
		/// </summary>
		/// <param name="serviceRoot">The <see cref="Uri"/> to the OData service.</param>
		/// <param name="representativeEntityTypes">Representative types to remote via odata - at least one type from each namespace and assembly must be included.</param>
		public ODataClient(Uri serviceRoot, params Type[] representativeEntityTypes)
		{
			Contract.Requires<ArgumentNullException>(serviceRoot != null);
			Contract.Requires<ArgumentNullException>(representativeEntityTypes != null);

			_entityAssemblies = new HashSet<Assembly>(representativeEntityTypes.Select(type => type.Assembly));
			_entityTypeNamespaces = new HashSet<string>(representativeEntityTypes.Select(type => type.Namespace));
			_dataServiceContext = new CustomDataServiceContext(serviceRoot, this);

			// Build _baseUriWithSlash
			UriBuilder uriBuilder = new UriBuilder(_dataServiceContext.BaseUri);
			if ((uriBuilder.Path.Length > 0) && !uriBuilder.Path.EndsWith("/"))
			{
				uriBuilder.Path += "/";
			}
			_baseUriWithSlash = uriBuilder.Uri;

			_initializeTask = BeginInitializeTask();
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

		/// <inheritdoc />
		public IEnumerable<IRepository> Repositories
		{
			get { return _cacheRepositoryForType.Values; }
		}

		#region IDisposable Members

		/// <summary>
		/// Disposes all resources held by the <c>ODataClient</c>.  Currently not implemented.
		/// </summary>
		public void Dispose()
		{
			// TODO
		}

		#endregion

		/// <inheritdoc />
		public IEditRepository<TEntity> Edit<TEntity>(string entitySetName) where TEntity : class
		{
			EnsureInitializationCompleted();
			Type entityType = typeof(TEntity);

			lock (this)
			{
				// Read from cached EditRepository s.
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

				// Validate against the server metadata
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

				EditRepository<TEntity> editRepository = new EditRepository<TEntity>(this, entitySetName);
				_editRepositories.Add(entitySetName, new Tuple<Type, IRepository>(entityType, editRepository));
				_cacheRepositoryForType.Add(entityType, editRepository);
				return editRepository;
			}
		}

		/// <inheritdoc />
		public IReadOnlyRepository<TEntity> ReadOnly<TEntity>(string entitySetName) where TEntity : class
		{
			EnsureInitializationCompleted();
			Type entityType = typeof(TEntity);

			lock (this)
			{
				// Read from cached ReadOnlyRepository s.
				Tuple<Type, IRepository> readonlyRepoRecord;
				if (_readOnlyRepositories.TryGetValue(entitySetName, out readonlyRepoRecord))
				{
					if (readonlyRepoRecord.Item1 == entityType)
					{
						return (IReadOnlyRepository<TEntity>) readonlyRepoRecord.Item2;
					}
					else
					{
						throw new InvalidOperationException(string.Format("EntitySet '{0}' is type {1} ; not {2}.", entitySetName, readonlyRepoRecord.Item1, entityType));
					}
				}

				// Validate against the server metadata
				Type metadataEntityType;
				if (!_entitySetTypes.TryGetValue(entitySetName, out metadataEntityType))
				{
					// TODO: Create a placeholder/mock IReadOnlyRepository?
					throw new ArgumentException(string.Format("No entity set found in {0} named {1}", _dataServiceContext.BaseUri, entitySetName));
				}
				if (metadataEntityType != entityType)
				{
					throw new InvalidOperationException(string.Format("EntitySet '{0}' is type {1} ; not {2}.", entitySetName, _entitySetTypes[entitySetName], entityType));
				}

				ReadOnlyRepository<TEntity> readOnlyRepository = new ReadOnlyRepository<TEntity>(this, entitySetName);
				_readOnlyRepositories.Add(entitySetName, new Tuple<Type, IRepository>(entityType, readOnlyRepository));
				_cacheRepositoryForType.Add(entityType, readOnlyRepository);
				return readOnlyRepository;
			}
		}

		/// <inheritdoc />
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

		/// <inheritdoc />
		public Task SaveChanges()
		{
			ValidateChangedEntities();

			Task<DataServiceResponse> taskResponse =
				Task.Factory.FromAsync<DataServiceResponse>((callback, state) => _dataServiceContext.BeginSaveChanges(SaveChangesOptions.Batch, callback, state),
				                                            _dataServiceContext.EndSaveChanges,
				                                            null); // state == null for now...
			return taskResponse.ContinueWith(ProcessSaveChangesResponse);
		}

		/// <inheritdoc />
		public void RevertChanges()
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
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
		/// Ensures that initialization has completed.
		/// </summary>
		private void EnsureInitializationCompleted()
		{
			if (! InitializeTask.IsCompleted)
			{
				throw new InitializationException("ODataClient initialization has not completed.");
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

		/// <summary>
		/// Returns the <see cref="Uri"/> for an odata entityset.
		/// </summary>
		/// <param name="entitySetName">The entity set name.</param>
		/// <returns>A <see cref="Uri"/> for the entity set.</returns>
		/// <remarks>Note that this method does not validate that the entity set exists or that the returned <c>Uri</c> is valid.</remarks>
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
			// Extract the requests from the async state property
			ODataClientRequest[] internalRequests = (ODataClientRequest[]) responseTask.AsyncState;

			// batchException is an exception that affects all requests in the batch
			Exception batchException = null;
			if (responseTask.IsFaulted)
			{
				batchException = responseTask.GetException();
			}
			else
			{
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

					foreach (OperationResponse operationResponse in batchResponse)
					{
						ODataClientRequest request = internalRequests.Single(req => req.IsRequestFor(operationResponse));
						request.HandleResponse(this, operationResponse);
					}
				}
				catch (CommunicationException comEx)
				{
					batchException = comEx;
				}
			}

			if (batchException != null)
			{
				foreach (ODataClientRequest oDataClientRequest in internalRequests)
				{
					oDataClientRequest.Failed(batchException);
				}
			}

			return new ReadOnlyCollection<IRequest>(internalRequests);
		}

		private void ValidateChangedEntities()
		{
			List<Exception> validationExceptions = new List<Exception>();
			foreach (EntityDescriptor entityDescriptor in _dataServiceContext.Entities)
			{
				EntityStates state = entityDescriptor.State;
				if ((EntityStates.Added == (state & EntityStates.Added)) || (EntityStates.Modified == (state & EntityStates.Modified)))
				{
					IValidatable validatable = entityDescriptor.Entity as IValidatable;
					if (validatable != null)
					{
						try
						{
							validatable.Validate();							
						}
						catch (Exception vex)
						{
							validationExceptions.Add(vex);
						}
					}
				}				
			}

			if (validationExceptions.Count > 0)
			{
				throw new AggregateException("One or more added or modified entities failed validation.", validationExceptions);
			}			
		}

		private void ProcessSaveChangesResponse(Task<DataServiceResponse> responseTask)
		{
			DataServiceResponse batchResponse = responseTask.Result;
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

			foreach (ChangeOperationResponse changeResponse in batchResponse)
			{
				// Log(changeResponse.Error);
				// Log("Object updated: " + changeResponse.Descriptor);				
			}
		}

		internal IEnumerable<TEntity> ProcessQueryResults<TEntity>(IEnumerable<TEntity> results)
		{
			// TODO: Break up related entities and process them
			// TODO: Join related entities

			// Find the repository for the type
			IRepository repository;
			if (! _cacheRepositoryForType.TryGetValue(typeof(TEntity), out repository))
			{
				// Doesn't match an existing repository, don't do anything
				return results;
			}

			// Delegate to the repository to do the processing
			BaseRepository<TEntity> baseRepository = (BaseRepository<TEntity>) repository;
			return baseRepository.ProcessQueryResults(results);
		}

		private void OnWritingEntity(object sender, ReadingWritingEntityEventArgs e)
		{
			// Find the repository for the entity by type
			// TODO: This may break for inheritance; TEST!
			IRepository repository;
			_cacheRepositoryForType.TryGetValue(e.Entity.GetType(), out repository);
			EntityTypeInfo entityTypeInfo = repository as EntityTypeInfo;
			if (entityTypeInfo != null)
			{
				// Remove any properties that shouldn't be serialized.
				if (entityTypeInfo.DontSerializeProperties.Length > 0)
				{
					XNamespace mNamespace = e.Data.GetNamespaceOfPrefix("m");
					XNamespace dNamespace = e.Data.GetNamespaceOfPrefix("d");
					if ((mNamespace != null) && (dNamespace != null))
					{
						XElement nodeProperties = e.Data.Descendants(mNamespace.GetName("properties")).FirstOrDefault();
						if (nodeProperties != null)
						{
							foreach (var propertyInfo in entityTypeInfo.DontSerializeProperties)
							{
								nodeProperties.Descendants(dNamespace.GetName(propertyInfo.Name)).Remove();	
							}							
						}
					}
				}
			}
		}

		private void OnReadingEntity(object sender, ReadingWritingEntityEventArgs e)
		{
			object entity = e.Entity;
			Uri uri = e.BaseUri;
		}

		#region Nested type: CustomDataServiceContext

		/// <summary>
		/// Custom subclass of <see cref="DataServiceContext"/>, to allow modification of default behavior.
		/// </summary>
		internal class CustomDataServiceContext : DataServiceContext
		{
			/// <summary>
			/// Creates a new <see cref="CustomDataServiceContext"/>.
			/// </summary>
			/// <param name="serviceRoot"></param>
			/// <param name="client"></param>
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
