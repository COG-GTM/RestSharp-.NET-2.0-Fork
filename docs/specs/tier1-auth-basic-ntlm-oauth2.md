# Tier 1 Specification — Basic, NTLM, Simple and OAuth 2 Authenticators

Component: the non-OAuth-1.0a authentication strategies shipped with this RestSharp fork, plus the client/transport plumbing that invokes them.
Scope of files read in full: `RestSharp/Authenticators/IAuthenticator.cs`, `RestSharp/Authenticators/HttpBasicAuthenticator.cs`, `RestSharp/Authenticators/NtlmAuthenticator.cs`, `RestSharp/Authenticators/SimpleAuthenticator.cs`, `RestSharp/Authenticators/OAuth2Authenticator.cs`, `RestSharp.IntegrationTests/AuthenticationTests.cs`, plus the invocation and transport paths in `RestSharp/RestClient.cs`, `RestSharp/RestClient.Sync.cs`, `RestSharp/RestClient.Async.cs`, `RestSharp/RestRequest.cs`, `RestSharp/Http.cs`, `RestSharp/Http.Sync.cs`, `RestSharp/Http.Async.cs`.

OAuth 1.0a (`RestSharp/Authenticators/OAuth1Authenticator.cs` and the `Authenticators/OAuth/**` helper tree) is explicitly **out of scope** and is covered by a separate specification.

---

## 1. Purpose

The authenticator component lets a caller attach credentials to every request made through a `RestClient` without re-stating those credentials on each call. The caller constructs one authenticator object, assigns it to the client, and the client applies it automatically each time a request is executed (`RestSharp/RestClient.cs:250-286`).

Four strategies are in scope:

| Strategy | Business purpose | Where the credential lands |
| --- | --- | --- |
| HTTP Basic (`HttpBasicAuthenticator`) | Username/password against APIs that accept RFC 2617 Basic auth | `Authorization` request header |
| NTLM (`NtlmAuthenticator`) | Call an intranet/Windows-authenticated service as the currently logged-on Windows user | Transport-level credential object, not a header |
| Simple / parameter-based (`SimpleAuthenticator`) | Legacy APIs that take credentials as ordinary query-string or form fields | Query string (GET-style verbs) or form body (POST/PUT/PATCH) |
| OAuth 2 (`OAuth2UriQueryParameterAuthenticator`, `OAuth2AuthorizationRequestHeaderAuthenticator`) | Send an already-obtained OAuth 2 bearer/access token | Query string or `Authorization` header, depending on which class is chosen |

Consumers are application developers using the RestSharp library; the README advertises this exact set ("oAuth 1, oAuth 2, Basic, NTLM and Parameter-based Authenticators included … Supports custom authentication schemes via IAuthenticator", `README.markdown:20-21`). The extension point is the `IAuthenticator` interface, so third parties can plug in their own scheme (`RestSharp/Authenticators/IAuthenticator.cs:19-22`).

Note: this component only *presents* credentials. It performs no token acquisition, no refresh, no challenge parsing, and no credential storage.

---

## 2. Inputs and outputs

### 2.1 Public API surface

| Type | Kind | Members consumers depend on | Citation |
| --- | --- | --- | --- |
| `RestSharp.IAuthenticator` | public interface | `void Authenticate(IRestClient client, IRestRequest request)` | `RestSharp/Authenticators/IAuthenticator.cs:19-22` |
| `RestSharp.HttpBasicAuthenticator` | public class, `IAuthenticator` | ctor `(string username, string password)`; `Authenticate(...)` | `RestSharp/Authenticators/HttpBasicAuthenticator.cs:23-48` |
| `RestSharp.NtlmAuthenticator` | public class, `IAuthenticator`, compiled only under `FRAMEWORK` | implicit default ctor; `Authenticate(...)` | `RestSharp/Authenticators/NtlmAuthenticator.cs:17-33` |
| `RestSharp.SimpleAuthenticator` | public class, `IAuthenticator` | ctor `(string usernameKey, string username, string passwordKey, string password)`; `Authenticate(...)` | `RestSharp/Authenticators/SimpleAuthenticator.cs:19-38` |
| `RestSharp.OAuth2Authenticator` | public **abstract** class, `IAuthenticator` | ctor `(string accessToken)`; read-only `string AccessToken`; abstract `Authenticate(...)` | `RestSharp/Authenticators/OAuth2Authenticator.cs:17-44` |
| `RestSharp.OAuth2UriQueryParameterAuthenticator` | public class : `OAuth2Authenticator` | ctor `(string accessToken)`; `Authenticate(...)` | `RestSharp/Authenticators/OAuth2Authenticator.cs:52-69` |
| `RestSharp.OAuth2AuthorizationRequestHeaderAuthenticator` | public class : `OAuth2Authenticator` | ctor `(string accessToken)`; `Authenticate(...)` | `RestSharp/Authenticators/OAuth2Authenticator.cs:77-105` |
| `RestSharp.IRestClient.Authenticator` | property `IAuthenticator` | assignment point for all of the above | `RestSharp/IRestClient.cs:48`, `RestSharp/RestClient.cs:254` |
| `RestSharp.IRestRequest.Credentials` | property `System.Net.ICredentials` | written by `NtlmAuthenticator`; documented as "In general you would not need to set this directly. Used by the NtlmAuthenticator." | `RestSharp/IRestRequest.cs:92-94`, `RestSharp/RestRequest.cs:431-433` |

