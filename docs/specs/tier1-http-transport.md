# Tier 1 Specification — HTTP Transport Engine

Component: the `Http` class and its supporting data types — the `HttpWebRequest` wrapper that actually performs requests.

Scope of files analysed: `RestSharp/Http.cs`, `RestSharp/Http.Sync.cs`, `RestSharp/Http.Async.cs`, `RestSharp/IHttp.cs`, `RestSharp/IHttpFactory.cs`, `RestSharp/HttpResponse.cs`, `RestSharp/IHttpResponse.cs`, `RestSharp/HttpFile.cs`, `RestSharp/HttpHeader.cs`, `RestSharp/HttpParameter.cs`, `RestSharp/HttpCookie.cs`, the vendored ZLib compression folder, and the integration tests under `RestSharp.IntegrationTests/`.

This document is a reverse-engineered description of **existing** behaviour. It does not propose or contain migration code.

---

## 1. Purpose

The HTTP transport engine is the lowest layer of the RestSharp library. It takes a fully-resolved request description — an absolute URL, a verb, headers, cookies, form parameters, files and an optional body — turns it into a .NET `HttpWebRequest`, executes it, and converts whatever comes back (including HTTP error responses and network failures) into a single, uniform `HttpResponse` value object.

Its defining business promise is **"never throw for a remote failure"**: a 404, a 500, a DNS failure, a socket reset or a timeout are all reported as data on the returned object rather than as an exception the caller must catch (`RestSharp/Http.Sync.cs:118-136`, `RestSharp/IHttpResponse.cs:67-71`). The integration test `StatusCodeTests.Handles_GET_Request_404_Error` asserts a 404 is returned as a status code, and `StatusCodeTests.Handles_Non_Existent_Domain` asserts a bad hostname yields `ResponseStatus.Error` rather than an exception (`RestSharp.IntegrationTests/StatusCodeTests.cs:11-35`).

Consumers:

| Consumer | How it uses the engine |
|---|---|
| `RestClient` (sync) | Creates an `IHttp` via `HttpFactory`, populates it in `ConfigureHttp()`, applies the proxy, dispatches on the verb, and maps `HttpResponse` → `RestResponse` (`RestSharp/RestClient.Sync.cs:73-110`, `RestSharp/RestClient.cs — ConfigureHttp()`, `RestSharp/RestClient.cs — ConvertToRestResponse()`) |
| `RestClient` (async) | Same population step, then calls the `*Async` verb methods with a callback (`RestSharp/RestClient.Async.cs:32-81`) |
| Test doubles / advanced callers | Anything implementing `IHttp`, supplied through `IHttpFactory` (`RestSharp/IHttpFactory.cs:1-15`) |

The engine has **no knowledge of** serialisation, deserialisation, authentication schemes, URL templating or retry policy; those live in higher layers. It does not build the URL — it receives an already-built absolute `Uri`.

---

## 2. Inputs and outputs

### 2.1 Request inputs (`IHttp` / `Http` public surface)

All properties are settable by the caller unless marked read-only. Source: `RestSharp/IHttp.cs:25-67` and `RestSharp/Http.cs:93-159`.

| Name | Type | Default | Required | Meaning |
|---|---|---|---|---|
| `Url` | `Uri` | `null` | Yes | Absolute target URL, already including any query string. No validation is performed (`RestSharp/Http.cs:152`). |
| `UserAgent` | `string` | `null` | No | Applied only when non-empty (`RestSharp/Http.Sync.cs:207-210`). |
| `Timeout` | `int` (ms) | `0` | No | `0` means "do not set" → the platform default of 100 s applies (`RestSharp/Http.Sync.cs:212-215`). |
| `Credentials` | `ICredentials` | `null` | No | Applied only when non-null (`RestSharp/Http.Sync.cs:217-220`). |
| `CookieContainer` | `CookieContainer` | `null` | No | If null, a fresh container is created per request (`RestSharp/Http.cs:238`). |
| `Files` | `IList<HttpFile>` | empty list | No | Read-only property; populated by adding to the list (`RestSharp/Http.cs:112`, `:167`). Presence switches the body to multipart. |
| `FollowRedirects` | `bool` | `false` | No | Not compiled under `SILVERLIGHT` (`RestSharp/Http.cs:113-118`). Note the C# default is `false`, i.e. redirects are **not** followed unless the caller opts in. |
| `ClientCertificates` | `X509CertificateCollection` | `null` | No | `FRAMEWORK` only (`RestSharp/Http.cs:119-123`). |
| `MaxRedirects` | `int?` | `null` | No | `FRAMEWORK` only; honoured only when `FollowRedirects` is true (`RestSharp/Http.Sync.cs:228-231`). |
| `Proxy` | `IWebProxy` | `null` | No | `FRAMEWORK` only (`RestSharp/Http.cs:154-159`). |
| `Headers` | `IList<HttpHeader>` | empty list | No | Read-only list of name/value pairs (`RestSharp/Http.cs:132`, `RestSharp/HttpHeader.cs`). |
| `Parameters` | `IList<HttpParameter>` | empty list | No | Form or query values, name/value strings (`RestSharp/Http.cs:136`, `RestSharp/HttpParameter.cs`). |
| `Cookies` | `IList<HttpCookie>` | empty list | No | Request cookies (only `Name`/`Value` are used on the way out) (`RestSharp/Http.cs:140`, `RestSharp/Http.cs:236-259`). |
| `RequestBody` | `string` | `null` | No | Raw body text. Mutually exclusive in practice with `Parameters` on POST/PUT/PATCH — see §3.3. |
| `RequestContentType` | `string` | `null` | No | Content type used only when a raw body is present and no files/parameters are (`RestSharp/Http.cs:285-288`). |

Derived, protected state:

| Name | Rule | Citation |
|---|---|---|
| `HasParameters` | any `Parameters` | `RestSharp/Http.cs:52-58` |
| `HasCookies` | any `Cookies` (declared but never used by the engine) | `RestSharp/Http.cs:63-69` |
| `HasBody` | `RequestBody` is non-null and non-empty | `RestSharp/Http.cs:74-80` |
| `HasFiles` | any `Files` | `RestSharp/Http.cs:85-91` |

### 2.2 File descriptor (`HttpFile`)

