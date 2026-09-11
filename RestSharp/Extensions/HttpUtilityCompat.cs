#region License
//   Copyright 2010 John Sheehan
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License. 
#endregion

using System.Collections.Specialized;
using System.Net;
using System.Text;

namespace RestSharp.Extensions
{
	/// <summary>
	/// Minimal replacements for System.Web.HttpUtility members that have no
	/// System.Net.WebUtility equivalent.
	/// </summary>
	internal static class HttpUtilityCompat
	{
		public static string HtmlAttributeEncode(string s)
		{
			if (string.IsNullOrEmpty(s))
			{
				return s;
			}

			var sb = new StringBuilder(s.Length + 16);
			foreach (var c in s)
			{
				switch (c)
				{
					case '&': sb.Append("&amp;"); break;
					case '<': sb.Append("&lt;"); break;
					case '"': sb.Append("&quot;"); break;
					case '\'': sb.Append("&#39;"); break;
					default: sb.Append(c); break;
				}
			}
			return sb.ToString();
		}

		public static NameValueCollection ParseQueryString(string query)
		{
			var result = new NameValueCollection();
			if (string.IsNullOrEmpty(query))
			{
				return result;
			}

			if (query[0] == '?')
			{
				query = query.Substring(1);
			}

			foreach (var pair in query.Split('&'))
			{
				if (pair.Length == 0)
				{
					continue;
				}

				var idx = pair.IndexOf('=');
				string name;
				string value;
				if (idx < 0)
				{
					name = null;
					value = WebUtility.UrlDecode(pair);
				}
				else
				{
					name = WebUtility.UrlDecode(pair.Substring(0, idx));
					value = WebUtility.UrlDecode(pair.Substring(idx + 1));
				}
				result.Add(name, value);
			}
			return result;
		}
	}
}