All four in-scope authenticator source files declare namespace `RestSharp` (`RestSharp/Authenticators/HttpBasicAuthenticator.cs:21`, `NtlmAuthenticator.cs:19`, `SimpleAuthenticator.cs:17`, `OAuth2Authenticator.cs:4`, `IAuthenticator.cs:17`) — **not** `RestSharp.Authenticators`, which is where OAuth 1 lives (`RestSharp/Authenticators/OAuth1Authenticator.cs:14`). See §8.

### 2.2 Constructor inputs

| Authenticator | Field | Type | Required | Default | Meaning |
| --- | --- | --- | --- | --- | --- |
| `HttpBasicAuthenticator` | `username` | string | yes (not validated) | none | User identity placed left of the colon | 
| | `password` | string | yes (not validated) | none | Secret placed right of the colon |
| `NtlmAuthenticator` | — | — | — | — | Takes no inputs; always uses the ambient Windows identity (`NtlmAuthenticator.cs:28`) |
| `SimpleAuthenticator` | `usernameKey` | string | yes | none | Parameter name under which the username is sent |
| | `username` | string | yes | none | Username value |
| | `passwordKey` | string | yes | none | Parameter name under which the password is sent |
| | `password` | string | yes | none | Password value |
| `OAuth2*` | `accessToken` | string | yes | none | Previously obtained OAuth 2 access token |

No constructor performs null/empty validation; see §4.

### 2.3 Outputs (effects on the request)

| Authenticator | Output | Parameter name | Parameter value | `ParameterType` | Citation |
| --- | --- | --- | --- | --- | --- |
| `HttpBasicAuthenticator` | one header parameter (conditionally) | `Authorization` | `Basic {base64(username:password)}` | `HttpHeader` | `HttpBasicAuthenticator.cs:42-47` |
| `NtlmAuthenticator` | sets `request.Credentials` | n/a | `System.Net.CredentialCache.DefaultCredentials` | n/a | `NtlmAuthenticator.cs:28` |
| `SimpleAuthenticator` | two parameters, unconditionally | `usernameKey`, `passwordKey` | the supplied username and password | `GetOrPost` (default overload) | `SimpleAuthenticator.cs:35-36`, `RestSharp/RestRequest.cs:302-305` |
| `OAuth2UriQueryParameterAuthenticator` | one parameter, unconditionally | `oauth_token` | access token | `GetOrPost` | `OAuth2Authenticator.cs:67` |
| `OAuth2AuthorizationRequestHeaderAuthenticator` | one header parameter (conditionally) | `Authorization` | `OAuth {accessToken}` | `HttpHeader` | `OAuth2Authenticator.cs:94,100-103` |

`GetOrPost` parameters become query-string items for GET/DELETE/HEAD/OPTIONS and form/body items for POST/PUT/PATCH (`RestSharp/RestClient.cs:319-332`, `RestSharp/RestRequest.cs:296-305`). This means **the same `SimpleAuthenticator` or OAuth2 query authenticator transmits credentials in the URL or in the body depending purely on the HTTP verb of the request.**

---

## 3. Behavioral rules

### 3.1 When and how often `Authenticate` runs

| # | Rule | Citation |
| --- | --- | --- |
| B1 | Authentication is applied only if `RestClient.Authenticator` is non-null; a null authenticator is a silent no-op. | `RestSharp/RestClient.cs:280-286` |
| B2 | In the synchronous path, `AuthenticateIfNeeded` runs **first**, before the `Accept` header default is added, before `ConfigureHttp`, and before the HTTP call. | `RestSharp/RestClient.Sync.cs:36-47` |
| B3 | In the asynchronous path, the `IHttp` instance is created first, then `AuthenticateIfNeeded`, then the `Accept` default, then `ConfigureHttp`. The observable ordering relative to request mutation is the same as the sync path. | `RestSharp/RestClient.Async.cs:33-42` |
| B4 | `Authenticate` runs **once per `Execute`/`ExecuteAsync` call**, not once per client and not once per retry — there is no retry loop in the client, so N executions of the same `IRestRequest` object cause N invocations. | `RestSharp/RestClient.Sync.cs:36-38`, `RestSharp/RestClient.Async.cs:36` |
| B5 | Generic `Execute<T>` delegates to non-generic `Execute`, so it authenticates exactly once as well. | `RestSharp/RestClient.Sync.cs:68-72` |
| B6 | The client passes itself as the `client` argument; none of the in-scope authenticators read it. Only OAuth 1 (out of scope) uses it. | `RestSharp/RestClient.cs:284` |
| B7 | Authenticator output is applied to the **request**, so it persists on the request object after execution; `RestClient.DefaultParameters` are merged in later, during `ConfigureHttp`, and are skipped when a parameter of the same name+type already exists — so an authenticator-added parameter wins over a same-named client default. | `RestSharp/RestClient.cs:354-363` |

### 3.2 Idempotency / re-execution