| Field | Type | Meaning | Citation |
|---|---|---|---|
| `Name` | `string` | Form field name | `RestSharp/HttpFile.cs:28-31` |
| `FileName` | `string` | File name advertised in the part header | `RestSharp/HttpFile.cs:20-23` |
| `ContentType` | `string` | Part content type; `application/octet-stream` when null | `RestSharp/HttpFile.cs:24-27`, `RestSharp/Http.cs:196-200` |
| `ContentLength` | `long` | Byte length; used **only** by the async length pre-calculation | `RestSharp/HttpFile.cs:12-15`, `RestSharp/Http.Async.cs:144-167` |
| `Writer` | `Action<Stream>` | Callback that writes the raw bytes into the request stream | `RestSharp/HttpFile.cs:16-19`, `RestSharp/Http.cs:310` |

### 2.3 Verb methods

| Sync (FRAMEWORK only) | Async (all targets) | Body written? |
|---|---|---|
| `Get()`, `Head()`, `Options()`, `Delete()` | `GetAsync`, `HeadAsync`, `OptionsAsync`, `DeleteAsync` | No |
| `Post()`, `Put()`, `Patch()` | `PostAsync`, `PutAsync`, `PatchAsync` | Yes |

Sync methods return `HttpResponse`; async methods return the underlying `HttpWebRequest` (so the caller can abort it) and deliver the `HttpResponse` through an `Action<HttpResponse>` callback (`RestSharp/Http.Sync.cs:34-105`, `RestSharp/Http.Async.cs:45-121`, `RestSharp/IHttp.cs:48-66`).

### 2.4 Response output (`HttpResponse` / `IHttpResponse`)

| Field | Type | Populated from | Citation |
|---|---|---|---|
| `StatusCode` | `HttpStatusCode` | `HttpWebResponse.StatusCode`; default `0` when the request never completed | `RestSharp/Http.cs:336` |
| `StatusDescription` | `string` | `HttpWebResponse.StatusDescription` | `RestSharp/Http.cs:337` |
| `ContentType` | `string` | Response `Content-Type` header, verbatim | `RestSharp/Http.cs:325` |
| `ContentLength` | `long` | `HttpWebResponse.ContentLength` (`-1` when chunked) | `RestSharp/Http.cs:326` |
| `ContentEncoding` | `string` | `HttpWebResponse.ContentEncoding`, `FRAMEWORK` only | `RestSharp/Http.cs:321-324` |
| `Server` | `string` | `HttpWebResponse.Server`, `FRAMEWORK` only | `RestSharp/Http.cs:323` |
| `RawBytes` | `byte[]` | Whole response stream read into memory | `RestSharp/Http.cs:327-334` |
| `Content` | `string` (read-only) | Lazily decoded from `RawBytes` on first access | `RestSharp/HttpResponse.cs:54-64` |
| `ResponseUri` | `Uri` | Final URI after redirects | `RestSharp/Http.cs:338` |
| `Headers` | `IList<HttpHeader>` | All response headers, in server order | `RestSharp/Http.cs:364-368` |
| `Cookies` | `IList<HttpCookie>` | Full cookie projection (13 fields) | `RestSharp/Http.cs:341-362` |
| `ResponseStatus` | enum `None`/`Completed`/`Error`/`TimedOut`/`Aborted` | Transport outcome, **not** HTTP outcome | `RestSharp/Enum.cs — ResponseStatus`, `RestSharp/IHttpResponse.cs:67-71` |
| `ErrorMessage` | `string` | `Exception.Message` of a transport failure | `RestSharp/Http.Sync.cs:130` |
| `ErrorException` | `Exception` | The transport exception itself | `RestSharp/Http.Sync.cs:131` |

---

## 3. Behavioral rules

### 3.1 Request construction order (sync, non-body verbs)

`GetStyleMethodInternal` → `ConfigureWebRequest` → `GetResponse` (`RestSharp/Http.Sync.cs:90-95`). Inside `ConfigureWebRequest` the order is fixed and significant (`RestSharp/Http.Sync.cs:183-234`):

1. `WebRequest.Create(url)` and `UseDefaultCredentials = false` — ambient Windows credentials are explicitly **disabled** for every request.
2. `ServicePointManager.Expect100Continue = false` — a **process-global** static is mutated on every single request, disabling the expect-100-continue handshake for the whole application.
3. `AppendHeaders` — caller headers (see §3.4).
4. `AppendCookies` — cookie container always assigned (see §3.6).
5. `Method` is assigned — **after** headers, so a header action that depends on the verb sees the wrong verb.
6. `ContentLength = 0` when there are no files — this **overwrites** any `Content-Length` header the caller supplied via the restricted-header action in step 3.
7. `AutomaticDecompression = Deflate | GZip | None`.
8. `ClientCertificates` if non-null.
9. `UserAgent` if non-empty.
10. `Timeout` if non-zero.
11. `Credentials` if non-null.
12. `Proxy` if non-null.
13. `AllowAutoRedirect = FollowRedirects`; `MaximumAutomaticRedirections = MaxRedirects` only when both `FollowRedirects` is true and `MaxRedirects` has a value.

### 3.2 Request construction order (sync, body verbs)

`PostPutInternal` → `ConfigureWebRequest` → `PreparePostData` → `WriteRequestBody` → `GetResponse` (`RestSharp/Http.Sync.cs:97-105`).

`PreparePostData` (`RestSharp/Http.Sync.cs:154-166`):
1. If files are present: set the multipart content type, open the request stream, write the whole multipart payload, close the stream.
2. Then call `PreparePostBody` **unconditionally** — which sets the content type again (see §3.3). For the files case this is a second assignment of `ContentType` after the request stream has already been opened and written.

`WriteRequestBody` (`RestSharp/Http.Sync.cs:168-181`) is a no-op unless `HasBody`; otherwise it encodes the body as UTF-8, sets `ContentLength` to the byte count, opens the request stream again and writes the bytes.

### 3.3 Body-shape precedence

`PreparePostBody` applies exactly one of three branches, first match wins (`RestSharp/Http.cs:274-289`):

| Priority | Condition | Content type set | Body |
|---|---|---|---|
| 1 | `HasFiles` | `multipart/form-data; boundary=<boundary>` | Multipart payload (parameters become form parts) |
| 2 | `HasParameters` | `application/x-www-form-urlencoded` | `RequestBody` is **overwritten** with the encoded parameters |
| 3 | `HasBody` | `RequestContentType` verbatim | `RequestBody` as supplied |

