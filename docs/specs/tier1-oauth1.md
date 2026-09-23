# Tier 1 Specification — OAuth 1.0a Authentication

**Component:** OAuth 1.0a signature generation and workflow
**Repository:** `COG-GTM/RestSharp-.NET-2.0-Fork`
**Status:** Reverse-engineered from source. Read-only analysis; no legacy code was modified.
**Audience:** Business/product reviewers and the team planning the .NET 8 migration.

Every rule below carries a citation to the legacy source. Line numbers refer to the files as they exist on the default branch at the time of writing.

---

## 1. Purpose

OAuth 1.0a is the "signed request" authentication scheme used by service providers such as Twitter and Netflix. Unlike a simple API key, every single HTTP request must carry a cryptographic signature computed over the request's method, URL and parameters, plus a one-time random value (a "nonce") and a timestamp. If any byte of that computation differs from what the service provider computes on its side, the provider rejects the request. **Signature construction is therefore byte-exact and unforgiving — this is the defining business risk of this component.**

This component is the part of the RestSharp client library that:

1. Lets an application declare which stage of the OAuth "dance" it is performing — requesting a temporary token, exchanging it for a long-lived access token, exchanging a username/password for an access token, or calling a protected business resource (`RestSharp/Authenticators/OAuth1Authenticator.cs:35-124`).
2. Collects the credentials and request details, validates that the required ones are present, generates a nonce and timestamp, builds the signature, and attaches the resulting OAuth fields to the outgoing request — either in an HTTP `Authorization` header or as ordinary URL/form parameters (`RestSharp/Authenticators/OAuth1Authenticator.cs:126-230`, `RestSharp/Authenticators/OAuth/OAuthWorkflow.cs:49-268`).

**Who consumes it.** Application developers using RestSharp set `RestClient.Authenticator` to an `OAuth1Authenticator` instance created by one of the five static factory methods. The RestSharp client then calls it automatically once per request execution (`RestSharp/RestClient.cs:280-286`, invoked from `RestSharp/RestClient.Sync.cs:38` and `RestSharp/RestClient.Async.cs:36`). The component is also the public contract that the eventual .NET 8 port must preserve: `OAuth1Authenticator`, `OAuthWebQueryInfo` and the four enums are `public`; everything else in the `RestSharp.Authenticators.OAuth` namespace is `internal`.

---

## 2. Inputs and outputs

### 2.1 Public API surface consumers depend on

| Type | Visibility | Purpose | Citation |
|---|---|---|---|
| `OAuth1Authenticator` | public class, implements `IAuthenticator` | Entry point; five static factories plus four public settings | `RestSharp/Authenticators/OAuth1Authenticator.cs:16-33` |
| `OAuthParameterHandling` | public enum: `HttpAuthorizationHeader`, `UrlOrPostParameters` | Where OAuth fields are placed on the request | `RestSharp/Authenticators/OAuth/OAuthParameterHandling.cs:8-12` |
| `OAuthSignatureMethod` | public enum: `HmacSha1`, `PlainText`, `RsaSha1` | Requested signing algorithm | `RestSharp/Authenticators/OAuth/OAuthSignatureMethod.cs:8-13` |
| `OAuthSignatureTreatment` | public enum: `Escaped`, `Unescaped` | Whether the finished signature is percent-encoded | `RestSharp/Authenticators/OAuth/OAuthSignatureTreatment.cs:8-12` |
| `OAuthType` | public enum: `RequestToken`, `AccessToken`, `ProtectedResource`, `ClientAuthentication` | Which workflow stage | `RestSharp/Authenticators/OAuth/OAuthType.cs:8-14` |
| `OAuthWebQueryInfo` | public class, 18 members | Result object produced by the workflow | `RestSharp/Authenticators/OAuth/OAuthWebQueryInfo.cs:8-28` |
| `OAuthTools`, `OAuthWorkflow`, `WebPair`, `WebPairCollection`, `WebParameter`, `WebParameterCollection`, `HttpPostParameter`, `HttpPostParameterType` | **internal** | Implementation detail; not part of the supported surface, but exercised by the test suite | `RestSharp/Authenticators/OAuth/OAuthTools.cs:12`, `OAuthWorkflow.cs:14`, `WebPair.cs:3`, `WebPairCollection.cs:9`, `WebParameter.cs:15`, `WebParameterCollection.cs:12`, `HttpPostParameter.cs:5`, `HttpPostParameterType.cs:8` |

### 2.2 Factory methods (how consumers supply credentials)

| Factory | Required arguments | Optional arguments | `OAuthType` set | Citation |
|---|---|---|---|---|
| `ForRequestToken` | consumerKey, consumerSecret | callbackUrl (second overload) | `RequestToken` | `OAuth1Authenticator.cs:35-54` |
| `ForAccessToken` | consumerKey, consumerSecret, token, tokenSecret | verifier (second overload) | `AccessToken` | `OAuth1Authenticator.cs:56-77` |
| `ForAccessTokenRefresh` | consumerKey, consumerSecret, token, tokenSecret, sessionHandle | verifier (second overload) | `AccessToken` | `OAuth1Authenticator.cs:79-92` |
| `ForClientAuthentication` | consumerKey, consumerSecret, username, password | — | `ClientAuthentication` | `OAuth1Authenticator.cs:94-108` |
| `ForProtectedResource` | consumerKey, consumerSecret, accessToken, accessTokenSecret | — | `ProtectedResource` | `OAuth1Authenticator.cs:110-124` |

All five factories set the same three defaults: `ParameterHandling = HttpAuthorizationHeader`, `SignatureMethod = HmacSha1`, `SignatureTreatment = Escaped` (`OAuth1Authenticator.cs:39-41, 60-62, 98-100, 115-117`).

### 2.3 `OAuth1Authenticator` fields