| # | Rule | Citation |
| --- | --- | --- |
| B8 | `HttpBasicAuthenticator` adds the `Authorization` parameter **only if no parameter named "Authorization" already exists on the request**, compared case-insensitively (`StringComparison.OrdinalIgnoreCase`) across **all** parameter types, not just headers. Re-executing the same request therefore does not double-add. | `HttpBasicAuthenticator.cs:42-47` |
| B9 | `OAuth2AuthorizationRequestHeaderAuthenticator` uses the identical guard and is likewise safe to re-execute. | `OAuth2Authenticator.cs:99-103` |
| B10 | The guard in B8/B9 also means a **caller-supplied** `Authorization` parameter of *any* type (even a cookie or query parameter coincidentally named "Authorization") suppresses the authenticator entirely, silently. | `HttpBasicAuthenticator.cs:42`, `OAuth2Authenticator.cs:100` |
| B11 | `SimpleAuthenticator` has **no guard**: each execution appends another pair of username/password parameters to the same request object. Executing the same `RestRequest` twice sends the credentials twice (duplicate query-string keys or duplicate form fields). | `SimpleAuthenticator.cs:33-37`, `RestSharp/RestRequest.cs:290-294` |
| B12 | `OAuth2UriQueryParameterAuthenticator` has **no guard**: re-execution appends a second `oauth_token` parameter. | `OAuth2Authenticator.cs:65-68` |
| B13 | `NtlmAuthenticator` assigns a property rather than appending, so it is naturally idempotent across re-executions. | `NtlmAuthenticator.cs:28` |
| B14 | `RestRequest.Attempts` is incremented after a successful transport round trip (`IncreaseNumAttempts`), but no authenticator consults it, so nothing distinguishes a first attempt from a retry. | `RestSharp/RestClient.Sync.cs:49`, `RestSharp/RestRequest.cs:446-466` |

### 3.3 How the output reaches the wire

| # | Rule | Citation |
| --- | --- | --- |
| B15 | `ConfigureHttp` copies `request.Credentials` onto the `IHttp` transport only when non-null. | `RestSharp/RestClient.cs:386-389` |
| B16 | `ConfigureHttp` projects every `HttpHeader` parameter into `http.Headers`, in request-parameter order, with `Value.ToString()`. | `RestSharp/RestClient.cs:391-402` |
| B17 | `ConfigureHttp` projects every non-null `GetOrPost` parameter into `http.Parameters`; `GetOrPost` parameters with a null value are dropped. | `RestSharp/RestClient.cs:417-429` |
| B18 | `Http` writes headers via a restricted-header dispatch table; `Authorization` is **not** in that table, so it is added through `webRequest.Headers.Add(name, value)` under `FRAMEWORK` and through the indexer otherwise. | `RestSharp/Http.cs:179-188,217-233`, `RestSharp/Http.Sync.cs:109-115`, `RestSharp/Http.Async.cs:326-332` |
| B19 | The transport sets `webRequest.UseDefaultCredentials = false` unconditionally, then assigns `webRequest.Credentials` only if the credential object is non-null. NTLM therefore works by explicit `DefaultCredentials` assignment, never by `UseDefaultCredentials`. | `RestSharp/Http.Sync.cs:185-186,217-220`, `RestSharp/Http.Async.cs:341-342,359-362` |
| B20 | `ServicePointManager.Expect100Continue = false` is set process-wide in the sync transport, which affects the challenge/response handshake for credential-based schemes (no 100-continue probe before the body is sent). | `RestSharp/Http.Sync.cs:187` |
| B21 | `PreAuthenticate` is **never set anywhere in the codebase** (no occurrence outside the explanatory comment in `HttpBasicAuthenticator`). Basic auth is pre-emptive purely because the header is attached up front; NTLM relies on the default `HttpWebRequest` challenge/response round trip. | `HttpBasicAuthenticator.cs:34-39`; absence verified across `RestSharp/**` |
| B22 | Query-string assembly for `GetOrPost` parameters URL-encodes both name and value, and only happens for non-POST/PUT/PATCH verbs. | `RestSharp/RestClient.cs:319-348` |

---

## 4. Validation and error behavior

