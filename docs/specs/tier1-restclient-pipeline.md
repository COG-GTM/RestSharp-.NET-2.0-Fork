# Tier 1 Specification — REST Client and Request Pipeline

Component: the public entrypoint that turns a `RestRequest` into an `IHttp` call and back into a `RestResponse`.

Scope of this document: `RestSharp/RestClient.cs`, `RestSharp/RestClient.Sync.cs`, `RestSharp/RestClient.Async.cs`, `RestSharp/RestClientExtensions.cs`, `RestSharp/IRestClient.cs`, `RestSharp/RestRequest.cs`, `RestSharp/IRestRequest.cs`, `RestSharp/RestRequestAsyncHandle.cs`, plus the parts of `RestSharp/Http*.cs` and `RestSharp/Extensions/StringExtensions.cs` needed to state a rule precisely. Concrete serializers/deserializers are covered by other specs; only their contracts (`ISerializer`, `IDeserializer`) appear here.

This is a read-only reverse-engineering document. It records what the legacy code does today, not what it should do.

---

## 1. Purpose

The REST client and request pipeline is the layer a business application actually programs against. A caller describes *what* they want — a base address, a resource path, some parameters, headers, cookies, files, and optionally an object to send as a body — and the pipeline turns that description into a concrete HTTP call, executes it through the low-level transport wrapper, and hands back a uniform response object that carries status, headers, cookies, raw bytes, text content, and (optionally) a strongly typed object deserialized from the payload.

Consumers:

| Consumer | How it uses the component |
|---|---|
| Application code | Creates a `RestClient` per API/base address, creates one `RestRequest` per call, calls `Execute`, `Execute<T>`, `ExecuteAsync`, `ExecuteAsync<T>`, or `DownloadData`. |
| Authenticators (`IAuthenticator`) | Invoked by the client immediately before each execution and allowed to mutate the request (add headers/parameters/credentials) — `RestSharp/Authenticators/IAuthenticator.cs`, `RestSharp/RestClient.cs:280-286`. |
| Deserializers (`IDeserializer`) | Registered per content type in the client's handler registry and selected by sniffing the response content type — `RestSharp/RestClient.cs:158-211`. |
| Transport (`IHttp` / `Http`) | The pipeline's downstream dependency; created through a pluggable `IHttpFactory` — `RestSharp/RestClient.cs:37`, `RestSharp/IHttpFactory.cs`. |
| Test doubles | Tests can swap `HttpFactory` to inject a fake transport (e.g. `RestSharp.Tests/Fakes/NullHttp.cs`). |

The component deliberately contains no socket, stream, or protocol logic: everything below `IHttp` (headers on the wire, multipart encoding, redirects, decompression) belongs to the transport component.

---

## 2. Inputs and outputs

### 2.1 `RestClient` — client-level configuration (per API/base address)

| Member | Type | Default | Required | Meaning |
|---|---|---|---|---|
| `BaseUrl` | `string` | `null` | Effectively yes | Scheme + host (+ optional path) prefix. Setter strips one trailing `/` — `RestSharp/RestClient.cs:264-278`. |
| `DefaultParameters` | `IList<Parameter>` (read-only property, mutable list) | empty | No | Parameters merged into every request executed by this client — `RestSharp/RestClient.cs:86`, `RestSharp/RestClient.cs:354-363`. |
| `Authenticator` | `IAuthenticator` | `null` | No | Invoked before each execution — `RestSharp/RestClient.cs:254`, `:280-286`. |
| `UserAgent` | `string` | `"RestSharp " + <assembly version>` | No | Copied to the transport when non-empty — `RestSharp/RestClient.cs:61-65`, `:367-370`. |
| `Timeout` | `int` (ms) | `0` (= no explicit timeout) | No | Client-level timeout, overridden by a non-zero request timeout — `RestSharp/RestClient.cs:244`, `:372`. |
| `CookieContainer` | `CookieContainer` | `null` | No | Passed straight to the transport, including when null — `RestSharp/RestClient.cs:234`, `:352`. |
| `FollowRedirects` | `bool` | `true` | No | Set in the constructor; propagated except under `SILVERLIGHT` — `RestSharp/RestClient.cs:66`, `:374-376`. |
| `MaxRedirects` | `int?` | `null` | No | Propagated only under `FRAMEWORK` — `RestSharp/RestClient.cs:216`, `:377-384`. |
| `ClientCertificates` | `X509CertificateCollection` | `null` | No | `FRAMEWORK` only; propagated when non-null — `RestSharp/RestClient.cs:218-223`, `:377-382`. |
| `Proxy` | `IWebProxy` | `null` | No | `FRAMEWORK` only, and applied **only on the synchronous path** — `RestSharp/RestClient.Sync.cs:18`, `:111-117`. |
| `UseSynchronizationContext` | `bool` | `false`; `true` under `WINDOWS_PHONE` | No | Marshals async callbacks onto the captured `SynchronizationContext` — `RestSharp/RestClient.cs:44-46`, `RestSharp/RestClient.Async.cs:49-54`. |
| `HttpFactory` | `IHttpFactory` (public field) | `new SimpleFactory<Http>()` | No | Transport factory; one `IHttp` instance is created per execution — `RestSharp/RestClient.cs:37`, `RestSharp/RestClient.Sync.cs:76`, `RestSharp/RestClient.Async.cs:35`. |
| `ContentHandlers` | `IDictionary<string, IDeserializer>` (private) | 7 default registrations | — | Content-type → deserializer registry — `RestSharp/RestClient.cs:47-58`, `:79`. |
| `AcceptTypes` | `IList<string>` (private) | the 6 non-wildcard registrations | — | Source of the generated `Accept` header — `RestSharp/RestClient.cs:80`, `:158-165`. |

Client methods: `AddDefaultParameter(Parameter)`, `AddDefaultParameter(name, value)`, `AddDefaultParameter(name, value, type)`, `AddDefaultHeader`, `AddDefaultUrlSegment`, `AddHandler`, `RemoveHandler`, `ClearHandlers`, `BuildUri`, `Execute`, `Execute<T>`, `DownloadData`, `ExecuteAsync`, `ExecuteAsync<T>` (`RestSharp/RestClient.cs`, `RestClient.Sync.cs`, `RestClient.Async.cs`). Two extension overloads accept a callback without the async handle — `RestSharp/RestClientExtensions.cs:13-28`.

