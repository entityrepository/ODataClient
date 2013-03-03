// -----------------------------------------------------------------------
// <copyright file="TaskExtensions.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------


using System;
using System.Threading.Tasks;

namespace PD.Base.EntityRepository.Api
{
	/// <summary>
	/// Static class providing extension methods to Task Processing Library classes.
	/// </summary>
	public static class TaskExtensions
	{

		 /// <summary>
		 /// Returns a non-<see cref="AggregateException"/> in cases where the <c>AggregateException</c> wraps 
		 /// a single inner exception.
		 /// </summary>
		 /// <param name="task">The task containing the exception to return.</param>
		/// <returns>A non-<see cref="AggregateException"/> in cases where the <c>AggregateException</c> wraps 
		/// a single inner exception.  Otherwise <see cref="Task.Exception"/> is returned.</returns>
		 public static Exception GetException(this Task task)
		 {
			 AggregateException aggEx = task.Exception;
			 if (aggEx == null)
			 {
				 return null;
			 }
			 else if (aggEx.InnerExceptions.Count == 1)
			 {
				 return aggEx.InnerExceptions[0];
			 }
			 else
			 {
				 return aggEx.Flatten();
			 }
		 }

	}
}