// -----------------------------------------------------------------------
// <copyright file="SilverlightUtil.cs" company="PrecisionDemand">
// Copyright (c) 2013 PrecisionDemand.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;

#if SILVERLIGHT

namespace PD.Base.EntityRepository.ODataClient
{
	/// <summary>
	/// Silverlight-only utility methods.
	/// </summary>
	internal static class SilverlightUtil
	{

		/// <summary>
		/// If <paramref name="uri"/> is a relative <see cref="Uri"/>, make it absolute
		/// by building it relative to the web app root.
		/// </summary>
		/// <param name="uri">A <see cref="Uri"/>, which may be absolute or relative.  If it's
		/// an absolute <c>Uri</c>, it is not changed.</param>
		public static void ConvertAppRelativeUriToAbsoluteUri(ref Uri uri)
		{
			if (uri.IsAbsoluteUri)
			{
				return;
			}

			uri = new Uri(ComputeWebAppUri(), uri);
		}

		/// <summary>
		/// Returns a best-guess <see cref="Uri"/> for the web application that the current silverlight code is running in.
		/// </summary>
		/// <returns></returns>
		public static Uri ComputeWebAppUri()
		{
			// For silverlight applications, build a full Uri based on the silverlight app URL
			Uri xapUri = System.Windows.Application.Current.Host.Source;
			string webAppPath = xapUri.LocalPath;
			int ichClientBin = webAppPath.IndexOf("/ClientBin/", System.StringComparison.Ordinal);
			if (ichClientBin >= 0)
			{
				// Strip the ClientBin/...
				webAppPath = webAppPath.Substring(0, ichClientBin + 1);
			}
			else
			{
				// Strip everything after the last slash...
				int ichSlash = webAppPath.LastIndexOf('/');
				if (ichSlash >= 0)
				{
					webAppPath = webAppPath.Substring(0, ichSlash + 1);
				}
			}

			Uri webAppUri = new Uri(xapUri, webAppPath);
			return webAppUri;
		}

	}
}

#endif