`IRestClient` exposes a strict subset: `CookieContainer`, `UserAgent`, `Timeout`, `UseSynchronizationContext`, `Authenticator`, `BaseUrl`, `DefaultParameters`, `ExecuteAsync`, `ExecuteAsync<T>`, `BuildUri`, and (under `FRAMEWORK`) `ClientCertificates`, `Execute`, `Execute<T>`, `Proxy` — `RestSharp/IRestClient.cs:27-80`.

### 2.2 `RestRequest` — per-call description

| Member | Type | Default | Required | Meaning |
|---|---|---|---|---|
| `Resource` | `string` | `null` | No | Path appended to `BaseUrl`; may contain `{token}` placeholders — `RestSharp/RestRequest.cs:390`. |
| `Method` | `Method` enum | `GET` | No | `GET, POST, PUT, DELETE, HEAD, OPTIONS, PATCH` — `RestSharp/Enum.cs:43-52`, `RestSharp/RestRequest.cs:367-376`. |
| `Parameters` | `List<Parameter>` | empty | No | All cookie/query/form/header/url-segment/body parameters — `RestSharp/RestRequest.cs:360`. |
| `Files` | `List<FileParameter>` | empty | No | Multipart uploads — `RestSharp/RestRequest.cs:365`. |
| `RequestFormat` | `DataFormat` | `Xml` | No | Chooses the serializer used by `AddBody` — `RestSharp/RestRequest.cs:392-407`. |
| `JsonSerializer` | `ISerializer` | `new JsonSerializer()` | No | Used when `RequestFormat == Json` — `RestSharp/RestRequest.cs:36`, `:52`. |
| `XmlSerializer` | `ISerializer` | `new XmlSerializer()` | No | Used when `RequestFormat == Xml` — `RestSharp/RestRequest.cs:42`, `:51`. |
| `RootElement` | `string` | `null` | No | Passed to the deserializer before deserialization — `RestSharp/RestRequest.cs:413`, `RestSharp/RestClient.cs:496`. |
| `DateFormat` | `string` | `null` | No | Passed to the deserializer — `RestSharp/RestRequest.cs:423`, `RestSharp/RestClient.cs:497`. |
| `XmlNamespace` | `string` | `null` | No | Passed to the deserializer as `Namespace` — `RestSharp/RestRequest.cs:428`, `RestSharp/RestClient.cs:498`. |
| `OnBeforeDeserialization` | `Action<IRestResponse>` | no-op lambda | No | Hook run before handler selection/deserialization — `RestSharp/RestRequest.cs:54`, `:418`, `RestSharp/RestClient.cs:493`. |
| `Credentials` | `ICredentials` | `null` | No | Copied to the transport when non-null — `RestSharp/RestRequest.cs:433`, `RestSharp/RestClient.cs:386-389`. |
| `Timeout` | `int` (ms) | `0` | No | Non-zero value overrides the client timeout — `RestSharp/RestRequest.cs:444`, `RestSharp/RestClient.cs:372`. |
| `UserState` | `object` | `null` | No | Caller-defined state; **not** on `IRestRequest` and never read by the pipeline — `RestSharp/RestRequest.cs:439`. |
| `Attempts` | `int` (read-only) | `0` | — | Incremented once per successful synchronous execution — `RestSharp/RestRequest.cs:446-466`, `RestSharp/RestClient.Sync.cs:49`. |

Request construction: `RestRequest()`, `RestRequest(Method)`, `RestRequest(string resource)`, `RestRequest(string resource, Method)`, `RestRequest(Uri)`, `RestRequest(Uri, Method)` — `RestSharp/RestRequest.cs:47-108`. A `Uri` resource is reduced to `AbsolutePath + Query` when absolute, otherwise `OriginalString` — `RestSharp/RestRequest.cs:104-108`.

Request builders: `AddParameter` (3 overloads), `AddHeader`, `AddCookie`, `AddUrlSegment`, `AddBody` (2 overloads), `AddObject` (2 overloads), `AddFile` (4 overloads), `IncreaseNumAttempts` — `RestSharp/RestRequest.cs:117-354`, `:451-454`.

### 2.3 Parameter types

| `ParameterType` | Where it lands |
|---|---|
| `GetOrPost` | Querystring for GET/DELETE/HEAD/OPTIONS; form/body parameters for POST/PUT/PATCH — `RestSharp/RestClient.cs:319-332`, `:417-429`. |
| `HttpHeader` | Transport header collection — `RestSharp/RestClient.cs:391-402`. |
| `Cookie` | Transport cookie collection — `RestSharp/RestClient.cs:404-415`. |
| `UrlSegment` | Token substitution in `Resource` — `RestSharp/RestClient.cs:296-300`. |
| `RequestBody` | Raw request body; parameter **Name carries the content type** — `RestSharp/RestClient.cs:436-444`, `RestSharp/RestRequest.cs:219-222`. |

### 2.4 Outputs

`RestResponse` / `RestResponse<T>` (`RestSharp/RestResponse.cs:27-170`) expose `Request`, `ContentType`, `ContentLength`, `ContentEncoding`, `Content` (lazily decoded from `RawBytes`), `StatusCode`, `StatusDescription`, `RawBytes`, `ResponseUri`, `Server`, `Cookies`, `Headers`, `ResponseStatus`, `ErrorMessage`, `ErrorException`, and for the generic form `Data`. `DownloadData` returns `RawBytes` only — `RestSharp/RestClient.Sync.cs:25-29`. `ExecuteAsync*` return a `RestRequestAsyncHandle` wrapping the underlying `HttpWebRequest` — `RestSharp/RestRequestAsyncHandle.cs`.

---

## 3. Behavioral rules

### 3.1 Ordering of the synchronous pipeline

1. Authenticate: `Authenticator.Authenticate(this, request)` if an authenticator is set — `RestSharp/RestClient.Sync.cs:38`, `RestSharp/RestClient.cs:280-286`.
2. Compute `Accept` from the registered non-wildcard content types and add it as a **client default parameter** — `RestSharp/RestClient.Sync.cs:40-42`.
3. Create a fresh `IHttp` from `HttpFactory` — `RestSharp/RestClient.Sync.cs:76`.
4. `ConfigureHttp`: merge defaults, build URI, copy client/request settings, headers, cookies, parameters, files, body — `RestSharp/RestClient.cs:350-445`.
5. `ConfigureProxy` (sync only) — `RestSharp/RestClient.Sync.cs:79`, `:111-117`.
6. Dispatch on `request.Method` to the matching transport verb — `RestSharp/RestClient.Sync.cs:83-105`.
7. Convert `HttpResponse` → `RestResponse` — `RestSharp/RestClient.cs:447-489`.
8. Attach the request to the response and increment `Attempts` — `RestSharp/RestClient.Sync.cs:48-49`.
9. For `Execute<T>`: run `OnBeforeDeserialization`, select handler, deserialize — `RestSharp/RestClient.cs:491-514`.