| Area | Behavior | Citation |
| --- | --- | --- |
| Null/empty username, password, keys, token | Not validated anywhere. `null` username/password flow into `string.Format("{0}:{1}", …)` and render as empty strings, producing a header such as `Basic Og==` (base64 of `:`) rather than an error. | `HttpBasicAuthenticator.cs:28-31,44` |
| Null access token | Not validated. `OAuth2AuthorizationRequestHeaderAuthenticator` concatenates at construction time, yielding the literal value `"OAuth "`. The query-parameter variant would add `oauth_token` with a null value, which is then **silently dropped** by `ConfigureHttp`'s `p.Value != null` filter — the request goes out unauthenticated. | `OAuth2Authenticator.cs:94,67`, `RestSharp/RestClient.cs:417-419` |
| Null `client` or `request` argument | Not validated; a null `request` produces a `NullReferenceException` inside `Authenticate`. | all four `Authenticate` implementations |
| Exceptions thrown during `Authenticate` (sync) | Thrown **before** the `try` block in `Execute`, so they propagate to the caller as a raw exception rather than being converted into a failed `RestResponse`. | `RestSharp/RestClient.Sync.cs:36-57` |
| Exceptions thrown during `Authenticate` (async) | Likewise thrown out of `ExecuteAsync` synchronously, before any callback is wired up; the caller's callback never fires. | `RestSharp/RestClient.Async.cs:33-47` |
| Transport-level failures after authentication | Caught in sync `Execute` and reported as `ResponseStatus.Error` with `ErrorMessage`/`ErrorException` set; HTTP 401/403 is **not** an error — it is returned as an ordinary response with that status code. | `RestSharp/RestClient.Sync.cs:45-57` |
| 401 challenge handling | No component in scope inspects `WWW-Authenticate` or retries on 401. There is no automatic re-authentication, token refresh, or credential re-prompt. | absence across `RestSharp/RestClient*.cs`, `RestSharp/Http*.cs` |
| Restartability / retry | The caller must re-invoke `Execute`. On re-invocation, B8/B9 make Basic and OAuth2-header safe, while B11/B12 cause duplicate credential parameters for Simple and OAuth2-query. | `RestClient.Sync.cs:36`, `SimpleAuthenticator.cs:35-36`, `OAuth2Authenticator.cs:67` |
| Silent suppression | A pre-existing `Authorization` parameter suppresses Basic/OAuth2-header with no warning or exception. | `HttpBasicAuthenticator.cs:42`, `OAuth2Authenticator.cs:100` |
| Credential leakage | No masking or redaction: `SimpleAuthenticator` and `OAuth2UriQueryParameterAuthenticator` place secrets in the URL for GET-style verbs, where they are logged by intermediaries. No warning is emitted. | `SimpleAuthenticator.cs:35-36`, `RestSharp/RestClient.cs:319-332` |

---

## 5. Core computations

### 5.1 HTTP Basic credential string

Formula, in order:

1. `pair = string.Format("{0}:{1}", username, password)` — a single colon separator; a colon inside the username is not escaped or rejected (`HttpBasicAuthenticator.cs:44`).
2. `bytes = Encoding.UTF8.GetBytes(pair)` — **UTF-8**, not Latin-1 and not the OS ANSI code page (`HttpBasicAuthenticator.cs:44`). Note `System.Text.Encoding.UTF8` in .NET emits no BOM from `GetBytes`, so the byte sequence is plain UTF-8.
3. `token = Convert.ToBase64String(bytes)` — standard base64 alphabet, `=` padding, no line breaks (`HttpBasicAuthenticator.cs:44`).
4. `headerValue = string.Format("Basic {0}", token)` — literal prefix `Basic`, capital B, exactly one space (`HttpBasicAuthenticator.cs:45`).
5. Added as a header parameter named exactly `Authorization` (`HttpBasicAuthenticator.cs:46`).

`string.Format` here uses the **current culture**, but with only string substitutions there is no culture-sensitive output. The duplicate-detection comparison uses `StringComparison.OrdinalIgnoreCase` — culture-insensitive, as intended (`HttpBasicAuthenticator.cs:42`).

The integration test decodes the header with `Encoding.ASCII.GetString(Convert.FromBase64String(header.Substring("Basic ".Length)))` and asserts round-trip of `testuser|testpassword`, confirming both the `Basic ` prefix (6 characters) and the base64 body (`RestSharp.IntegrationTests/AuthenticationTests.cs:30-36,14-28`). Because the oracle decodes as ASCII, **non-ASCII credentials are not covered by any test**.

### 5.2 OAuth 2 header value

`_authorizationValue = "OAuth " + accessToken`, computed once in the constructor "so that it is only done once. can improve performance" (`OAuth2Authenticator.cs:93-94`). The prefix is the literal `OAuth` with one trailing space — **not** `Bearer`. The token is inserted verbatim: no trimming, no encoding, no base64, no validation. If the caller passes a token that already contains a scheme prefix, the result is doubled (e.g. `OAuth Bearer abc`).

### 5.3 OAuth 2 query value and Simple parameters

No transformation is applied at authentication time. Encoding happens later, when the query string is built: each name and value is passed through `UrlEncode()` (`RestSharp/RestClient.cs:337-348`). For POST/PUT/PATCH the values are handed to the transport as form parameters instead (`RestSharp/RestClient.cs:319-332,417-429`).

Parameter name `oauth_token`, lowercase with underscore, is fixed (`OAuth2Authenticator.cs:67`). Parameter names for `SimpleAuthenticator` are caller-chosen and used verbatim, with no casing normalization (`SimpleAuthenticator.cs:35-36`).

### 5.4 Ordering

Within a single `Execute`: authenticator parameters are appended to `request.Parameters` before the `Accept` default and before client `DefaultParameters` are merged (`RestClient.Sync.cs:36-42`, `RestClient.cs:354-363`). Header emission order onto the wire follows parameter insertion order (`RestClient.cs:391-402`), so `Authorization` precedes `Accept`.

---

## 6. Per-target conditional behavior

