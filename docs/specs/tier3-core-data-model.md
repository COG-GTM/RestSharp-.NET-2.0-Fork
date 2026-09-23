# Tier 3 Specification — Core Data Model

Reverse-engineered from the legacy C# 2.0 / .NET Framework fork of RestSharp. Read-only analysis; no source file was modified.

Scope: the data-transfer types and enumerations every other component depends on —
`RestResponse` / `RestResponse<T>` / `IRestResponse`, `RestResponseCookie`, `Parameter`, `FileParameter`, the enumerations in `Enum.cs`, and the transport-facing twins `HttpCookie`, `HttpHeader`, `HttpParameter`, `HttpFile`, `HttpResponse` / `IHttpResponse` (data shape and mapping only — the transport that produces them is specified elsewhere).

---

## 1. Purpose

The core data model is the vocabulary the library speaks in. It defines three things in business terms:

1. **What a caller asks for** — a request is expressed as a bag of named `Parameter` values, each tagged with a *role* (`ParameterType`: query/form value, URL token, HTTP header, cookie, or request body), plus a list of `FileParameter` attachments for multi-part uploads (`RestSharp/Parameter.cs:22-44`, `RestSharp/FileParameter.cs:9-68`, `RestSharp/Enum.cs:22-29`).
2. **What a caller gets back** — a `RestResponse` carries the raw bytes, the decoded string content, the HTTP status, the headers and cookies the server returned, and — separately from the HTTP status — a *transport outcome* (`ResponseStatus`) plus an error message/exception when the call never produced a real HTTP answer (`RestSharp/RestResponse.cs:27-131`). `RestResponse<T>` adds the deserialized business object (`RestSharp/RestResponse.cs:137-162`).
3. **What the transport layer speaks** — a parallel, deliberately dumber set of types (`HttpParameter`, `HttpHeader`, `HttpCookie`, `HttpFile`, `HttpResponse`) in which every value is already a string. `RestClient` translates between the two families (`RestSharp/RestClient.cs — ConfigureHttp()` at `RestSharp/RestClient.cs:350-445`, and `RestSharp/RestClient.cs — ConvertToRestResponse()` at `RestSharp/RestClient.cs:447-489`).

**Consumers:** application code calling `RestClient.Execute` / `Execute<T>` / `ExecuteAsync`; the serializers and deserializers (which consume `IRestResponse.Content` and honour `DateFormat`); the authenticators (which add `Parameter`s of various types); the transport (`Http`), which consumes only the `Http*` family.

The two families exist because the translation is where policy lives: role-based routing of parameters, precedence of client defaults vs. request values, and the conversion of arbitrary .NET values to strings.

---

## 2. Inputs and outputs

### 2.1 `Parameter` — the request-side unit of input (`RestSharp/Parameter.cs:22-44`)

| Field | Type | Default | Required | Meaning |
|---|---|---|---|---|
| `Name` | `string` | `null` | Yes in practice | Parameter name. For `RequestBody` it is overloaded to carry the **content type**, not a name (`RestSharp/RestRequest.cs:219-222`). |
| `Value` | `object` | `null` | Effectively yes | Arbitrary value; converted with `ToString()` at translation time. |
| `Type` | `ParameterType` | `Cookie` (enum value 0, i.e. the default of an uninitialised field) | Yes | The role the parameter plays. Note: the CLR default is `Cookie`, not `GetOrPost`; all library factory methods set it explicitly (`RestSharp/Enum.cs:22-29`, `RestSharp/RestRequest.cs:302-321`). |

`ToString()` returns `"{Name}={Value}"` via `string.Format` — current-culture, no encoding, no escaping; intended for human/debug display only (`RestSharp/Parameter.cs:41-43`).

`Parameter` is reused on the **response** side as the element type of `IRestResponse.Headers`, where every entry is created with `Type = ParameterType.HttpHeader` (`RestSharp/RestClient.cs:463-466`).

### 2.2 `FileParameter` — attachment input (`RestSharp/FileParameter.cs:9-68`)

| Field | Type | Default | Required | Meaning |
|---|---|---|---|---|
| `Name` | `string` | `null` | Yes | Multi-part form field name. |
| `FileName` | `string` | `null` | Yes | File name advertised in the `Content-Disposition` header. |
| `ContentType` | `string` | `null` | No | MIME type; when null the transport substitutes `application/octet-stream` (`RestSharp/Http.cs:196-200`). |
| `ContentLength` | `long` | `0` | Only for async | Byte count of what `Writer` will emit. Used solely to pre-compute the request `Content-Length` on the asynchronous path (`RestSharp/Http.Async.cs:144-167`). |
| `Writer` | `Action<Stream>` | `null` | Yes | Callback that writes the file bytes into the request stream. Documented contract: it must **not** close the stream (`RestSharp/RestRequest.cs:162`, `RestSharp/RestRequest.cs:174`). |

Factory methods: `FileParameter.Create(name, data, filename, contentType)` and `Create(name, data, filename)` (the latter passes `contentType = null`) (`RestSharp/FileParameter.cs:19-46`).

### 2.3 Enumerations (`RestSharp/Enum.cs`)

| Enum | Members (in declaration order) | Default member (value 0) | Notes |
|---|---|---|---|
| `ParameterType` | `Cookie`, `GetOrPost`, `UrlSegment`, `HttpHeader`, `RequestBody` | `Cookie` | Ordering matters because the CLR default is `Cookie` (`RestSharp/Enum.cs:22-29`). |
| `DataFormat` | `Json`, `Xml` | `Json` | `RestRequest.RequestFormat` explicitly initialises to `Xml`, overriding the CLR default (`RestSharp/RestRequest.cs:392`). |
| `Method` | `GET`, `POST`, `PUT`, `DELETE`, `HEAD`, `OPTIONS`, `PATCH` | `GET` | `RestRequest` also explicitly defaults to `GET` (`RestSharp/RestRequest.cs:367`). |
| `ResponseStatus` | `None`, `Completed`, `Error`, `TimedOut`, `Aborted` | `None` | Transport outcome, distinct from HTTP status (`RestSharp/Enum.cs:72-79`). |
| `DateFormat` | **Not an enum** — a `struct` holding two `const string`s: `Iso8601 = "s"`, `RoundTrip = "u"` | n/a | These are .NET format specifiers, not ISO strings themselves (`RestSharp/Enum.cs:57-67`). |

### 2.4 `IRestResponse` / `RestResponseBase` — response output (`RestSharp/IRestResponse.cs:10-90`, `RestSharp/RestResponse.cs:27-131`)