The async pipeline differs in order and content: the transport is created **before** authentication, proxy is never applied, and steps 8 (`Request`, `Attempts`) are omitted entirely — `RestSharp/RestClient.Async.cs:35-42`.

### 3.2 URL building (`BuildUri`)

Applied strictly in this order — `RestSharp/RestClient.cs:293-335`:

1. Start from `request.Resource` (may be `null`).
2. Substitute every `UrlSegment` parameter: each occurrence of `{Name}` is replaced by `Value.ToString().UrlEncode()` — `:296-300`.
3. If the result is non-empty and starts with `/`, strip exactly one leading slash — `:302-305`. Evidence: `RestSharp.Tests/UrlBuilderTests.cs:15-36` (`GET_with_leading_slash`, `POST_with_leading_slash`).
4. If `BaseUrl` is non-empty: use `BaseUrl` alone when the resource is empty, otherwise `"{BaseUrl}/{resource}"` — `:307-317`. Because the `BaseUrl` setter already removed a trailing slash, `http://example.com/` + `/resource` yields `http://example.com/resource` — `:273-277`, evidence `RestSharp.Tests/UrlBuilderTests.cs:39-61`.
5. Querystring is appended **only** when the method is not POST, PUT or PATCH, and only if at least one `GetOrPost` parameter exists — `:319-332`. In that case a single trailing `/` is stripped first, then `"?{encoded}"` is appended.
6. Return `new Uri(assembled)` — `:334`.

Notable consequences:
- For POST/PUT/PATCH, `GetOrPost` parameters never appear in the URL; they are handed to the transport as parameters and become either a form body or multipart fields — `RestSharp/RestClient.cs:417-429`, `RestSharp/Http.cs:274-289`.
- With an empty resource and a bare host, `Uri` normalization produces the trailing slash: `http://example.com` → `http://example.com/` — evidence `RestSharp.Tests/UrlBuilderTests.cs:126-135`.
- A `BaseUrl` that already contains a path is preserved and the resource is appended to it — evidence `RestSharp.Tests/UrlBuilderTests.cs:114-123`.
- URL-segment substitution is literal text replacement, so a token may appear anywhere in the resource, more than once, and the same token name may also exist as another parameter type.

### 3.3 Default-parameter merge and precedence

- `ConfigureHttp` copies each client default into the request unless the request already contains a parameter with the **same `Name` and same `Type`** — `RestSharp/RestClient.cs:354-363`. Request-level wins; this matches the documented contract "If specified in both client and request, the request wins" — `RestSharp/RestClient.cs:82-85`.
- Matching is by exact, case-sensitive string equality on the name (default `string ==`), so `accept` and `Accept` are distinct.
- The merge **mutates the request**: defaults are appended to `request.Parameters` and stay there after execution — `:362`.
- `AddDefaultParameter(Parameter)` rejects `RequestBody` defaults with `NotSupportedException` — `:93-101`.
- `AddDefaultParameter(name, value)` defaults the type to `GetOrPost`; `AddDefaultHeader` and `AddDefaultUrlSegment` are thin shortcuts — `:110-151`.

### 3.4 Accept header and the handler registry

- The constructor registers seven handlers: `application/json`, `text/json`, `text/x-json`, `text/javascript` → `JsonDeserializer`; `application/xml`, `text/xml`, `*` → `XmlDeserializer` — `RestSharp/RestClient.cs:51-58`.
- `AddHandler` overwrites any existing registration for that content type and appends the type to `AcceptTypes`, except for the wildcard `*` which is registered but never advertised — `:158-165`.
- `RemoveHandler` removes from both collections; `ClearHandlers` empties both — `:171-184`.
- Every `Execute`/`ExecuteAsync` recomputes `Accept` as the `AcceptTypes` joined with `", "` and adds it as a client default header — `RestSharp/RestClient.Sync.cs:40-42`, `RestSharp/RestClient.Async.cs:38-40`. With the stock registry the value is `application/json, application/xml, text/json, text/x-json, text/javascript, text/xml`.
- Handler selection (`GetHandler`) — `RestSharp/RestClient.cs:191-211`:
  1. If the content type is null/empty **and** `*` is registered, return the wildcard handler.
  2. Otherwise truncate the content type at the first `;` (dropping `charset=…`).
  3. Exact, case-sensitive dictionary lookup; on miss fall back to `*`; if neither exists return `null`.
- Deserialization is driven by the response content type, not by `RequestFormat` — `RestSharp/RestClient.cs:495`.

### 3.5 Propagation from client/request to transport (`ConfigureHttp`)

In source order — `RestSharp/RestClient.cs:350-445`:

| Step | Rule |
|---|---|
| Cookie container | `http.CookieContainer = CookieContainer` unconditionally, including `null` — `:352`. |
| Defaults merge | See 3.3 — `:354-363`. |
| URL | `http.Url = BuildUri(request)`, i.e. after the merge, so default `UrlSegment`/`GetOrPost` parameters participate in URL building — `:365`. |
| User agent | Copied only when non-empty (`HasValue()`) — `:367-370`. |
| Timeout | `request.Timeout == 0 ? client.Timeout : request.Timeout` — `:372`. A request timeout of 0 can never mean "no timeout" when the client sets one. |
| Redirects | `http.FollowRedirects = FollowRedirects` except under `SILVERLIGHT`; `http.MaxRedirects = MaxRedirects` and client certificates only under `FRAMEWORK` — `:374-384`. |
| Credentials | Copied only when the request's `Credentials` is non-null; the client has no credentials property — `:386-389`. |
| Headers | Every `HttpHeader` parameter copied, value via `ToString()` — `:391-402`. |
| Cookies | Every `Cookie` parameter copied — `:404-415`. |
| Parameters | Every `GetOrPost` parameter **with a non-null value** copied — `:417-429`. |
| Files | Every `FileParameter` copied field-for-field — `:431-434`. |
| Body | The **first** `RequestBody` parameter only: `http.RequestBody = Value.ToString()`, `http.RequestContentType = Name` — `:436-444`. |

