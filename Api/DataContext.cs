// -----------------------------------------------------------------------
// <copyright file="DataContext.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using PD.Base.EntityRepository.Api.Exceptions;

namespace PD.Base.EntityRepository.Api
{
	/// <summary>
	/// Base class for implementation-independent datacontext definition.  Subclasses of <c>DataContext</c> can be initialized with any
	/// <see cref="IDataContextImpl"/> implementation.  And, they can define repository properties for each repository to be exposed by the
	/// context object.
	/// </summary>
	public abstract class DataContext : IDisposable
	{

		private readonly IDataContextImpl _dataContextImpl;
		private readonly Action<DataContext> _initializeAction;
		private Task _initializeTask;

		/// <summary>
		/// Initializes a new <see cref="DataContext"/>.
		/// </summary>
		/// <param name="implementationDataContext"></param>
		/// <param name="initializeAction">An optional callback which can initialize the <c>DataContext</c>, both when it is
		/// initially created, and when <see cref="Clear"/> is called.</param>
		protected DataContext(IDataContextImpl implementationDataContext, Action<DataContext> initializeAction = null)
		{
			Contract.Requires<ArgumentNullException>(implementationDataContext != null);

			_dataContextImpl = implementationDataContext;
			_initializeAction = initializeAction;
			Task initTask = _dataContextImpl.InitializeTask.ContinueWith(task => FinishInitialization());
			_initializeTask = initTask;
		}

		/// <summary>
		/// Returns a task that signals when initialization of the <see cref="DataContext"/> is complete.
		/// </summary>
		public Task InitializeTask
		{
			get { return _initializeTask; }
		}

		/// <summary>
		/// Returns all <see cref="IRepository"/> objects that exist within this <see cref="DataContext"/>.
		/// </summary>
		public IEnumerable<IRepository> Repositories
		{
			get { return _dataContextImpl.Repositories; }
		}

		internal IDataContextImpl DataContextImpl
		{
			get { return _dataContextImpl; }
		}

		#region IDisposable Members

		/// <summary>
		/// Release any <see cref="IDisposable"/> resources.
		/// </summary>
		public virtual void Dispose()
		{
			IDisposable dataContextImplDisposable = _dataContextImpl as IDisposable;
			if (dataContextImplDisposable != null)
			{
				dataContextImplDisposable.Dispose();
			}
		}

		#endregion

		/// <summary>
		/// Completes <c>DataContext</c> initialization after the <see cref="_dataContextImpl"/> class finished its initialization.
		/// </summary>
		private void FinishInitialization()
		{
			if (_dataContextImpl.InitializeTask.IsFaulted)
			{
				throw new InitializationException("Could not initialize repository properties because " + _dataContextImpl.ToString() + " initialization failed.", _dataContextImpl.InitializeTask.GetException());
			}

			InitializeRepositoryProperties();

			// Run the initialization action, if one was specified.
			if (_initializeAction != null)
			{
				_initializeAction(this);
			}
		}