| Member | Type | Default | Meaning |
|---|---|---|---|
| `Request` | `IRestRequest` | `null` | The request that produced this response; set only on the synchronous path (`RestSharp/RestClient.Sync.cs:48`). |
| `ContentType` | `string` | `null` | MIME type reported by the server. Drives deserializer selection (`RestSharp/RestClient.cs:495`). |
| `ContentLength` | `long` | `0` | Server-reported length; copied verbatim, never recomputed from `RawBytes` (`RestSharp/RestClient.cs:452`). |
| `ContentEncoding` | `string` | `null` | Server-reported content encoding; `FRAMEWORK`-only at the source (`RestSharp/Http.cs:321-324`). |
| `Content` | `string` | lazily derived | Getter: if the backing field is null, decode `RawBytes` via `AsString()`; setter overrides (`RestSharp/RestResponse.cs:61-75`). |
| `StatusCode` | `HttpStatusCode` | `0` | HTTP status. `0` is not a valid `HttpStatusCode`; it is what you observe when the call failed before a response arrived. |
| `StatusDescription` | `string` | `null` | HTTP reason phrase. |
| `RawBytes` | `byte[]` | `null` | Full response body as bytes. |
| `ResponseUri` | `Uri` | `null` | Final URL after redirects. |
| `Server` | `string` | `null` | `Server` response header (`FRAMEWORK`-only at the source). |
| `Cookies` | `IList<RestResponseCookie>` | empty list | Constructed eagerly; setter is `protected` (`RestSharp/RestResponse.cs:34-38`, `:99`). |
| `Headers` | `IList<Parameter>` | empty list | Same; each element tagged `ParameterType.HttpHeader` (`RestSharp/RestResponse.cs:103`, `RestSharp/RestClient.cs:465`). |
| `ResponseStatus` | `ResponseStatus` | `None` | Explicitly initialised to `None` by a backing field (`RestSharp/RestResponse.cs:105`). |
| `ErrorMessage` | `string` | `null` | Transport/deserialization error text. |
| `ErrorException` | `Exception` | `null` | The exception itself. Declared on `IRestResponse` (`RestSharp/IRestResponse.cs:89`). |
| `Data` (on `IRestResponse<T>` / `RestResponse<T>`) | `T` | `default(T)` | Deserialized entity (`RestSharp/RestResponse.cs:142`). |

Note that `Headers` and `Cookies` are exposed on `IRestResponse` as **get-only** (`RestSharp/IRestResponse.cs:68`, `:73`) but are settable from inside `RestResponse<T>` because the base declares `protected set` (`RestSharp/RestResponse.cs:99-103`).

### 2.5 `RestResponseCookie` (`RestSharp/RestResponseCookie.cs:4-62`)

Fields, all public get/set, all CLR defaults: `Comment` (`string`), `CommentUri` (`Uri`), `Discard` (`bool`), `Domain` (`string`), `Expired` (`bool`), `Expires` (`DateTime`, default `DateTime.MinValue`), `HttpOnly` (`bool`), `Name` (`string`), `Path` (`string`), `Port` (`string`), `Secure` (`bool`), `TimeStamp` (`DateTime`), `Value` (`string`), `Version` (`int`).

`HttpCookie` (`RestSharp/HttpCookie.cs:8-66`) declares **exactly the same fourteen members with the same types and XML comments**. The two classes are field-for-field identical; the only difference is which layer owns them.

### 2.6 Transport-facing twins

| Type | Fields | Maps from / to |
|---|---|---|
| `HttpHeader` | `Name: string`, `Value: string` (`RestSharp/HttpHeader.cs:6-16`) | Built from request `Parameter`s of type `HttpHeader` (`RestSharp/RestClient.cs:391-402`); converted back into response `Parameter`s (`RestSharp/RestClient.cs:463-466`). |
| `HttpParameter` | `Name: string`, `Value: string` (`RestSharp/HttpParameter.cs:11-21`) | Built from request `Parameter`s of type `GetOrPost` (`RestSharp/RestClient.cs:417-429`). Request-side only; has no response counterpart. |
| `HttpCookie` | 14 fields, identical to `RestResponseCookie` (`RestSharp/HttpCookie.cs:8-66`) | Request direction: only `Name`/`Value` are populated from `Parameter`s of type `Cookie` (`RestSharp/RestClient.cs:404-415`). Response direction: all 14 fields are copied one-for-one into `RestResponseCookie` (`RestSharp/RestClient.cs:468-486`). |
| `HttpFile` | `ContentLength: long`, `Writer: Action<Stream>`, `FileName: string`, `ContentType: string`, `Name: string` (`RestSharp/HttpFile.cs:9-32`) | Field-for-field copy of `FileParameter` (`RestSharp/RestClient.cs:431-434`). The two classes are structurally identical. |
| `HttpResponse` / `IHttpResponse` | Same shape as `RestResponseBase` minus `Request`, with `Headers: IList<HttpHeader>` and `Cookies: IList<HttpCookie>`, and with `Content` **read-only** (`RestSharp/HttpResponse.cs:27-121`, `RestSharp/IHttpResponse.cs:10-82`) | Mapped into `RestResponse` field-by-field (`RestSharp/RestClient.cs:447-489`). |

---

## 3. Behavioral rules

### 3.1 Response content decoding

| # | Rule | Citation |
|---|---|---|
| R1 | `Content` is lazily computed: the getter decodes `RawBytes` only if the backing field is still `null`, then caches the result. | `RestSharp/RestResponse.cs:61-75` |
| R2 | Setting `Content` explicitly suppresses decoding entirely; `RawBytes` is then never consulted. This is how the unit tests inject content. | `RestSharp/RestResponse.cs:71-74`; `RestSharp.Tests/JsonTests.cs:139` (`new RestResponse { Content = data }`); `RestSharp.Tests/XmlTests.cs:277` |
| R3 | A `null` `RawBytes` decodes to the empty string, never to `null` and never throws. | `RestSharp/Extensions/MiscExtensions.cs:110` |
| R4 | `HttpResponse.Content` has **no setter** — it is get-only on both the class and the interface — so the lazy decode is the only path there. | `RestSharp/HttpResponse.cs:55-65`, `RestSharp/IHttpResponse.cs:30` |
| R5 | Caching is one-shot and not invalidated: assigning `RawBytes` after `Content` has been read returns the stale string. | `RestSharp/RestResponse.cs:61-75` |

### 3.2 `RestResponse` → `RestResponse<T>` conversion

An explicit conversion operator copies twelve members: `ContentEncoding`, `ContentLength`, `ContentType`, `Cookies`, `ErrorMessage`, `Headers`, `RawBytes`, `ResponseStatus`, `ResponseUri`, `Server`, `StatusCode`, `StatusDescription` (`RestSharp/RestResponse.cs:144-161`).

