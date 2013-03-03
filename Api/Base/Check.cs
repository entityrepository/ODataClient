// -----------------------------------------------------------------------
// <copyright file="Check.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Diagnostics.Contracts;

namespace PD.Base.EntityRepository.Api.Base
{
	/// <summary>
	/// Parameter validation functions.
	/// </summary>
	public static class Check
	{

		/// <summary>
		/// Returns <c>true</c> if <paramref name="s"/> is non-<c>null</c>, non-empty, and contains non-whitespace characters.
		/// </summary>
		/// <param name="s">A string</param>
		/// <returns><c>true</c> if <paramref name="s"/> is non-<c>null</c>, non-empty, and contains non-whitespace characters.
		/// <c>false</c> if <paramref name="s"/> is <c>null</c>, empty, or consists only of whitespace characters.</returns>
		[Pure]
		public static bool NotNullOrWhiteSpace(string s)
		{
			if (s == null)
			{
				return false;
			}

			for (int i = 0; i < s.Length; i++)
			{
				if (! Char.IsWhiteSpace(s[i]))
				{
					return true;
				}
			}

			// All are whitespace
			return false;
		}

	}
}