| Target / define | Behavior of this component | Citation |
| --- | --- | --- |
| Full .NET (`FRAMEWORK`) — `RestSharp.csproj` | All four strategies compile. Sync `Execute` path exists. | `RestSharp/RestSharp.csproj:41,51`; `RestSharp/RestClient.Sync.cs:1` |
| .NET 2.0 head (`NET_2_0;Net2;FRAMEWORK`) — `RestSharp.Net2.csproj` | Same as full framework: `NtlmAuthenticator` is included because `FRAMEWORK` is defined; all five authenticator files are `<Compile Include>`-linked from `..\RestSharp\`. `System.Linq` usage in Basic/OAuth2 resolves against the vendored LinqBridge. | `RestSharp.Net2/RestSharp.Net2.csproj:22,30,44-57,110-111` |
| Silverlight (`SILVERLIGHT`) | `NtlmAuthenticator.cs` is linked into the project but its entire body is inside `#if FRAMEWORK`, and `FRAMEWORK` is **not** defined — so the type **does not exist** for Silverlight consumers. Basic, Simple and OAuth2 are available. Only the async path exists, and the transport registers the client HTTP stack before creating the request. | `RestSharp/Authenticators/NtlmAuthenticator.cs:17,33`; `RestSharp.Silverlight/RestSharp.Silverlight.csproj:32,43,68-81`; `RestSharp/Http.Async.cs:337-340` |
| Windows Phone (`WINDOWS_PHONE`, and `MANGO` for the Mango head) | Same as Silverlight: `NtlmAuthenticator` type absent (no `FRAMEWORK`). Additionally the transport skips setting `ContentLength`, which changes the request shape credentials travel in. | `RestSharp.WindowsPhone/RestSharp.WindowsPhone.csproj:27,38,60-73,126-127`; `RestSharp.WindowsPhone.Mango/RestSharp.WindowsPhone.Mango.csproj:27,38`; `RestSharp/Http.Async.cs:350-357` |
| MonoTouch (`MONOTOUCH;FRAMEWORK`) | All four strategies compile, including NTLM, because `FRAMEWORK` is defined. NTLM's `CredentialCache.DefaultCredentials` on a non-Windows host yields no usable Windows identity in practice. | `RestSharp.MonoTouch/RestSharp.MonoTouch.csproj:22,42,53,73,188-198,230-231` |
| MonoDroid (`MONODROID;FRAMEWORK`) | Same as MonoTouch. | `RestSharp.MonoDroid/RestSharp.MonoDroid.csproj:22,30,144-154,186-187` |
| `PocketPC` | No `#if PocketPC` block exists in any in-scope authenticator file, and no head in this repository defines it; the component has no PocketPC-specific behavior. | absence across `RestSharp/Authenticators/*.cs` |
| Header emission difference | Under `FRAMEWORK` the `Authorization` header is added with `Headers.Add(name, value)` (appends, and validates the name); otherwise with the indexer `Headers[name] = value` (replaces). This changes behavior only if the same header name is emitted twice. | `RestSharp/Http.cs:227-231` |
| Restricted-header table difference | The sync (FRAMEWORK) build registers additional restricted headers (`Connection`, `Expect`, `If-Modified-Since`, `Referer`, `Transfer-Encoding`, `User-Agent`); the async/Silverlight build registers a no-op for `Content-Length`. `Authorization` is unrestricted in all builds. | `RestSharp/Http.Sync.cs:109-115`, `RestSharp/Http.Async.cs:326-332`, `RestSharp/Http.cs:179-188` |

---

## 7. Target-stack (.NET 8) concerns