| # | Rule | Citation |
|---|---|---|
| R6 | **`Content` is not copied.** Because `RawBytes` *is* copied, the typed response re-derives `Content` from the bytes on first access. When the source response's `Content` had been set explicitly and `RawBytes` is null, the typed response's `Content` becomes `""`, not the original string. | `RestSharp/RestResponse.cs:144-161` vs `:61-75` |
| R7 | **`ErrorException` is not copied.** A transport failure that populated `ErrorException` on the untyped response loses the exception object on conversion; only `ErrorMessage` survives. | `RestSharp/RestResponse.cs:144-161` |
| R8 | **`Request` is not copied**, so `RestResponse<T>.Request` is always `null` even on the synchronous path where the untyped response had it set. | `RestSharp/RestResponse.cs:144-161`, `RestSharp/RestClient.Sync.cs:48` |
| R9 | `Cookies` and `Headers` are copied **by reference** — the typed and untyped responses share the same list instances. | `RestSharp/RestResponse.cs:150`, `:153` |
| R10 | The conversion is a cast on a *concrete* `RestResponse`; it is not defined for `IRestResponse`. | `RestSharp/RestResponse.cs:144` |

### 3.3 Ordering and precedence when a request is translated (`RestSharp/RestClient.cs — ConfigureHttp()`)

Order of operations, which matters because later steps read state written by earlier ones:

1. Cookie container assigned (`RestSharp/RestClient.cs:352`).
2. **Client `DefaultParameters` are merged into the request**, skipping any whose `(Name, Type)` pair already exists on the request — i.e. **request-level parameters win over client defaults**, and the merge mutates the caller's request object (`RestSharp/RestClient.cs:354-363`).
3. URL is built — and therefore built *after* the defaults merge, so default `UrlSegment` and `GetOrPost` parameters participate in URL construction (`RestSharp/RestClient.cs:365`, `:293-335`).
4. User agent applied only if non-empty (`RestSharp/RestClient.cs:367-370`).
5. Timeout: `request.Timeout` wins unless it is exactly `0`, in which case the client's `Timeout` is used. `0` is therefore not expressible as "no timeout at request level" (`RestSharp/RestClient.cs:372`).
6. Redirect/certificate/proxy settings (conditional, see §6).
7. Parameters are fanned out by `Type` into four separate collections: headers, cookies, GetOrPost, and body (`RestSharp/RestClient.cs:391-444`).

| # | Rule | Citation |
|---|---|---|
| R11 | `UrlSegment` parameters are substituted into `Resource` by replacing the literal token `{Name}`, with the value URL-encoded. Unmatched tokens are left in the URL as-is; unmatched parameters are silently ignored. | `RestSharp/RestClient.cs:296-300`; tested in `RestSharp.Tests/UrlBuilderTests.cs` |
| R12 | `GetOrPost` parameters become query string **only** when the method is not POST, PUT or PATCH; otherwise they are handed to the transport as form parameters. | `RestSharp/RestClient.cs:319-332`, `:417-429` |
| R13 | Only `GetOrPost` parameters with a non-null `Value` are forwarded to the transport. Header, cookie and URL-segment parameters have **no** null guard and will throw on a null value. | `RestSharp/RestClient.cs:417-424` vs `:391-415`, `:299` |
| R14 | At most **one** `RequestBody` parameter is honoured — `FirstOrDefault()` silently discards the rest. | `RestSharp/RestClient.cs:436-444` |
| R15 | For the body parameter, `Name` is used as the **content type** and `Value` as the payload — an explicit, documented hack. | `RestSharp/RestRequest.cs:219-222`, `RestSharp/RestClient.cs:442-443` |
| R16 | `RequestBody` may not be added as a client default; attempting it throws. | `RestSharp/RestClient.cs:95-98` |
| R17 | Every forwarded value is stringified with `Value.ToString()` — current-culture, no format specifier. | `RestSharp/RestClient.cs:396`, `:409`, `:423`, `:442`, `:299`, `:344` |
| R18 | `FileParameter` → `HttpFile` is a straight five-field copy, including `ContentLength`. | `RestSharp/RestClient.cs:431-434` |
| R19 | The `Accept` header is added as a **client default** on every execute call, accumulating a duplicate entry on the client's `DefaultParameters` list each time (the dedupe in step 2 protects the request, not the client's own list). | `RestSharp/RestClient.Sync.cs:41-42`, `RestSharp/RestClient.Async.cs:38-40` |

### 3.4 Response translation (`RestSharp/RestClient.cs — ConvertToRestResponse()`)

| # | Rule | Citation |
|---|---|---|
| R20 | Twelve scalar members are copied verbatim from `HttpResponse`, including `ErrorException` (unlike the `RestResponse<T>` cast). | `RestSharp/RestClient.cs:449-461` |
| R21 | `Content` is copied **eagerly** here — reading `httpResponse.Content` forces the lazy decode on the transport object, and the decoded string is then assigned to the Rest-level response. Both objects therefore hold their own copy of the string. | `RestSharp/RestClient.cs:450` |
| R22 | Every response header becomes a `Parameter` with `Type = HttpHeader`; duplicate header names are preserved as separate entries because the transport iterates `AllKeys` and the list is append-only. | `RestSharp/RestClient.cs:463-466`, `RestSharp/Http.cs:364-368` |
| R23 | All fourteen cookie fields round-trip `Cookie` → `HttpCookie` → `RestResponseCookie` without transformation. | `RestSharp/Http.cs:341-362`, `RestSharp/RestClient.cs:468-486` |
| R24 | `Request` is attached after the fact, only on the synchronous path, and the attempt counter is incremented at the same point. Asynchronous responses have `Request == null`. | `RestSharp/RestClient.Sync.cs:47-50` vs `RestSharp/RestClient.Async.cs:85-89` |

### 3.5 `ResponseStatus` vs `HttpStatusCode`

The two are orthogonal and the distinction is the single most load-bearing rule in this component. `ResponseStatus` answers *"did we get an HTTP reply at all?"*; `StatusCode` answers *"what did the server say?"* (`RestSharp/IRestResponse.cs:75-79`).

