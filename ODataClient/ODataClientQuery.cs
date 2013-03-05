// -----------------------------------------------------------------------
// <copyright file="ODataClientQuery.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Services.Client;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using PD.Base.EntityRepository.Api;

namespace PD.Base.EntityRepository.ODataClient
{
	/// <summary>
	/// Provides shared functionality for <see cref="EditRepository{TEntity}"/> and <see cref="ReadOnlyRepository{TEntity}"/>. 
	/// </summary>
	/// <typeparam name="TEntity"></typeparam>
	/// <remarks>
	/// Note that the IQueryable and IQueryProvider implementations are primarily wrappers of the DataServiceQuery and DataServiceQueryProvider
	/// classes in the WCF Data Services Client assembly; as well as wrappers of the <see cref="EnumerableQuery{T}"/> class.  This class builds DataServiceQuery
	/// objects before the request is issued; and after results are returned further query building is built upon the <c>IEnumerable</c> as <see cref="IQueryable{T}"/>.
	/// 
	/// Also, in this case the IQueryable class also implements IQueryProvider, b/c it is
	/// simplest to do so.  If the IQueryProvider weren't the same class, there would be a 1-1 relationship between queryprovider and queryable, and
	/// they'd continually be passing state back and forth.
	/// </remarks>
	internal class ODataClientQuery<TEntity> : ODataClientRequest, IQueryRequest<TEntity>, IQueryProvider
	{
		/// <summary>
		/// The WCF Data Services Client query associated with this query.  Never <c>null</c>.
		/// </summary>
		private readonly DataServiceQuery<TEntity> _dataServiceQuery;
		/// <summary>
		/// The results returned for this query.  May be <c>null</c>.
		/// </summary>
		private TEntity[] _results;

		/// <summary>
		/// Initializes an <see cref="ODataClientQuery{TEntity}"/> to return all entities in the repository.
		/// </summary>
		/// <param name="dataServiceContext"></param>
		/// <param name="repository">The <see cref="BaseRepository{TEntity}"/> to create a default query for.</param>
		internal ODataClientQuery(DataServiceContext dataServiceContext, BaseRepository<TEntity> repository)
		{
			Contract.Requires<ArgumentNullException>(dataServiceContext != null);
			Contract.Requires<ArgumentNullException>(repository != null);

			_dataServiceQuery = dataServiceContext.CreateQuery<TEntity>(repository.Name);
		}

		/// <summary>
		/// Initializes an <see cref="ODataClientQuery{TEntity}"/> based on a new <see cref="DataServiceQuery{TElement}"/> that has not yet been sent.
		/// </summary>
		/// <param name="dataServiceQuery">The <see cref="DataServiceQuery{TElement}"/> wrapped by this <see cref="ODataClientQuery{TEntity}"/>.</param>
		internal ODataClientQuery(DataServiceQuery<TEntity> dataServiceQuery)
		{
			Contract.Requires<ArgumentNullException>(dataServiceQuery != null);

			_dataServiceQuery = dataServiceQuery;
		}

		internal ODataClientQuery<TEntity> Clone()
		{
			return new ODataClientQuery<TEntity>(_dataServiceQuery);
		}

		#region IQueryable<TEntity>

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public IEnumerator<TEntity> GetEnumerator()
		{
			RequireQuerySuccessfullyCompleted();
			return ((IEnumerable<TEntity>) _results).GetEnumerator();
		}

		public Expression Expression
		{
			get
			{
				if (IsCompletedSuccessfully)
				{
					return _results.AsQueryable().Expression;
				}
				else if (IsFaulted)
				{
					throw new InvalidOperationException("Exception occured during request", Exception);
				}
				else
				{
					return GetDataServiceQuery().Expression;
				}
			}
		}

		public Type ElementType
		{
			get { return typeof(TEntity); }
		}

		public IQueryProvider Provider
		{
			get { return this; }
		}

		#endregion

		#region IQueryProvider

		public IQueryable CreateQuery(Expression expression)
		{
			if (expression == null)
			{
				throw new ArgumentNullException("expression");
			}

			// Delegate to the generic CreateQuery by reflection.
			// Use the expression type to determine the correct IQueryable type parameter.
			Type expressionType = expression.GetType();
			MethodInfo genericCreateQueryMethod = GetType().GetMethod("CreateQuery",
			                                                          BindingFlags.Instance | BindingFlags.Public | BindingFlags.InvokeMethod,
			                                                          new GenericMethodBinder(expressionType),
			                                                          new[] { typeof(Expression) },
			                                                          null);
			return (IQueryable) genericCreateQueryMethod.Invoke(this, new object[] { expression });
		}