Implicit contracts that follow:
- Files win over everything; a raw body supplied alongside files is silently ignored by `PreparePostBody` but is still written by `WriteRequestBody` afterwards on the sync path (§8).
- Form parameters win over a raw body, and **destroy** the caller's `RequestBody` value in the process (`RestSharp/Http.cs:283`) — the mutation is visible to the caller afterwards because `RequestBody` is a public property.
- On GET-style verbs `PreparePostBody` is never invoked, so `Parameters` are **not** sent at all — the higher layer is expected to have folded them into the URL.

### 3.4 Header handling and restricted headers

Headers are applied one by one in list order. If the header name matches a registered "restricted header action" (case-insensitive, `StringComparer.OrdinalIgnoreCase`) the action is invoked; otherwise the header is added to the raw header collection (`RestSharp/Http.cs:171`, `:217-234`).

Registered actions:

| Header | Effect | Registered where | Citation |
|---|---|---|---|
| `Accept` | `request.Accept = value` | shared | `RestSharp/Http.cs:181` |
| `Content-Type` | `request.ContentType = value` | shared | `RestSharp/Http.cs:182` |
| `Date` | **ignored** (comment: "Set by system") | shared | `RestSharp/Http.cs:183` |
| `Host` | **ignored** (comment: "Set by system") | shared | `RestSharp/Http.cs:184` |
| `Range` | parsed and applied via `AddRange` | shared, `FRAMEWORK` only | `RestSharp/Http.cs:186`, `:374-387` |
| `Connection` | `request.Connection = value` | sync | `RestSharp/Http.Sync.cs:109` |
| `Content-Length` | `request.ContentLength = Convert.ToInt64(value)` | sync | `RestSharp/Http.Sync.cs:110` |
| `Expect` | `request.Expect = value` | sync | `RestSharp/Http.Sync.cs:111` |
| `If-Modified-Since` | `request.IfModifiedSince = Convert.ToDateTime(value)` | sync | `RestSharp/Http.Sync.cs:112` |
| `Referer` | `request.Referer = value` | sync | `RestSharp/Http.Sync.cs:113` |
| `Transfer-Encoding` | sets `TransferEncoding` **and** `SendChunked = true` | sync | `RestSharp/Http.Sync.cs:114` |
| `User-Agent` | `request.UserAgent = value` | sync | `RestSharp/Http.Sync.cs:115` |

Ordering rule: duplicate header names are applied in list order, so the **last** occurrence wins for property-backed headers, whereas non-restricted headers are *appended* on `FRAMEWORK` (`Headers.Add`) and *replaced* on other targets (`Headers[name] = value`) (`RestSharp/Http.cs:227-231`).

The restricted-header table is built in the constructor by calling `AddSharedHeaderActions()` and `AddSyncHeaderActions()` (`RestSharp/Http.cs:173-174`). `AddAsyncHeaderActions()` is defined but never called — see §8.

### 3.5 URL-encoded body construction

`EncodeParameters` joins parameters as `name=value` with `&`, URL-encoding both sides (`RestSharp/Http.cs:261-272`). The separator is emitted when the accumulated string length is greater than **1** (not greater than 0), which is a latent off-by-one — see §8.

### 3.6 Cookie handling

- A `CookieContainer` is **always** attached to the request, even when no cookies were supplied: the caller's container, or a brand-new empty one (`RestSharp/Http.cs:238`). This means the response's `Set-Cookie` values are always parsed into `HttpWebResponse.Cookies` and hence surfaced on the response.
- Under `FRAMEWORK`, each request cookie is added with `Domain` set to the request host (path defaults to `/`). Under non-framework targets the cookie is added against a `scheme://host` URI instead, losing the port (`RestSharp/Http.cs:241-257`).
- Response cookies are copied field-by-field into `HttpCookie` (13 fields including `Comment`, `CommentUri`, `Discard`, `HttpOnly`, `Port`, `TimeStamp`, `Version`) (`RestSharp/Http.cs:341-362`).

### 3.7 Response extraction

`ExtractResponseData` (`RestSharp/Http.cs:317-372`) always runs inside a `using` on the web response and additionally calls `Close()` explicitly at the end. It sets `ResponseStatus = Completed` for **every** response it can read, including 4xx and 5xx (`RestSharp/Http.cs:339`) — this is the documented contract on `IHttpResponse.ResponseStatus` (`RestSharp/IHttpResponse.cs:67-71`) and is exercised by `StatusCodeTests.Handles_Different_Root_Element_On_Error`, which receives a 400 with a body (`RestSharp.IntegrationTests/StatusCodeTests.cs:37-59`).

The response body is read fully into memory with a 16 KB buffer loop (`RestSharp/Extensions/MiscExtensions.cs — ReadAsBytes()`); there is no streaming option. `FileTests.Handles_Binary_File_Download` asserts byte-exact fidelity for a binary download (`RestSharp.IntegrationTests/FileTests.cs:11-24`).

### 3.8 Async flow

| Step | Behaviour | Citation |
|---|---|---|
| Non-body verbs | `ConfigureAsyncWebRequest` → create `TimeOutState` → `BeginGetResponse` → arm timeout | `RestSharp/Http.Async.cs:80-100` |
| Body verbs | `ConfigureAsyncWebRequest` → `PreparePostBody` → `WriteRequestBodyAsync` | `RestSharp/Http.Async.cs:102-121` |
| `WriteRequestBodyAsync` | If body or files: pre-compute and set `ContentLength`, then `BeginGetRequestStream`; else `BeginGetResponse`. Timeout armed on whichever handle was created. | `RestSharp/Http.Async.cs:123-142` |
| `RequestStreamCallback` | If already timed out, report `TimedOut` and stop. Else write multipart or the raw body string, then `BeginGetResponse`. | `RestSharp/Http.Async.cs:169-217` |
| `ResponseCallback` | If already timed out, report `TimedOut`; else extract data and invoke the callback. | `RestSharp/Http.Async.cs:281-316` |
| Timeout | `ThreadPool.RegisterWaitForSingleObject` on the async wait handle, one-shot, `Timeout` ms; on fire it flags `TimedOut` under a lock and calls `Request.Abort()`. Only compiled under `FRAMEWORK`; `Timeout == 0` disarms it. | `RestSharp/Http.Async.cs:219-250` |
| Callback dispatch | `ExecuteCallback` simply invokes the delegate on whatever thread the completion arrived on; marshalling to a synchronization context is the **caller's** job (`RestClient` does it) | `RestSharp/Http.Async.cs:318-321`, `RestSharp/RestClient.Async.cs:48-53` |