| Legacy construct | Modern equivalent | Behavioral risk |
| --- | --- | --- |
| `Encoding.UTF8.GetBytes(user + ":" + pass)` then `Convert.ToBase64String` (`HttpBasicAuthenticator.cs:44`) | Same BCL APIs, or `AuthenticationHeaderValue("Basic", token)` | Low risk in itself, but the existing oracle decodes as ASCII (`AuthenticationTests.cs:34`). Many servers assume ISO-8859-1 per RFC 7617. Any change of encoding silently changes the wire bytes for non-ASCII credentials. Decide and pin the encoding explicitly. |
| Manual `Authorization` header string (`HttpBasicAuthenticator.cs:45-46`, `OAuth2Authenticator.cs:94`) | `HttpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue(scheme, parameter)` | `HttpClient` validates header syntax strictly and throws `FormatException` on values RestSharp currently sends through unchecked (e.g. a token containing CR/LF or a space). Today a malformed token is transmitted or rejected by the server; under `HttpClient` it may throw client-side. |
| `"OAuth " + accessToken` (`OAuth2Authenticator.cs:94`) | `AuthenticationHeaderValue("OAuth", token)` — or `"Bearer"` per RFC 6750 | The legacy scheme name is the pre-final draft `OAuth`, not `Bearer`. Preserving byte-compatibility means keeping `OAuth`; modernizing means changing what servers see. This is a product decision, not a mechanical port (see §9). |
| `System.Net.CredentialCache.DefaultCredentials` (`NtlmAuthenticator.cs:28`) | `HttpClientHandler { UseDefaultCredentials = true }` or `Credentials = CredentialCache.DefaultNetworkCredentials` | `CredentialCache.DefaultCredentials` still exists in .NET 8 but returns an empty/unusable identity on Linux and macOS; NTLM/Negotiate on non-Windows requires GSSAPI/Kerberos configuration. Handler-level credentials are also per-`HttpClient`, not per-request, which conflicts with RestSharp's per-request `Credentials` property — a shared `HttpClient` cannot carry different credentials per request without a handler-per-credential or `SocketsHttpHandler` pooling strategy. |
| `request.Credentials` copied to `HttpWebRequest.Credentials` per request (`RestClient.cs:386-389`, `Http.Sync.cs:217-220`) | Handler-level `Credentials`, or a per-request `HttpMessageHandler` | Connection reuse: `HttpClient` pools connections and NTLM/Negotiate is connection-bound. Mixing authenticated and anonymous requests on a pooled connection changes observed behavior versus `HttpWebRequest`'s per-request `ServicePoint` model. |
| `webRequest.UseDefaultCredentials = false` set unconditionally (`Http.Sync.cs:186`, `Http.Async.cs:342`) | Handler property, set once | The legacy code resets this on every request; a handler-level setting is sticky. Ensure the ported code does not accidentally enable default credentials for anonymous requests. |
| `ServicePointManager.Expect100Continue = false` (`Http.Sync.cs:187`) | `HttpClient` does not send Expect: 100-continue by default; `HttpRequestMessage.Headers.ExpectContinue` per request | Mostly aligned, but the legacy line mutates **global process state**; anything else in the host process relying on 100-continue is affected today and would not be after the port. |
| No `PreAuthenticate` anywhere (§3 B21) | `HttpClientHandler.PreAuthenticate` | .NET 8 handlers default `PreAuthenticate` to `false` too, so NTLM continues to require a 401 round trip. If any consumer depends on the current two-trip behavior (or on avoiding it), state the requirement explicitly. |
| `webRequest.AutomaticDecompression` set to Deflate + GZip + None (`Http.Sync.cs:200`) | `SocketsHttpHandler.AutomaticDecompression` | Not auth-specific, but decompression and redirects interact with auth: see next row. |
| `AllowAutoRedirect` + `MaximumAutomaticRedirections` (`Http.Sync.cs:227-231`) | `HttpClientHandler.AllowAutoRedirect`/`MaxAutomaticRedirections` | `HttpClient` **strips the `Authorization` header on cross-origin redirects**; `HttpWebRequest` also drops credentials on redirect but the exact rules differ. A Basic- or OAuth2-header-authenticated call that currently survives a redirect may start failing with 401 after the port. This is the single highest-risk behavioral difference for this component. |
| Cookies via `http.CookieContainer` (`RestClient.cs:352`) | `HttpClientHandler.CookieContainer`, or `UseCookies=false` | Session cookies issued after authentication are container-scoped; a shared handler shares them across logical clients. |
| `webRequest.Proxy` / `RestClient.Proxy` (`RestClient.Sync.cs:111-117`, `Http.Sync.cs:222-225`) | `HttpClientHandler.Proxy`, `DefaultProxyCredentials` | Proxy credentials are separate from request credentials in `HttpClient`; NTLM-through-proxy needs `DefaultProxyCredentials` set explicitly. |
| `KeepAlive` | `HttpClient` keeps connections alive by default and there is no per-request `KeepAlive` | The legacy code never sets `KeepAlive`, so it inherits `HttpWebRequest`'s default `true`. Behaviorally close, but connection lifetime is governed by `SocketsHttpHandler.PooledConnectionLifetime` in .NET 8 — relevant because NTLM authenticates a connection, not a request. |
| `Timeout` in milliseconds per request (`Http.Sync.cs:212-215`, `RestClient.cs:372`) | `HttpClient.Timeout` (whole operation) or `CancellationToken` per request | `HttpWebRequest.Timeout` covers only the response-headers phase; `HttpClient.Timeout` covers the entire operation including body read. Authenticated calls with a 401→re-send round trip consume that single budget. |
| `IAuthenticator.Authenticate(IRestClient, IRestRequest)` returning `void`, synchronous (`IAuthenticator.cs:21`) | `Task Authenticate(...)` / `ValueTask` | Modern authenticators (token refresh, MSAL, credential providers) are inherently async. Keeping the sync signature forces sync-over-async in consumers; changing it is a breaking API change for third-party implementors that the README explicitly invites (`README.markdown:21`). |
| `RestClient.ExecuteAsync` returning `RestRequestAsyncHandle` with callbacks, built over `IAsyncResult`/`BeginGetResponse` (`RestClient.Async.cs:33-83`, `Http.Async.cs`) | `Task<RestResponse> ExecuteAsync(..., CancellationToken)` | Authentication currently runs **synchronously on the calling thread** inside `ExecuteAsync` (`RestClient.Async.cs:36`); an async authenticator changes when exceptions surface — today they are thrown synchronously from `ExecuteAsync`, afterwards they would arrive on the returned task. |
| `SynchronizationContext.Post` callback marshalling (`RestClient.Async.cs:49-54`) | `await` with the ambient context, or `ConfigureAwait(false)` | No auth-specific behavior, but the legacy opt-in (`UseSynchronizationContext`) has no direct equivalent. |
| `System.Linq` `Any(...)` resolving to vendored LinqBridge under `NET_2_0` (`HttpBasicAuthenticator.cs:18,42`; `OAuth2Authenticator.cs:2,100`) | BCL LINQ | Semantics of `Any` with a predicate are identical; the shim can simply be dropped. Low risk. |
| Culture: `string.Format` with current culture (`HttpBasicAuthenticator.cs:44-45`) | `string.Create(CultureInfo.InvariantCulture, ...)` or interpolation | No numeric or date arguments today, so no observable difference — but if a token type ever becomes non-string, culture-sensitive formatting would appear. Pin invariant formatting during the port. |
| Casing comparisons: `Equals(..., StringComparison.OrdinalIgnoreCase)` (`HttpBasicAuthenticator.cs:42`, `OAuth2Authenticator.cs:100`) | Unchanged | Already ordinal. Do **not** replace with `ToLower()`; note that `RestClient.ConfigureHttp` compares parameter names with plain `==` (ordinal, case-*sensitive*) when merging defaults (`RestClient.cs:357`), so the two code paths disagree on casing. Preserve or deliberately reconcile. |
| Numeric precision / rounding | n/a | No numeric computation exists in this component. Nothing to preserve. |
| `DateTime` / timezone | n/a in scope | This component performs no time computation (unlike OAuth 1, which does). The only date handling nearby is `Convert.ToDateTime` for the `If-Modified-Since` restricted header (`Http.Sync.cs:112`), which is culture-sensitive and out of scope. |
| Reflection / serialization attributes | n/a in scope | The authenticators use no reflection or serialization attributes. |
| Test framework: xUnit 1.9 `[Fact]` with `HttpListener`-based `SimpleServer` (`AuthenticationTests.cs:8,14-28`) | xUnit v2/v3 | The single live Basic-auth test binds `http://localhost:8080/` via `HttpListener`, which requires a URL ACL on Windows and is port-collision prone. Port the oracle to a modern in-process test host and keep the exact assertion (`testuser|testpassword`). |