Proxy is applied only on the sync path — `RestSharp/RestClient.Sync.cs:111-117`.

### 3.6 Body selection when several sources are present

`AddBody` serializes with the JSON or XML serializer according to `RequestFormat` and adds a `RequestBody` parameter whose **name is the serializer's content type** — `RestSharp/RestRequest.cs:195-223`. Nothing prevents multiple `RequestBody` parameters; `ConfigureHttp` silently uses the first — `RestSharp/RestClient.cs:436-438`.

Downstream, the transport resolves the final body by precedence **files > form parameters > explicit body** — `RestSharp/Http.cs:274-289`:
1. If files exist → `multipart/form-data` with the fixed boundary; the explicit `RequestBody` is not sent.
2. Else if `GetOrPost` parameters exist → `application/x-www-form-urlencoded`, and `RequestBody` is **overwritten** with the encoded parameters, discarding anything set by `AddBody`.
3. Else if a body exists → the body is sent with `RequestContentType`.

So on POST/PUT/PATCH, mixing `AddParameter(...)` (GetOrPost) with `AddBody(...)` silently drops the serialized body.

### 3.7 Async execution and abort

- `ExecuteAsync` creates the transport, authenticates, adds `Accept`, configures the transport, then dispatches to the matching `*Async` transport verb, which returns the underlying `HttpWebRequest` — `RestSharp/RestClient.Async.cs:33-83`.
- The returned `RestRequestAsyncHandle` carries that `HttpWebRequest`; `Abort()` calls `HttpWebRequest.Abort()` when non-null and is otherwise a no-op — `RestSharp/RestRequestAsyncHandle.cs:18-22`.
- When `UseSynchronizationContext` is true and `SynchronizationContext.Current` is non-null, the completion callback is posted to that context — `RestSharp/RestClient.Async.cs:49-54`.
- `ExecuteAsync<T>` deserializes **only when** `ResponseStatus != Aborted`; for an aborted response the caller receives the casted, undeserialized `RestResponse<T>` — `RestSharp/RestClient.Async.cs:97-109`. Note `ResponseStatus.Error` and `TimedOut` are *not* excluded, so failed responses are still passed to a deserializer.
- The two extension overloads simply discard the handle argument — `RestSharp/RestClientExtensions.cs:13-28`. Evidence for both async shapes: `RestSharp.IntegrationTests/AsyncTests.cs:14-53`.

### 3.8 Deserialization (`Execute<T>`)

`RestSharp/RestClient.cs:491-514`, `RestSharp/RestClient.Sync.cs:68-72`:
1. Invoke `request.OnBeforeDeserialization(raw)` — the hook may inspect the status code and change `RootElement` before it is read. Evidence: `RestSharp.IntegrationTests/StatusCodeTests.cs:38-84` switches the root element to `Error` on HTTP 400 and to the default on success.
2. Select the handler from `raw.ContentType`.
3. Copy `RootElement`, `DateFormat`, `XmlNamespace` onto the handler — note this mutates the shared, client-registered handler instance.
4. Cast the raw response to `RestResponse<T>` via the explicit operator, then deserialize into `Data`.

---

## 4. Validation and error behavior

| Situation | Behavior | Citation |
|---|---|---|
| `AddDefaultParameter` with `RequestBody` | Throws `NotSupportedException("Cannot set request body from default headers…")` | `RestSharp/RestClient.cs:95-98` |
| Any exception during synchronous execution | Swallowed: a new `RestResponse` is returned with `ResponseStatus = Error`, `ErrorMessage = ex.Message`, `ErrorException = ex`. `Request` and `Attempts` are **not** set in this path | `RestSharp/RestClient.Sync.cs:44-59` |
| Transport-level failure (DNS, socket, timeout) | Surfaced by the transport as `ResponseStatus.Error` with the exception attached; the pipeline passes it through | `RestSharp/Http.Sync.cs:118-136`; evidence `RestSharp.IntegrationTests/StatusCodeTests.cs:29-36` |
| HTTP error status (4xx/5xx) | **Not** an error: `ResponseStatus` stays `Completed` and the caller must inspect `StatusCode` | `RestSharp/RestResponse.cs:105-120`, `RestSharp/Http.Sync.cs:138-152`; evidence `RestSharp.IntegrationTests/StatusCodeTests.cs:10-22` |
| Deserialization failure | Swallowed: `ResponseStatus = Error`, `ErrorMessage`, `ErrorException` set on the typed response; `Data` stays default | `RestSharp/RestClient.cs:501-511` |
| No handler for the response content type and no `*` registered | `GetHandler` returns `null` and the **unguarded** `handler.RootElement = …` throws `NullReferenceException` out of `Execute<T>` — this line sits before the try block | `RestSharp/RestClient.cs:495-499` |
| Response content type null/empty and `*` removed | `contentType.IndexOf(';')` throws `NullReferenceException` (the null guard only returns early when `*` is registered) | `RestSharp/RestClient.cs:193-199` |
| `BaseUrl` set to `null` | Setter throws `NullReferenceException` on `_baseUrl.EndsWith("/")` | `RestSharp/RestClient.cs:270-277` |
| `UrlSegment`/header/cookie parameter with a null value | `Value.ToString()` throws `NullReferenceException`; only `GetOrPost` parameters are null-filtered, and only in `ConfigureHttp` (not in `EncodeParameters`) | `RestSharp/RestClient.cs:299`, `:396`, `:409`, `:417-419`, `:344` |
| Assembled URL not a valid absolute URI (e.g. no `BaseUrl`) | `new Uri(...)` throws `UriFormatException`, surfaced as `ResponseStatus.Error` when inside `Execute` | `RestSharp/RestClient.cs:334` |
| Async setup failure | Transport catches and reports through the callback with `ResponseStatus.Error`; `ExecuteAsync` returns whatever `HttpWebRequest` was created (possibly `null`) | `RestSharp/Http.Async.cs:80-121`, `RestSharp/RestClient.Async.cs:81-82` |
| Async timeout | Transport aborts the request and reports `ResponseStatus.TimedOut` | `RestSharp/Http.Async.cs:219-250`, `:287-292` |
| Async abort by caller | `WebExceptionStatus.RequestCanceled` → `ResponseStatus.Aborted`, and `ExecuteAsync<T>` skips deserialization | `RestSharp/Http.Async.cs:300-308`, `RestSharp/RestClient.Async.cs:102-105` |