The async timeout is armed only on the *first* async operation of a POST/PUT/PATCH; the `BeginGetResponse` issued at the end of `RequestStreamCallback` is **not** given a new timeout registration (`RestSharp/Http.Async.cs:216`).

`AsyncTests.Can_Perform_GET_Async` and `Can_Perform_GET_Async_Without_Async_Handle` are the only async integration tests; both are simple GET echo round-trips (`RestSharp.IntegrationTests/AsyncTests.cs:13-52`).

### 3.9 Proxy application

The proxy is applied by `ConfigureWebRequest` on the sync path (`RestSharp/Http.Sync.cs:222-225`) and by `ConfigureAsyncWebRequest` on the async path (`RestSharp/Http.Async.cs:385-388`). However, `RestClient` only populates `http.Proxy` on the sync path via `ConfigureProxy` (`RestSharp/RestClient.Sync.cs:79`, `:111-117`); `RestClient.ExecuteAsync` never calls it (`RestSharp/RestClient.Async.cs:32-81`), so a client-level proxy is silently ignored for async calls.

---

## 4. Validation and error behavior

**What is validated:** essentially nothing. There is no null check on `Url`, no verb validation, no check that a body and files are not both present, no header-name validation, and no size limit on the buffered response.

| Situation | Outcome | Citation |
|---|---|---|
| Non-2xx HTTP response (sync) | `WebException` is caught, its `HttpWebResponse` is unwrapped and treated as a normal response: `ResponseStatus = Completed`, real `StatusCode`, body available | `RestSharp/Http.Sync.cs:138-152`, `RestSharp/Http.cs:339` |
| `WebException` with no `HttpWebResponse` (DNS failure, connection refused) | Rethrown by `GetRawResponse`, caught by `GetResponse`: `ResponseStatus = Error`, `ErrorMessage`/`ErrorException` populated, `StatusCode` left at `0` | `RestSharp/Http.Sync.cs:118-152`; test `StatusCodeTests.Handles_Non_Existent_Domain` |
| Any other exception during the sync request (including stream/write failures) | Same `Error` mapping — the catch is `catch (Exception)` | `RestSharp/Http.Sync.cs:128-133` |
| Exception while *constructing* an async request | Caught in `GetStyleMethodInternalAsync`/`PutPostInternalAsync`, converted to an `Error` response, callback invoked; the method still returns the (possibly null) `HttpWebRequest` | `RestSharp/Http.Async.cs:91-99`, `:111-118` |
| Async timeout fires | Request aborted; the next callback observes `TimedOut` and reports `ResponseStatus = TimedOut` with **no** `ErrorException` | `RestSharp/Http.Async.cs:229-250`, `:173-178`, `:287-292` |
| Async `WebException` with `RequestCanceled` while writing the body | Reported as `TimedOut` | `RestSharp/Http.Async.cs:195-203` |
| Async `WebException` with `RequestCanceled` while reading the response | Reported as `Aborted` | `RestSharp/Http.Async.cs:300-307` |
| Async `WebException` *not* `RequestCanceled` while writing the body | **Swallowed silently**: the catch block has no `return` and no callback, so execution falls through to `BeginGetResponse` on a request whose body was never written | `RestSharp/Http.Async.cs:195-203`, `:216` |
| Async `WebException` *not* `RequestCanceled` surfacing from `ResponseCallback` | **Swallowed silently**: no callback is invoked, so the caller waits forever | `RestSharp/Http.Async.cs:300-308` |
| `GetRawResponseAsync` receives a `WebException` with a non-HTTP response | `raw` stays `null`; the callback then dereferences it and `raw.Close()` throws `NullReferenceException`, which is remapped to `ResponseStatus = Error` by the outer catch | `RestSharp/Http.Async.cs:252-279`, `:309-315` |
| Sync timeout | Surfaces as a `WebException` → `ResponseStatus = Error` (not `TimedOut`); there is no sync equivalent of the `TimedOut` mapping | `RestSharp/Http.Sync.cs:118-136` |

**Restartability / retry semantics:** none at this layer. There is no retry, no backoff and no idempotency handling. An `Http` instance is *not* safely reusable for a second execution: `RequestBody` may have been overwritten by `EncodeParameters`, `_timeoutState` is instance-level and overwritten per call, and file `Writer` delegates are single-shot by convention. Attempt counting exists only one layer up (`RestSharp/RestClient.Sync.cs:48`). Aborting an in-flight async request is the caller's responsibility through the returned `HttpWebRequest` or `RestRequestAsyncHandle.Abort()` (`RestSharp/RestRequestAsyncHandle.cs`).

---

## 5. Core computations

### 5.1 Multipart boundary and part layout

- Line break constant: `\r\n` (`RestSharp/Http.cs:37`).
- Boundary literal, hard-coded and never randomised: `-----------------------------28947758029299` (`RestSharp/Http.cs:190`).
- Request content type: `multipart/form-data; boundary=-----------------------------28947758029299` (`RestSharp/Http.cs:191-194`).
- Field part: `--{boundary}\r\nContent-Disposition: form-data; name="{name}"\r\n\r\n{value}\r\n` (`RestSharp/Http.cs:202-206`).
- File part header: `--{boundary}\r\nContent-Disposition: form-data; name="{name}"; filename="{fileName}"\r\nContent-Type: {contentType or application/octet-stream}\r\n\r\n`, followed by the raw file bytes and then a bare `\r\n` (`RestSharp/Http.cs:196-200`, `:304-312`).
- Footer: `--{boundary}--\r\n` (`RestSharp/Http.cs:208-211`). Note the closing delimiter is followed by a CRLF rather than preceded by one.
- Write order: **all parameters first, then all files, then the footer** (`RestSharp/Http.cs:297-315`).
- Field names, file names and field values are inserted **verbatim** — no encoding, escaping or quoting of embedded `"`, CR/LF or non-ASCII characters (`RestSharp/Http.cs:196-206`).

### 5.2 Encodings