| Situation | `ResponseStatus` | `StatusCode` | Citation |
|---|---|---|---|
| Object just constructed, never executed | `None` | `0` | `RestSharp/RestResponse.cs:105`, `RestSharp/HttpResponse.cs:95` |
| Any HTTP reply received, including 4xx/5xx | `Completed` | actual code | `RestSharp/Http.cs:336-339`; `RestSharp.IntegrationTests/StatusCodeTests.cs:10-22` asserts a 404 surfaces as `StatusCode == NotFound` |
| A `WebException` that still carries an `HttpWebResponse` (i.e. the server answered with an error status) | `Completed` | actual code | `RestSharp/Http.Sync.cs:138-152` |
| DNS failure, connection refused, protocol error with no response | `Error` | `0` | `RestSharp/Http.Sync.cs:128-133`; `RestSharp.IntegrationTests/StatusCodeTests.cs:29-36` asserts `ResponseStatus.Error` for a non-existent domain |
| Async timeout fires (request aborted by the timeout watchdog) | `TimedOut` | `0` | `RestSharp/Http.Async.cs:173-178`, `:196-202`, `:287-292` |
| Caller aborts via `RestRequestAsyncHandle.Abort()` | `Aborted` | `0` | `RestSharp/Http.Async.cs:300-306`, `RestSharp/RestRequestAsyncHandle.cs:18-22` |
| Deserialization of a successfully-received body fails | overwritten to `Error` | unchanged (still the real code) | `RestSharp/RestClient.cs:506-511` |

`TimedOut` is only ever produced on the asynchronous path; the synchronous path maps a timeout into the generic `Error` bucket via the `WebException` handler (`RestSharp/Http.Sync.cs:128-133` vs `RestSharp/Http.Async.cs:173-178`). `Aborted` likewise has no synchronous producer.

### 3.6 When `ErrorMessage` / `ErrorException` are populated

| Producer | Sets | Citation |
|---|---|---|
| Synchronous transport catch-all | `ErrorMessage = ex.Message`, `ErrorException = ex`, `ResponseStatus = Error` | `RestSharp/Http.Sync.cs:128-133` |
| Async request-setup / request-stream / response-callback failures | same triple | `RestSharp/Http.Async.cs:91-98`, `:204-214`, `:307-314` |
| `RestClient.Execute` outer catch (failures in `ConfigureHttp`, URL building, etc.) | same triple, on a **fresh** `RestResponse` that has no content, no status and no `Request` | `RestSharp/RestClient.Sync.cs:44-57` |
| Deserialization failure | same triple, overwriting any prior `ResponseStatus` | `RestSharp/RestClient.cs:506-511` |
| Async timeout and abort paths | **Neither** is set — the response carries only `ResponseStatus` | `RestSharp/Http.Async.cs:175`, `:199`, `:289`, `:304` |

So: a populated `ErrorException` always implies `ResponseStatus == Error`, but `ResponseStatus != Completed` does **not** imply a populated `ErrorException`.

---

## 4. Validation and error behavior

**The data-model types validate nothing.** Every property is a plain auto-property with no guards, no null checks, no range checks, and no invariants. There are no constructors that require values, so any instance can be fully default-constructed and left incomplete.

| Concern | Actual behavior | Citation |
|---|---|---|
| Null `Parameter.Value` for a header/cookie/url-segment | `NullReferenceException` inside the LINQ projection or the URL builder; surfaces to the caller of `Execute` as a `RestResponse` with `ResponseStatus.Error` because of the outer catch, not as a thrown exception | `RestSharp/RestClient.cs:396`, `:409`, `:299`; `RestSharp/RestClient.Sync.cs:52-57` |
| Null `Parameter.Value` for `GetOrPost` | Silently filtered out | `RestSharp/RestClient.cs:417-419` |
| Null `Parameter.Name` | Not validated; propagates into the URL/header as `null` | `RestSharp/RestClient.cs:391-402` |
| Null `FileParameter.Writer` | Not validated; `NullReferenceException` when the transport invokes it | `RestSharp/Http.cs:310` |
| `FileParameter.ContentLength` inconsistent with what `Writer` emits | Not validated. On the async path a wrong value yields a wrong `Content-Length` header and a malformed request | `RestSharp/Http.Async.cs:144-167` |
| Null `FileParameter.ContentType` | Tolerated; defaults to `application/octet-stream` at write time | `RestSharp/Http.cs:196-200` |
| Adding `RequestBody` as a client default | `NotSupportedException("Cannot set request body from default headers. Use Request.AddBody() instead.")` — the only exception thrown by choice anywhere in this component's translation path | `RestSharp/RestClient.cs:95-98` |
| More than one `RequestBody` parameter | Silently ignored beyond the first | `RestSharp/RestClient.cs:436-438` |
| Unknown `Method` value in the switch | No `default` branch; the response object stays the freshly-constructed `HttpResponse` with `ResponseStatus.None` and `StatusCode == 0` | `RestSharp/RestClient.Sync.cs:81-105` |
| Byte-array decoding of a malformed payload | Never throws; a null buffer yields `""` | `RestSharp/Extensions/MiscExtensions.cs:108-152` |
| Deserialization failure | Swallowed into the response object (`Error` + message + exception); never rethrown | `RestSharp/RestClient.cs:501-511` |
| Async `WebException` with status `RequestCanceled` during request-stream write | Swallowed and converted to `TimedOut`; **no callback is invoked for other `WebException`s on that path** (the `catch (WebException)` block falls through without calling back) | `RestSharp/Http.Async.cs:195-203` |

**Restartability / retry:** the model supports retry only in the weakest sense. `IRestRequest` carries an `Attempts` counter incremented once per synchronous execution (`RestSharp/RestRequest.cs:446-466`, `RestSharp/RestClient.Sync.cs:49`); the async path never increments it. Nothing in the data model is reset between attempts, and because `ConfigureHttp` *mutates* the request by appending client defaults (`RestSharp/RestClient.cs:354-363`), re-executing the same `RestRequest` object is not a clean idempotent operation. A `FileParameter.Writer` that consumes a forward-only stream cannot be replayed on a retry — nothing in the contract requires the writer to be re-invocable.

---

## 5. Core computations