**Restartability / retry.** There is no retry logic anywhere in the pipeline. `Attempts` is a counter only, incremented once per synchronous execution that returned without throwing — `RestSharp/RestClient.Sync.cs:49`, `RestSharp/RestRequest.cs:451-454`. Re-executing the same `RestRequest` object is *not* side-effect free: each execution appends merged default parameters to the request (3.3) and appends a fresh `Accept` default to the client (3.4), and on POST/PUT the transport may overwrite `RequestBody` (3.6).

---

## 5. Core computations

| Computation | Exact rule |
|---|---|
| Base URL normalization | One trailing `/` removed at set time, once, non-recursively — `RestSharp/RestClient.cs:273-277`. |
| Resource normalization | One leading `/` removed if present — `RestSharp/RestClient.cs:302-305`. |
| URL join | `string.Format("{0}/{1}", BaseUrl, resource)`; when the resource is empty the base URL is used verbatim — `RestSharp/RestClient.cs:307-317`. |
| Querystring | `"{url}?{name1}={value1}&{name2}={value2}…"`, in the parameter insertion order of `request.Parameters` (defaults appended last) — `RestSharp/RestClient.cs:329-330`, `:337-348`. |
| Percent-encoding | `Uri.EscapeDataString` for both names and values (URL segments, query names/values) — `RestSharp/Extensions/StringExtensions.cs:54-57`. This escapes space as `%20`, not `+`, and leaves `-._~` unescaped. |
| Value stringification | Every parameter value becomes a string via `object.ToString()` with **no explicit culture** — `RestSharp/RestClient.cs:299`, `:344`, `:396`, `:409`, `:423`, `:442`. Numeric and `DateTime` values therefore render under `CurrentCulture`. |
| Accept header | `string.Join(", ", AcceptTypes)` — `RestSharp/RestClient.Sync.cs:41`, `RestSharp/RestClient.Async.cs:39`. |
| User agent | `"RestSharp " + assemblyName.Version.ToString()` (four-part version) — `RestSharp/RestClient.cs:61-65`. |
| Timeout selection | `request.Timeout == 0 ? client.Timeout : request.Timeout`, milliseconds — `RestSharp/RestClient.cs:372`. |
| Content-type sniffing | Truncate at the first `;`, then case-sensitive exact match, then `*` — `RestSharp/RestClient.cs:198-208`. |
| Body content type | Carried in the `RequestBody` parameter's `Name` field — `RestSharp/RestRequest.cs:219-222`, `RestSharp/RestClient.cs:443`. |
| Response text decoding | `RawBytes` decoded as UTF-8 under `FRAMEWORK` regardless of the response charset; BOM sniffing exists only on non-`FRAMEWORK` targets — `RestSharp/RestResponse.cs:61-75`, `RestSharp/Extensions/MiscExtensions.cs:108-152`. |
| Request body encoding | UTF-8 for bodies and multipart sections — `RestSharp/Http.cs:38`, `:291-295`. |
| `AddObject` array flattening | Array-valued properties are joined with `,` after a cast to `string[]`; null-valued properties are skipped; whitelist match is exact and case-sensitive — `RestSharp/RestRequest.cs:244-272`. |

---

## 6. Per-target conditional behavior

The `RestSharp.Net2` head compiles the shared sources with `Net2;FRAMEWORK;NET_2_0` and adds the vendored LINQ bridge and Mono `System.Xml.Linq` — `RestSharp.Net2/RestSharp.Net2.csproj:22`, `:30`, `:278`. Other heads: `FRAMEWORK` (desktop), `SILVERLIGHT`, `WINDOWS_PHONE` (+`MANGO`), `MONOTOUCH;FRAMEWORK`, `MONODROID;FRAMEWORK` — see the corresponding `.csproj` `DefineConstants`.

| Define | Effect on this component |
|---|---|
| `FRAMEWORK` | Enables the entire synchronous API: `Execute`, `Execute<T>`, `DownloadData`, `Proxy`, `ConfigureProxy` — the whole of `RestSharp/RestClient.Sync.cs` is inside `#if FRAMEWORK`. Also enables `ClientCertificates` and `MaxRedirects` propagation (`RestSharp/RestClient.cs:218-223`, `:377-384`) and the `FRAMEWORK`-only members of `IRestClient` (`RestSharp/IRestClient.cs:68-77`) and `IRestRequest.AddFile` overloads (`RestSharp/IRestRequest.cs:110-138`). Without it, only the async API exists. |
| `SILVERLIGHT` | `FollowRedirects` is not propagated to the transport (`RestSharp/RestClient.cs:374-376`) and the transport does not set `UserAgent` or `AllowAutoRedirect` (`RestSharp/Http.Async.cs:364-398`). Silverlight registers the client HTTP stack for http/https prefixes (`RestSharp/Http.Async.cs:337-340`). `RestRequest(Uri)` deliberately avoids `Uri.PathAndQuery` because Silverlight lacks it (`RestSharp/RestRequest.cs:104-108`). |
| `WINDOWS_PHONE` | `UseSynchronizationContext` defaults to **true**, so async callbacks are marshalled to the UI context (`RestSharp/RestClient.cs:44-46`). `Content-Length` cannot be set, so the transport suppresses it (`RestSharp/Http.Async.cs:328-332`, `:350-357`, `:130-132`) and gzip responses are inflated with the vendored zlib (`RestSharp/Http.cs:327-331`). `SaveAs` is unavailable (`RestSharp/Extensions/MiscExtensions.cs:29-39`). |
| `MONOTOUCH` / `MONODROID` | Compiled with `FRAMEWORK`, so the full sync API is present, but `System.Web` is not imported and `HttpUtility` comes from the vendored Mono copy (`RestSharp/Http.Sync.cs:21-23`, `RestSharp/Extensions/StringExtensions.cs:36-38`). |
| `Net2` / `NET_2_0` | Uses the vendored `RestSharp.Contrib.HttpUtility` (`RestSharp/Extensions/StringExtensions.cs:25-27`), the vendored Mono `System.Xml.Linq` (`RestSharp.Net2/System.Xml.Linq/*.cs`), LinqBridge for LINQ-to-objects, and a distinct assembly-info block (`RestSharp/SharedAssemblyInfo.cs:10`). All LINQ used by `ConfigureHttp`/`BuildUri` (`Where`, `Any`, `FirstOrDefault`, query expressions) resolves to LinqBridge on this head; behavior is intended to match but it is a separate implementation. |
| `PocketPC` | No occurrences in this repository; no head defines it. |
| Non-`FRAMEWORK` header handling | Headers are assigned via `webRequest.Headers[name] = value` instead of `Headers.Add`, and cookies are added per scheme+host URI rather than by host domain — `RestSharp/Http.cs:227-232`, `:241-257`. |