| Where | Encoding | Citation |
|---|---|---|
| Every string written to a request stream | UTF-8, no BOM (`_defaultEncoding`) | `RestSharp/Http.cs:38`, `:291-295` |
| Sync raw body byte count | `Encoding.UTF8.GetBytes(RequestBody).Length` | `RestSharp/Http.Sync.cs:173-175` |
| Async content length | `Encoding.UTF8.GetByteCount(...)` over each part plus `HttpFile.ContentLength` per file | `RestSharp/Http.Async.cs:144-167` |
| Response text decoding | `Encoding.UTF8.GetString(RawBytes)` under `FRAMEWORK` — **the response charset is ignored entirely**; non-framework targets sniff a BOM instead | `RestSharp/Extensions/MiscExtensions.cs — AsString(byte[])` |
| URL encoding of parameters | `Uri.EscapeDataString` (RFC 3986 style, `%20` for space, uppercase hex) | `RestSharp/Extensions/StringExtensions.cs — UrlEncode()` |

### 5.3 Async content-length formula

```
if no files:  length = UTF8.GetByteCount(RequestBody)
else:         length = Σ files ( UTF8.GetByteCount(fileHeader) + file.ContentLength + 2 )
                     + Σ params ( UTF8.GetByteCount(paramPart) )
                     + UTF8.GetByteCount(footer)
```
(`RestSharp/Http.Async.cs:144-167`.) The `+ 2` is the trailing CRLF after each file. Correctness depends entirely on the caller setting `HttpFile.ContentLength` accurately; the value is never cross-checked against what `Writer` actually writes.

### 5.4 Range header parsing

`AddRange` matches the regular expression `=(\d+)-(\d+)$` against the header value and calls `HttpWebRequest.AddRange(from, to)` with `Convert.ToInt32` values. A non-matching value (for example an open-ended `bytes=500-`, or a suffix range) is **silently discarded** (`RestSharp/Http.cs:374-387`).

### 5.5 Culture sensitivity

`Convert.ToInt64`, `Convert.ToInt32` and especially `Convert.ToDateTime` (for `If-Modified-Since`) are all called **without** an explicit culture, so they use the current thread culture (`RestSharp/Http.Sync.cs:110`, `:112`, `RestSharp/Http.cs:383-384`). `string.Format` in the multipart helpers is likewise culture-current, though its arguments are already strings. Header-name matching is explicitly `OrdinalIgnoreCase` (`RestSharp/Http.cs:171`). The gzip check on Windows Phone uses `StringComparison.OrdinalIgnoreCase` (`RestSharp/Http.cs:328`).

### 5.6 Compressed responses

- On `FRAMEWORK` targets decompression is delegated to the platform: `AutomaticDecompression = Deflate | GZip | None`, which also makes `HttpWebRequest` advertise `Accept-Encoding` automatically (`RestSharp/Http.Sync.cs:200`, `RestSharp/Http.Async.cs:377`). `CompressionTests` proves gzip, deflate and plain responses all decode to the original text (`RestSharp.IntegrationTests/CompressionTests.cs:11-51`).
- On Windows Phone there is no `AutomaticDecompression`, so the engine inspects the response `Content-Encoding` header itself and, when it equals `gzip` (case-insensitive), wraps the response stream in the vendored `RestSharp.Compression.ZLib.GZipStream` before reading (`RestSharp/Http.cs:327-331`, `RestSharp/Compression/ZLib/GZipStream.cs:269-272`). **Deflate is not handled on that path** — a deflate-encoded response would be returned as raw compressed bytes.
- The entire vendored ZLib folder is wrapped in `#if WINDOWS_PHONE` (`RestSharp/Compression/ZLib/GZipStream.cs:29`, `RestSharp/Compression/ZLib/ZLibStream.cs:28`), so although the files are compiled into the .NET 2.0 head (`RestSharp.Net2/RestSharp.Net2.csproj:113-138`) they contribute no code there.

---

## 6. Per-target conditional behavior

Define symbols per head: `RestSharp` → `FRAMEWORK`; `RestSharp.Net2` → `Net2;FRAMEWORK;NET_2_0`; `RestSharp.Silverlight` → `SILVERLIGHT`; `RestSharp.WindowsPhone` → `WINDOWS_PHONE`; `RestSharp.MonoTouch` → `MONOTOUCH;FRAMEWORK`; `RestSharp.MonoDroid` → `MONODROID;FRAMEWORK` (see the `DefineConstants` lines in each `.csproj`).

| Capability | FRAMEWORK / Net2 / MonoTouch / MonoDroid | SILVERLIGHT | WINDOWS_PHONE |
|---|---|---|---|
| Synchronous verbs | Available — the whole of `Http.Sync.cs` is inside `#if FRAMEWORK` (`RestSharp/Http.Sync.cs:17`, `:237`) | Absent | Absent |
| `Proxy`, `ClientCertificates`, `MaxRedirects` | Present | Absent (`RestSharp/Http.cs:119-128`, `:154-159`) | Absent |
| `FollowRedirects` | Present | **Absent** (`RestSharp/Http.cs:113-118`); `AllowAutoRedirect` is not touched (`RestSharp/Http.Async.cs:396-398`) | Present |
| Automatic decompression | Set on the request (`RestSharp/Http.Async.cs:371-377`) | Not set | Not set; manual gzip only |
| `Expect100Continue = false` | Applied | Not applied | Not applied |
| Async timeout | Armed via the thread pool | **Never armed** — `SetTimeout` body is `#if FRAMEWORK` (`RestSharp/Http.Async.cs:219-227`) | Never armed |
| `ContentLength = 0` default / pre-computed length | Applied | Applied | **Skipped** — WP7 cannot set it (`RestSharp/Http.Async.cs:130-132`, `:350-357`) |
| `UserAgent` | Applied | **Not applied** (`RestSharp/Http.Async.cs:364-369`) | Applied |
| WebRequest creator | Default | `ClientHttp` prefixes registered for `http://`/`https://` on every request (`RestSharp/Http.Async.cs:337-340`) | Default |
| Non-restricted header write | `Headers.Add(name, value)` — appends | `Headers[name] = value` — replaces | `Headers[name] = value` — replaces |
| Request cookie domain | `Domain = RequestUri.Host` | `CookieContainer.Add(scheme://host, cookie)` | Same as Silverlight |
| `ContentEncoding` / `Server` on the response | Populated | Not populated (`RestSharp/Http.cs:321-324`) | Not populated |
| Response text decoding | `UTF8.GetString` | BOM sniffing via `StreamReader` | BOM sniffing |
| `Range` header action | Registered | Not registered | Not registered |
| `System.Web` import | Excluded for MonoTouch/MonoDroid (`RestSharp/Http.Sync.cs:21-23`, `RestSharp/Http.Async.cs:32-34`) | n/a | n/a |
| `HttpUtility` source | Vendored Mono copy for Net2/MonoTouch/MonoDroid (`RestSharp/Extensions/StringExtensions.cs:25-38`) | `System.Windows.Browser` | BCL |

