// -----------------------------------------------------------------------
// <copyright file="ODataClient.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Data.Services.Client;
using System.Data.Services.Common;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net;
using System.Reflection;
using System.ServiceModel;
using System.Text;
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
	public class ODataClient : IDataContextImpl, IDisposable, ITypeResolver
	{
#if ! SILVERLIGHT
		/// <summary>
		/// <see cref="TraceSource"/> for logging.
		/// </summary>
		private static readonly TraceSource s_trace = new TraceSource(typeof(ODataClient).FullName, SourceLevels.Verbose);
#endif

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
		private readonly Dictionary<string, BaseRepository> _editRepositories = new Dictionary<string, BaseRepository>();

		/// <summary>
		/// Collection of readonly repositories - only modified within a lock.
		/// </summary>
		private readonly Dictionary<string, BaseRepository> _readOnlyRepositories = new Dictionary<string, BaseRepository>();

		/// <summary>
		/// The assemblies containing the entity types
		/// </summary>
		private readonly ISet<Assembly> _entityAssemblies;

		/// <summary>
		/// All the namespaces containing the entity types in this service.
		/// </summary>
		private readonly ISet<string> _entityTypeNamespaces;

		/// <summary>
		/// All the namespaces in the odata service metadata.
		/// </summary>
		private readonly ISet<string> _entityMetadataNamespaces = new HashSet<string>();

		/// <summary>
		/// Map of all entitySet names to <see cref="EntitySetInfo"/> - not modified after initialization.
		/// </summary>
		private readonly Dictionary<string, EntitySetInfo> _entitySets = new Dictionary<string, EntitySetInfo>();

		/// <summary>
		/// Map of entity type to <see cref="EntityTypeInfo"/> - not modified after initialization.  Used for breaking apart entities and tracking their links.
		/// </summary>
		private readonly Dictionary<Type, EntityTypeInfo> _entityTypeInfos = new Dictionary<Type, EntityTypeInfo>();

		/// <summary>
		/// Map of entity type to repository - not modified after initialization.  This ensures that only a single repository is used per type.
		/// </summary>
		private readonly Dictionary<Type, BaseRepository> _repositoriesByType = new Dictionary<Type, BaseRepository>();

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
		/// <param name="serviceRoot">The <see cref="Uri"/> to the OData service.  For silverlight apps, this may be a relative URL.</param>
		/// <param name="entityAssemblies">The set of assemblies containing the entity types used in this service.</param>
		/// <param name="entityTypeNamespaces">The set of namespaces containing the entity types used in this service.</param>
		public ODataClient(Uri serviceRoot, IEnumerable<Assembly> entityAssemblies, IEnumerable<string> entityTypeNamespaces)
		{
			Contract.Requires<ArgumentNullException>(serviceRoot != null);

			_entityAssemblies = new HashSet<Assembly>(entityAssemblies);
			_entityTypeNamespaces = new HashSet<string>(entityTypeNamespaces);

			CommonInit(serviceRoot, out _dataServiceContext, out _baseUriWithSlash);
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

			CommonInit(serviceRoot, out _dataServiceContext, out _baseUriWithSlash);
			_initializeTask = BeginInitializeTask();
		}

		private void CommonInit(Uri serviceRoot, out CustomDataServiceContext dataServiceContext, out Uri baseUriWithSlash)
		{
			Contract.Requires<ArgumentNullException>(serviceRoot != null);

#if SILVERLIGHT
			// For silverlight applications, build a full Uri based on the silverlight app URL
			SilverlightUtil.ConvertAppRelativeUriToAbsoluteUri(ref serviceRoot);
#endif

			if (!serviceRoot.IsAbsoluteUri)
			{
				throw new UriFormatException("Service URI " + serviceRoot + " must be absolute, or resolvable to an absolute URI from the application URI.");
			}

			dataServiceContext = new CustomDataServiceContext(serviceRoot, this);

			// These events aren't supported when reading/writing JSON
			//_dataServiceContext.WritingEntity += OnWritingEntity;
			//_dataServiceContext.ReadingEntity += OnReadingEntity;

			// Build _baseUriWithSlash
			UriBuilder uriBuilder = new UriBuilder(_dataServiceContext.BaseUri);
			if ((uriBuilder.Path.Length > 0) && !uriBuilder.Path.EndsWith("/"))
			{
				uriBuilder.Path += "/";
			}
			baseUriWithSlash = uriBuilder.Uri;
		}

		/// <summary>
		/// Returns the <see cref="DataServiceContext"/> managed by this <see cref="ODataClient"/> instance.
		/// </summary>
		public DataServiceContext DataServiceContext
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
			get { return _repositoriesByType.Values; }
		}

		internal Dictionary<Type, BaseRepository> RepositoriesByType
		{
			get { return _repositoriesByType; }
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
				BaseRepository editRepoRecord;
				if (_editRepositories.TryGetValue(entitySetName, out editRepoRecord))
				{
					if (editRepoRecord.ElementType == entityType)
					{
						return (IEditRepository<TEntity>) editRepoRecord;
					}
					else
					{
						throw new InvalidOperationException(string.Format("EntitySet '{0}' is type {1} ; not {2}.", entitySetName, editRepoRecord.ElementType, entityType));
					}
				}

				// Validate against the server metadata
				EntitySetInfo entitySetInfo;
				if (! _entitySets.TryGetValue(entitySetName, out entitySetInfo))
				{
					// TODO: Create a placeholder/mock IEditRepository
					throw new ArgumentException(string.Format("No entity set found in {0} named {1}", _dataServiceContext.BaseUri, entitySetName));
				}
				if (entitySetInfo.ElementType.EntityType != entityType)
				{
					throw new InvalidOperationException(string.Format("EntitySet '{0}' is type {1} ; not {2}.", entitySetName, entitySetInfo.ElementType.EntityType, entityType));
				}

				EditRepository<TEntity> editRepository = new EditRepository<TEntity>(this, entitySetInfo);
				_editRepositories.Add(entitySetName, editRepository);
				foreach (Type type in editRepository.EntityTypes)
				{
					_repositoriesByType.Add(type, editRepository);
				}
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
				BaseRepository readonlyRepoRecord;
				if (_readOnlyRepositories.TryGetValue(entitySetName, out readonlyRepoRecord))
				{
					if (readonlyRepoRecord.ElementType == entityType)
					{
						return (IReadOnlyRepository<TEntity>) readonlyRepoRecord;
					}
					else
					{
						throw new InvalidOperationException(string.Format("EntitySet '{0}' is type {1} ; not {2}.", entitySetName, readonlyRepoRecord.ElementType, entityType));
					}
				}

				// Validate against the server metadata
				EntitySetInfo entitySetInfo;
				if (!_entitySets.TryGetValue(entitySetName, out entitySetInfo))
				{
					// TODO: Create a placeholder/mock IEditRepository
					throw new ArgumentException(string.Format("No entity set found in {0} named {1}", _dataServiceContext.BaseUri, entitySetName));
				}
				if (entitySetInfo.ElementType.EntityType != entityType)
				{
					throw new InvalidOperationException(string.Format("EntitySet '{0}' is type {1} ; not {2}.", entitySetName, entitySetInfo.ElementType.EntityType, entityType));
				}

				ReadOnlyRepository<TEntity> readOnlyRepository = new ReadOnlyRepository<TEntity>(this, entitySetInfo);
				_readOnlyRepositories.Add(entitySetName, readOnlyRepository);
				foreach (Type type in readOnlyRepository.EntityTypes)
				{
					_repositoriesByType.Add(type, readOnlyRepository);
				}
				return readOnlyRepository;
			}
		}

		/// <inheritdoc />
		public Task<ReadOnlyCollection<IRequest>> InvokeAsync(params IRequest[] requests)
		{
			// Convert the queries into DataServiceRequests
			ODataClientRequest[] internalRequests = requests.Cast<ODataClientRequest>().ToArray();
#if SILVERLIGHT
			Debug.WriteLine("Issuing batch requests:\n  {0}", string.Join("\n  ", (IEnumerable<ODataClientRequest>) internalRequests));
#else
			if (s_trace.Switch.ShouldTrace(TraceEventType.Information))
			{
				s_trace.TraceInformation("Issuing batch requests:\n  {0}", string.Join("\n  ", (IEnumerable<ODataClientRequest>) internalRequests));
			}
#endif

			DataServiceRequest[] dataServiceRequests = internalRequests.Select(internalRequest => internalRequest.SendingRequest()).ToArray();
			Task<DataServiceResponse> taskResponse =
				Task.Factory.FromAsync<DataServiceRequest[], DataServiceResponse>((reqs, callback, state) => _dataServiceContext.BeginExecuteBatch(callback, state, reqs),
				                                                                  _dataServiceContext.EndExecuteBatch,
				                                                                  dataServiceRequests,
																				  internalRequests);
			return taskResponse.ContinueWith<ReadOnlyCollection<IRequest>>(ProcessBatchResponse);
		}

		/// <inheritdoc />
		public int ReportChanges(Action<EntityState, object> onChangedEntity, Action<EntityState, object, string, object> onChangedLink)
		{
			// This just updates the DataServiceContext with changes based on the current state.
			foreach (var editRepository in _editRepositories.Values)
			{
				editRepository.ReportChanges();
			}

			int countChanges = 0;
			foreach (EntityDescriptor entityDescriptor in DataServiceContext.Entities.Where(ed => ed.State != EntityStates.Unchanged))
			{
				if (onChangedEntity != null)
				{
					onChangedEntity(EntityStateFromDataServiceState(entityDescriptor.State), entityDescriptor.Entity);
				}
				countChanges++;
			}
			foreach (LinkDescriptor linkDescriptor in DataServiceContext.Links.Where(l => l.State != EntityStates.Unchanged))
			{
				if (onChangedLink != null)
				{
					onChangedLink(EntityStateFromDataServiceState(linkDescriptor.State), linkDescriptor.Source, linkDescriptor.SourceProperty, linkDescriptor.Target);
				}
				countChanges++;
			}
			return countChanges;
		}

		/// <inheritdoc />
		public Task SaveChanges()
		{
			foreach (var editRepository in _editRepositories.Values)
			{
				editRepository.ReportChanges();
			}

			int countChanges = ValidateChangedEntities();
			if (countChanges == 0)
			{
#if SILVERLIGHT
				Debug.WriteLine("No changes in SaveChanges().");
#else
				s_trace.TraceInformation("No changes in SaveChanges().");
#endif
				return new Task(() => {});
			}

			LogChanges("Saving changes:");
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
				foreach (var editRepository in _editRepositories.Values)
				{
					editRepository.ClearLocal();
				}
				foreach (var readOnlyRepository in _readOnlyRepositories.Values)
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

					// Store all the namespaces - needed for ResolveTypeFromName() to work
					foreach (IEdmEntityContainer entityContainer in _edmModel.EntityContainers())
					{
						_entityMetadataNamespaces.Add(entityContainer.Namespace);
					}

					// Store all the EntitySets
					foreach (IEdmEntityContainer entityContainer in _edmModel.EntityContainers())
					{
						foreach (IEdmEntitySet edmEntitySet in entityContainer.EntitySets())
						{
							var entitySetInfo = new EntitySetInfo(_edmModel, edmEntitySet, this);
							_entitySets.Add(entitySetInfo.Name, entitySetInfo);

							// Store the entity types contained in each entity set
							foreach (EntityTypeInfo typeInfo in entitySetInfo.EntityTypes)
							{
								_entityTypeInfos.Add(typeInfo.EntityType, typeInfo);
							}
						}
					}

					// Store any additional entity types not already added from the EntitySets
					HashSet<IEdmEntityType> storedEdmEntityTypes = new HashSet<IEdmEntityType>(_entityTypeInfos.Values.Select(value => value.EdmEntityType));
					foreach (IEdmEntityType edmEntityType in _edmModel.SchemaElements.OfType<IEdmEntityType>())
					{
						if (! storedEdmEntityTypes.Contains(edmEntityType))
						{
							var entityTypeInfo = new EntityTypeInfo(_edmModel, edmEntityType, this);
							_entityTypeInfos.Add(entityTypeInfo.EntityType, entityTypeInfo);
							storedEdmEntityTypes.Add(edmEntityType);
						}
					}

					// Connect any derived EntityTypeInfo with their base class EntityTypeInfo
					foreach (EntityTypeInfo typeInfo in _entityTypeInfos.Values.Where(eti => eti.BaseTypeInfo == null && eti.EdmEntityType.BaseEntityType() != null))
					{
						IEdmEntityType baseEdmEntityType = typeInfo.EdmEntityType.BaseEntityType();
						typeInfo.BaseTypeInfo = _entityTypeInfos.Values.FirstOrDefault(eti => eti.EdmEntityType == baseEdmEntityType);
					}

					_dataServiceContext.Format.LoadServiceModel = () => _edmModel;
					// REVIEW: In case of difficult-to-understand errors, it can be useful to comment out this .UseJson() line
					// Reason: The Atom code path seems to have more testing and better error messages.
					_dataServiceContext.Format.UseJson();
				}
			}
		}

		internal EntityTypeInfo GetEntityTypeInfoFor(Type entityType)
		{
			EntityTypeInfo entityTypeInfo;
			_entityTypeInfos.TryGetValue(entityType, out entityTypeInfo);
			return entityTypeInfo;
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
		public Type ResolveTypeFromName(string typeName)
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

		private int ValidateChangedEntities()
		{
			int countChanges = 0;
			List<ValidationException> validationExceptions = new List<ValidationException>();
			foreach (EntityDescriptor entityDescriptor in _dataServiceContext.Entities)
			{
				EntityStates state = entityDescriptor.State;
				bool isAdded = EntityStates.Added == (state & EntityStates.Added);
				bool isModified = EntityStates.Modified == (state & EntityStates.Modified);
				if (isAdded || isModified)
				{
					object entity = entityDescriptor.Entity;
					var validationContextValues = new Dictionary<object, object>(2);
					validationContextValues[EntityValidation.IsModifiedKey] = isModified;
					validationContextValues[EntityValidation.IsAddedKey] = isAdded;
					System.ComponentModel.DataAnnotations.ValidationContext validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(entity, null, validationContextValues);

					// Validate attributes, but don't require [Required] when the full object graph isn't loaded
					BaseRepository repository;
					if (RepositoriesByType.TryGetValue(entity.GetType(), out repository))
					{
						EntityTracker entityTracker = repository.GetEntityTracker(entity);
						if (entityTracker != null)
						{
							validationExceptions.AddRange(entityTracker.Validate(validationContext));
						}
					}

					// Allow the entity to validate itself, Validate attributes, but don't require [Required] when the full object graph isn't loaded
					IValidatable validatable = entity as IValidatable;
					if (validatable != null)
					{
						var validationResults = validatable.Validate(validationContext);
						foreach (ValidationResult result in validationResults)
						{
							validationExceptions.Add(new ValidationException(result, null, entity));
						}
					}
					countChanges++;
				}
				else if (EntityStates.Deleted == (state & EntityStates.Deleted))
				{
					countChanges++;
				}
			}

			if (validationExceptions.Count > 0)
			{
				throw new AggregateException("One or more added or modified entities failed validation.", validationExceptions);
			}

			countChanges += _dataServiceContext.Links.Count(linkDescriptor => linkDescriptor.State != EntityStates.Unchanged);
			return countChanges;
		}

		/// <summary>
		/// Logs all the changes in the <see cref="DataServiceContext"/>.
		/// </summary>
		/// <param name="message"></param>
		public void LogChanges(string message = null)
		{
#if ! SILVERLIGHT
			if (!s_trace.Switch.ShouldTrace(TraceEventType.Information))
			{
				return;
			}
#endif

			StringBuilder sb = new StringBuilder();
			if (message != null)
			{
				sb.AppendLine(message);
			}

			foreach (EntityDescriptor entityDescriptor in _dataServiceContext.Entities.Where(entityDescriptor => entityDescriptor.State != EntityStates.Unchanged))
			{
				sb.Append(entityDescriptor.State);
				sb.Append(": ");
				sb.AppendLine(entityDescriptor.Entity.ToString());
			}
			foreach (LinkDescriptor linkDescriptor in _dataServiceContext.Links.Where(linkDescriptor => linkDescriptor.State != EntityStates.Unchanged))
			{
				sb.Append(linkDescriptor.State);
				sb.Append(" Link : ");
				sb.Append(linkDescriptor.Source);
				sb.Append(".");
				sb.Append(linkDescriptor.SourceProperty);
				sb.Append(" -> ");
				sb.Append(linkDescriptor.Target);
				sb.AppendLine();
			}

#if SILVERLIGHT
			Debug.WriteLine(sb.ToString());
#else
			s_trace.TraceInformation(sb.ToString());
#endif
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
				Descriptor descriptor = changeResponse.Descriptor;
				if (changeResponse.Error != null)
				{
#if SILVERLIGHT
					Debug.WriteLine("Error saving changes to {0} - status code: {1}, exception: {2}", descriptor, changeResponse.StatusCode, changeResponse.Error);
#else
					s_trace.TraceEvent(TraceEventType.Error, -2, "Error saving changes to {0} - status code: {1}, exception: {2}", descriptor, changeResponse.StatusCode, changeResponse.Error);
#endif
				}
				else
				{
					EntityDescriptor entityDescriptor = descriptor as EntityDescriptor;
					if (entityDescriptor != null)
					{
						object entity = entityDescriptor.Entity;
#if SILVERLIGHT
						Debug.WriteLine("Completed SaveChanges for {0}", entity);
#else
						s_trace.TraceEvent(TraceEventType.Verbose, 0, "Completed SaveChanges for {0}", entity);
#endif
						EntityTracker entityTracker = _repositoriesByType[entity.GetType()].GetEntityTracker(entity);
						if (entityTracker != null)
						{
							entityTracker.CaptureUnmodifiedState();
						}
					}
					else
					{
						LinkDescriptor linkDescriptor = (LinkDescriptor) descriptor;
						object entity = linkDescriptor.Source;
						string linkCollectionPropertyName = linkDescriptor.SourceProperty;
#if SILVERLIGHT
						Debug.WriteLine("Completed SaveChanges for link {0} -> {1} -> {2}", entity, linkCollectionPropertyName, linkDescriptor.Target);
#else
						s_trace.TraceEvent(TraceEventType.Verbose, 0, "Completed SaveChanges for link {0} -> {1} -> {2}", entity, linkCollectionPropertyName, linkDescriptor.Target);
#endif
						LinkCollectionTracker linkTracker = _repositoriesByType[entity.GetType()].GetLinkCollectionTracker(entity, linkCollectionPropertyName);
						if (linkTracker != null)
						{
							linkTracker.CaptureUnmodifiedState();
						}
					}
				}
			}

			// Clear all changes from all edit repositories - b/c just clearing things where there was a response is not always sufficient.
			foreach (BaseRepository editRepository in _editRepositories.Values)
			{
				editRepository.ClearChanges();
			}
		}

		internal object AddEntityGraph(object entity, BaseRepository repository = null, object parent = null, string parentPropertyName = null)
		{
			if (DataServiceContext.GetEntityDescriptor(entity) != null)
			{
				// Entity is already tracked in the DataServiceContext, don't need to add it again.
				// This is also the recursive exit, in the case of circular references.
				return entity;
			}
			if (repository == null)
			{
				repository = _repositoriesByType[entity.GetType()];
				if (repository == null)
				{
					throw new ArgumentException("Couldn't find an EditRepository for entity type {0}.", entity.GetType().FullName);
				}
			}
			if (! repository.IsEditable)
			{
				// Can't add entities that are not editable.
				throw new InvalidOperationException(string.Format("Entity \"{0}\" is not attached; it must be attached before it can be referenced.", entity));
			}

			// Add the entity to the local cache, and to the DataServiceContext
			entity = repository.AddToLocal(entity, EntityState.Added);
			if (parent == null)
			{
				DataServiceContext.AddObject(repository.Name, entity);
			}
			else
			{
				DataServiceContext.AddRelatedObject(parent, parentPropertyName, entity);
			}

			// Recursively add connected entities
			EntityTypeInfo entityTypeInfo;
			if (!_entityTypeInfos.TryGetValue(entity.GetType(), out entityTypeInfo))
			{
				// entity type not in the definition; unexpected 
				throw new ArgumentException(string.Format("Entity type {0} not in service definition.", entity.GetType()));
			}

			// Add all referenced entity properties
			foreach (PropertyInfo property in entityTypeInfo.NavigationProperties)
			{
				object referencedEntity = property.GetValue(entity, null);
				if (referencedEntity != null)
				{
					// Recursively add the referencedEntity
					AddEntityGraph(referencedEntity, _repositoriesByType[referencedEntity.GetType()]);
				}
			}

			// Add links for all connected properties
			foreach (PropertyInfo property in entityTypeInfo.CollectionProperties)
			{
				IEnumerable collection = (IEnumerable) property.GetValue(entity, null);
				if (collection != null)
				{
					// Recursively add the collection elements
					foreach (object linkedEntity in collection)
					{
						AddEntityGraph(linkedEntity, _repositoriesByType[linkedEntity.GetType()], entity, property.Name);
					}
				}
			}

			return entity;
		}
		
		internal bool DeleteEntityFromGraph(object entity, BaseRepository repository)
		{
			bool deletedFromDataServiceContext = false;
			try
			{
				DataServiceContext.DeleteObject(entity);
				deletedFromDataServiceContext = true;
			}
			catch (InvalidOperationException)
			{ // Not in the context				
			}

			bool deletedFromLocal = repository.RemoveFromLocal(entity);

			foreach (LinkDescriptor link in DataServiceContext.Links.Where(l => Equals(l.Source, entity) || Equals(l.Target, entity)))
			{
				// Determine whether to call SetLink() or DeleteLink().
				EntityTypeInfo entityTypeInfo = repository.ODataClient.GetEntityTypeInfoFor(link.Source.GetType());
				PropertyInfo navigationProperty = entityTypeInfo.NavigationProperties.FirstOrDefault(p => p.Name.Equals(link.SourceProperty));
				if (navigationProperty != null)
				{ // link.SourceProperty is a navigation property
					if (ReferenceEquals(link.Target, entity))
					{
						// Navigation property points to entity being deleted; set it to null
						navigationProperty.SetValue(link.Source, null, null);
						DataServiceContext.SetLink(link.Source, link.SourceProperty, null);
					}
					else
					{	// Navigation property sourced from entity being deleted; stop tracking it
						DataServiceContext.DetachLink(link.Source, link.SourceProperty, link.Target);
					}
				}
				else
				{ // link.SourceProperty is a link collection (one to many) property
					// For references to entity in a link collection, delete the link
					if (ReferenceEquals(link.Target, entity))
					{
						EntityDescriptor sourceDescriptor = DataServiceContext.GetEntityDescriptor(link.Source);
						if ((sourceDescriptor == null)
							|| (sourceDescriptor.State == EntityStates.Deleted))
						{
							// Stop tracking the link
							DataServiceContext.DetachLink(link.Source, link.SourceProperty, link.Target);
						}
						else
						{
							// Delete the link to entity
							// REVIEW: Should entity actually be removed from the collection?
							DataServiceContext.DeleteLink(link.Source, link.SourceProperty, link.Target);
						}
					}
					else if (ReferenceEquals(null, link.Target))
					{ // The link is no longer needed
						DataServiceContext.DetachLink(link.Source, link.SourceProperty, link.Target);
					}
				}
			}

			return deletedFromDataServiceContext && deletedFromLocal;
		}

		private void OnWritingEntity(object sender, ReadingWritingEntityEventArgs e)
		{
			// Find the EntityTypeInfo for the entity type
			EntityTypeInfo entityTypeInfo;
			if (_entityTypeInfos.TryGetValue(e.Entity.GetType(), out entityTypeInfo))
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
							foreach (var propertyName in entityTypeInfo.DontSerializeProperties)
							{
								nodeProperties.Descendants(dNamespace.GetName(propertyName)).Remove();	
							}							
						}
					}
				}
			}
		}

		private void OnReadingEntity(object sender, ReadingWritingEntityEventArgs e)
		{
			object entity = e.Entity;
		}

		private EntityState EntityStateFromDataServiceState(EntityStates dataServiceState)
		{
			switch (dataServiceState)
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
					return EntityState.Uninitialized;
			}
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