---

## 7. Target-stack (.NET 8) concerns

| Legacy construct | Modern equivalent | Behavioral risk |
|---|---|---|
| `HttpWebRequest` per call via `IHttp`/`IHttpFactory` | `HttpClient` + `HttpRequestMessage` (ideally pooled/`IHttpClientFactory`) | `HttpClient` is designed to be shared; a per-execution instance exhausts sockets. Connection reuse, DNS refresh, and `ServicePointManager` tuning all change. |
| `ServicePointManager.Expect100Continue = false` set globally per request configuration (`RestSharp/Http.Sync.cs:187`, `RestSharp/Http.Async.cs:378`) | Per-request `HttpRequestHeaders.ExpectContinue` | Legacy mutates **global** process state; the modern equivalent is per-request. Servers relying on the absence of `Expect: 100-continue` may behave differently if not set explicitly. |
| `AutomaticDecompression = Deflate | GZip | None` (`RestSharp/Http.Sync.cs:200`) | `SocketsHttpHandler.AutomaticDecompression` | Default in .NET 8 is `None`; must be set explicitly or gzip bodies arrive compressed. Brotli is additionally available and changes the `Accept-Encoding` sent. |
| `AllowAutoRedirect` / `MaximumAutomaticRedirections` (`RestSharp/Http.Sync.cs:227-231`) | `HttpClientHandler.AllowAutoRedirect` / `MaxAutomaticRedirections` | `HttpClient` drops `Authorization` headers across redirects to a different host; `HttpWebRequest` behavior differs. Redirect method rewriting (302 + POST) also differs subtly. |
| `webRequest.ContentLength = 0` forced on bodyless requests (`RestSharp/Http.Sync.cs:195-198`) | No content ⇒ no `Content-Length` header | Legacy sends `Content-Length: 0` on GET/HEAD/DELETE; some APIs depend on its presence or absence. |
| `CookieContainer` propagated even when null (`RestSharp/RestClient.cs:352`) | `SocketsHttpHandler.CookieContainer` / `UseCookies` | Handler-level cookies are shared across requests once the handler is pooled; per-call cookie containers are no longer natural. |
| `IWebProxy Proxy` (sync only) | `HttpClientHandler.Proxy` | Proxy is a handler-level setting in .NET 8, so the current sync-only, per-call behavior cannot be reproduced literally; also `KeepAlive` no longer exists as a request property. |
| `Timeout` in ms on the request (`RestSharp/RestClient.cs:372`) | `HttpClient.Timeout` or per-request `CancellationToken` | `HttpWebRequest.Timeout` bounds only the response headers phase; `HttpClient.Timeout` bounds the whole operation including content read. Timeouts surface as `TaskCanceledException`/`TimeoutException` rather than `WebException` + `ResponseStatus.TimedOut`. |
| `IAsyncResult`/`BeginGetResponse` + `ThreadPool.RegisterWaitForSingleObject` timeout (`RestSharp/Http.Async.cs:88-89`, `:219-227`) | `async`/`await` + `CancellationTokenSource` | The APM callback model must become `Task`-based. `RestRequestAsyncHandle.Abort()` maps to cancellation; any consumer holding the raw `HttpWebRequest` field breaks. |
| Sync `Execute` implemented over sync transport calls | `HttpClient` has `Send` (sync) but async is idiomatic | Keeping a sync facade over `HttpClient` risks deadlocks in contexts with a synchronization context; `UseSynchronizationContext` has no direct `HttpClient` analogue (`ConfigureAwait` semantics instead). |
| `object.ToString()` for every parameter value (`RestSharp/RestClient.cs:299`, `:344`, `:423`) | Explicit `IFormattable.ToString(null, CultureInfo.InvariantCulture)` | **Numeric precision/rounding and decimal separators are culture-sensitive today.** `1.5` renders as `1,5` under e.g. `de-DE`, silently producing different querystrings. Also, .NET Core 3.0+ changed `double`/`float` `ToString()` to shortest round-trippable form, so numeric text can differ from .NET Framework output even under the invariant culture. |
| `DateTime` values stringified through `ToString()` | Explicit round-trip format (`"O"`) or `DateTimeOffset` | Today a `DateTime` parameter serializes in the current culture's short pattern and loses offset/kind. Migration should decide a single wire format; `DateTime.Kind` and `DateTimeOffset` conversion are behavior-visible. Note the deserializer contract carries a `DateFormat` string (`RestSharp/Deserializers/IDeserializer.cs:24`) that is set per request (`RestSharp/RestClient.cs:497`). |
| `Uri.EscapeDataString` (`RestSharp/Extensions/StringExtensions.cs:54-57`) | `Uri.EscapeDataString` (unchanged) / `UriHelper`-style builders | Escaping rules changed across framework versions for a handful of characters (e.g. `!`, `'`, `(`, `)`, `*`); URLs and OAuth signatures may differ byte-for-byte. |
| Case-sensitive dictionary lookups for content types and case-sensitive default-parameter matching (`RestSharp/RestClient.cs:201-208`, `:357`) | `StringComparer.OrdinalIgnoreCase` dictionaries | HTTP content types and header names are case-insensitive; the legacy behavior is a latent bug that a migration will likely "fix", changing observable behavior. |
| `ToLower()` / `ToUpper()` without culture in helper paths (`RestSharp/Extensions/StringExtensions.cs:270`, `:328-340`) | `ToLowerInvariant()` / `ToUpperInvariant()` | Turkish-I problem: `ToLower()` under `tr-TR` maps `I` to `ı`, changing name matching during deserialization. |
| UTF-8-only response decoding under `FRAMEWORK` (`RestSharp/Extensions/MiscExtensions.cs:115-116`) | Decode using the response `charset` | Non-UTF-8 responses are currently mojibake; fixing it changes `Content` for those responses. |
| Vendored zlib (`RestSharp/Compression/ZLib/*`), Mono `HttpUtility`/`HtmlEncoder`, Mono `System.Xml.Linq`, LinqBridge | BCL `GZipStream`/`BrotliStream`, `System.Net.WebUtility`, BCL `System.Xml.Linq`, BCL LINQ | Mostly behavior-preserving, but HTML/URL encoding edge cases and XML namespace handling can differ; `WebUtility.UrlEncode` (`+` for space) is **not** equivalent to `Uri.EscapeDataString` (`%20`). |
| Newtonsoft.Json 4.5 (`JsonSerializer`/`JsonDeserializer`) + `JToken` helpers (`RestSharp/Extensions/MiscExtensions.cs:81-100`) | `System.Text.Json` | Different default naming, null handling, date handling, and much stricter parsing; attribute model (`DeserializeAs`, `SerializeAs`) must be re-implemented. Belongs to the serializer specs but reaches this component through the handler registry. |
| Reflection over `Type.GetProperties()` in `AddObject` (`RestSharp/RestRequest.cs:244-272`) | Same API, but trimming/AOT-sensitive | Property ordering from reflection is unspecified and has changed between runtimes, which changes querystring ordering. Trimming/AOT requires annotations. |
| `Assembly.GetExecutingAssembly()` for the user-agent version (`RestSharp/RestClient.cs:61-65`) | `AssemblyName`/`FileVersionInfo`, or a constant | Single-file/AOT publishing can change or empty this value, altering the default `User-Agent`. |
| `X509CertificateCollection ClientCertificates` (`RestSharp/RestClient.cs:218-223`) | `SocketsHttpHandler.SslOptions.ClientCertificates` | Handler-level, not per-request; `X509Certificate2` lifetime/disposal rules are stricter. |

