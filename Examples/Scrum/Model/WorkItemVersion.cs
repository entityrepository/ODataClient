// // -----------------------------------------------------------------------
// <copyright file="WorkItemVersion.cs" company="PrecisionDemand">
// Copyright (c) 2014 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------


using PD.Base.PortableUtil.Model;

namespace Scrum.Model
{

	/// <summary>
	/// An example entity with multiple keys - just to test the "more than one PK column" case.
	/// </summary>
	public sealed class WorkItemVersion
	{

		private EntityRef<WorkItem, int> _workItem = new EntityRef<WorkItem, int>(workItem => workItem.ID);

		public WorkItemVersion(WorkItem workItem, byte version)
		{
			WorkItem = workItem;
			Version = version;
		}

		/// <summary>
		/// Useful for deserialization.
		/// </summary>
		public WorkItemVersion()
		{
		}

		public WorkItem WorkItem
		{
			get
			{
				return _workItem.Entity;
			}
			set
			{
				_workItem.Entity = value;
			}
		}
		public int WorkItemId
		{
			get
			{
				return _workItem.ForeignKey;
			}
			set
			{
				_workItem.ForeignKey = value;
			}
		}

		public byte Version
		{
			get;
			set;
		}
	}

}