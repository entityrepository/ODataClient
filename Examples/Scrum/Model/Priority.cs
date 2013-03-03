﻿// -----------------------------------------------------------------------
// <copyright file="Priority.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace Scrum.Model
{


	public class Priority
	{

		/// <summary>
		/// Function to obtain the key for a <see cref="Status"/>.
		/// </summary>
		public static readonly Func<Priority, short> KeyFunc = priority => priority.ID;

		// Needed for WCF Data Services; also requires public setters. :(

		public static readonly Priority Unknown = new Priority(0, "Unknown");
		public static readonly Priority Optional = new Priority(1, "Optional");
		public static readonly Priority Low = new Priority(2, "Low");
		public static readonly Priority Normal = new Priority(3, "Normal");
		public static readonly Priority High = new Priority(4, "High");
		public static readonly Priority Critical = new Priority(5, "Critical");
		public static readonly Priority Blocking = new Priority(6, "Blocking");

		public Priority()
		{}

		protected Priority(short id, string name)
		{
			ID = id;
			Name = name;
		}

		public short ID { get; set; }
		public string Name { get; set; }

	}
}