---

## 8. Gaps, stubs, dead code and discrepancies

1. **`Accept` default parameter accumulates without bound.** Every `Execute`/`ExecuteAsync` calls `AddDefaultParameter("Accept", …, HttpHeader)`, which appends to `DefaultParameters` unconditionally — `RestSharp/RestClient.Sync.cs:40-42`, `RestSharp/RestClient.Async.cs:38-40`, `RestSharp/RestClient.cs:93-101`. After N executions the client holds N `Accept` defaults. The request-side merge only de-duplicates against the *request*, not within the defaults list (`RestSharp/RestClient.cs:354-363`).
2. **Async path ignores `Proxy`.** `ConfigureProxy` is called only from the sync `GetResponse` — `RestSharp/RestClient.Sync.cs:79`, `:111-117`. `ExecuteAsync` never applies it — `RestSharp/RestClient.Async.cs:33-83`.
3. **Async path never sets `response.Request` nor increments `Attempts`.** Both happen only in `Execute` — `RestSharp/RestClient.Sync.cs:48-49`. The XML doc on `Attempts` claims it is incremented "each time the RestClient sends the request" and is "useful when using Asynchronous Execution with Callbacks" — `RestSharp/RestRequest.cs:456-462`. That is not what the code does.
4. **`Request` and `ErrorException` are lost by the `RestResponse → RestResponse<T>` cast.** The explicit operator copies 12 properties but omits `Request` and `ErrorException` — `RestSharp/RestResponse.cs:144-161`. Therefore `Execute<T>` returns a response whose `Request` is null even though `Execute` had set it, and a transport error's exception object disappears (its message survives).
5. **`GetHandler` can return null and the caller dereferences it unguarded.** `RestSharp/RestClient.cs:200-210` vs `:495-499`; the assignment sits outside the try/catch, so the `NullReferenceException` escapes `Execute<T>` rather than being converted to `ResponseStatus.Error`.
6. **`GetHandler` null-content-type guard is incomplete.** If the content type is null/empty and `*` is *not* registered, execution falls through to `contentType.IndexOf(';')` and throws — `RestSharp/RestClient.cs:193-199`.
7. **Handler instances are shared and mutated per request.** `RootElement`, `DateFormat`, `Namespace` are written onto the registered deserializer instance — `RestSharp/RestClient.cs:496-498` — making concurrent `Execute<T>` calls on one client racy.
8. **`EncodeParameters` uses `querystring.Length > 1` as its "not first" test.** `RestSharp/RestClient.cs:342-343` (and the same idiom in the transport, `RestSharp/Http.cs:266-267`). If the first parameter renders to a single character (empty name and empty value, i.e. `"="`), the `&` separator is omitted for the second parameter.
9. **`EncodeParameters` does not null-filter values, but `ConfigureHttp` does.** `RestSharp/RestClient.cs:344` vs `:417-419`. A `GetOrPost` parameter with a null value throws on a GET-style request and is silently dropped on a POST.
10. **`Method.PATCH` is supported in code but undocumented.** `RestSharp/Enum.cs:51`, `RestSharp/RestClient.Sync.cs:102-104`; the XML docs on `Method`/`IRestRequest.Method` list only "GET, POST, PUT, DELETE, HEAD, OPTIONS" (`RestSharp/RestRequest.cs:369`, `RestSharp/IRestRequest.cs:50-52`) and the README feature list omits PATCH (`README.markdown`).
11. **Unhandled method value produces a silent empty response.** The `switch` in `GetResponse` has no `default` branch, so an out-of-range `Method` yields the freshly constructed `HttpResponse` with `ResponseStatus.None` and no error — `RestSharp/RestClient.Sync.cs:81-108`. Same shape in the async dispatcher, where `webRequest` stays null — `RestSharp/RestClient.Async.cs:56-82`.
12. **`AuthenticateIfNeeded(RestClient client, IRestRequest request)` takes a client parameter it does not need**; it reads the instance field and passes the parameter through — `RestSharp/RestClient.cs:280-286`.
13. **`RestClient.AddDefaultParameter` XML doc says "QueryString for GET, DELETE, OPTIONS and HEAD; Encoded form for POST and PUT"** — `RestSharp/RestClient.cs:103-105`; PATCH is also treated as a body method (`RestSharp/RestClient.cs:319`). The `AddParameter` doc block lists "four types of parameters" while five exist — `RestSharp/RestClient.cs:115-121`, `RestSharp/RestRequest.cs:307-313`; `IRestRequest` says five — `RestSharp/IRestRequest.cs:188-195`.
14. **README example does not compile against this API.** `README.markdown` shows `request.AddFile(path)`; no single-argument overload exists — `RestSharp/RestRequest.cs:117-181`. The README also advertises "Supports .NET 3.5+…" for what is a .NET 2.0 fork.
15. **`UserState` is dead in this component.** Declared on `RestRequest` (`RestSharp/RestRequest.cs:435-439`) but absent from `IRestRequest` and never read by the pipeline.
16. **`Http` implements `IHttpFactory` on itself** (`RestSharp/Http.cs:35`, `:44-47`) in addition to `SimpleFactory<Http>` (`RestSharp/IHttpFactory.cs:8-14`) — two ways to create transports, one unused by the client.
17. **`RestSharp.Tests/Fakes/NullHttp.cs` uses `new` rather than `override`.** `RestSharp/Http.Sync.cs:53` is not virtual, so the fake's `Get()` is only seen through a `NullHttp`-typed reference; through `IHttp` the real implementation runs. The fake is not referenced by any test in `RestSharp.Tests` today.
18. **`RestSharp.Tests` and `RestSharp.IntegrationTests` use xUnit (`Xunit`, `[Fact]`)**, e.g. `RestSharp.Tests/UrlBuilderTests.cs:5,14`, `RestSharp.IntegrationTests/AsyncTests.cs:7,13` — not NUnit. There is **no unit-test coverage** for `ConfigureHttp`, default-parameter precedence, handler selection, `Execute<T>` error paths, `DownloadData`, or `RestRequestAsyncHandle.Abort()`; `UrlBuilderTests` covers only URL assembly and even its own header comment says querystring building is "handled in Http, not RestClient" (`RestSharp.Tests/UrlBuilderTests.cs:9-11`), which contradicts `RestSharp/RestClient.cs:329-330`.
19. **Global mutable state in the transport.** `ServicePointManager.Expect100Continue = false` is assigned on every request configuration — `RestSharp/Http.Sync.cs:187`, `RestSharp/Http.Async.cs:378` — affecting the whole process, including code outside RestSharp.
20. **`GetRawResponseAsync` can dereference null.** If `EndGetResponse` throws a `WebException` whose `Response` is not an `HttpWebResponse`, `raw` stays null and `raw.Close()` throws — `RestSharp/Http.Async.cs:252-279`.
21. **`ExecuteAsync<T>` deserializes error and timed-out responses.** Only `Aborted` is excluded — `RestSharp/RestClient.Async.cs:102-105` — so a timed-out response goes through handler selection with a null content type (see gap 6).
22. **`RestRequest.AddFile(string name, string path)` reads through a `StreamReader` only to use its `BaseStream`** and never sets `ContentLength` — `RestSharp/RestRequest.cs:117-131`; the async content-length calculation therefore counts 0 bytes for such files (`RestSharp/Http.Async.cs:144-167`).

