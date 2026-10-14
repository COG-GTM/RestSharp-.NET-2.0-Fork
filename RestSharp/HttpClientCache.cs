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

using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;

namespace RestSharp
{
	/// <summary>
	/// Shares <see cref="HttpClient"/> instances across requests. Handler-level options (redirects, proxy,
	/// credentials, client certificates) can't change per request, so one client is kept per distinct
	/// combination. Cookies are handled per request (UseCookies = false) so handlers can be shared safely.
	/// </summary>
	internal static class HttpClientCache
	{
		private const int MaxCachedClients = 64;
		private static readonly ConcurrentDictionary<Key, HttpClient> _clients = new ConcurrentDictionary<Key, HttpClient>();

		public static Lease Acquire(Key key)
		{
			if (_clients.TryGetValue(key, out var client))
				return new Lease(client, false);

			if (_clients.Count >= MaxCachedClients)
				return new Lease(CreateClient(key), true);

			return new Lease(_clients.GetOrAdd(key, CreateClient), false);
		}

		private static HttpClient CreateClient(Key key)
		{
			var handler = new HttpClientHandler
			{
				AllowAutoRedirect = key.FollowRedirects,
				AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
				UseCookies = false,
				UseDefaultCredentials = false
			};

			if (key.FollowRedirects && key.MaxRedirects.HasValue && key.MaxRedirects.Value > 0)
				handler.MaxAutomaticRedirections = key.MaxRedirects.Value;

			if (key.Proxy != null)
			{
				handler.Proxy = key.Proxy;
				handler.UseProxy = true;
			}

			if (key.Credentials != null)
				handler.Credentials = key.Credentials;

			if (key.ClientCertificates != null && key.ClientCertificates.Count > 0)
			{
				handler.ClientCertificateOptions = ClientCertificateOption.Manual;
				handler.ClientCertificates.AddRange(key.ClientCertificates);
			}

			var client = new HttpClient(handler, true) { Timeout = System.Threading.Timeout.InfiniteTimeSpan };
			// replaces the old global ServicePointManager.Expect100Continue = false
			client.DefaultRequestHeaders.ExpectContinue = false;
			return client;
		}

		internal struct Lease : IDisposable
		{
			private readonly bool _owned;

			public Lease(HttpClient client, bool owned)
			{
				Client = client;
				_owned = owned;
			}

			public HttpClient Client { get; }

			public void Dispose()
			{
				if (_owned)
					Client?.Dispose();
			}
		}

		internal sealed class Key : IEquatable<Key>
		{
			public Key(bool followRedirects, int? maxRedirects, IWebProxy proxy, ICredentials credentials, X509CertificateCollection clientCertificates)
			{
				FollowRedirects = followRedirects;
				MaxRedirects = maxRedirects;
				Proxy = proxy;
				Credentials = credentials;
				ClientCertificates = clientCertificates;
			}

			public bool FollowRedirects { get; }
			public int? MaxRedirects { get; }
			public IWebProxy Proxy { get; }
			public ICredentials Credentials { get; }
			public X509CertificateCollection ClientCertificates { get; }

			public bool Equals(Key other) =>
				other != null
				&& FollowRedirects == other.FollowRedirects
				&& MaxRedirects == other.MaxRedirects
				&& ReferenceEquals(Proxy, other.Proxy)
				&& ReferenceEquals(Credentials, other.Credentials)
				&& ReferenceEquals(ClientCertificates, other.ClientCertificates);

			public override bool Equals(object obj) => Equals(obj as Key);

			public override int GetHashCode()
			{
				unchecked
				{
					var hash = FollowRedirects.GetHashCode();
					hash = hash * 31 + MaxRedirects.GetHashCode();
					hash = hash * 31 + (Proxy == null ? 0 : System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(Proxy));
					hash = hash * 31 + (Credentials == null ? 0 : System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(Credentials));
					hash = hash * 31 + (ClientCertificates == null ? 0 : System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(ClientCertificates));
					return hash;
				}
			}
		}
	}
}