		public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
		{
			if (expression == null)
			{
				throw new ArgumentNullException("expression");
			}

			if (! IsCompleted)
			{
				// Use the WCF Data Services query
				DataServiceQuery<TElement> createdDataServiceQuery = (DataServiceQuery<TElement>) GetDataServiceQuery().Provider.CreateQuery<TEntity>(expression);
				return new ODataClientQuery<TElement>(createdDataServiceQuery);
			}
			else
			{
				// After completion, use the IEnumerable query
				return new EnumerableQuery<TElement>(expression);
			}
		}

		public object Execute(Expression expression)
		{
			if (expression == null)
			{
				throw new ArgumentNullException("expression");
			}

			RequireQuerySuccessfullyCompleted();
			return _results.AsQueryable().Provider.Execute(expression);
		}

		public TResult Execute<TResult>(Expression expression)
		{
			if (expression == null)
			{
				throw new ArgumentNullException("expression");
			}

			RequireQuerySuccessfullyCompleted();
			return _results.AsQueryable().Provider.Execute<TResult>(expression);
		}

		#endregion

		#region ODataClientRequest overrides

		internal override DataServiceRequest SendingRequest()
		{
			base.SendingRequest();

			return GetDataServiceQuery();
		}

		internal override bool IsRequestFor(OperationResponse operationResponse)
		{
			QueryOperationResponse queryResponse = operationResponse as QueryOperationResponse;
			return (queryResponse != null)
			       && Equals(queryResponse.Query, _dataServiceQuery);
		}

		internal override void HandleResponse(ODataClient client, OperationResponse operationResponse)
		{
			IEnumerable<TEntity> results = operationResponse as IEnumerable<TEntity>;
			if (results == null)
			{
				throw new InvalidOperationException("Expected results from " + operationResponse + " to be IEnumerable<" + typeof(TEntity) + ">.");
			}
			IEnumerable<TEntity> processedResults = client.ProcessQueryResults(results);
			_results = processedResults.ToArray();
			base.HandleResponse(client, operationResponse);
		}

		#endregion

		internal DataServiceQuery<TEntity> GetDataServiceQuery()
		{
			return _dataServiceQuery;
		}

		private void RequireQuerySuccessfullyCompleted()
		{
			if (IsFaulted)
			{
				throw new InvalidOperationException("OData query previously faulted.", Exception);
			}
			if (_results == null)
			{
				throw new InvalidOperationException("OData query has not successfully completed.");
			}
		}

		/// <summary>
		/// Supports binding to a generic method.  Type.GetMethod() doesn't support finding appropriate generic methods out of the box,
		/// so this is required.
		/// </summary>
		// TODO: Test me.
		private class GenericMethodBinder : Binder
		{

			private readonly Type[] _genericMethodTypeParameters;

			public GenericMethodBinder(params Type[] genericMethodTypeParameters)
			{
				_genericMethodTypeParameters = genericMethodTypeParameters;
			}

			public override MethodBase BindToMethod(BindingFlags bindingAttr, MethodBase[] match, ref object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] names, out object state)
			{
				foreach (MethodBase evalMethod in match)
				{
					if (evalMethod.IsGenericMethodDefinition)
					{
						Type[] genericArguments = evalMethod.GetGenericArguments();
						if (genericArguments.Length == _genericMethodTypeParameters.Length)
						{
							// This is always safe, since MethodInfo and ConstructorInfo are the only subclasses of MethodBase,
							// and constructors don't support generic method parameters.
							MethodInfo methodInfo = (MethodInfo) evalMethod;
							MethodInfo genericMethod = methodInfo.MakeGenericMethod(_genericMethodTypeParameters);
							state = null; // Required by signature.
							return genericMethod;
						}
					}
				}

				throw new MissingMethodException("Method not found that can be bound to the specified type parameters.");
			}

			public override FieldInfo BindToField(BindingFlags bindingAttr, FieldInfo[] match, object value, CultureInfo culture)
			{
				throw new NotImplementedException();
			}

			public override MethodBase SelectMethod(BindingFlags bindingAttr, MethodBase[] match, Type[] types, ParameterModifier[] modifiers)
			{
				throw new NotImplementedException();
			}

			public override PropertyInfo SelectProperty(BindingFlags bindingAttr, PropertyInfo[] match, Type returnType, Type[] indexes, ParameterModifier[] modifiers)
			{
				throw new NotImplementedException();
			}

			public override object ChangeType(object value, Type type, CultureInfo culture)
			{
				throw new NotImplementedException();
			}

			public override void ReorderArgumentArray(ref object[] args, object state)
			{
				throw new NotImplementedException();
			}

		}

	}
}
