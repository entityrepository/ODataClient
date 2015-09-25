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

		private static readonly Task s_completedTask;

		static TaskExtensions()
		{
			// The shorter version is 
			//    s_completedTask = Task.FromResult(true);
			//	- but that doesn't exist in Silverlight.
			// TaskCompletionSource exists in Silverlight.
			var taskCompletionSource = new TaskCompletionSource<bool>();
			taskCompletionSource.SetResult(true);
			s_completedTask = taskCompletionSource.Task;
		}

		/// <summary>
		/// Returns a completed <see cref="Task"/>.
		/// </summary>
		public static Task CompletedTask
		{
			get { return s_completedTask; }
		}

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
