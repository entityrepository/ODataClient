// -----------------------------------------------------------------------
// <copyright file="SimpleClient.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------


using System;
using PD.Base.EntityRepository.Api;
using PD.Base.EntityRepository.ODataClient;
using Simple.Model;

namespace Simple.IntegrationTests
{
	/// <summary>
	/// An odata client for <c>Simple.Web/odata.svc</c>.
	/// </summary>
	public class SimpleClient : DataContext
	{

		public const string TestServiceUrl = "http://localhost:37623/odata.svc/";

		public SimpleClient(string serviceUrl)
			: base(new ODataClient(new Uri(serviceUrl), typeof(EqualityTestRecord)), DataContextExtensions.SynchronousPreLoadDbEnums)
		{}

		public IEditRepository<EqualityTestRecord> EqualityTestRecords { get; private set; }

		public IReadOnlyRepository<EqualitySemantics> EqualitySemantics { get; private set; }

	}
}