| Field | Type | Default | Required | Meaning |
|---|---|---|---|---|
| `Realm` | string, public settable | `null` | No | Optional `realm="…"` prefix in the `Authorization` header (`OAuth1Authenticator.cs:18, 208-211`) |
| `ParameterHandling` | enum, public settable | `HttpAuthorizationHeader` (from factories) | Yes | Header vs. URL/body placement (`OAuth1Authenticator.cs:19, 187-202`) |
| `SignatureMethod` | enum, public settable | `HmacSha1` (from factories) | Yes | Only `HmacSha1` actually works (see §4) (`OAuth1Authenticator.cs:20`) |
| `SignatureTreatment` | enum, public settable | `Escaped` (from factories) | Yes | Percent-encode the base64 signature or not (`OAuth1Authenticator.cs:21`) |
| `Type` | enum, **internal** | set by factory | Yes | Selects the workflow branch (`OAuth1Authenticator.cs:23, 166-185`) |
| `ConsumerKey` / `ConsumerSecret` | string, internal | none | Yes (all flows) | Application identity (`OAuth1Authenticator.cs:24-25`) |
| `Token` / `TokenSecret` | string, internal | `null` | Flow-dependent (see §4.1) | Request or access token (`OAuth1Authenticator.cs:26-27`) |
| `Verifier` | string, internal | `null` | No | `oauth_verifier` from the authorization redirect (`OAuth1Authenticator.cs:28`) |
| `Version` | string, internal | `null` → emitted as `"1.0"` | No | OAuth protocol version (`OAuth1Authenticator.cs:29`, defaulted at `OAuthWorkflow.cs:355`) |
| `CallbackUrl` | string, internal | `null` | No | `oauth_callback` (`OAuth1Authenticator.cs:30`) |
| `SessionHandle` | string, internal | `null` | No | `oauth_session_handle` for token refresh (`OAuth1Authenticator.cs:31`) |
| `ClientUsername` / `ClientPassword` | string, internal | `null` | Yes for `ClientAuthentication` | xAuth credentials (`OAuth1Authenticator.cs:32-33`) |

Note that every credential field is `internal`: consumers can only set them through the factories, and **`Version` and `Realm` are the only settings adjustable after construction** apart from the three enums (`Version` is internal, so in practice only `Realm`, `ParameterHandling`, `SignatureMethod`, `SignatureTreatment`).

### 2.4 `OAuthWebQueryInfo` (the workflow's output object)

| Member | Type | Populated by workflow | Actually consumed downstream? |
|---|---|---|---|
| `Signature` | string | always | **Yes** — the only field read by the authenticator (`OAuth1Authenticator.cs:190, 194`) |
| `ConsumerKey`, `Token`, `Nonce`, `Timestamp`, `SignatureMethod`, `Version`, `Callback`, `Verifier`, `ClientMode`, `ClientUsername`, `ClientPassword`, `WebMethod`, `ParameterHandling`, `SignatureTreatment` | string / enum | varies by flow | No — see §8 |
| `UserAgent` | string | never set | No |
| `ConsumerSecret`, `TokenSecret` | string, **internal** | always | No |

Citations: `RestSharp/Authenticators/OAuth/OAuthWebQueryInfo.cs:10-27`; population at `OAuthWorkflow.cs:80-94, 137-153, 184-200, 250-265`.

### 2.5 Parameter model

| Type | Shape | Notes |
|---|---|---|
| `WebPair` | mutable `Name` + `Value` strings | `WebPair.cs:3-13` |
| `WebParameter` | `WebPair` subclass, adds nothing | `WebParameter.cs:15-20` |
| `HttpPostParameter` | `WebParameter` subclass, adds `Type`, `FileName`, `FilePath`, `FileStream`, `ContentType` and two `CreateFile` factories | `HttpPostParameter.cs:5-41` |
| `HttpPostParameterType` | enum `Field`, `File` (`Field` = default 0) | `HttpPostParameterType.cs:8-12` |
| `WebPairCollection` | `IList<WebPair>` over a `List<WebPair>`; indexer by name returns `SingleOrDefault` | `WebPairCollection.cs:9-191` |
| `WebParameterCollection` | `WebPairCollection` subclass; `Add(name, value)` creates a `WebParameter` | `WebParameterCollection.cs:12-42` |

### 2.6 Outputs attached to the outgoing request

| Placement mode | What is added to the request |
|---|---|
| `HttpAuthorizationHeader` | One HTTP header `Authorization: OAuth [realm="…",]oauth_x="y",…` (`OAuth1Authenticator.cs:189-192, 205-230`) |
| `UrlOrPostParameters` | One `GetOrPost` parameter per entry in the working parameter collection, including `oauth_signature` (`OAuth1Authenticator.cs:193-199`) |

---

## 3. Behavioral rules

### 3.1 Invocation and ordering

| # | Rule | Citation |
|---|---|---|
| 3.1.1 | The authenticator runs **once per request execution**, at the very start of `Execute` / `ExecuteAsync`, before the `Accept` header is added and before `ConfigureHttp` runs. | `RestSharp/RestClient.Sync.cs:36-42`, `RestSharp/RestClient.Async.cs:36`, `RestSharp/RestClient.cs:280-286` |
| 3.1.2 | The URL signed is `client.BuildUri(request).ToString()`, computed **before** `RestClient.DefaultParameters` are merged into the request (that merge happens later, inside `ConfigureHttp`). Default parameters of type `GetOrPost` therefore appear in the final URL but are **not** part of the signature. | `OAuth1Authenticator.cs:150`; merge at `RestSharp/RestClient.cs:354-365` |
| 3.1.3 | For GET/DELETE/HEAD/OPTIONS, `BuildUri` appends all `GetOrPost` parameters as a query string; for POST/PUT/PATCH it does not. | `RestSharp/RestClient.cs:319-332` |
| 3.1.4 | The HTTP method used in the signature is `request.Method.ToString().ToUpperInvariant()`. | `OAuth1Authenticator.cs:153` |
| 3.1.5 | For any method **other than** GET and DELETE, every request parameter of type `GetOrPost` is copied into the signing pool before the OAuth fields are added. For GET and DELETE they are not (but see 3.1.6). | `OAuth1Authenticator.cs:157-164` |
| 3.1.6 | In the `ProtectedResource` flow only, the query string of the already-built URL is parsed and every pair is added to the signing pool — so GET query parameters are signed in that flow. If the method is POST, they are added as `HttpPostParameter`; otherwise as plain `WebParameter`. | `OAuthWorkflow.cs:214-237` |
| 3.1.7 | Order of assembly into the signing pool: (a) request `GetOrPost` parameters, (b) URL query parameters (protected resource only), (c) the OAuth/xAuth fields. The subsequent sort makes insertion order irrelevant for the signature base, but it does affect nothing else. | `OAuth1Authenticator.cs:155-185`, `OAuthWorkflow.cs:242` |

### 3.2 Flow selection