No `PocketPC` conditional exists anywhere in this component.

---

## 7. Target-stack (.NET 8) concerns

| Legacy construct | Modern equivalent | Behavioral risk |
|---|---|---|
| `HttpWebRequest` per call, created via `WebRequest.Create` | `HttpClient` + `HttpRequestMessage` | `HttpClient` is designed to be long-lived and shared; a naive one-instance-per-request port causes socket exhaustion, while a shared instance changes connection-reuse and DNS-refresh behaviour relative to `ServicePoint`. |
| `ServicePointManager.Expect100Continue = false` set per request | `HttpClient.DefaultRequestHeaders.ExpectContinue = false` | The legacy code mutates **global** state, so the whole host process inherits it; a per-client setting is narrower. Decide whether the global effect was intentional. |
| `webRequest.ContentLength = 0` on every bodyless request | `HttpClient` omits `Content-Length` when there is no content | Servers that distinguish "absent" from "zero" `Content-Length` on GET/DELETE will see different requests. Reproducing `Content-Length: 0` requires an explicit empty `ByteArrayContent`. |
| Restricted-header property table | `HttpRequestMessage.Headers` vs `Content.Headers` split | `Content-Type`, `Content-Length`, `Expect`, `Transfer-Encoding` move to different collections; `HttpClient` validates header values strictly and throws on malformed ones instead of silently coercing. `Host`/`Date` are settable in modern .NET, so today's "ignored" behaviour is a deliberate choice that must be re-decided. |
| `AutomaticDecompression = Deflate \| GZip \| None` | `SocketsHttpHandler.AutomaticDecompression` | Modern .NET also offers Brotli; also, once decompression is automatic the `Content-Encoding` and `Content-Length` values reported back differ from the raw ones. Legacy surfaces `ContentLength` from the *compressed* response header. |
| Vendored DotNetZip `GZipStream` (WP only) | BCL `GZipStream`/`ZLibStream`/`BrotliStream` | Straight replacement; note the legacy WP path handles gzip only and ignores deflate. |
| `AllowAutoRedirect` / `MaximumAutomaticRedirections` | `SocketsHttpHandler.AllowAutoRedirect` / `MaxAutomaticRedirections` | Modern .NET strips `Authorization` on cross-host redirects and does not downgrade HTTPS→HTTP; the legacy stack's behaviour differs. Default in this codebase is **not** to follow redirects, whereas `HttpClient`'s default is to follow. |
| `Timeout` (int ms, connect+read for the whole sync call) | `HttpClient.Timeout` / `CancellationToken` | `HttpClient.Timeout` covers headers **and** content read by default and surfaces as `TaskCanceledException`, not `WebException`; response-streaming changes when the clock stops. No `ReadWriteTimeout` is used today, so nothing maps to `SocketsHttpHandler` read timeouts. |
| Timeout via `ThreadPool.RegisterWaitForSingleObject` + `Request.Abort()` | `CancellationTokenSource(TimeSpan)` | The legacy design leaks a distinction between `TimedOut` and `Aborted` that both collapse to `OperationCanceledException`; preserving the three-way `TimedOut`/`Aborted`/`Error` contract needs explicit inspection of the cancellation reason. |
| `IAsyncResult`/`Begin*`/`End*` + `Action<HttpResponse>` callbacks | `Task<HttpResponse>` | Continuations today run on an arbitrary thread-pool thread and marshalling is the caller's job; `await` introduces `SynchronizationContext` capture by default. Any sync-over-async bridge (`.Result`, `.GetAwaiter().GetResult()`) to preserve `Get()`/`Post()` risks deadlocks and changes exception wrapping to `AggregateException`. |
| `catch (WebException)` + `ex.Response as HttpWebResponse` | `HttpResponseMessage` returned for all status codes (no exception unless `EnsureSuccessStatusCode`) | The main simplification: non-2xx no longer needs exception unwrapping. But `HttpRequestException` now covers what was several distinct `WebExceptionStatus` values, so the `Error` vs `TimedOut` vs `Aborted` classification must be rebuilt from inner exception types. |
| `CookieContainer` always attached | `SocketsHttpHandler.CookieContainer` + `UseCookies` | Cookie containers are per-handler, not per-request, in the modern model; per-request containers require either a per-request handler or manual `Cookie` header management. Legacy per-request isolation is easy to lose. |
| `UseDefaultCredentials = false`, `Credentials`, no `PreAuthenticate` | `HttpClientHandler.Credentials` / `PreAuthenticate` | Handler-level again, not request-level. Basic auth in this codebase is done by an authenticator writing an `Authorization` header (`RestSharp/Authenticators/HttpBasicAuthenticator.cs`), verified by `AuthenticationTests.Can_Authenticate_With_Basic_Http_Auth` (`RestSharp.IntegrationTests/AuthenticationTests.cs:14-29`), so that path ports cleanly; `ICredentials`-based NTLM/negotiate does not. |
| `IWebProxy Proxy` per request | `SocketsHttpHandler.Proxy` | Per-handler; combined with the existing async gap (§3.9) this is a behaviour-change hotspot. |
| `X509CertificateCollection ClientCertificates` | `SocketsHttpHandler.SslOptions.ClientCertificates` | Per-handler; certificate selection callbacks differ. |
| `KeepAlive` | `SocketsHttpHandler` connection pooling | **`KeepAlive` is never set anywhere in this component**, so the legacy default (`true`) applies. Nothing to port, but do not introduce `Connection: close` inadvertently. |
| `Encoding.UTF8.GetString(RawBytes)` ignoring charset | `HttpContent.ReadAsStringAsync()` | `HttpClient` honours the `charset` parameter of `Content-Type` and falls back to UTF-8. Bodies labelled e.g. `iso-8859-1` will decode **differently** (today they are mojibake; after migration they would be correct) — a visible behaviour change that must be signed off. |
| `Convert.ToDateTime(value)` for `If-Modified-Since`, `Convert.ToInt64` for `Content-Length` | `DateTimeOffset.Parse` with `CultureInfo.InvariantCulture` / typed header parsing | Culture-sensitive parsing today; on a non-English machine the same header string can parse differently or throw. HTTP dates are RFC 1123 and must be parsed invariantly. `DateTime` → `DateTimeOffset` also changes how unspecified-kind values are serialised to the wire. |
| `HttpCookie.Expires`/`TimeStamp` as `DateTime` | `DateTimeOffset` in modern cookie APIs | Kind/offset information is currently lost; round-tripping cookies across the boundary can shift by the local UTC offset. |
| `long ContentLength` arithmetic for multipart | `MultipartFormDataContent` | Modern content classes compute length themselves, so the hand-rolled formula (§5.3) disappears — but so does the caller-supplied `HttpFile.ContentLength` contract, which is a public API break. Rounding/precision is not otherwise a factor in this component: there is no floating-point arithmetic anywhere in it. |
| Hard-coded multipart boundary, unescaped names | `MultipartFormDataContent` (random boundary, quoted/encoded names) | Byte-for-byte output **will** change: boundary text, part ordering guarantees, and `Content-Disposition` encoding of non-ASCII names. Any consumer asserting on exact bytes, or any server with a strict parser, is at risk. |
| `StringComparer.OrdinalIgnoreCase` header table, `ToLower()`-free code | Same comparer available | This component is already ordinal for header matching; no `ToLower()`/`ToLowerInvariant()` occurrences exist in it. The culture risk is confined to `Convert.*` and `string.Format` noted above. |
| No serialization attributes in this component | — | `HttpResponse`/`HttpCookie` are plain POCOs with no `[Serializable]`, `[DataContract]` or Json.NET attributes, so the `System.Text.Json` switch has no direct effect here; reflection risk is limited to the test helper `Handlers.Generic<T>()` which resolves handler methods by name (`RestSharp.IntegrationTests/Helpers/Handlers.cs:57-71`). |
| NUnit/xUnit v1 integration tests bound to `HttpListener` on a fixed port 8080 | xUnit v3 + `HttpListener` or an in-memory `TestServer` | The current `SimpleServer` handles exactly **one** request per instance and aborts its thread on dispose (`RestSharp.IntegrationTests/Helpers/SimpleServer.cs:26-46`); `Thread.Abort` does not exist in .NET 8 and the fixed port prevents parallel test execution. |