| Computation | Exact rule | Citation |
|---|---|---|
| `Parameter.ToString()` | `string.Format("{0}={1}", Name, Value)` — current culture, no encoding, no null handling (a null `Name`/`Value` renders as empty) | `RestSharp/Parameter.cs:41-43` |
| Value stringification | `object.ToString()` with the **current culture**. A `decimal` `19.95` renders as `"19,95"` under a comma-decimal locale; a `DateTime` renders in the current culture's short pattern | `RestSharp/RestClient.cs:299`, `:344`, `:396`, `:409`, `:423`, `:442` |
| URL encoding | `Uri.EscapeDataString(input)` — RFC 3986 percent-encoding; note it does **not** encode spaces as `+` | `RestSharp/Extensions/StringExtensions.cs:54-57` |
| Query string assembly | `"{name}={value}"` joined with `&`, each side URL-encoded; the separator is appended when the builder length is `> 1` | `RestSharp/RestClient.cs:337-348` |
| URL assembly | `{BaseUrl}/{Resource}`; a trailing `/` is stripped from `BaseUrl` at assignment time, a leading `/` is stripped from the resource, and a trailing `/` is stripped again before a query string is appended | `RestSharp/RestClient.cs:264-278`, `:293-335`; `RestSharp.Tests/UrlBuilderTests.cs:14-61` |
| Byte → string decoding | Under `FRAMEWORK`: unconditional `Encoding.UTF8.GetString(buffer)`, ignoring any byte-order mark and ignoring the server-declared charset. Under non-`FRAMEWORK`: BOM sniffing (UTF-8 / UTF-16LE / UTF-16BE) then a `StreamReader` | `RestSharp/Extensions/MiscExtensions.cs:108-152` |
| Multi-part boundary | Fixed constant `-----------------------------28947758029299`; content type `multipart/form-data; boundary={boundary}` | `RestSharp/Http.cs:190-194` |
| Multi-part file part | `--{boundary}\r\nContent-Disposition: form-data; name="{Name}"; filename="{FileName}"\r\nContent-Type: {ContentType ?? "application/octet-stream"}\r\n\r\n` followed by the writer's bytes and a `\r\n` | `RestSharp/Http.cs:196-200`, `:304-312` |
| Async `Content-Length` for multi-part | Σ over files of (UTF-8 byte count of the file header + `FileParameter.ContentLength` + 2) + Σ over parameters of the UTF-8 byte count of each form part + byte count of the footer | `RestSharp/Http.Async.cs:144-167` |
| `FileParameter.Create` length | `data.LongLength` under `FRAMEWORK`, `(long)data.Length` otherwise — identical results for arrays below 2³¹ elements | `RestSharp/FileParameter.cs:21-25` |
| `DateFormat` constants | `Iso8601 = "s"` → `yyyy-MM-ddTHH:mm:ss` (no timezone, no fractional seconds); `RoundTrip = "u"` → `yyyy-MM-dd HH:mm:ssZ` (a literal `Z`, with no conversion to UTC) | `RestSharp/Enum.cs:57-67`; serializer usage at `RestSharp/Serializers/XmlSerializer.cs:168-171`; test `RestSharp.Tests/SerializerTests.cs:71-86` |
| `DateFormat` on the read path | When a request sets `DateFormat`, deserializers call `DateTime.ParseExact(value, DateFormat, Culture)` — exact match required, culture explicit | `RestSharp/RestClient.cs:497`, `RestSharp/Deserializers/JsonDeserializer.cs:134-137`, `RestSharp/Deserializers/XmlDeserializer.cs:161-163`; tests `RestSharp.Tests/JsonTests.cs:126-146`, `RestSharp.Tests/XmlTests.cs:257-281` |
| Serializer date formatting | `((DateTime)obj).ToString(DateFormat)` — **no culture argument**, i.e. current culture | `RestSharp/Serializers/XmlSerializer.cs:168-171` |
| Casing in name matching | Deserializer name candidates are generated with `ToLower(culture)` using an explicit `CultureInfo` | `RestSharp/Extensions/StringExtensions.cs:328-340` |

---

## 6. Per-target conditional behavior

The `RestSharp.Net2` head compiles with `Net2;FRAMEWORK;NET_2_0` and links the shared sources (`RestSharp.Net2/RestSharp.Net2.csproj:22`, `:30`, `:158-249`). Other heads: `FRAMEWORK` (desktop), `SILVERLIGHT`, `WINDOWS_PHONE` (+`MANGO`), `MONOTOUCH;FRAMEWORK`, `MONODROID;FRAMEWORK` (`RestSharp/RestSharp.csproj:41`, `RestSharp.Silverlight/RestSharp.Silverlight.csproj:32`, `RestSharp.WindowsPhone/RestSharp.WindowsPhone.csproj:27`, `RestSharp.WindowsPhone.Mango/RestSharp.WindowsPhone.Mango.csproj:27`, `RestSharp.MonoTouch/RestSharp.MonoTouch.csproj:22`, `RestSharp.MonoDroid/RestSharp.MonoDroid.csproj:22`).