| `Type` | URL assigned to | Builder called | Signing key uses token secret? | Citation |
|---|---|---|---|---|
| `RequestToken` | `workflow.RequestTokenUrl` | `BuildRequestTokenInfo` | No (consumer secret only) | `OAuth1Authenticator.cs:168-171`, `OAuthWorkflow.cs:63-97` |
| `AccessToken` | `workflow.AccessTokenUrl` | `BuildAccessTokenInfo` | Yes | `OAuth1Authenticator.cs:172-175`, `OAuthWorkflow.cs:119-156` |
| `ClientAuthentication` | `workflow.AccessTokenUrl` | `BuildClientAuthAccessTokenInfo` | No | `OAuth1Authenticator.cs:176-179`, `OAuthWorkflow.cs:166-203` |
| `ProtectedResource` | passed directly as the `url` argument | `BuildProtectedResourceInfo` | Yes | `OAuth1Authenticator.cs:180-182`, `OAuthWorkflow.cs:205-268` |
| anything else | — | — | `ArgumentOutOfRangeException` | `OAuth1Authenticator.cs:183-184` |

For `AccessToken` and `ClientAuthentication` the URL is round-tripped through `new Uri(AccessTokenUrl).ToString()` before signing; for `RequestToken` and `ProtectedResource` the raw string is used. `Uri.ToString()` performs its own normalisation (unescaping of unreserved sequences, default-port removal), so the two paths are not guaranteed to be byte-identical for exotic URLs (`OAuthWorkflow.cs:128, 134, 175, 181` vs. `OAuthWorkflow.cs:77, 244`).

### 3.3 OAuth fields added to the signing pool

Standard flows (`RequestToken`, `AccessToken`, `ProtectedResource`) — `OAuthWorkflow.cs:347-382`:

| Field | Value | Condition |
|---|---|---|
| `oauth_consumer_key` | `ConsumerKey` | always |
| `oauth_nonce` | generated nonce | always |
| `oauth_signature_method` | `"HMAC-SHA1"` / `"RSA-SHA1"` / `"PLAINTEXT"` | always |
| `oauth_timestamp` | generated timestamp | always |
| `oauth_version` | `Version` or `"1.0"` | always |
| `oauth_token` | `Token` | only when `Token` is non-blank |
| `oauth_callback` | `CallbackUrl` **unencoded** | only when non-blank |
| `oauth_verifier` | `Verifier` | only when non-blank |
| `oauth_session_handle` | `SessionHandle` | only when non-blank |

xAuth flow (`ClientAuthentication`) — `OAuthWorkflow.cs:384-402`:

| Field | Value |
|---|---|
| `x_auth_username` | `ClientUsername` |
| `x_auth_password` | `ClientPassword` |
| `x_auth_mode` | `"client_auth"` |
| `oauth_consumer_key`, `oauth_signature_method`, `oauth_timestamp`, `oauth_nonce`, `oauth_version` | as above |

The xAuth path never adds `oauth_token`, `oauth_callback`, `oauth_verifier` or `oauth_session_handle`, even if those properties are set.

### 3.4 Placement of the finished signature