---

## 8. Gaps, stubs, dead code and discrepancies

1. **`AddAsyncHeaderActions()` is never invoked.** It is declared in `Http.cs:178`, implemented in `Http.Async.cs:323-333`, but the constructor calls only `AddSharedHeaderActions()` and `AddSyncHeaderActions()` (`RestSharp/Http.cs:173-174`). Consequence: on Silverlight and Windows Phone the `Content-Length` restricted-header action is never registered, so a caller-supplied `Content-Length` header is written into the raw header collection, where those platforms reject or ignore it. On `FRAMEWORK` heads the method compiles to an empty body anyway.
2. **`AddSyncHeaderActions()` is a no-op on non-FRAMEWORK targets** because its implementation lives inside `#if FRAMEWORK` (`RestSharp/Http.Sync.cs:17`, `:107-116`). Combined with item 1, Silverlight/WP have only the four shared actions.
3. **`HasCookies` is computed but never used** (`RestSharp/Http.cs:63-69`); cookies are appended unconditionally.
4. **Off-by-one in `EncodeParameters`**: the separator is appended when `querystring.Length > 1` rather than `> 0` (`RestSharp/Http.cs:266-267`). After a first parameter whose encoded form is a single character (only possible when both name and value are empty, yielding `"="`), the `&` before the second parameter is omitted and the two parameters merge.
5. **`ContentLength = 0` overwrites a caller-supplied `Content-Length` header** because it runs after `AppendHeaders` (`RestSharp/Http.Sync.cs:189`, `:195-198`).
6. **`Method` is assigned after headers and cookies are applied** (`RestSharp/Http.Sync.cs:189-192`, `RestSharp/Http.Async.cs:344-347`), so any header action that would need the verb sees the default `GET`.
7. **`Transfer-Encoding` conflicts with the forced `Content-Length`**: the header action sets `SendChunked = true` (`RestSharp/Http.Sync.cs:114`) but `ContentLength = 0` is then assigned for non-file requests (`RestSharp/Http.Sync.cs:195-198`).
8. **Content type is set twice for multipart on the sync path**, the second time after the request stream has been opened and written (`RestSharp/Http.Sync.cs:156-165` then `RestSharp/Http.cs:274-279`).
9. **Sync body + files together opens the request stream twice.** `PreparePostData` writes the multipart payload and closes the stream; `WriteRequestBody` then sees `HasBody` and calls `GetRequestStream()` again (`RestSharp/Http.Sync.cs:97-105`, `:154-181`). There is no guard against this combination.
10. **Async swallows non-cancellation `WebException`s.** In `RequestStreamCallback` the catch block neither reports nor returns, so a failed body write proceeds to `BeginGetResponse` (`RestSharp/Http.Async.cs:195-203`). In `ResponseCallback` the equivalent catch invokes no callback at all, leaving the caller permanently pending (`RestSharp/Http.Async.cs:300-308`).
11. **`GetRawResponseAsync` can dereference null.** When the `WebException` carries no `HttpWebResponse`, `raw` remains null and both `callback(raw)` and `raw.Close()` throw (`RestSharp/Http.Async.cs:252-279`). It also builds a local `HttpResponse` that is never used — dead code (`RestSharp/Http.Async.cs:254-255`).
12. **`_timeoutState` is instance state overwritten on each call** (`RestSharp/Http.Async.cs:43`, `:87`, `:126`), so a single `Http` instance used for two concurrent async requests will cross-wire their timeouts.
13. **The async timeout is not re-armed for the response phase of POST/PUT/PATCH** (`RestSharp/Http.Async.cs:216`).
14. **Sync timeouts are reported as `Error`, async timeouts as `TimedOut`** — the same business event produces two different statuses depending on the call style (`RestSharp/Http.Sync.cs:128-133` vs `RestSharp/Http.Async.cs:287-292`).
15. **The multipart boundary is a fixed literal** (`RestSharp/Http.cs:190`), so a payload containing that exact string corrupts the request. There is no collision check.
16. **Multipart field names, file names and values are never escaped** (`RestSharp/Http.cs:196-206`), so a quote or CRLF in a name breaks the message framing.
17. **Response charset is ignored on FRAMEWORK targets** — `Content-Type: ...; charset=iso-8859-1` still decodes as UTF-8 (`RestSharp/Extensions/MiscExtensions.cs — AsString(byte[])`). The non-framework branch of that same method has its own bug: the UTF-16 little-endian and big-endian tests are the identical byte comparison, so `BigEndianUnicode` is unreachable.
18. **`Range` values that do not match `=(\d+)-(\d+)$` are dropped silently** (`RestSharp/Http.cs:374-387`).
19. **Proxy is silently ignored for async calls from `RestClient`** (§3.9) — the `IHttp.Proxy` contract suggests otherwise.
20. **The vendored ZLib sources are compiled into the .NET 2.0 head but are entirely `#if WINDOWS_PHONE`** (`RestSharp.Net2/RestSharp.Net2.csproj:113-138`, `RestSharp/Compression/ZLib/GZipStream.cs:29`) — dead weight in that build.
21. **`ZLibStream.cs` contains an `#if NOT` block** (lines 642-652) — permanently disabled code.
22. **README/feature-list discrepancies.** The README states "Supports .NET 3.5+, Silverlight 4, Windows Phone 7, Mono, MonoTouch" and lists "GET, POST, PUT, HEAD, OPTIONS, DELETE supported" (`README.markdown:14-21`), whereas this fork targets .NET 2.0 and the engine also implements `PATCH` (`RestSharp/Http.Sync.cs:82-88`, `RestSharp/Http.Async.cs:75-78`). The README also advertises "Automatic detection of type of content returned", which this component does not do — it copies `Content-Type` verbatim and always decodes as UTF-8.
23. **`Http` implements both `IHttp` and `IHttpFactory`** and its `Create()` returns a *different* new `Http` (`RestSharp/Http.cs:35`, `:44-47`), which is an unusual dual role; the generic `SimpleFactory<T>` alternative exists but is unused in the library (`RestSharp/IHttpFactory.cs:7-14`).
24. **No integration test coverage** exists for: multipart upload, timeouts, redirects, proxy, client certificates, cookies, `PATCH`, `HEAD`/`OPTIONS`, or async error paths. The async suite covers only two GET happy paths (`RestSharp.IntegrationTests/AsyncTests.cs`). The OAuth integration test is commented out of the runner (`RestSharp.IntegrationTests/AuthenticationTests.cs:37-38`). `RestSharp.Tests` contains no direct tests of this component at all.