		/// <summary>
		/// Initializes all the  <see cref="IEditRepository{TEntity}"/> and <see cref="IReadOnlyRepository{TEntity}"/> properties in the concrete subclass.
		/// The default implementation uses reflection to set the properties.  This method can be overridden, and the properties can be set using the
		/// <see cref="EditRepositoryFor{TEntity}"/> and <see cref="ReadOnlyRepositoryFor{TEntity}"/> methods.
		/// </summary>
		protected virtual void InitializeRepositoryProperties()
		{
			PropertyInfo[] properties = GetType().GetProperties(BindingFlags.Instance | BindingFlags.SetProperty | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (var propertyInfo in properties)
			{
				if (propertyInfo.PropertyType.IsGenericType)
				{
					Type propertyTypeDef = propertyInfo.PropertyType.GetGenericTypeDefinition();
					if ((propertyTypeDef == typeof(IEditRepository<>)) || (propertyTypeDef == typeof(IReadOnlyRepository<>)))
					{
						Type entityType = propertyInfo.PropertyType.GetGenericArguments()[0];
						string entitySetName = propertyInfo.Name;

						MethodInfo repositoryMethod;
						if (propertyTypeDef == typeof(IEditRepository<>))
						{
							repositoryMethod = typeof(IDataContextImpl).GetMethod("Edit");
						}
						else
						{
							repositoryMethod = typeof(IDataContextImpl).GetMethod("ReadOnly");
						}
						// repository = _dataContextImpl.(ReadOnly | EditRepositoryFor)<{entityType}>(entitySetName)
						object repository = repositoryMethod.MakeGenericMethod(entityType).Invoke(_dataContextImpl, new object[] { entitySetName });
						propertyInfo.SetValue(this, repository, null);
					}
				}
			}

		}

		/// <summary>
		/// Returns the <see cref="IEditRepository{TEntity}"/> value for the specified property in the derived class.  This method
		/// can be used in overrides of <see cref="InitializeRepositoryProperties"/> in <c>DataContext</c> subclasses to initialize the values of
		/// <see cref="IEditRepository{TEntity}"/>-typed properties.  This is useful in Silverlight apps when you want to keep the property setters
		/// private, since Silverlight doesn't support using reflection to set non-public properties.
		/// </summary>
		/// <typeparam name="TEntity">The entity type for the <see cref="IEditRepository{TEntity}"/></typeparam>
		/// <param name="propertySelector">Lambda function that selects the <see cref="IEditRepository{TEntity}"/> property.  The name of the property
		/// is assumed to match the name of the entity set.</param>
		/// <returns>The <see cref="IEditRepository{TEntity}"/> to store in the property indicated by <paramref name="propertySelector"/>.</returns>
		protected IEditRepository<TEntity> EditRepositoryFor<TEntity>(Expression<Func<IEditRepository<TEntity>>> propertySelector)
			where TEntity : class
		{
			MemberExpression memberExpression = propertySelector.Body as MemberExpression;
			if (memberExpression == null)
			{
				throw new ArgumentException(propertySelector + " must evaluate to a MemberExpression.");
			}
			PropertyInfo propertyInfo = memberExpression.Member as PropertyInfo;
			if (propertyInfo == null)
			{
				throw new ArgumentException(propertySelector + " must select a Property (not a field).");
			}
			if (propertyInfo.DeclaringType != GetType())
			{
				throw new ArgumentException(propertySelector + " must select a Property of the current DataContext subclass");
			}
			if (propertyInfo.PropertyType != typeof(IEditRepository<TEntity>))
			{
				throw new ArgumentException(propertySelector + " must select a Property of type IEditRepository<TEntity>");
			}

			return DataContextImpl.Edit<TEntity>(propertyInfo.Name);
		}

		/// <summary>
		/// Returns the <see cref="IReadOnlyRepository{TEntity}"/> value for the specified property in the derived class.  This method
		/// can be used in overrides of <see cref="InitializeRepositoryProperties"/> in <c>DataContext</c> subclasses to initialize the values of
		/// <see cref="IReadOnlyRepository{TEntity}"/>-typed properties.  This is useful in Silverlight apps when you want to keep the property setters
		/// private, since Silverlight doesn't support using reflection to set non-public properties.
		/// </summary>
		/// <typeparam name="TEntity">The entity type for the <see cref="IReadOnlyRepository{TEntity}"/></typeparam>
		/// <param name="propertySelector">Lambda function that selects the <see cref="IReadOnlyRepository{TEntity}"/> property.  The name of the property
		/// is assumed to match the name of the entity set.</param>
		/// <returns>The <see cref="IReadOnlyRepository{TEntity}"/> to store in the property indicated by <paramref name="propertySelector"/>.</returns>
		protected IReadOnlyRepository<TEntity> ReadOnlyRepositoryFor<TEntity>(Expression<Func<IReadOnlyRepository<TEntity>>> propertySelector)
			where TEntity : class
		{
			MemberExpression memberExpression = propertySelector.Body as MemberExpression;
			if (memberExpression == null)
			{
				throw new ArgumentException(propertySelector + " must evaluate to a MemberExpression.");
			}
			PropertyInfo propertyInfo = memberExpression.Member as PropertyInfo;
			if (propertyInfo == null)
			{
				throw new ArgumentException(propertySelector + " must select a Property (not a field).");
			}
			if (propertyInfo.DeclaringType != GetType())
			{
				throw new ArgumentException(propertySelector + " must select a Property of the current DataContext subclass");
			}
			if (propertyInfo.PropertyType != typeof(IReadOnlyRepository<TEntity>))
			{
				throw new ArgumentException(propertySelector + " must select a Property of type IReadOnlyRepository<TEntity>");
			}

			return DataContextImpl.ReadOnly<TEntity>(propertyInfo.Name);
		}

		/// <summary>
		/// Ensures that initialization has completed.
		/// </summary>
		public void EnsureInitializationCompleted()
		{
			if (! InitializeTask.IsCompleted)
			{
				throw new InitializationException("Initialization has not completed.");
			}

			if (InitializeTask.Status != TaskStatus.RanToCompletion)
			{
				throw new InitializationException(_dataContextImpl.GetType().FullName + " initialization did not complete successfully.", InitializeTask.GetException());
			}
		}

		/// <summary>
		/// Provides asynchronous execution of one or more requests from the remote repository.  When the task is successfully completed, the request objects
		/// will include results or errors.
		/// </summary>
		/// <param name="requests">The set of requests to execute.</param>
		/// <returns>A TPL <see cref="Task"/> object containing the <see cref="IRequest"/> objects that were passed in.  It provides completion status
		/// and error information for the requests that were passed in.</returns>
		/// <remarks>
		/// Each <see cref="IRequest"/> contains its own results, completion status, and exception tracking.
		/// </remarks>
		public Task<ReadOnlyCollection<IRequest>> InvokeAsync(params IRequest[] requests)
		{
			if (requests.Length < 1)
			{
				throw new ArgumentException("At least one IRequest must be passed in", "requests");
			}

			EnsureInitializationCompleted();
			return _dataContextImpl.InvokeAsync(requests);
		}

		/// <summary>
		/// Provides asynchronous execution of one or more requests from the remote repository.  When the task is successfully completed, the request objects
		/// will include results or errors.
		/// </summary>
		/// <param name="requests">The set of requests to execute.  Each object must be castable to <see cref="IRequest"/>.</param>
		/// <returns>A TPL <see cref="Task"/> object containing the <see cref="IRequest"/> objects that the <paramref name="requests"/> 
		/// were converted into.  It provides completion status and error information for the requests that were passed in.</returns>
		public Task<ReadOnlyCollection<IRequest>> InvokeAsync(params object[] requests)
		{
			IRequest[] requestArray = new IRequest[requests.Length];
			for (int i = 0; i < requests.Length; ++i)
			{
				IRequest request = requests[i] as IRequest;
				if (request == null)
				{
					throw new ArgumentException(string.Format("Argument #{0} is type {1}, and cannot be cast to {2}.", i, requests[i].GetType().FullName, typeof(IRequest).FullName),
						"Argument #" + i);
				}
				requestArray[i] = request;
			}

			return InvokeAsync(requestArray);
		}

		/// <summary>
		/// Reports all changes to the caller.  Each of the optional delegates are called once for each change.
		/// </summary>
		/// <param name="onChangedEntity">Called for each changed entity.</param>
		/// <param name="onChangedLink">Called for each changed link.</param>
		/// <returns>The total number of changes.</returns>
		public int ReportChanges(Action<EntityState, object> onChangedEntity, Action<EntityState, object, string, object> onChangedLink)
		{
			EnsureInitializationCompleted();
			return _dataContextImpl.ReportChanges(onChangedEntity, onChangedLink);
		}

		/// <summary>
		/// Provides an asynchronous batch save of all modified entities in all <see cref="IEditRepository{TEntity}"/>s in this <see cref="IDataContextImpl"/>.
		/// </summary>
		/// <returns>A TPL <see cref="Task"/> that manages execution and completion of the batch save operation.</returns>
		/// <remarks>
		/// Upon completion, all previously modified entities will be updated to their current state, and <see cref="IEditRepository{TEntity}.GetEntityState"/> for each
		/// object will return <see cref="EntityState.Unmodified"/>.
		/// 
		/// If an entity that is about to be saved implements <c>IValidatable</c>, <c>IValidatable.Validate</c> will be called before the entity is saved.
		/// </remarks>
		public Task SaveChanges()
		{
			EnsureInitializationCompleted();
			return _dataContextImpl.SaveChanges();
		}

		/// <summary>
		/// Changes all modified entities in all <see cref="IEditRepository{TEntity}"/>s in this <see cref="IDataContextImpl"/> back to the state they were in
		/// when last returned from the remote repository.  In other words, all changes are erased.
		/// </summary>
		public void RevertChanges()
		{
			EnsureInitializationCompleted();
			_dataContextImpl.RevertChanges();
		}

		/// <summary>
		/// Clears the local cache for all entity types, so that the <see cref="IDataContextImpl"/> can start new. Then
		/// any initialize actions passed to the constructor are run asynchronously.
		/// </summary>
		public Task Clear()
		{
			EnsureInitializationCompleted();
			_dataContextImpl.Clear();

			if (_initializeAction != null)
			{
				_initializeTask = Task.Factory.StartNew(() => _initializeAction(this));
			}
			return _initializeTask;
		}

		/// <summary>
		/// Provides a mechanism for loading all entities of specific entity types on startup, when this method is used
		/// in the <c>initializeAction</c> argument to <see cref="DataContext"/>.
		/// 
		/// This method iterates over all established repositories in <paramref name="dataContext"/>, and provides
		/// the option to load all entities in each repository.
		/// </summary>
		/// <param name="dataContext">The <see cref="DataContext"/>.</param>
		/// <param name="typeSelector">A predicate that returns <c>true</c> for the repositories that should be pre-loaded.</param>
		/// <param name="perResultInitializer">An optional per-result initializer that is called for each loaded entity set.</param>
		/// <returns>A task that can be used to track completion of pre-loading.</returns>
		public static Task PreLoad(DataContext dataContext, Predicate<Type> typeSelector, Action<IEnumerable> perResultInitializer = null)
		{
			// Obtain repository.All queries for each type that matches typeSelector
			List<IRequest> queries = new List<IRequest>();
			foreach (IRepository repository in dataContext.Repositories)
			{
				if (typeSelector(repository.ElementType))
				{
					// query = repository.All; // using reflection b/c .All is on a generic interface
					IRequest query = (IRequest) typeof(IRepository<>).MakeGenericType(repository.ElementType).GetProperty("All").GetValue(repository, null);
					queries.Add(query);
				}
			}

			if (! queries.Any())
			{
				// Log("No types preloaded");
				return new Task(() => { });
			}

			// Synchronous call for all entities that match the queries
			return dataContext.DataContextImpl.InvokeAsync(queries.ToArray()).ContinueWith(
				completion =>
				{
					if (completion.IsFaulted)
					{
						// Log(completion.GetException(), "Error occurred while PreLoading...");
					}
					// If action is provided, allow post query intialization
					else if (perResultInitializer != null)
					{
						foreach (object query in queries)
						{
							perResultInitializer((IEnumerable) query);
						}
					}
				});
		}		 

	}
}