| Mode | Rules | Citation |
|---|---|---|
| `HttpAuthorizationHeader` | `oauth_signature` is added to the working collection with the **escaped** signature value, then an `Authorization` header is built and added via `request.AddHeader`. | `OAuth1Authenticator.cs:189-192` |
| `UrlOrPostParameters` | `oauth_signature` is added after being passed through `HttpUtility.UrlDecode`, then **every** parameter in the working collection (OAuth fields *and* the copies of the request's own `GetOrPost` parameters) is added to the request as a `GetOrPost` parameter. | `OAuth1Authenticator.cs:193-199` |
| anything else | `ArgumentOutOfRangeException` | `OAuth1Authenticator.cs:200-201` |

### 3.5 Authorization header construction

| # | Rule | Citation |
|---|---|---|
| 3.5.1 | Header value starts with the literal `"OAuth "` (one trailing space). | `OAuth1Authenticator.cs:207` |
| 3.5.2 | If `Realm` is non-blank, `realm="<UrlEncodeRelaxed(Realm)>",` is appended — always with a trailing comma. | `OAuth1Authenticator.cs:208-211` |
| 3.5.3 | The collection is sorted by `Name` using `string.CompareTo` — i.e. the **current-culture** comparison, not ordinal. | `OAuth1Authenticator.cs:213` |
| 3.5.4 | Only parameters whose name **and** value are non-blank *and* whose name starts with `oauth_` (case-sensitive, ordinal prefix) are emitted. All other parameters — including all `x_auth_*` fields and any business parameters — are silently dropped from the header. | `OAuth1Authenticator.cs:216-220` |
| 3.5.5 | Each emitted pair is rendered `name="value"`, comma-separated, with no comma after the last pair. Values are **not** escaped at this point; they carry whatever encoding the normalisation step left on them (§5.4). | `OAuth1Authenticator.cs:221-226` |
| 3.5.6 | `IsNullOrBlank` treats null, empty and whitespace-only as blank. | `RestSharp/Authenticators/OAuth/Extensions/StringExtensions.cs:24-28` |

---

## 4. Validation and error behavior

### 4.1 Pre-flight validation per flow

| Flow | Validated | Exception on failure | Citation |
|---|---|---|---|
| `RequestToken` | request-token URL, consumer key, consumer secret all non-blank | `ArgumentException("You must specify a request token URL" / "…a consumer key" / "…a consumer secret")` | `OAuthWorkflow.cs:270-286` |
| `AccessToken` | access-token URL, consumer key, consumer secret, **token** | `ArgumentException`, messages as above plus `"You must specify a token"` | `OAuthWorkflow.cs:288-309` |
| `ClientAuthentication` | access-token URL, consumer key, consumer secret, and **both** username and password | `ArgumentException("You must specify user credentials")` when either credential is blank | `OAuthWorkflow.cs:311-332` |
| `ProtectedResource` | consumer key, consumer secret only — **token and token secret are not validated** | `ArgumentException` | `OAuthWorkflow.cs:334-345` |

Validation runs first thing in each builder, before nonce/timestamp generation (`OAuthWorkflow.cs:65, 121, 168, 207`).

### 4.2 Other error paths

| Condition | Result | Citation |
|---|---|---|
| `SignatureMethod` is `PlainText` or `RsaSha1` | `NotImplementedException("Only HMAC-SHA1 is currently supported.")` at signing time — i.e. the enum advertises three algorithms but only one exists | `OAuthTools.cs:288-289` |
| Unknown `OAuthType` or `OAuthParameterHandling` | `ArgumentOutOfRangeException` with no message | `OAuth1Authenticator.cs:183-184, 200-201` |
| A `GetOrPost` parameter with a `null` value on a non-GET/DELETE request | `NullReferenceException` from `p.Value.ToString()` | `OAuth1Authenticator.cs:162` |
| `ConstructRequestUrl` called with a null URI | `ArgumentNullException("url")` | `OAuthTools.cs:163-166` |
| Consumer secret is `null` | `ArgumentNullException` from `Uri.EscapeDataString` | `OAuthTools.cs:272` |
| Token secret is null/blank | Silently coerced to empty string (so the signing key ends with `&`) | `OAuthTools.cs:267-270` |
| Protected-resource URL contains a valueless query parameter (`?flag`) | Under .NET/Mono `HttpUtility.ParseQueryString` the key is `null`, which then reaches `UrlEncodeStrict(null)` and throws `NullReferenceException` | `OAuthWorkflow.cs:217-237`, `RestSharp/Extensions/MonoHttp/HttpUtility.cs:706-760`, `OAuthTools.cs:105-116` |
| Exceptions thrown here escape the authenticator but are caught by `RestClient.Execute`, which converts them into a response with `ResponseStatus.Error` and a populated `ErrorException` rather than propagating | Caller sees a failed response, not a thrown exception | `RestSharp/RestClient.Sync.cs:44-58` |

**Nothing is swallowed inside the OAuth component itself** — there is no `try`/`catch` anywhere in the OAuth source files. The silent behaviors are: the token-secret coercion above, the dropping of non-`oauth_` parameters from the header (§3.5.4), and the ignored boolean result of `RemoveAll` (`OAuthTools.cs:143`, `WebPairCollection.cs:104-114`).

### 4.3 Restartability and retry semantics

| # | Rule | Citation |
|---|---|---|
| 4.3.1 | A fresh nonce and timestamp are generated on every `Authenticate` call, so retries are not replays. | `OAuthWorkflow.cs:72-73, 129-130, 176-177, 239-240` |
| 4.3.2 | `request.AddHeader` / `AddParameter` **append** without de-duplication. Re-executing the *same* `IRestRequest` instance through `Execute` a second time therefore adds a second `Authorization` header parameter and, in `UrlOrPostParameters` mode, a second full set of `oauth_*` parameters. Requests are effectively single-use under this authenticator. | `RestSharp/RestRequest.cs:290-294, 318-334`; per-execution call at `RestSharp/RestClient.Sync.cs:38` |
| 4.3.3 | The normalisation step mutates the shared parameter objects in place (§5.4), so a second pass over the same collection would percent-encode already-encoded text (double-encoding). | `OAuthTools.cs:138-151`, `WebPairCollection.cs:28-31` |
| 4.3.4 | No automatic retry, refresh or 401-handling exists in this component. Token refresh is a manual re-authentication using `ForAccessTokenRefresh`. | `OAuth1Authenticator.cs:79-92` |

---

## 5. Core computations

### 5.1 Nonce

- 16 characters drawn from `abcdefghijklmnopqrstuvwxyz1234567890` (lowercase letters then digits, 36 symbols) (`OAuthTools.cs:14-18, 49-62`).
- Drawn from a single shared `System.Random`, guarded by a lock (`OAuthTools.cs:20-21, 54-60`).
- On full-framework targets the `Random` is *seeded* from 4 non-zero cryptographic random bytes once per process; the sequence itself is not cryptographically random (`OAuthTools.cs:23-36`).

### 5.2 Timestamp

- Value = `(long)(DateTime.UtcNow - new DateTime(1970,1,1)).TotalSeconds`, i.e. whole seconds since the Unix epoch, **truncated toward zero**, not rounded (`OAuthTools.cs:69-84`, `Extensions/TimeExtensions.cs:20-26`).
- The epoch `DateTime` has `Kind = Unspecified`; the subtraction is purely arithmetic, so the result is correct only because `UtcNow` is passed in. Any caller passing a local `DateTime` to the public `GetTimestamp(DateTime)` overload gets a silently wrong timestamp (`OAuthTools.cs:80-84`).
- Rendered with `long.ToString()` — current-culture formatting (digits are culture-invariant in practice; a negative pre-1970 value would use the culture's negative sign) (`OAuthTools.cs:83`).

### 5.3 Percent-encoding — two different encoders

This is the single most migration-sensitive area.

**`UrlEncodeRelaxed(value)` = `Uri.EscapeDataString(value)`** (`OAuthTools.cs:93-96`). Used for: the realm, the encoded request URL inside the signature base, the encoded normalized parameter string, the consumer/token secrets in the signing key, and the finished signature under `Escaped` treatment. Its XML doc comment claims RFC 3986 strictness, which the implementation does not itself guarantee — it inherits whatever the running framework's `EscapeDataString` does (see §7).

**`UrlEncodeStrict(value)`** (`OAuthTools.cs:105-116`). Used only inside parameter normalisation. Algorithm:

1. Unreserved set = `A-Z a-z 0-9 - . _ ~` (`OAuthTools.cs:14-18`).
2. For every character of the input that is **not** unreserved and **not** `%`, replace *all* occurrences of that character in the accumulating string with its percent-encoding (`OAuthTools.cs:109-113`).
3. Finally, replace every `%%` with `%25%` — described in the source comment as a placeholder: `// Revisit to encode actual %'s` (`OAuthTools.cs:115`).

**`PercentEncode(s)`** (`Extensions/StringExtensions.cs:81-98`): UTF-8-encodes the character, then emits one group per byte:
- bytes 8, 9, 10 and 13 → `%0X` format (zero-padded to two hex digits), e.g. tab (9) → `%09`;
- every other byte → `%X` with **no zero padding** and **uppercase** hex, e.g. `[` (0x5B) → `%5B`, but form-feed (0x0C) → `%C` (a malformed escape — see §8).
- Hex casing comes from the `X` format specifier, so it is uppercase, matching the OAuth spec.

Evidence: the only unit-level assertion in the component's test file checks `name[first]` → `name%5Bfirst%5D` (`RestSharp.IntegrationTests/oAuth1Tests.cs:166-173`).

### 5.4 Parameter normalisation (and its in-place mutation side effect)

`SortParametersExcludingSignature(parameters)` (`OAuthTools.cs:138-151`):

1. Copy the collection. **The copy is shallow** — `new WebParameterCollection(parameters)` wraps the *same* `WebPair` objects in a new list (`WebParameterCollection.cs:14-17` → `WebPairCollection.cs:28-31`).
2. Remove any entry whose name equals `oauth_signature` ignoring case, using `StringComparison.OrdinalIgnoreCase` (`OAuthTools.cs:141-143`, `Extensions/StringExtensions.cs:30-33`).
3. Apply `UrlEncodeStrict` to every name and value, **assigning back onto the shared objects** (`OAuthTools.cs:144`). Because of step 1 this rewrites the caller's parameters too — which is precisely why the `Authorization` header emitted later contains percent-encoded values even though the header builder does no encoding of its own (§3.5.5).
4. Sort by `string.CompareOrdinal(name)`, breaking ties with `string.CompareOrdinal(value)` — ordinal, culture-insensitive, comparing the *already-encoded* text (`OAuthTools.cs:145-149`).

`NormalizeRequestParameters` then joins the sorted pairs as `name=value` separated by `&` with no trailing separator (`OAuthTools.cs:126-131`, `Extensions/CollectionExtensions.cs:93-114`).

### 5.5 Request URL normalisation

`ConstructRequestUrl(uri)` (`OAuthTools.cs:161-180`) produces:

```
scheme "://" host [ ":" port ] absolutePath
```

- The port is appended **unless** (scheme is exactly `"http"` and port is 80) or (scheme is exactly `"https"` and port is 443) (`OAuthTools.cs:170-176`). The scheme comparison is an ordinal `==` against lowercase literals; `System.Uri` already lowercases the scheme and host during parsing.
- The query string and fragment are dropped.
- The path is taken from `Uri.AbsolutePath`, i.e. already in whatever escaped form `System.Uri` normalised it to.
- The final lowercasing the OAuth spec asks for is **present but commented out**: `return sb.ToString(); //.ToLower();` (`OAuthTools.cs:179`).

### 5.6 Signature base string

`ConcatenateRequestElements(method, url, parameters)` (`OAuthTools.cs:192-206`):

```
UPPERCASE(method) & UrlEncodeRelaxed(ConstructRequestUrl(url)) & UrlEncodeRelaxed(Normalize(parameters))
```

The two literal `&` separators are not encoded. `method.ToUpper()` is **culture-sensitive** (`OAuthTools.cs:197`), though in practice the caller has already uppercased it invariantly (`OAuth1Authenticator.cs:153`). Note the double encoding of parameters: names and values are encoded once by `UrlEncodeStrict` during normalisation and the whole joined string is encoded again by `UrlEncodeRelaxed` — this is what the OAuth spec requires.

### 5.7 Signature

`GetSignature(method, treatment, signatureBase, consumerSecret, tokenSecret)` (`OAuthTools.cs:261-297`):

1. A null/blank token secret becomes `""`.
2. Both secrets are passed through `UrlEncodeRelaxed`.
3. Signing key = `consumerSecret + "&" + tokenSecret`, UTF-8 bytes (`OAuthTools.cs:281-283`; encoding fixed at `OAuthTools.cs:42`).
4. Signature = Base64( HMAC-SHA1( UTF-8 bytes of the signature base ) ) (`OAuthTools.cs:284`, `Extensions/OAuthExtensions.cs:29-34`).
5. If treatment is `Escaped`, the Base64 string is percent-encoded with `UrlEncodeRelaxed`; otherwise it is returned raw (`OAuthTools.cs:292-294`).

### 5.8 Signature-method wire value

`ToRequestValue()` upper-cases the enum name with the **culture-sensitive** `ToUpper()` and inserts a hyphen before `SHA1`: `HmacSha1` → `HMACSHA1` → `HMAC-SHA1`; `RsaSha1` → `RSA-SHA1`; `PlainText` → `PLAINTEXT` (no `SHA1`, so unchanged) (`Extensions/OAuthExtensions.cs:9-14`). The inverse `FromRequestValue` maps anything unrecognised to `PlainText` (`Extensions/OAuthExtensions.cs:16-27`) and is not called anywhere in this component.

---

## 6. Per-target conditional behavior

| Symbol | Where it changes OAuth behavior | Effect | Citation |
|---|---|---|---|
| `NET_2_0` / `Net2` / `FRAMEWORK` | The .NET 2.0 head links every OAuth source file from `RestSharp/` rather than owning copies. No OAuth file branches on `NET_2_0` itself. | Behavior identical to the main framework build; differences come from the 2.0 BCL (see §7). | `RestSharp.Net2/RestSharp.Net2.csproj:22, 30, 53-96` |
| `!SILVERLIGHT && !WINDOWS_PHONE` | `[Serializable]` applied to `OAuthTools`, `OAuthWebQueryInfo`, `WebParameter`, and all four enums. | Types are BinaryFormatter-serializable on desktop only. | `OAuthTools.cs:9-11`, `OAuthWebQueryInfo.cs:5-7`, `WebParameter.cs:12-14`, `OAuthType.cs:5-7`, `OAuthSignatureMethod.cs:5-7`, `OAuthSignatureTreatment.cs:5-7`, `OAuthParameterHandling.cs:5-7`, `HttpPostParameterType.cs:5-7` |
| `SILVERLIGHT` / `WINDOWS_PHONE` | Nonce RNG falls back to `new Random()` (time-seeded, no crypto seeding) and no `RandomNumberGenerator` field exists. | Weaker, more predictable nonces on those two heads. | `OAuthTools.cs:23-36` |
| `SILVERLIGHT` / `WINDOWS_PHONE` | Protected-resource query parsing switches from `HttpUtility.ParseQueryString` to the hand-rolled `StringExtensions.ParseQueryString`. | The hand-rolled version does **not** URL-decode, throws `IndexOutOfRangeException` on a valueless pair, and throws on duplicate keys (`ToDictionary`); the Mono/BCL version decodes, HTML-decodes first, and merges duplicates into a comma-joined value. Signatures for URLs with encoded or repeated query parameters differ between heads. | `OAuthWorkflow.cs:216-226`, `Extensions/StringExtensions.cs:100-116`, `RestSharp/Extensions/MonoHttp/HttpUtility.cs:685-760` |
| `WINDOWS_PHONE` | `System.Net.HttpUtility` is imported instead of the vendored `RestSharp.Contrib` one. | Different `UrlDecode` implementation behind `OAuth1Authenticator.cs:194`. | `OAuth1Authenticator.cs:7-11`, `OAuthWorkflow.cs:4-6` |
| `WINDOWS_PHONE` | `WebPairCollection`'s `NameValueCollection` constructor and `AddRange` overload, and `CollectionExtensions.AddRange`/`ToQueryString`, are compiled out. | Narrower internal API on that head; no OAuth-path impact. | `WebPairCollection.cs:33-52`, `WebParameterCollection.cs:19-23`, `Extensions/CollectionExtensions.cs:58-91` |
| `Smartphone` | `[DebuggerDisplay]` removed from `WebParameter`; the `using System.Diagnostics` block is also removed. | Diagnostics only. | `WebParameter.cs:1-11` |
| `SILVERLIGHT` | `RegexOptions.Compiled` is OR-ed into the private `Options` constant, and `Hammock.Silverlight.Compat` is imported by `CollectionExtensions` / `WebParameterCollection`. | `Options` is never used by any code (dead); the Hammock import is an external dependency not present in this repository. | `Extensions/StringExtensions.cs:118-123`, `Extensions/CollectionExtensions.cs:6-10`, `WebParameterCollection.cs:4-8` |
| `MONOTOUCH`, `MONODROID` | Compile with `FRAMEWORK` and neither `SILVERLIGHT` nor `WINDOWS_PHONE`. | Behave exactly like the desktop framework build for this component. | `RestSharp.MonoTouch/RestSharp.MonoTouch.csproj:22`, `RestSharp.MonoDroid/RestSharp.MonoDroid.csproj:22` |
| `PocketPC` | Not referenced by any file in this component. | No OAuth-specific behavior. | — |

---

## 7. Target-stack (.NET 8) concerns

| Legacy construct | Modern equivalent | Behavioral risk |
|---|---|---|
| `Uri.EscapeDataString` as `UrlEncodeRelaxed` (`OAuthTools.cs:93-96`) | `Uri.EscapeDataString` still exists | **Highest-risk item.** The set of characters this method escapes changed across framework generations (notably the treatment of `! * ' ( )` and `~`, and the legacy input-length limit). Any difference changes the signature base byte-for-byte and every signature with it. The `// [JD]: We need to escape the apostrophe as well or the signature will fail` comment at `OAuthTools.cs:107` shows the legacy behavior was already known to be non-conformant. *Verify empirically against the .NET 2.0 head before porting; do not assume equivalence.* |
| `UrlEncodeStrict` hand-rolled encoder (`OAuthTools.cs:105-116`, `Extensions/StringExtensions.cs:81-98`) | A single RFC 3986 encoder | Fixing the known defects (single-digit escapes for bytes < 16, un-encoded lone `%`) changes signatures for inputs containing those characters. Behavior change must be an explicit, reviewed decision, not a side effect of the port. |
| `string.CompareOrdinal` sorting (`OAuthTools.cs:145-149`) | Same | Safe: ordinal comparison is stable across runtimes. |
| `name.CompareTo(...)` when sorting header parameters (`OAuth1Authenticator.cs:213`) | `string.CompareOrdinal` / `StringComparer.Ordinal` | Culture-sensitive today; result differs under some cultures. Header parameter order does not affect the signature, but it does change the literal bytes sent, which a few providers are sensitive to. |
| `method.ToUpper()` (`OAuthTools.cs:197`) and `signatureMethod.ToString().ToUpper()` (`Extensions/OAuthExtensions.cs:11`) | `ToUpperInvariant()` | Under `tr-TR` the dotted/dotless-i rules can alter these strings. `ToUpperInvariant` is the correct target; confirm no provider depends on the legacy quirk. |
| `(DateTime.UtcNow - new DateTime(1970,1,1)).TotalSeconds` cast to `long` (`Extensions/TimeExtensions.cs:20-26`) | `DateTimeOffset.UtcNow.ToUnixTimeSeconds()` | Same value for positive times (both truncate); differs for pre-epoch instants (cast truncates toward zero, `ToUnixTimeSeconds` floors). Also removes the `DateTimeKind.Unspecified` epoch hazard. |
| `TimeExtensions.FromUnixTime` returning `ToLocalTime()` (`Extensions/TimeExtensions.cs:12-18`) | `DateTimeOffset.FromUnixTimeSeconds` | Legacy converts an unspecified-kind value to local time — silently wrong by the machine's UTC offset. Dead code today (see §8), so the safest port is deletion. |
| `long.ToString()` for the timestamp (`OAuthTools.cs:83`), `string.Format("%{0:X}", b)` (`Extensions/StringExtensions.cs:90-94`) | Invariant-culture formatting | Digits and hex are culture-invariant in practice, but pinning `CultureInfo.InvariantCulture` removes the whole class of risk. |
| `new Random(seed from RandomNumberGenerator)` for nonces (`OAuthTools.cs:20-36, 49-62`) | `RandomNumberGenerator.GetString` / `GetBytes` | Nonces are currently pseudo-random from a crypto seed. Moving to a real CSPRNG is a strict improvement and changes no signature semantics. `RandomNumberGenerator.Create()` is never disposed today. |
| `new HMACSHA1()` then key assignment (`OAuthTools.cs:280-283`) | `new HMACSHA1(keyBytes)` inside a `using`, or `HMACSHA1.HashData(key, data)` | `HMACSHA1` is available in .NET 8 but flagged by security analyzers; the legacy instance is never disposed. Algorithm output is identical. |
| `HttpWebRequest` pipeline behind this component | `HttpClient` | Several defaults differ and all of them can break OAuth: (a) **redirects** — `HttpClientHandler` strips the `Authorization` header on cross-host redirects, and a redirect invalidates the signature anyway because the signed URL changes; (b) **automatic decompression** is off by default on `HttpClient`; (c) **expect-100-continue** is on by default for `HttpWebRequest` (`ServicePointManager`) and off for `HttpClient`, which changes how some OAuth token endpoints respond to POSTs; (d) **connection reuse/KeepAlive** moves from per-request to `SocketsHttpHandler` pooling; (e) **timeouts** move from per-request `Timeout` to `HttpClient.Timeout`/`CancellationToken`; (f) **cookies and proxy** move to handler-level configuration; (g) restricted-header special-casing (`RestSharp/Http.cs:171-213`, `Http.Sync.cs:109-115`) disappears — `HttpClient` splits request/content headers instead. `Authorization` itself is not a restricted header on either stack. |
| Sync authentication called from the async path (`RestSharp/RestClient.Async.cs:36`) | Keep `Authenticate` synchronous | The OAuth computation is pure CPU work with no I/O, so no sync-over-async hazard exists inside this component. The surrounding `IAsyncResult`→`Task` conversion is out of scope here but changes when `Authenticate` runs relative to cancellation. |
| `[Serializable]` on OAuth types (§6) | Remove | `BinaryFormatter` is removed in .NET 8; the attribute is inert. Any consumer relying on binary serialization of `OAuthWebQueryInfo` must be re-platformed. |
| Vendored `RestSharp.Contrib.HttpUtility` (`OAuth1Authenticator.cs:10`, `OAuthWorkflow.cs:5`) | `System.Web.HttpUtility` / `System.Net.WebUtility` / `QueryHelpers` | The vendored Mono `ParseQueryString` **HTML-decodes the query string before parsing** (`RestSharp/Extensions/MonoHttp/HttpUtility.cs:712`), which the BCL implementation does not. URLs containing `&amp;` or other entities will parse — and therefore sign — differently after the swap. Its `UrlDecode` also maps `+` to space and tolerates `%uXXXX` sequences (`HttpUtility.cs:132-185`), which affects the `UrlOrPostParameters` branch at `OAuth1Authenticator.cs:194`. |
| Hand-vendored LINQ (`RestSharp.Net2/LinqBridge-1.2.cs`) used by `Where`/`Aggregate`/`Select`/`Sort` in this component | BCL LINQ | Semantics match for the operators used here. One subtlety: `Unreserved.Contains(c)` (`OAuthTools.cs:110`) binds to the LINQ `Enumerable.Contains<char>` on the legacy stack and to `string.Contains(char)` on .NET 8 — both ordinal, so the result is the same. |
| Newtonsoft.Json / XML serialization attributes in the test fixtures (`RestSharp.IntegrationTests/oAuth1Tests.cs:82-99`) | `System.Text.Json` / `System.Xml.Serialization` | Not used by the OAuth component itself; only the Netflix test model depends on `XmlRoot`/`XmlElement`. |
| NUnit 2 / xUnit mix in the test project | xUnit on .NET 8 | The single executable OAuth assertion (`oAuth1Tests.cs:166-173`) already uses xUnit `[Fact]` and ports directly. |

---

## 8. Gaps, stubs, dead code and discrepancies

| # | Finding | Citation |
|---|---|---|
| G1 | `OAuthSignatureMethod` advertises `PlainText` and `RsaSha1`, but both throw `NotImplementedException` at signing time. The public API promises capability the implementation does not have. | `OAuthSignatureMethod.cs:8-13` vs. `OAuthTools.cs:288-289` |
| G2 | `GetSignature(signatureMethod, signatureBase, consumerSecret, tokenSecret)` forwards its arguments incorrectly: it passes `consumerSecret` where the signature base is expected and `tokenSecret` where the consumer secret is expected, and drops `signatureBase` entirely. Any caller would silently produce a wrong signature. The overload is unreferenced in this repository. | `OAuthTools.cs:246-249` |
| G3 | `OAuthWebQueryInfo` carries 18 members, but the authenticator reads only `Signature`. Nonce, timestamp, consumer key, token, version, callback, verifier and the xAuth fields reach the wire only because they were separately added to the parameter collection. `UserAgent` is never assigned by anyone. | `OAuthWebQueryInfo.cs:10-27`, `OAuth1Authenticator.cs:190, 194` |
| G4 | In `HttpAuthorizationHeader` mode the header filter keeps only names starting with `oauth_`, so the xAuth flow's `x_auth_username`, `x_auth_password` and `x_auth_mode` are signed but never transmitted. The `ClientAuthentication` flow therefore appears non-functional in its own default parameter-handling mode. | `OAuth1Authenticator.cs:98, 216-220`, `OAuthWorkflow.cs:386-396` |
| G5 | In `UrlOrPostParameters` mode the request's own `GetOrPost` parameters were copied into the working collection and are then added back to the request, producing duplicate parameters on non-GET/DELETE requests. | `OAuth1Authenticator.cs:158-164, 195-198` |
| G6 | For `RequestToken` and `AccessToken` flows issued as GET, query-string parameters are placed in the URL by `BuildUri` but never enter the signing pool (only the `ProtectedResource` flow harvests them), so the signature omits parameters the server will include. | `OAuth1Authenticator.cs:158`, `RestSharp/RestClient.cs:319-332`, `OAuthWorkflow.cs:214-237` |
| G7 | `ConstructRequestUrl`'s XML comment states "Resulting URLs must be lower case", but the lowercasing call is commented out. Documentation and code disagree. | `OAuthTools.cs:153-157, 179` |
| G8 | `UrlEncodeRelaxed`/`UrlEncodeStrict` share an XML comment claiming RFC 3986 percent-encoding with upper-cased hex; `UrlEncodeRelaxed` merely delegates to `Uri.EscapeDataString` and guarantees none of that itself. | `OAuthTools.cs:86-96` |
| G9 | `PercentEncode` emits a single hex digit for byte values below 16 other than 8/9/10/13 (e.g. form feed → `%C`), which is not a valid percent-escape. The zero-padding special case covers only backspace, tab, line feed and carriage return. | `Extensions/StringExtensions.cs:81-98` |
| G10 | `UrlEncodeStrict` deliberately skips `%` and then patches `%%` → `%25%`, with the source comment `// Revisit to encode actual %'s`. A single literal `%` in a parameter name or value is left unencoded. Explicit unfinished work. | `OAuthTools.cs:107-115` |
| G11 | `SortParametersExcludingSignature` mutates the caller's `WebPair` objects because the "copy" is shallow. The `Authorization` header depends on this side effect for its encoding, making it an undocumented implicit contract. | `OAuthTools.cs:138-151`, `WebPairCollection.cs:28-31`, `WebParameterCollection.cs:14-17` |
| G12 | `RemoveAll` returns a success flag that is discarded, and returns `false` when nothing matched — a semantics mismatch with its name that no caller notices. | `WebPairCollection.cs:104-114`, `OAuthTools.cs:143` |
| G13 | When `Realm` is set, the header always gets a trailing comma after the realm segment; if no `oauth_*` parameters survived the filter the header would end with a dangling comma. | `OAuth1Authenticator.cs:208-226` |
| G14 | `ForAccessTokenRefresh` sets `SessionHandle` but the underlying `Type` remains `AccessToken`, so refresh is indistinguishable from a first-time exchange except for the extra `oauth_session_handle` parameter. Whether that matches the provider contract is unverified. | `OAuth1Authenticator.cs:79-92`, `OAuthWorkflow.cs:373-376` |
| G15 | `BuildRequestTokenInfo` sets `info.Callback = UrlEncodeRelaxed(CallbackUrl ?? "")` while `AddAuthParameters` adds the raw, unencoded `oauth_callback` — two different encodings of the same value, only the raw one of which is used. | `OAuthWorkflow.cs:91, 363-366` |
| G16 | Dead or unreachable code: `BuildRequestTokenInfo(method)` and `BuildAccessTokenInfo(method)` single-argument overloads; `GetSignature` 3-argument overload; `OAuthExtensions.FromRequestValue`; `TimeExtensions.FromNow` and `FromUnixTime`; `StringExtensions.Options`, `EqualsAny`, `FormatWithInvariantCulture`, `ToBase64String`, the `UrlDecode` extension, and `UrlEncode` (reachable only from the dead `ToQueryString`); most of `CollectionExtensions` (`AsEnumerable`, `And`, `TryWithKey`, `ToEnumerable`, `ToQueryString`); `HttpPostParameter.CreateFile` (both overloads) and the `Type`/`FileName`/`FilePath`/`FileStream`/`ContentType` members; the entire `HttpPostParameterType` enum is only ever read as its default value. | `OAuthWorkflow.cs:49-52, 106-109`, `OAuthTools.cs:217-220, 246-249`, `Extensions/OAuthExtensions.cs:16-27`, `Extensions/TimeExtensions.cs:7-18`, `Extensions/StringExtensions.cs:35-38, 45-48, 55-59, 118-123`, `Extensions/CollectionExtensions.cs:16-48, 68-89`, `HttpPostParameter.cs:11-40` |
| G17 | The Silverlight branch imports `Hammock.Silverlight.Compat`, a package not present in this repository — that head cannot build as checked in. | `Extensions/CollectionExtensions.cs:6-7`, `WebParameterCollection.cs:4-7` |
| G18 | Test coverage is effectively one assertion. `Can_Authenticate_With_OAuth` is `[Fact(Skip = "Provide your own consumer key/secret before running")]`, `Can_Authenticate_Netflix_With_OAuth` has its `[Fact]` attribute commented out, and both require live third-party credentials and a browser (`Process.Start`). Only `Properly_Encodes_Parameter_Names` executes. There are **no** tests under `RestSharp.Tests/**` touching OAuth. | `RestSharp.IntegrationTests/oAuth1Tests.cs:16, 103-104, 166-173` |
| G19 | The README advertises "oAuth 1, oAuth 2, Basic, NTLM and Parameter-based Authenticators included" without qualifying that only HMAC-SHA1 is implemented. | `README.markdown:20` vs. `OAuthTools.cs:288-289` |
| G20 | Re-executing the same request instance accumulates duplicate `Authorization` headers and OAuth parameters (§4.3.2). No guard exists. | `RestSharp/RestRequest.cs:290-294`, `RestSharp/RestClient.Sync.cs:38` |
| G21 | A protected-resource URL whose query contains a valueless parameter produces a `null` parameter name that later dereferences to a `NullReferenceException` inside `UrlEncodeStrict`. | `OAuthWorkflow.cs:217-237`, `OAuthTools.cs:105-116` |

*These are recorded, not resolved. No decision about correct behavior is made here.*

---

## 9. Open questions for the human reviewer

1. **Byte-exact parity vs. spec conformance.** The legacy encoders have known defects (G9, G10) and depend on the running framework's `Uri.EscapeDataString` (§7). Should the .NET 8 port (a) reproduce legacy output byte-for-byte, defects included, with a characterization test suite pinned to the .NET 2.0 head's output; (b) implement a clean RFC 3986/RFC 5849 encoder and accept that signatures change for inputs containing `%`, control characters and `! * ' ( ) ~`; or (c) ship both behind a compatibility switch?

2. **`PlainText` and `RsaSha1` (G1).** Implement them in the port, or narrow the public enum to `HmacSha1` only and treat the removal as a documented breaking change?

3. **xAuth / `ClientAuthentication` (G4).** The `x_auth_*` fields never reach the wire in the default header mode. Should the port (a) include `x_auth_*` parameters in the header, (b) force `UrlOrPostParameters` for this flow, or (c) drop the flow entirely — Twitter's xAuth, its only known consumer, is retired?

4. **Duplicate parameters in `UrlOrPostParameters` mode (G5).** Fix by not re-adding parameters that already exist on the request, or preserve the duplication for compatibility with providers that tolerate it?

5. **Unsigned query parameters on GET request-token/access-token calls (G6).** Extend the signing pool to all flows uniformly, or keep the current per-flow asymmetry?

6. **Request URL lowercasing (G7).** Restore the commented-out `ToLower()` (spec-conformant, but changes existing signatures for mixed-case paths), or keep current behavior and delete the misleading comment?

7. **The in-place mutation contract (G11).** The `Authorization` header currently relies on normalisation having rewritten the shared parameter objects. Should the port make encoding explicit at header-construction time (clearer, but must be verified to produce identical bytes), or keep the mutation?

8. **Header parameter ordering (§3.5.3).** Move to ordinal sorting (removes culture sensitivity, may reorder headers versus today), or preserve `CompareTo`?

9. **Redirect and retry policy under `HttpClient` (§7).** Should the port disable automatic redirects for OAuth-signed requests (since a redirect invalidates the signature), re-sign on redirect, or leave handler defaults and document the hazard?

10. **Nonce generation.** Replace the seeded `System.Random` with `RandomNumberGenerator` across all remaining targets? This changes no signature semantics but does change reproducibility for any test that seeds the generator.

11. **Test strategy (G18).** The component has essentially no automated oracle. Should the migration be gated on a characterization suite built from published OAuth 1.0a test vectors plus captured legacy output, and who supplies live provider credentials (or a stub provider) for the end-to-end flows?

12. **`OAuthWebQueryInfo` (G3).** Keep this public, largely-unused DTO in the .NET 8 surface for source compatibility, or remove it and expose only the authenticator?

13. **Dead code (G16) and dropped heads.** Delete the unused extension methods, `HttpPostParameter` file support and the Silverlight/WindowsPhone/MonoTouch/MonoDroid conditionals outright during the port, or keep them until a consumer audit confirms nothing external references the `internal` surface via `InternalsVisibleTo` or reflection?