These are recorded as observations only; none has been resolved or changed.

---

## 9. Open questions for the human reviewer

1. **Multipart byte compatibility.** Should the migrated transport reproduce the legacy multipart bytes exactly (fixed boundary, parameters-before-files ordering, unescaped names, CRLF after the closing delimiter), or adopt `MultipartFormDataContent` and accept a different-but-standard encoding? Option A preserves any server-side or snapshot-test dependency; option B fixes items 15-16 but changes the wire format.
2. **Response charset.** Keep the current "always UTF-8" decoding for bit-compatibility, or honour the `charset` parameter as `HttpClient` does by default? The latter silently changes `Content` for non-UTF-8 responses.
3. **`Content-Length: 0` on bodyless requests.** Continue to emit an explicit zero-length header on GET/HEAD/DELETE/OPTIONS, or let `HttpClient` omit it? Some APIs and proxies behave differently.
4. **Redirect default.** Legacy defaults `FollowRedirects` to `false`; `HttpClient` defaults to following. Keep the legacy default (explicit opt-in), or align with the platform default and treat it as a breaking change?
5. **Expect-100-continue.** The legacy code disables it process-globally. Should the migration (a) disable it per-`HttpClient`, (b) leave the platform default, or (c) expose it as a setting?
6. **Timeout semantics.** Should sync and async converge on a single status (`TimedOut`) for timeouts — fixing item 14 — or must the historical difference (`Error` for sync) be preserved for callers that branch on it?
7. **`TimedOut` vs `Aborted`.** Both become `OperationCanceledException` under `HttpClient`. Is the distinction still meaningful to consumers, and if so, is a caller-supplied `CancellationToken` allowed to be reported as `Aborted`?
8. **Silently swallowed async failures (item 10).** Should these now always invoke the callback with `ResponseStatus = Error`? That is a behaviour change for any caller that currently relies on the request simply never completing (unlikely, but it changes timeout-driven flows).
9. **Sync API retention.** Should `Get()`/`Post()`/... survive as sync-over-async wrappers, be removed in favour of `Task`-returning methods, or both be offered? This drives whether the deadlock/`AggregateException` risks in §7 need mitigating at all.
10. **Per-request vs per-handler settings.** Proxy, credentials, client certificates, cookie container and decompression are per-request today but per-handler in `HttpClient`. Is a handler-per-distinct-configuration cache acceptable, or must per-request isolation be preserved exactly (at the cost of connection pooling)?
11. **Proxy on the async path (item 19).** Is the current async proxy omission a bug to fix during migration, or relied-upon behaviour?
12. **`HttpFile.ContentLength` contract.** Modern content classes compute the length themselves. Should the property remain on the public API (ignored), be removed as a breaking change, or continue to be honoured as an override?
13. **Restricted headers `Date` and `Host`.** Modern .NET allows both to be set. Continue ignoring caller-supplied values, or start honouring them?
14. **Dropped heads and vendored shims.** Confirm that the Silverlight/WindowsPhone/MonoTouch/MonoDroid-specific branches documented in §6 (manual gzip, `ClientHttp` registration, header-replace semantics, cookie-without-port) can be deleted outright rather than translated.
15. **Cookie `DateTime` fields.** Move `Expires`/`TimeStamp` to `DateTimeOffset` (correct, breaking) or keep `DateTime` (compatible, ambiguous kind)?
16. **Test coverage before migration.** Given item 24, should characterisation tests for multipart, timeouts, redirects and cookies be written against the legacy build *first* (so the migration has an oracle), or is the existing suite considered sufficient?