**All six data-model source files are linked into every head** — `Enum.cs`, `Parameter.cs`, `FileParameter.cs`, `HttpCookie.cs`, `HttpFile.cs`, `HttpHeader.cs`, `RestResponseCookie.cs` and the response types are present identically everywhere (verified against each head's `.csproj`). The differences are therefore not in the shapes but in the values that reach them.

| Construct | Under `FRAMEWORK` (incl. `NET_2_0`, MonoTouch, MonoDroid) | Under `SILVERLIGHT` / `WINDOWS_PHONE` | Citation |
|---|---|---|---|
| `FileParameter.Create` length | `data.LongLength` | `(long)data.Length` | `RestSharp/FileParameter.cs:21-25` |
| `byte[].AsString()` | UTF-8, BOM ignored | BOM sniffing via `StreamReader` | `RestSharp/Extensions/MiscExtensions.cs:115-151` |
| `IRestResponse.ContentEncoding` | Populated from `HttpWebResponse.ContentEncoding` | **Never populated** — stays `null` | `RestSharp/Http.cs:321-324` |
| `IRestResponse.Server` | Populated | **Never populated** — stays `null` | `RestSharp/Http.cs:321-324` |
| Response body under gzip | Handled by `AutomaticDecompression` at the transport level | `WINDOWS_PHONE` decompresses manually via the vendored zlib `GZipStream` before filling `RawBytes` | `RestSharp/Http.cs:26-28`, `:327-334` |
| `byte[].SaveAs(path)` | Available | **Absent** under `WINDOWS_PHONE` | `RestSharp/Extensions/MiscExtensions.cs:29-39` |
| Redirect control reaching the model | `FollowRedirects` + `MaxRedirects` applied (`MaxRedirects` is `FRAMEWORK`-only) | `FollowRedirects` not applied under `SILVERLIGHT`; `MaxRedirects` absent | `RestSharp/RestClient.cs:374-384`, `RestSharp/Http.cs:113-128` |
| Client certificates | Present | Absent | `RestSharp/RestClient.cs:218-223` |
| Timeout → `TimedOut` status | Watchdog registered, so `ResponseStatus.TimedOut` is reachable | `SetTimeout` compiles to a no-op outside `FRAMEWORK`, so `TimedOut` is **unreachable** on those heads | `RestSharp/Http.Async.cs:219-227` |
| Async `Content-Length` pre-computation | Performed | Skipped under `WINDOWS_PHONE`; `Content-Length` handled indirectly | `RestSharp/Http.Async.cs:128-134`, `:317-330` |
| Cookie attachment | Cookie gets `Domain = RequestUri.Host` | Cookie added against a scheme+host `Uri` instead | `RestSharp/Http.cs:241-257` |
| Synchronous API (`Execute`, `DownloadData`, `Proxy`) | Available | Entire file excluded — async only | `RestSharp/RestClient.Sync.cs:1`, `:120` |
| `HttpUtility` source | Mono-vendored under `MONOTOUCH`/`MONODROID`; BCL `System.Web` otherwise | n/a | `RestSharp/Http.Sync.cs:21-23` |
| `UseSynchronizationContext` | Defaults to `false` | Defaults to `true` under `WINDOWS_PHONE` | `RestSharp/RestClient.cs:44-46` |

**`PocketPC` / Windows CE:** no such symbol exists anywhere in the RestSharp sources (verified by full-tree search). Any `PocketPC` conditional in the solution belongs to vendored third-party code, not to this component.

---

## 7. Target-stack (.NET 8) concerns

| Legacy construct | Modern equivalent | Behavioral risk |
|---|---|---|
| `Parameter.Value` as `object` + `ToString()` | Keep `object?`, or introduce a typed/`IFormattable` contract | **Culture-sensitive stringification.** Today a `decimal`/`double`/`DateTime` parameter is formatted with the ambient culture (`RestSharp/RestClient.cs:423`). Any move to invariant formatting changes the bytes on the wire for non-English hosts — arguably a bug fix, definitely a behavior change that some server may depend on. |
| `DateFormat.Iso8601 = "s"`, `RoundTrip = "u"` | `DateTimeOffset` + `"O"` / ISO-8601 round-trip | `"s"` drops the offset and fractional seconds entirely; `"u"` appends a literal `Z` **without converting to UTC**, so a local-time value is mislabelled as UTC (`RestSharp/Enum.cs:57-67`). Switching to `"O"`/`DateTimeOffset` changes emitted strings and fixes the mislabelling — both are visible to servers and to `ParseExact` on the read path. |
| `DateTime` throughout (`RestResponseCookie.Expires`, `TimeStamp`) | `DateTimeOffset` | `DateTime` carries a `Kind` that is lost across serialization; cookie expiry comparisons silently depend on the machine's timezone. `default(DateTime)` (`0001-01-01`) is the "no value" sentinel and is indistinguishable from a real value. |
| `((DateTime)obj).ToString(DateFormat)` with no culture | `ToString(format, CultureInfo.InvariantCulture)` | Current-culture calendars (e.g. Hijri, Thai Buddhist) produce non-ISO output today (`RestSharp/Serializers/XmlSerializer.cs:168-171`). |
| `long ContentLength` copied from the server | `HttpContentHeaders.ContentLength` (`long?`) | `HttpClient` reports `null` when the server omits the header and when the body is chunked; today the field is whatever `HttpWebResponse.ContentLength` said, which is `-1` in those cases (`RestSharp/Http.cs:326`). Consumers testing `ContentLength == 0` or `> 0` will behave differently. |
| `Encoding.UTF8.GetString(RawBytes)`, charset ignored | Honour `Content-Type; charset=`, or keep forced UTF-8 | Today a Latin-1 or UTF-16 response is mojibake under `FRAMEWORK`, and the BOM is not stripped, so a leading `U+FEFF` is prepended to `Content` — which breaks strict JSON/XML parsers (`RestSharp/Extensions/MiscExtensions.cs:108-152`). Fixing this changes `Content` for existing callers. |
| `AsString()` returns `""` for null | `string.Empty` or `null` | Callers cannot distinguish "empty body" from "no body". Preserve `""` unless a deliberate breaking change is agreed. |
| `HttpWebRequest` + `ServicePointManager` | `HttpClient` / `SocketsHttpHandler` | Connection reuse becomes handler-scoped rather than per-`ServicePoint`; `KeepAlive`, `Expect100Continue = false` (set **globally and as a side effect** at `RestSharp/Http.Sync.cs:187`), `AutomaticDecompression` (`RestSharp/Http.Sync.cs:200`), redirect following, cookies and proxy all move onto the handler and are no longer per-request. The current code mutates a process-wide static per request — that cannot be reproduced and should not be. |
| `HttpWebResponse.StatusDescription` | `HttpResponseMessage.ReasonPhrase` | HTTP/2 and HTTP/3 have **no** reason phrase: `StatusDescription` will be `null`/empty where it is populated today (`RestSharp/Http.cs:337`). |
| `HttpWebResponse.Server`, `.ContentEncoding` | Response header lookups | Not first-class properties on `HttpResponseMessage`; must be read from `Headers`. Multiple values must be joined — pick a join rule explicitly. |
| `CookieContainer` + `Cookie` (14 fields) | `CookieContainer` still exists, but `HttpClient` exposes no per-response `Cookies` collection | `HttpWebResponse.Cookies` has no direct equivalent; response cookies must be parsed from `Set-Cookie` headers. Fields such as `Comment`, `CommentUri`, `Port`, `Discard` and `Version` are RFC 2965 relics that modern servers never send — they will always be defaults (`RestSharp/RestResponseCookie.cs:4-62`). |
| `ResponseStatus.TimedOut` / `Aborted` (async only) | `TaskCanceledException` + `CancellationToken` | In .NET 8 a timeout and a caller cancellation both surface as `TaskCanceledException`/`OperationCanceledException` and are distinguished by inspecting the token. Preserving today's exact `TimedOut` vs `Aborted` split requires deliberate mapping (`RestSharp/Http.Async.cs:287-306`). |
| `IAsyncResult` / `Begin*`–`End*` / `ThreadPool.RegisterWaitForSingleObject` | `async`/`await` + `CancellationTokenSource(timeout)` | The `RestRequestAsyncHandle.Abort()` surface disappears. The current watchdog aborts the request from a pool thread and races with the response callback; `await` semantics remove the race but also remove the `_timeoutState` flag that today decides `TimedOut` vs `Error`. |
| `RestClient.Execute` (sync) over `HttpWebRequest` | `HttpClient.Send` (sync) or async-only | Sync-over-async on `HttpClient` is deadlock-prone under a synchronization context and is not supported by all handlers. `UseSynchronizationContext` (`RestSharp/RestClient.cs:247-249`) is meaningless in .NET 8 ASP.NET Core. |
| Newtonsoft.Json 4.5 default serialization | `System.Text.Json` | Case sensitivity (STJ is case-insensitive only if configured), default null handling, `DateTime` parsing (STJ requires ISO-8601 and will reject formats Newtonsoft accepts), and enum handling all differ. The fuzzy element-name matching the README advertises has no STJ equivalent and must be reimplemented as a naming policy (`RestSharp/Extensions/StringExtensions.cs:328-340`). |
| `DeserializeAsAttribute` / `SerializeAsAttribute` + `Type.GetProperties()` reflection | `[JsonPropertyName]`, source generators | Reflection-based property discovery is trim/AOT-hostile; property **ordering** from `GetProperties()` is unspecified in both worlds but is relied on implicitly by `AddObject` (`RestSharp/RestRequest.cs:244-272`). |
| Hand-vendored LINQ (`LinqBridge-1.2.cs`), vendored `System.Xml.Linq`, vendored `HttpUtility`, vendored zlib | BCL equivalents | Mostly mechanical, but vendored `HttpUtility.HtmlDecode`/`HtmlEncode` may differ from the BCL's in entity coverage, and the vendored zlib path only ever ran under `WINDOWS_PHONE` (a dropped head), so it can be deleted outright. |
| `Uri.EscapeDataString` | Unchanged in .NET 8 | Behavior is stable, but note it never produced `+` for spaces — a form-encoded body today contains `%20`. Any switch to `FormUrlEncodedContent` changes that (`RestSharp/Extensions/StringExtensions.cs:54-57`, `RestSharp/Http.cs:261-272`). |
| Fixed multi-part boundary constant | `MultipartFormDataContent` (random boundary) | A caller that asserts on the literal boundary string breaks; also, a payload containing the constant boundary corrupts the request today (`RestSharp/Http.cs:190`). |
| `Action<Stream> Writer` + manual `ContentLength` | `HttpContent` / `StreamContent` | `HttpContent` computes length itself and can be asked to buffer for retries. The "must not close the stream" contract (`RestSharp/RestRequest.cs:162`) disappears. Non-replayable writers block automatic retry in both worlds. |
| `data.LongLength` vs `data.Length` | `long` everywhere | No behavioral difference below 2 GiB; arrays above `int.MaxValue` are still exotic. |
| xUnit 1.x style tests / NUnit 2 | xUnit v2/v3 | `Assert.Equal(expected, actual)` argument order and collection comparison semantics are compatible; `[Fact]` is unchanged. The integration tests bind `http://localhost:8080/` via `HttpListener` and use a Windows path separator literal (`RestSharp.IntegrationTests/FileTests.cs:20`) — they will not run cross-platform as written. |

---

## 8. Gaps, stubs, dead code and discrepancies

| # | Finding | Citation |
|---|---|---|
| G1 | **`HttpCookie` and `RestResponseCookie` are byte-for-byte duplicates** — same 14 members, same types, same doc comments. Likewise `HttpFile` and `FileParameter` (the latter merely adds the static factories). | `RestSharp/HttpCookie.cs:8-66` vs `RestSharp/RestResponseCookie.cs:4-62`; `RestSharp/HttpFile.cs:9-32` vs `RestSharp/FileParameter.cs:48-67` |
| G2 | The `RestResponse` → `RestResponse<T>` cast silently drops `Content`, `ErrorException` and `Request`. In the async typed path the cast is used for the abort case, so an aborted typed response loses its `Content` unless `RawBytes` was set. | `RestSharp/RestResponse.cs:144-161`, `RestSharp/RestClient.Async.cs:99-108` |
| G3 | Copy-paste bug in BOM sniffing: the `0xfe 0xff` branch is tested twice, so the `Encoding.BigEndianUnicode` branch is **unreachable**, and the first of the two assigns `Encoding.Unicode` (little-endian) to a big-endian BOM. Non-`FRAMEWORK` heads only. | `RestSharp/Extensions/MiscExtensions.cs:133-140` |
| G4 | Dead null check: `AsString` returns `""` for a null buffer at the top of the method, then re-tests `buffer == null` inside the non-`FRAMEWORK` branch. | `RestSharp/Extensions/MiscExtensions.cs:110`, `:118` |
| G5 | Commented-out code left in the transport: `//response.Content = GetString(response.RawBytes);`. | `RestSharp/Http.cs:335` |
| G6 | `FileParameter.ContentLength` / `HttpFile.ContentLength` is **only** consumed on the asynchronous path. `RestRequest.AddFile(name, path)` never sets it, so an async file upload from a path sends `Content-Length` short by the file's size. | `RestSharp/Http.Async.cs:156`, `RestSharp/RestRequest.cs:117-131` |
| G7 | `RestRequest.AddFile(name, path)` opens a `StreamReader` purely to reach `.BaseStream` — a text reader wrapping a binary file. | `RestSharp/RestRequest.cs:123-129` |
| G8 | The XML doc on `IRestResponse.Cookies`/`Headers` and on `RestResponseBase` says the collections are get-only, yet `RestResponse<T>`'s conversion operator reassigns them through the `protected` setter, replacing rather than populating the eagerly-constructed lists. | `RestSharp/IRestResponse.cs:68`, `:73`; `RestSharp/RestResponse.cs:99-103`, `:150`, `:153` |
| G9 | README advertises `request.AddFile(path)` (single argument); no such overload exists — the narrowest is `AddFile(name, path)`. | `README.markdown` (code sample) vs `RestSharp/RestRequest.cs:117` |
| G10 | README and the `RestRequest.Method` doc comment both list "GET, POST, PUT, HEAD, OPTIONS, DELETE" and omit `PATCH`, which the `Method` enum and the whole execution path support. | `README.markdown`; `RestSharp/RestRequest.cs:369` vs `RestSharp/Enum.cs:43-52` |
| G11 | README claims ".NET 3.5+" support while this fork's whole purpose is .NET 2.0. | `README.markdown` |
| G12 | `AddDefaultParameter` doc comments say "There are four types of parameters" and enumerate four, but `ParameterType` has five (`Cookie` is omitted from the list). | `RestSharp/RestClient.cs:115-121`, `RestSharp/RestRequest.cs:307-312` vs `RestSharp/Enum.cs:22-29` |
| G13 | The `Accept` header is pushed onto the client's `DefaultParameters` on **every** `Execute`, so a long-lived client accumulates unbounded duplicate `Accept` entries in that list. | `RestSharp/RestClient.Sync.cs:41-42`, `RestSharp/RestClient.Async.cs:38-40` |
| G14 | `ConfigureHttp` mutates the caller's `IRestRequest` by appending the client's default parameters — a hidden side effect that makes request objects non-reusable across clients. | `RestSharp/RestClient.cs:354-363` |
| G15 | `ResponseStatus.TimedOut` and `Aborted` have **no producer on the synchronous path**; a sync timeout is reported as `Error`. Neither value is reachable at all on heads where `SetTimeout` compiles out. | `RestSharp/Http.Sync.cs:128-133`, `RestSharp/Http.Async.cs:219-227` |
| G16 | On the async request-stream path, a `WebException` whose status is **not** `RequestCanceled` is caught and discarded with no callback invoked — the caller's callback never fires. | `RestSharp/Http.Async.cs:195-203` |
| G17 | `GetRawResponseAsync` calls `raw.Close()` after the callback even when `raw` is `null` (the `WebException` branch can leave it null), and `ExtractResponseData` both `using`s and explicitly `Close()`s the response. | `RestSharp/Http.Async.cs:252-278`, `RestSharp/Http.cs:319-371` |
| G18 | `GetHandler` guards `string.IsNullOrEmpty(contentType)` only when a `"*"` handler is registered; if `ClearHandlers()` was called, a null content type reaches `contentType.IndexOf(';')` and throws. `Deserialize<T>` then dereferences a possibly-null handler. | `RestSharp/RestClient.cs:191-211`, `:495-496` |
| G19 | `RestClient.Execute`'s catch-all returns a **fresh** `RestResponse` that has neither `Request` nor any transport data, so a failure inside `ConfigureHttp` is indistinguishable from a network failure. | `RestSharp/RestClient.Sync.cs:44-57` |
| G20 | The only unit-test coverage touching this component is indirect: `RestResponse` is used purely as a content holder in the serializer/deserializer tests (`new RestResponse { Content = ... }`), and `ResponseStatus` is asserted in exactly two integration tests. There is **no test at all** for the `RestResponse<T>` conversion operator, for `Parameter.ToString()`, or for `FileParameter`. | `RestSharp.Tests/JsonTests.cs:139`, `RestSharp.Tests/XmlTests.cs:277`, `RestSharp.IntegrationTests/StatusCodeTests.cs:20`, `:35` |
| G21 | `ParameterType`'s zero value is `Cookie`, so a `new Parameter { Name, Value }` without an explicit `Type` becomes a cookie rather than a query/form value. | `RestSharp/Enum.cs:22-29` |
| G22 | `HttpParameter` carries `using System.Linq;` and `using System.Text;` for a two-property DTO — harmless, but `System.Linq` in this fork resolves to the vendored LinqBridge. | `RestSharp/HttpParameter.cs:1-4` |
| G23 | `ServicePointManager.Expect100Continue = false` is set globally as a side effect of configuring each individual request. | `RestSharp/Http.Sync.cs:187` |

None of the above has been resolved or "fixed" in this session; they are recorded for the reviewer.

---

## 9. Open questions for the human reviewer

1. **Cookie/file type duplication (G1).** Should the .NET 8 model collapse `HttpCookie`/`RestResponseCookie` and `HttpFile`/`FileParameter` into one type each? Options: (a) collapse to a single public type per concept — simplest, but a breaking API change for existing consumers; (b) keep both and make one an alias/record of the other; (c) keep both verbatim for source compatibility.
2. **The `RestResponse<T>` conversion's dropped fields (G2).** Is losing `Content`, `ErrorException` and `Request` on conversion a bug to fix or behavior to preserve? Options: (a) copy all fields (changes what callers observe on typed error responses — most likely what they expected anyway); (b) preserve verbatim for bug-compatibility; (c) remove the cast entirely and construct typed responses directly in `Deserialize<T>`.
3. **Response text decoding (§5, G3/G4).** Should `Content` honour the `Content-Type` charset in .NET 8? Options: (a) honour charset, fall back to UTF-8, strip BOM — correct but changes `Content` for non-UTF-8 and BOM-prefixed responses; (b) keep forced UTF-8 without BOM stripping; (c) honour charset but keep the BOM, to change one thing at a time.
4. **Parameter stringification culture (R17).** Should values be formatted with `InvariantCulture`? Options: (a) invariant everywhere — fixes a latent locale bug, changes wire output on non-English hosts; (b) keep current culture; (c) make it a client-level setting with invariant as the default.
5. **`DateFormat` constants (§5, §7).** Keep `"s"`/`"u"` as-is, or move to `"O"`/`DateTimeOffset`? Note `"u"` currently labels local times as `Z` without converting them. Options: (a) keep both constants unchanged; (b) keep the names but redefine `RoundTrip` to convert to UTC first; (c) deprecate both in favour of `DateTimeOffset` + `"O"`.
6. **`ResponseStatus` in an async-first world (G15, §7).** Should `TimedOut` and `Aborted` remain distinct? Options: (a) keep all five values and map from `CancellationToken.IsCancellationRequested` vs a timeout token; (b) collapse `TimedOut`/`Aborted` into a single `Cancelled`; (c) drop `ResponseStatus` entirely and surface exceptions, with an opt-in "don't throw" mode.
7. **`ContentLength` nullability (§7).** `HttpClient` reports `null` when the header is absent or the body is chunked, where the legacy stack reported `-1`. Options: (a) expose `long?`; (b) keep `long` and map absent to `-1`; (c) keep `long` and map absent to `0` (silently changes the meaning of `== 0`).
8. **`StatusDescription` under HTTP/2+ (§7).** Options: (a) keep the property and accept that it is empty on HTTP/2+; (b) synthesise the canonical reason phrase from the status code; (c) mark it obsolete.
9. **`FileParameter.ContentLength` contract (G6).** Should the writer-based API be replaced by `HttpContent` so length is computed rather than declared? Options: (a) replace with `HttpContent`/`Stream` and drop `ContentLength` from the public surface; (b) keep `Writer` but compute the length by buffering (memory cost on large uploads); (c) keep the current declare-it-yourself contract and document that a wrong value corrupts the request.
10. **`RestRequest` mutation and reuse (G14, §4).** Should the modern client stop mutating the request when merging defaults? Options: (a) build an internal per-execution copy — makes requests safely reusable and retryable, changes the observable state of the caller's object; (b) preserve current mutation; (c) preserve mutation but make the accumulating `Accept` default (G13) idempotent.
11. **README/doc discrepancies (G9–G12).** Should the spec treat the code or the README as authoritative for the supported verb list and the `AddFile` surface? These are documentation-only defects today; the reviewer must decide whether the migration corrects the docs, the API, or both.
12. **Test coverage before migration (G20).** The behavioral oracle for this component is essentially empty. Should characterisation tests (conversion operator, `Parameter.ToString()`, `Content`/`RawBytes` laziness, `FileParameter.Create`) be written against the legacy build **before** any migration work starts, or written only against the new .NET 8 implementation?