---

## 8. Gaps, stubs, dead code and discrepancies

| # | Finding | Evidence |
| --- | --- | --- |
| G1 | **Namespace inconsistency.** Basic, NTLM, Simple, OAuth2 and `IAuthenticator` are declared in namespace `RestSharp`, while OAuth 1 is in `RestSharp.Authenticators`. The integration test imports `RestSharp.Authenticators` and then constructs `HttpBasicAuthenticator` — it compiles only because the test's own namespace is `RestSharp.IntegrationTests`, which resolves `RestSharp` implicitly. | `IAuthenticator.cs:17`, `HttpBasicAuthenticator.cs:21`, `NtlmAuthenticator.cs:19`, `SimpleAuthenticator.cs:17`, `OAuth2Authenticator.cs:4` vs `OAuth1Authenticator.cs:14`; `AuthenticationTests.cs:5,10,21` |
| G2 | **Commented-out credential code** in `HttpBasicAuthenticator` documents a rejected `NetworkCredential` approach ("always makes two trips, even if with PreAuthenticate, it is also unsafe for many partial trust scenarios"). Dead code carrying design rationale. | `HttpBasicAuthenticator.cs:34-39` |
| G3 | **Test coverage is nearly absent.** Exactly one executable test exercises this component (`Can_Authenticate_With_Basic_Http_Auth`). There is **no test at all** for `NtlmAuthenticator`, `SimpleAuthenticator`, `OAuth2UriQueryParameterAuthenticator` or `OAuth2AuthorizationRequestHeaderAuthenticator`, and none in `RestSharp.Tests/**`. | `AuthenticationTests.cs:14-28`; no other `Authenticator` references in `RestSharp.Tests/**` |
| G4 | **Disabled tests.** `Can_Authenticate_With_OAuth` has its `[Fact]` commented out and contains a hard-coded breakpoint-driven verifier plus `Process.Start(url)`; four further OAuth tests are fully commented out. Hard-coded consumer key/secret strings are checked into the file. | `AuthenticationTests.cs:38-45,60-65,89-143` |
| G5 | **`NtlmAuthenticator` is compiled into Silverlight and Windows Phone projects but produces no type** because its body is `#if FRAMEWORK`. The project files list a file that contributes nothing on those heads. | `NtlmAuthenticator.cs:17,33`; `RestSharp.Silverlight.csproj:74-75`; `RestSharp.WindowsPhone.csproj:66-67` |
| G6 | **Class-name / doc-comment mismatch.** The class doc says "Tries to Authenticate with the credentials of the currently logged in user" — accurate — but the type is named `NtlmAuthenticator` while it sets `DefaultCredentials`, which negotiates whatever scheme the server offers (Negotiate/Kerberos/NTLM/Digest), not NTLM specifically. | `NtlmAuthenticator.cs:21-29` |
| G7 | **Inconsistent duplicate-prevention.** Basic and OAuth2-header guard against re-adding; Simple and OAuth2-query do not. Nothing documents this asymmetry. | `HttpBasicAuthenticator.cs:42`, `OAuth2Authenticator.cs:100` vs `SimpleAuthenticator.cs:35-36`, `OAuth2Authenticator.cs:67` |
| G8 | **Over-broad duplicate guard.** The `Authorization` check scans *all* parameter types, so a cookie, URL-segment or query parameter named "Authorization" silently suppresses authentication. | `HttpBasicAuthenticator.cs:42`, `OAuth2Authenticator.cs:100` |
| G9 | **Typos in shipped XML doc comments** ("Conatenate", "Authoriztion") which surface in generated documentation/IntelliSense. | `OAuth2Authenticator.cs:80,93` |
| G10 | **README claims vs. code.** README advertises "Parameter-based Authenticators" and shows `new HttpBasicAuthenticator(username, password)` in a commented sample; it does not mention that `SimpleAuthenticator` transmits credentials in the URL for GET requests. | `README.markdown:20-21,27` vs `SimpleAuthenticator.cs:35-36`, `RestClient.cs:319-332` |
| G11 | **`OAuth2Authenticator._accessToken` is stored but unused by the header variant**, which snapshots `"OAuth " + accessToken` at construction. If a derived class mutated the token (it cannot — the field is readonly), the two would diverge. The base `AccessToken` property exists purely for derived classes. | `OAuth2Authenticator.cs:22,38-41,94` |
| G12 | **`client` parameter unused** by all four in-scope authenticators, yet it is part of the interface contract. | `IAuthenticator.cs:21`; all four `Authenticate` bodies |
| G13 | **No 401 challenge handling, no token refresh, no `WWW-Authenticate` parsing** anywhere in the client or transport. | absence across `RestClient*.cs`, `Http*.cs` |
| G14 | **`RestRequest.IncreaseNumAttempts` is a public method documented as "Internal Method"** — a public surface that the doc comment says is not for consumers, and it is the only retry-adjacent hook available. | `RestRequest.cs:448-454` |