---

## 9. Open questions for the human reviewer

1. **Client lifetime and `HttpClient` mapping.** Should one `RestClient` map to one pooled `HttpClient`/handler (best for sockets, but forces proxy, cookies, certificates, decompression and redirects to become client-level rather than per-call), or should per-request settings be preserved by constructing handlers per call (faithful, but costly)? A hybrid (handler pooled by settings tuple) is a third option.
2. **Accept-header accumulation (gap 1).** Preserve the legacy duplicate-accumulating behavior, or de-duplicate/compute `Accept` per execution without touching `DefaultParameters`? The latter changes `client.DefaultParameters` contents that callers can observe.
3. **Case sensitivity (gaps in §7).** Should default-parameter matching, header names, and content-type lookup become case-insensitive (correct per HTTP) even though this changes which parameter wins today?
4. **Culture-sensitive value formatting.** Should all parameter values be formatted with `CultureInfo.InvariantCulture` (recommended for wire formats, but changes output for callers currently running under non-invariant cultures), or keep `ToString()` semantics?
5. **`DateTime` on the wire.** Pick one: `"O"` round-trip, ISO-8601 seconds (`"s"`, the existing `DateFormat.Iso8601` constant in `RestSharp/Enum.cs:62`), RFC 1123, or caller-supplied format. Should `DateTimeOffset` be accepted and preserved?
6. **Null handling (gaps 9, and null `BaseUrl`).** Should null parameter values throw a clear `ArgumentNullException` at add time, be skipped uniformly, or be rendered as empty strings? Today the behavior differs by parameter type and by HTTP verb.
7. **Missing handler (gaps 5, 6).** When no deserializer matches, should `Execute<T>` throw a typed exception, return `ResponseStatus.Error` with a descriptive message, or keep the current `NullReferenceException`?
8. **Body vs form-parameter conflict (3.6).** When both `AddBody` and `GetOrPost` parameters are present on a POST/PUT/PATCH, should the migration keep silently discarding the body, throw, or send the body and move parameters to the querystring?
9. **Async surface shape.** Should the migrated API be `Task<RestResponse> ExecuteAsync(request, CancellationToken)` only (dropping callbacks and `RestRequestAsyncHandle` entirely), or should a callback-compatible shim remain for source compatibility? If the handle stays, `Abort()` must map to a `CancellationTokenSource` and the public `HttpWebRequest` field must be removed.
10. **Sync API retention.** Keep a synchronous `Execute`/`Execute<T>`/`DownloadData` facade (implemented over `HttpClient.Send`) for compatibility, or make the library async-only?
11. **`Request`/`ErrorException` loss in the generic cast (gap 4).** Fix the copy, or preserve the current (arguably broken) behavior for compatibility?
12. **Shared mutable deserializer instances (gap 7).** Should handlers be registered as factories (one instance per call) to make `RestClient` thread-safe, or is single-threaded use per client an acceptable documented constraint?
13. **PATCH and other verbs (gap 10).** Should the migrated client accept an arbitrary `HttpMethod` rather than a closed enum, and should the docs be corrected to include PATCH?
14. **Global `Expect100Continue` (gap 19).** Move to a per-request `ExpectContinue = false`, keep a global default, or expose it as a client setting?
15. **Test strategy.** The existing xUnit tests cover almost none of this component (gap 18). Should characterization tests be written against the legacy behavior *before* migration (recommended, and they would pin down the answers to questions 2-8), and should the integration tests' hard-coded `http://localhost:8080/` listener be replaced?
