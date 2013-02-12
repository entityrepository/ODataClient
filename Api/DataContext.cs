// -----------------------------------------------------------------------
// <copyright file="DataContext.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace PD.Base.EntityRepository.Api
{
	/// <summary>
	/// Base class for implementation-independent datacontext definition.  Subclasses of <c>DataContext</c> can be initialized with any
	/// <see cref="IDataContext"/> implementation.  And, they can define repository properties for each repository to be exposed by the
	/// context object.
	/// </summary>
	public abstract class DataContext : IDisposable
	{
		// TODO: Make private
		protected readonly IDataContext _dataContextImpl;
		private readonly Action<DataContext> _initializeAction;
		private Task _initializeTask;

		/// <summary>
		/// Initializes a new <see cref="DataContext"/>.
		/// </summary>
		/// <param name="implementationDataContext"></param>
		/// <param name="initializeAction">An optional callback which can initialize the <c>DataContext</c>, both when it is
		/// initially created, and when <see cref="Clear"/> is called.</param>
		protected DataContext(IDataContext implementationDataContext, Action<DataContext> initializeAction = null)
		{
			Contract.Requires<ArgumentNullException>(implementationDataContext != null);

			_dataContextImpl = implementationDataContext;
			_initializeAction = initializeAction;
			Task initTask = _dataContextImpl.InitializeTask.ContinueWith(task => InitializeRepositoryProperties());
			if (_initializeAction != null)
			{
				initTask = initTask.ContinueWith(task => _initializeAction(this));
			}
			_initializeTask = initTask;
		}

		/// <summary>
		/// Returns a task that signals when initialization of the <see cref="DataContext"/> is complete.
		/// </summary>
		public Task InitializeTask
		{
			get { return _initializeTask; }
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
		/// Sets all <see cref="IEditRepository{TEntity}"/> and <see cref="IReadOnlyRepository{TEntity}"/> properties in the concrete class.
		/// </summary>
		private void InitializeRepositoryProperties()
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
							repositoryMethod = typeof(IDataContext).GetMethod("Edit");
						}
						else
						{
							repositoryMethod = typeof(IDataContext).GetMethod("ReadOnly");
						}
						// repository = _dataContextImpl.(ReadOnly | Edit)<{entityType}>(entitySetName)
						object repository = repositoryMethod.MakeGenericMethod(entityType).Invoke(_dataContextImpl, new object[] { entitySetName });
						propertyInfo.SetValue(this, repository, null);
					}
				}
			}
		}

		/// <summary>
		/// Ensures that initialization has completed.
		/// </summary>
		public void EnsureInitializationCompleted()
		{
			if (! InitializeTask.IsCompleted)
			{
				// REVIEW: Make the timeout a ctor parameter?
				InitializeTask.Wait(); //60000);
			}

			if (InitializeTask.Status != TaskStatus.RanToCompletion)
			{
				throw new InvalidOperationException(_dataContextImpl.GetType().FullName + " initialization did not complete successfully.");
			}
		}

		/// <summary>
		/// Provides asynchronous execution of one or more queries from the remote repository.  When the task is successfully completed, all <see cref="IQueryable"/> parameters
		/// will contain results that can be enumerated.
		/// </summary>
		/// <param name="queries">The set of queries to execute.</param>
		/// <returns>A TPL <see cref="Task"/> that manages execution and completion of the specified queries.</returns>
		public Task QueryAsync(params IQueryable[] queries)
		{
			EnsureInitializationCompleted();
			return _dataContextImpl.QueryAsync(queries);
		}

		/// <summary>
		/// Provides an asynchronous batch save of all modified entities in all <see cref="IEditRepository{TEntity}"/>s in this <see cref="IDataContext"/>.
		/// </summary>
		/// <returns>A TPL <see cref="Task"/> that manages execution and completion of the batch save operation.</returns>
		/// <remarks>
		/// Upon completion, all previously modified entities will be updated to their current state, and <see cref="IEditRepository{TEntity}.GetEntityState"/> for each
		/// object will return <see cref="EntityState.Unmodified"/>.
		/// 
		/// If an entity that is about to be saved implements <see cref="IValidatable"/>, <see cref="IValidatable.Validate"/> will be called before the entity is saved.
		/// </remarks>
		public Task SaveChanges()
		{
			EnsureInitializationCompleted();
			return _dataContextImpl.SaveChanges();
		}

		/// <summary>
		/// Changes all modified entities in all <see cref="IEditRepository{TEntity}"/>s in this <see cref="IDataContext"/> back to the state they were in
		/// when last returned from the remote repository.  In other words, all changes are erased.
		/// </summary>
		public void RevertChanges()
		{
			EnsureInitializationCompleted();
			_dataContextImpl.RevertChanges();
		}

		/// <summary>
		/// Clears the local cache for all entity types, so that the <see cref="IDataContext"/> can start new. 
		/// </summary>
		public void Clear()
		{
			EnsureInitializationCompleted();
			_dataContextImpl.Clear();

			if (_initializeAction != null)
			{
				_initializeTask = Task.Factory.StartNew(() => _initializeAction(this));
			}
		}
	}
}