---

## 9. Open questions for the human reviewer

1. **Basic credential encoding.** The code encodes `username:password` as UTF-8 (`HttpBasicAuthenticator.cs:44`) while the only test decodes as ASCII (`AuthenticationTests.cs:34`), so non-ASCII behavior is untested and unspecified. For .NET 8, do we (a) keep UTF-8 byte-for-byte, (b) switch to ISO-8859-1 per RFC 7617, or (c) make the encoding a constructor option defaulting to UTF-8?
2. **OAuth 2 scheme name.** The header variant emits `OAuth <token>` per the 2010 draft (`OAuth2Authenticator.cs:94`). Do we (a) preserve `OAuth` exactly for compatibility, (b) change to `Bearer` per RFC 6750, or (c) ship both with `OAuth` kept as a deprecated alias?
3. **NTLM on non-Windows.** `CredentialCache.DefaultCredentials` yields no usable identity on Linux/macOS (`NtlmAuthenticator.cs:28`). Do we (a) keep the type Windows-only and throw `PlatformNotSupportedException` elsewhere, (b) broaden it to accept an explicit `NetworkCredential` (domain/user/password) as an optional constructor, or (c) drop the type and direct users to configure `HttpClientHandler` themselves?
4. **Per-request credentials vs. pooled `HttpClient`.** `HttpClient` carries credentials on the handler, not the request (`RestClient.cs:386-389`). Do we (a) create a handler per distinct credential (losing some connection pooling), (b) keep a single handler and forbid per-request credentials, or (c) route credentialed requests through a dedicated client instance?
5. **Duplicate-parameter behavior on re-execution.** Should `SimpleAuthenticator` and `OAuth2UriQueryParameterAuthenticator` gain the same "already present" guard Basic and OAuth2-header have (`SimpleAuthenticator.cs:35-36` vs `HttpBasicAuthenticator.cs:42`) — accepting a behavior change for anyone relying on duplicates — or is today's append-on-every-execute behavior intentional and to be preserved?
6. **Over-broad `Authorization` guard.** Should the duplicate check be narrowed to `ParameterType.HttpHeader` only (`HttpBasicAuthenticator.cs:42`), or preserved as an all-parameter-types scan?
7. **Async authenticator interface.** Do we change `IAuthenticator.Authenticate` to return `Task`/`ValueTask` (`IAuthenticator.cs:21`) — breaking every third-party implementor the README invites — or keep the sync signature and add a parallel `IAsyncAuthenticator`?
8. **Redirect behavior.** `HttpClient` strips `Authorization` on cross-origin redirects while the current stack follows up to `MaxRedirects` with the header already attached to the initial request (`Http.Sync.cs:227-231`). Do we accept the new (safer) behavior, or add an opt-in that re-attaches credentials across redirects to preserve today's outcomes?
9. **Global `Expect100Continue`.** Should the port keep the process-wide mutation's effect (`Http.Sync.cs:187`) as a per-request `ExpectContinue = false`, or simply rely on `HttpClient`'s default?
10. **Namespace.** Do we move the in-scope authenticators into `RestSharp.Authenticators` to match OAuth 1 (G1), accepting a breaking change for existing consumers, or keep them in `RestSharp`?
11. **Credential-in-URL exposure.** `SimpleAuthenticator` and the OAuth2 query variant put secrets in the query string for GET-style verbs (§3, B22). Do we keep this silently, emit a warning/analyzer diagnostic, or require an explicit opt-in flag?
12. **Test oracle.** With only one live test (G3), what minimum coverage must the migrated component carry before it is considered equivalent — e.g. per-authenticator wire-shape tests, a re-execution/duplicate test, and a non-ASCII credential test?
