# Modernization Inventory — `RestSharp/`

Research spike (branch `modernize/B-spike`, not for merge). Baseline: `origin/master` @ `e9e1048`.

Method: `rg -n '^\s*#(if|elif|else|endif)' RestSharp/` (cross-checked with `rg -n '^\s*#\s*(if|elif|else|endif)'` to catch spaced directives — none found) plus manual reading of every guarded block. Line numbers are 1-based and refer to the current file contents.

Legend for "survives": the branch that should be kept when targeting net8.0 / netstandard2.0 with only `FRAMEWORK` defined (all of `NET_2_0`, `Net2`, `SILVERLIGHT`, `WINDOWS_PHONE`, `WindowsPhone`, `MONOTOUCH`, `MONODROID`, `Smartphone`, `NET_4_0`, `NOT`, `NOTUSED`, `POINTLESS` treated as never defined / dead).

Symbols actually present in `RestSharp/`:

| Symbol | Where defined | Status for modern build |
|---|---|---|
| `FRAMEWORK` | `RestSharp/RestSharp.csproj` `<DefineConstants>TRACE;DEBUG;FRAMEWORK</DefineConstants>` (Debug) / `TRACE;FRAMEWORK` (Release) | **surviving** |
| `NET_2_0` | not defined in `RestSharp/RestSharp.csproj` (only in `RestSharp.Net2` project) | dead |
| `Net2` (note casing) | never defined anywhere in repo | dead |
| `SILVERLIGHT` | `RestSharp.Silverlight` project only | dead |
| `WINDOWS_PHONE` | `RestSharp.WindowsPhone*` projects only | dead |
| `WindowsPhone` (note casing) | never defined anywhere in repo | dead |
| `MONOTOUCH` / `MONODROID` | `RestSharp.MonoTouch` / `RestSharp.MonoDroid` projects only | dead |
| `Smartphone` | never defined | dead |
| `NET_4_0` | never defined in this repo (vendored Mono source) — only in `Extensions/MonoHttp/*` | dead (whole folder proposed for removal) |
| `NOT`, `NOTUSED`, `POINTLESS` | never defined (vendored DotNetZip "commented-out" idiom) — only in `Compression/ZLib/*` | dead (whole folder proposed for removal) |

`PocketPC` and `NETFX_CORE` do **not** occur anywhere under `RestSharp/`.

---

## 1. Preprocessor blocks (all `#if` / `#elif` / `#else` / `#endif` under `RestSharp/`)

### 1a. Core library files (non-vendored)

| # | File | `#if` line | `#else`/`#elif` | `#endif` line | Condition | Guarded code | Survives |
|---|---|---|---|---|---|---|---|
| 1 | `RestSharp/Authenticators/NtlmAuthenticator.cs` | 17 | – | 33 | `FRAMEWORK` | Entire `NtlmAuthenticator` class (uses `System.Net.NetworkCredential` / `CredentialCache.DefaultCredentials`). | Keep body; drop directive. |
| 2 | `RestSharp/Authenticators/OAuth/Extensions/CollectionExtensions.cs` | 6 | 8 | 10 | `SILVERLIGHT` | `using Hammock.Silverlight.Compat;` (else-branch is empty). | Delete whole block (else-branch is empty). |
| 3 | `RestSharp/Authenticators/OAuth/Extensions/CollectionExtensions.cs` | 58 | – | 91 | `!WINDOWS_PHONE` | `AddRange(IDictionary<string,string>, NameValueCollection)` and related `NameValueCollection` helpers. | Keep body; drop directive. |
| 4 | `RestSharp/Authenticators/OAuth/Extensions/StringExtensions.cs` | 8 | – | 10 | `SILVERLIGHT && !WindowsPhone` | `using System.Windows.Browser;` | Delete. |
| 5 | `RestSharp/Authenticators/OAuth/Extensions/StringExtensions.cs` | 12 | – | 14 | `WindowsPhone` | `using System.Web;` | Delete. |
| 6 | `RestSharp/Authenticators/OAuth/Extensions/StringExtensions.cs` | 16 | – | 18 | `!SILVERLIGHT` | Empty block (no code between directives). | Delete. |
| 7 | `RestSharp/Authenticators/OAuth/Extensions/StringExtensions.cs` | 119 | 121 | 123 | `SILVERLIGHT` | `RegexOptions` for a static regex: SL uses `Compiled \| IgnoreCase`, else `IgnoreCase`. | Keep `#else` (`RegexOptions.IgnoreCase`). |
| 8 | `RestSharp/Authenticators/OAuth/HttpPostParameterType.cs` | 5 | – | 7 | `!SILVERLIGHT && !WINDOWS_PHONE` | `[Serializable]` on `HttpPostParameterType` enum. | Keep attribute; drop directive. |
| 9 | `RestSharp/Authenticators/OAuth/OAuthParameterHandling.cs` | 5 | – | 7 | `!SILVERLIGHT && !WINDOWS_PHONE` | `[Serializable]` on enum. | Keep attribute; drop directive. |
| 10 | `RestSharp/Authenticators/OAuth/OAuthSignatureMethod.cs` | 5 | – | 7 | `!SILVERLIGHT && !WINDOWS_PHONE` | `[Serializable]` on enum. | Keep attribute; drop directive. |
| 11 | `RestSharp/Authenticators/OAuth/OAuthSignatureTreatment.cs` | 5 | – | 7 | `!SILVERLIGHT && !WINDOWS_PHONE` | `[Serializable]` on enum. | Keep attribute; drop directive. |
| 12 | `RestSharp/Authenticators/OAuth/OAuthTools.cs` | 9 | – | 11 | `!SILVERLIGHT && !WINDOWS_PHONE` | `[Serializable]` on static class `OAuthTools`. | Keep attribute; drop directive. |
| 13 | `RestSharp/Authenticators/OAuth/OAuthTools.cs` | 23 | – | 25 | `!SILVERLIGHT && !WINDOWS_PHONE` | `private static readonly RandomNumberGenerator _rng = RandomNumberGenerator.Create();` | Keep; drop directive. |
| 14 | `RestSharp/Authenticators/OAuth/OAuthTools.cs` | 29 | 33 | 35 | `!SILVERLIGHT && !WINDOWS_PHONE` | Static ctor seeds `_random` from `_rng.GetNonZeroBytes` (if) vs `new Random()` (else). | Keep `#if` branch (RNG-seeded). |
| 15 | `RestSharp/Authenticators/OAuth/OAuthType.cs` | 5 | – | 7 | `!SILVERLIGHT && !WINDOWS_PHONE` | `[Serializable]` on enum. | Keep attribute; drop directive. |
| 16 | `RestSharp/Authenticators/OAuth/OAuthWebQueryInfo.cs` | 5 | – | 7 | `!SILVERLIGHT && !WINDOWS_PHONE` | `[Serializable]` on class. | Keep attribute; drop directive. |
| 17 | `RestSharp/Authenticators/OAuth/OAuthWorkflow.cs` | 4 | – | 6 | `!WINDOWS_PHONE` | `using RestSharp.Contrib;` (vendored MonoHttp `HttpUtility`). | Keep for now; becomes `using System.Web;`/`System.Net` when MonoHttp is replaced (see §2). |
| 18 | `RestSharp/Authenticators/OAuth/OAuthWorkflow.cs` | 216 | 218 | 220 | `!SILVERLIGHT && !WINDOWS_PHONE` | `HttpUtility.ParseQueryString(uri.Query)` vs custom `uri.Query.ParseQueryString()`. | Keep `#if` branch (`HttpUtility.ParseQueryString`). |
| 19 | `RestSharp/Authenticators/OAuth/OAuthWorkflow.cs` | 222 | 224 | 226 | `!SILVERLIGHT && !WINDOWS_PHONE` | Iterate `urlParameters.AllKeys` (NameValueCollection) vs `.Keys` (dictionary). | Keep `#if` branch (`.AllKeys`). |
| 20 | `RestSharp/Authenticators/OAuth/WebPairCollection.cs` | 33 | – | 52 | `!WINDOWS_PHONE` | `WebPairCollection(NameValueCollection)` ctor, `AddRange(NameValueCollection)`, `AddCollection(NameValueCollection)`. | Keep body; drop directive. |
| 21 | `RestSharp/Authenticators/OAuth/WebParameter.cs` | 1 | – | 5 | `!Smartphone` | `using System; using System.Diagnostics;` | Keep; drop directive. |
| 22 | `RestSharp/Authenticators/OAuth/WebParameter.cs` | 9 | – | 11 | `!Smartphone` | `[DebuggerDisplay("{Name}:{Value}")]` | Keep; drop directive. |
| 23 | `RestSharp/Authenticators/OAuth/WebParameter.cs` | 12 | – | 14 | `!SILVERLIGHT && !WINDOWS_PHONE` | `[Serializable]` on `WebParameter`. | Keep attribute; drop directive. |
| 24 | `RestSharp/Authenticators/OAuth/WebParameterCollection.cs` | 4 | 6 | 8 | `!SILVERLIGHT` | Empty `#if` branch; `#else` is `using Hammock.Silverlight.Compat;`. | Delete whole block. |
| 25 | `RestSharp/Authenticators/OAuth/WebParameterCollection.cs` | 19 | – | 23 | `!WINDOWS_PHONE` | `WebParameterCollection(NameValueCollection)` ctor. | Keep body; drop directive. |
| 26 | `RestSharp/Authenticators/OAuth1Authenticator.cs` | 7 | 9 | 11 | `WINDOWS_PHONE` | `using System.Net;` (WP) vs `using RestSharp.Contrib;` (else). | Keep `#else` (`RestSharp.Contrib`) until MonoHttp replaced, then `System.Web`/`System.Net`. |
| 27 | `RestSharp/Extensions/MiscExtensions.cs` | 29 | – | 39 | `!WINDOWS_PHONE` | `byte[].SaveAs(path)` → `File.WriteAllBytes`. | Keep body; drop directive. |
| 28 | `RestSharp/Extensions/MiscExtensions.cs` | 115 | 117 | 151 | `FRAMEWORK` | `byte[].AsString(Encoding)`: `encoding.GetString(buffer)` (if) vs hand-rolled BOM-aware decode loop (else). | Keep `#if` (`encoding.GetString`). |
| 29 | `RestSharp/Extensions/ReflectionExtensions.cs` | 68 | 70 | 72 | `FRAMEWORK` | `Convert.ChangeType(source, newType)` vs `Convert.ChangeType(source, newType, null)`. | Keep `#if`. |
| 30 | `RestSharp/Extensions/ReflectionExtensions.cs` | 77 | 79 | 81 | `FRAMEWORK` | `Convert.ChangeType(source, newType, culture)` vs `(…, null)`. | Keep `#if`. |
| 31 | `RestSharp/Extensions/ReflectionExtensions.cs` | 94 | 98 | 100 | `FRAMEWORK` | `FindEnumValue`: LINQ over `Enum.GetValues` with name variants vs plain `Enum.Parse(type, value, true)`. | Keep `#if` (LINQ variant). |
| 32 | `RestSharp/Extensions/StringExtensions.cs` | 25 | – | 27 | `Net2` | `using RestSharp.Contrib;` | Delete (duplicate of #35; `Net2` never defined). |
| 33 | `RestSharp/Extensions/StringExtensions.cs` | 29 | – | 31 | `SILVERLIGHT` | `using System.Windows.Browser;` | Delete. |
| 34 | `RestSharp/Extensions/StringExtensions.cs` | 33 | – | 34 | `WINDOWS_PHONE` | Empty block. | Delete. |
| 35 | `RestSharp/Extensions/StringExtensions.cs` | 36 | – | 38 | `FRAMEWORK \|\| MONOTOUCH \|\| MONODROID` | `using RestSharp.Contrib;` | Keep using (→ `System.Web`/`System.Net` after MonoHttp replacement); drop directive. |
| 36 | `RestSharp/Extensions/StringExtensions.cs` | 69 | – | 74 | `FRAMEWORK` | `string.HtmlAttributeEncode()` → `HttpUtility.HtmlAttributeEncode`. | Keep body; drop directive. |
| 37 | `RestSharp/FileParameter.cs` | 21 | 23 | 25 | `FRAMEWORK` | `data.LongLength` vs `(long)data.Length`. | Keep `#if` (`LongLength`). |
| 38 | `RestSharp/Http.Async.cs` | 22 | – | 25 | `SILVERLIGHT` | `using System.Windows.Browser; using System.Net.Browser;` | Delete. |
| 39 | `RestSharp/Http.Async.cs` | 27 | – | 30 | `WINDOWS_PHONE` | `using System.Windows.Threading; using System.Windows;` | Delete. |
| 40 | `RestSharp/Http.Async.cs` | 32 | – | 34 | `(FRAMEWORK && !MONOTOUCH && !MONODROID)` | `using System.Web;` | Keep using (verify still needed — nothing in file appears to use `System.Web` types; likely removable); drop directive. |
| 41 | `RestSharp/Http.Async.cs` | 130 | – | 132 | `!WINDOWS_PHONE` | `webRequest.ContentLength = CalculateContentLength();` before `BeginGetRequestStream`. | Keep body; drop directive. |
| 42 | `RestSharp/Http.Async.cs` | 221 | – | 226 | `FRAMEWORK` | Registers `ThreadPool.RegisterWaitForSingleObject` timeout callback for async requests when `Timeout != 0`. | Keep body; drop directive. |
| 43 | `RestSharp/Http.Async.cs` | 325 | – | 327 | `SILVERLIGHT` | Adds `Content-Length` restricted-header action (sets `ContentLength`). | Delete. |
| 44 | `RestSharp/Http.Async.cs` | 328 | – | 332 | `WINDOWS_PHONE` | Adds no-op `Content-Length` restricted-header action. | Delete. |
| 45 | `RestSharp/Http.Async.cs` | 337 | – | 340 | `SILVERLIGHT` | `WebRequest.RegisterPrefix(... WebRequestCreator.ClientHttp)` for http/https. | Delete. |
| 46 | `RestSharp/Http.Async.cs` | 350 | – | 357 | `!WINDOWS_PHONE` | `if(!HasFiles) webRequest.ContentLength = 0;` | Keep body; drop directive. |
| 47 | `RestSharp/Http.Async.cs` | 364 | – | 369 | `!SILVERLIGHT` | Sets `webRequest.UserAgent` when `UserAgent.HasValue()`. | Keep body; drop directive. |
| 48 | `RestSharp/Http.Async.cs` | 371 | – | 394 | `FRAMEWORK` | Sets `ClientCertificates`, `AutomaticDecompression = Deflate\|GZip\|None`, `ServicePointManager.Expect100Continue = false`, `Timeout`, `Proxy`, `MaximumAutomaticRedirections` on async `HttpWebRequest`. | Keep body; drop directive. |
| 49 | `RestSharp/Http.Async.cs` | 396 | – | 398 | `!SILVERLIGHT` | `webRequest.AllowAutoRedirect = FollowRedirects;` | Keep body; drop directive. |
| 50 | `RestSharp/Http.Sync.cs` | 17 | – | 237 | `FRAMEWORK` | Entire file: synchronous `Http` partial (`Get/Post/Put/…`, `ConfigureWebRequest`, `GetResponse`). Contains nested block #51. | Keep body; drop directive. |
| 51 | `RestSharp/Http.Sync.cs` | 21 | – | 23 | `!MONOTOUCH && !MONODROID` (nested in #50) | `using System.Web;` | Keep using (verify still needed; likely removable); drop directive. |
| 52 | `RestSharp/Http.cs` | 26 | – | 28 | `WINDOWS_PHONE` | `using RestSharp.Compression.ZLib;` | Delete (only consumer of vendored ZLib — see §2). |
| 53 | `RestSharp/Http.cs` | 113 | – | 118 | `!SILVERLIGHT` | `public bool FollowRedirects { get; set; }` | Keep body; drop directive. |
| 54 | `RestSharp/Http.cs` | 119 | – | 128 | `FRAMEWORK` | `X509CertificateCollection ClientCertificates` and `int? MaxRedirects` properties. | Keep body; drop directive. |
| 55 | `RestSharp/Http.cs` | 154 | – | 159 | `FRAMEWORK` | `public IWebProxy Proxy { get; set; }` | Keep body; drop directive. |
| 56 | `RestSharp/Http.cs` | 185 | – | 187 | `FRAMEWORK` | Registers `Range` restricted-header action → `AddRange(r, v)`. | Keep body; drop directive. |
| 57 | `RestSharp/Http.cs` | 227 | 229 | 231 | `FRAMEWORK` | `webRequest.Headers.Add(name, value)` vs `webRequest.Headers[name] = value`. | Keep `#if` (`Headers.Add`). |
| 58 | `RestSharp/Http.cs` | 241 | 249 | 257 | `FRAMEWORK` | Builds `System.Net.Cookie` from `HttpCookie` including `Domain`/`Path`/etc. (if) vs only `Name`/`Value` (else). | Keep `#if` (full cookie). |
| 59 | `RestSharp/Http.cs` | 321 | – | 324 | `FRAMEWORK` | Copies `webResponse.ContentEncoding` and `webResponse.Server` onto `HttpResponse`. | Keep body; drop directive. |
| 60 | `RestSharp/Http.cs` | 327 | 332 | 334 | `WINDOWS_PHONE` | WP: manual gzip inflate via vendored `GZipStream(webResponse.GetResponseStream()).ReadAsBytes()`; else: `webResponse.GetResponseStream().ReadAsBytes()` (relies on `AutomaticDecompression`). | Keep `#else` (plain `ReadAsBytes()`). |
| 61 | `RestSharp/Http.cs` | 374 | – | 387 | `FRAMEWORK` | `AddRange(HttpWebRequest, string)` helper parsing `bytes=a-b` and calling `r.AddRange(...)`. | Keep body; drop directive. |
| 62 | `RestSharp/IHttp.cs` | 31 | – | 33 | `!SILVERLIGHT` | `bool FollowRedirects { get; set; }` | Keep; drop directive. |
| 63 | `RestSharp/IHttp.cs` | 34 | – | 37 | `FRAMEWORK` | `X509CertificateCollection ClientCertificates`, `int? MaxRedirects`. | Keep; drop directive. |
| 64 | `RestSharp/IHttp.cs` | 56 | – | 66 | `FRAMEWORK` | Sync `HttpResponse Delete()/Get()/Head()/Options()/Post()/Put()/Patch()` and `IWebProxy Proxy`. | Keep; drop directive. |
| 65 | `RestSharp/IRestClient.cs` | 68 | – | 77 | `FRAMEWORK` | `X509CertificateCollection ClientCertificates`, sync `Execute(IRestRequest)` / `Execute<T>`, `IWebProxy Proxy`. | Keep; drop directive. |
| 66 | `RestSharp/IRestRequest.cs` | 110 | – | 138 | `FRAMEWORK` | Three `AddFile(...)` overloads (path / bytes / bytes+contentType). | Keep; drop directive. |
| 67 | `RestSharp/RestClient.Sync.cs` | 1 | – | 120 | `FRAMEWORK` | Entire file: synchronous `RestClient.Execute*` partial. | Keep body; drop directive. |
| 68 | `RestSharp/RestClient.cs` | 44 | – | 46 | `WINDOWS_PHONE` | `UseSynchronizationContext = true;` in ctor. | Delete. |
| 69 | `RestSharp/RestClient.cs` | 218 | – | 223 | `FRAMEWORK` | `public X509CertificateCollection ClientCertificates { get; set; }` | Keep; drop directive. |
| 70 | `RestSharp/RestClient.cs` | 374 | – | 376 | `!SILVERLIGHT` | `http.FollowRedirects = FollowRedirects;` | Keep; drop directive. |
| 71 | `RestSharp/RestClient.cs` | 377 | – | 384 | `FRAMEWORK` | Copies `ClientCertificates` and `MaxRedirects` onto `IHttp`. | Keep; drop directive. |
| 72 | `RestSharp/SharedAssemblyInfo.cs` | 10 | 12 | 15 | `NET_2_0` | `AssemblyCompany` = fork GitHub URL (if) vs `AssemblyCompany("restsharp.org")` + `AssemblyCopyright` (else). | Keep `#else` (or move to SDK-style csproj properties). |

**Core-file total: 72 blocks** (48 single-branch `#if…#endif`, 24 with `#else`; nested once — #51 inside #50).

### 1b. Vendored `RestSharp/Compression/ZLib/*` (DotNetZip-derived)

Every file in this folder is wrapped whole in `#if WINDOWS_PHONE … #endif`, i.e. the entire vendored zlib is **already dead code** in the `FRAMEWORK` build. The inner `NOT`/`NOTUSED`/`POINTLESS` blocks are the upstream author's way of commenting code out.

| File | `#if` line | `#endif` line | Condition | Guarded code | Survives |
|---|---|---|---|---|---|
| `Compression/ZLib/Crc32.cs` | 35 | 469 | `WINDOWS_PHONE` | whole file (`CRC32` class, `CrcCalculatorStream`) | nothing — delete file |
| `Compression/ZLib/FlushType.cs` | 1 | 52 | `WINDOWS_PHONE` | whole file (`FlushType` enum) | delete file |
| `Compression/ZLib/GZipStream.cs` | 29 | 598 | `WINDOWS_PHONE` | whole file (`GZipStream`) | delete file |
| `Compression/ZLib/InfTree.cs` | 62 | 442 | `WINDOWS_PHONE` | whole file | delete file |
| `Compression/ZLib/Inflate.cs` | 64 | 1830 | `WINDOWS_PHONE` | whole file | delete file |
| `Compression/ZLib/ZLib.cs` | 65 | 295 | `WINDOWS_PHONE` | whole file; nested `#if NOTUSED` (122–146) and `#if POINTLESS` (148–177) = commented-out helpers | delete file |
| `Compression/ZLib/ZLibCodec.cs` | 66 | 358 | `WINDOWS_PHONE` | whole file (`ZlibCodec`) | delete file |
| `Compression/ZLib/ZLibConstants.cs` | 63 | 127 | `WINDOWS_PHONE` | whole file | delete file |
| `Compression/ZLib/ZLibStream.cs` | 28 | 875 | `WINDOWS_PHONE` | whole file (`ZlibBaseStream`, `ZlibStream`); nested `#if NOT` (642–652) = commented-out `Read()` | delete file |

**Vendored ZLib total: 12 blocks** (9 file-level `WINDOWS_PHONE` + 3 nested).

### 1c. Vendored `RestSharp/Extensions/MonoHttp/*` (Mono `System.Web` sources, namespace `RestSharp.Contrib`)

All blocks are `#if NET_4_0` (or one `#if !NET_4_0`), which is never defined in this repo, so the `#else`/non-4.0 branch is what currently compiles. Listed compactly; every one is a `NET_4_0`-vs-legacy Mono implementation detail (e.g. `HttpEncoder.Current` delegation vs inline encoding, `AntiXss`-style `HtmlAttributeEncode`, `JavaScriptStringEncode`, `UrlPathEncode`).

| File | Block spans (`#if`→`#endif`, `#else` if any) | Count |
|---|---|---|
| `Extensions/MonoHttp/HtmlEncoder.cs` | 38–40, 44–46, 52/55–57, 78–81, 84–90, 97/99–101, 107/110–113, 119/121–123, 163–216, 217/219–221, 271–273, 304–308, 336/339–345, 351–353, 378–382, 401–403, 424–426, 473–475, 490–492, 499–503, 516–518, 525–527, 533–535, 540–542, 571–573 (`!NET_4_0`) | 25 |
| `Extensions/MonoHttp/HttpUtility.cs` | 83/85–87, 89/91–93, 98/106–108, 472/474–476, 510/518–520, 532/534–536, 541/543–545, 551/559–561, 573/575–577, 582/584–586, 589–675, 678/680–682 | 12 |
| `Extensions/MonoHttp/Helpers.cs` | none | 0 |

Survives: none — replace folder with BCL (`System.Net.WebUtility` / `System.Web.HttpUtility`), see §2.

**MonoHttp total: 37 blocks.**

**Grand total under `RestSharp/`: 121 `#if` blocks (72 core + 12 ZLib + 37 MonoHttp), 275 directive lines.**

---

## 2. Call sites of vendored code from outside the vendored folders

### 2a. `RestSharp/Compression/ZLib` (`RestSharp.Compression.ZLib` namespace)

| File:line | Calling expression | Active in `FRAMEWORK` build? | Proposed replacement |
|---|---|---|---|
| `RestSharp/Http.cs:27` | `using RestSharp.Compression.ZLib;` (inside `#if WINDOWS_PHONE`) | No | Delete. |
| `RestSharp/Http.cs:329` | `response.RawBytes = new GZipStream(webResponse.GetResponseStream()).ReadAsBytes();` (inside `#if WINDOWS_PHONE`) | No | Delete; the `#else` branch already relies on `HttpWebRequest.AutomaticDecompression = Deflate \| GZip` (set in `Http.Async.cs:377` and in `Http.Sync.cs` `ConfigureWebRequest`). If manual inflate were ever needed, use `System.IO.Compression.GZipStream(stream, CompressionMode.Decompress)`. |

No other file under `RestSharp/` (or `RestSharp.csproj` besides the `<Compile>` entries) references `ZlibStream`, `GZipStream`, `DeflateStream`, `ZlibCodec`, or `CRC32`. The 9 `<Compile Include="Compression\ZLib\*.cs">` entries (`RestSharp.csproj:96–104`) can be removed together with the folder.

### 2b. `RestSharp/Extensions/MonoHttp` (`RestSharp.Contrib` namespace — `HttpUtility`, `HttpEncoder`/`HtmlEncoder`, `Helpers`)

| File:line | Calling expression | Proposed BCL replacement (netstandard2.0 / net8.0) |
|---|---|---|
| `RestSharp/Extensions/StringExtensions.cs:26` | `using RestSharp.Contrib;` (`#if Net2`, dead) | Delete. |
| `RestSharp/Extensions/StringExtensions.cs:37` | `using RestSharp.Contrib;` (`#if FRAMEWORK \|\| MONOTOUCH \|\| MONODROID`) | `using System.Net;` (for `WebUtility`) and/or `using System.Web;` (for `HttpUtility`, needs `System.Web.HttpUtility` package on netstandard2.0; built into net8.0). |
| `RestSharp/Extensions/StringExtensions.cs:47` | `HttpUtility.UrlDecode(input)` | `System.Net.WebUtility.UrlDecode(input)` (or `System.Web.HttpUtility.UrlDecode`). |
| `RestSharp/Extensions/StringExtensions.cs:61` | `HttpUtility.HtmlDecode(input)` | `System.Net.WebUtility.HtmlDecode(input)`. |
| `RestSharp/Extensions/StringExtensions.cs:66` | `HttpUtility.HtmlEncode(input)` | `System.Net.WebUtility.HtmlEncode(input)`. |
| `RestSharp/Extensions/StringExtensions.cs:72` | `HttpUtility.HtmlAttributeEncode(input)` | `System.Web.HttpUtility.HtmlAttributeEncode(input)` (no `WebUtility` equivalent; `System.Web.HttpUtility` NuGet package for netstandard2.0, in-box on net8.0). |
| `RestSharp/Authenticators/OAuth/OAuthWorkflow.cs:5` | `using RestSharp.Contrib;` (`#if !WINDOWS_PHONE`) | `using System.Web;` |
| `RestSharp/Authenticators/OAuth/OAuthWorkflow.cs:217` | `HttpUtility.ParseQueryString(uri.Query)` | `System.Web.HttpUtility.ParseQueryString(uri.Query)` (returns `NameValueCollection`, keeps `.AllKeys` loop at line 223 unchanged). No `WebUtility` equivalent. |
| `RestSharp/Authenticators/OAuth1Authenticator.cs:10` | `using RestSharp.Contrib;` (`#else` of `#if WINDOWS_PHONE`) | `using System.Net;` (or `System.Web`). |
| `RestSharp/Authenticators/OAuth1Authenticator.cs:194` | `HttpUtility.UrlDecode(oauth.Signature)` | `System.Net.WebUtility.UrlDecode(oauth.Signature)`. |

Notes:
- `RestSharp/Authenticators/OAuth/Extensions/StringExtensions.cs:57` mentions `HttpUtility` only in a comment (its own `UrlEncodeRelaxed`/`UrlEncodeStrict` implementation); no code dependency.
- Behavioural caveat: Mono `HttpUtility.UrlDecode` decodes `+` to space, as does `WebUtility.UrlDecode` and `System.Web.HttpUtility.UrlDecode`; `HtmlEncode` output for non-ASCII differs slightly between Mono and BCL (`WebUtility.HtmlEncode` encodes chars 160–255 as numeric entities) — unlikely to matter for RestSharp usage.
- The 3 `<Compile Include="Extensions\MonoHttp\*.cs">` entries (`RestSharp.csproj:107–109`) go away with the folder. For netstandard2.0 add `<PackageReference Include="System.Web.HttpUtility" />` only if `HtmlAttributeEncode`/`ParseQueryString` are kept; otherwise `System.Net.WebUtility` suffices.

---

## 3. Summary table

| File (under `RestSharp/`) | `#if` blocks | Dead symbols involved | Uses vendored ZLib / MonoHttp? |
|---|---|---|---|
| `Authenticators/NtlmAuthenticator.cs` | 1 | – (FRAMEWORK only) | no |
| `Authenticators/OAuth/Extensions/CollectionExtensions.cs` | 2 | SILVERLIGHT, WINDOWS_PHONE | no |
| `Authenticators/OAuth/Extensions/StringExtensions.cs` | 4 | SILVERLIGHT, WindowsPhone | no (comment mention only) |
| `Authenticators/OAuth/HttpPostParameterType.cs` | 1 | SILVERLIGHT, WINDOWS_PHONE | no |
| `Authenticators/OAuth/OAuthParameterHandling.cs` | 1 | SILVERLIGHT, WINDOWS_PHONE | no |
| `Authenticators/OAuth/OAuthSignatureMethod.cs` | 1 | SILVERLIGHT, WINDOWS_PHONE | no |
| `Authenticators/OAuth/OAuthSignatureTreatment.cs` | 1 | SILVERLIGHT, WINDOWS_PHONE | no |
| `Authenticators/OAuth/OAuthTools.cs` | 3 | SILVERLIGHT, WINDOWS_PHONE | no |
| `Authenticators/OAuth/OAuthType.cs` | 1 | SILVERLIGHT, WINDOWS_PHONE | no |
| `Authenticators/OAuth/OAuthWebQueryInfo.cs` | 1 | SILVERLIGHT, WINDOWS_PHONE | no |
| `Authenticators/OAuth/OAuthWorkflow.cs` | 3 | SILVERLIGHT, WINDOWS_PHONE | **MonoHttp** (`HttpUtility.ParseQueryString`) |
| `Authenticators/OAuth/WebPairCollection.cs` | 1 | WINDOWS_PHONE | no |
| `Authenticators/OAuth/WebParameter.cs` | 3 | Smartphone, SILVERLIGHT, WINDOWS_PHONE | no |
| `Authenticators/OAuth/WebParameterCollection.cs` | 2 | SILVERLIGHT, WINDOWS_PHONE | no |
| `Authenticators/OAuth1Authenticator.cs` | 1 | WINDOWS_PHONE | **MonoHttp** (`HttpUtility.UrlDecode`) |
| `Extensions/MiscExtensions.cs` | 2 | WINDOWS_PHONE (+FRAMEWORK) | no |
| `Extensions/ReflectionExtensions.cs` | 3 | – (FRAMEWORK only) | no |
| `Extensions/StringExtensions.cs` | 5 | Net2, SILVERLIGHT, WINDOWS_PHONE, MONOTOUCH, MONODROID | **MonoHttp** (`UrlDecode`, `HtmlDecode`, `HtmlEncode`, `HtmlAttributeEncode`) |
| `FileParameter.cs` | 1 | – (FRAMEWORK only) | no |
| `Http.Async.cs` | 12 | SILVERLIGHT, WINDOWS_PHONE, MONOTOUCH, MONODROID | no |
| `Http.Sync.cs` | 2 | MONOTOUCH, MONODROID | no |
| `Http.cs` | 10 | WINDOWS_PHONE, SILVERLIGHT | **ZLib** (`GZipStream`, WP-only branch) |
| `IHttp.cs` | 3 | SILVERLIGHT | no |
| `IRestClient.cs` | 1 | – (FRAMEWORK only) | no |
| `IRestRequest.cs` | 1 | – (FRAMEWORK only) | no |
| `RestClient.Sync.cs` | 1 | – (FRAMEWORK only) | no |
| `RestClient.cs` | 4 | WINDOWS_PHONE, SILVERLIGHT | no |
| `SharedAssemblyInfo.cs` | 1 | NET_2_0 | no |
| `Compression/ZLib/Crc32.cs` | 1 | WINDOWS_PHONE | *is* vendored ZLib |
| `Compression/ZLib/FlushType.cs` | 1 | WINDOWS_PHONE | *is* vendored ZLib |
| `Compression/ZLib/GZipStream.cs` | 1 | WINDOWS_PHONE | *is* vendored ZLib |
| `Compression/ZLib/InfTree.cs` | 1 | WINDOWS_PHONE | *is* vendored ZLib |
| `Compression/ZLib/Inflate.cs` | 1 | WINDOWS_PHONE | *is* vendored ZLib |
| `Compression/ZLib/ZLib.cs` | 3 | WINDOWS_PHONE, NOTUSED, POINTLESS | *is* vendored ZLib |
| `Compression/ZLib/ZLibCodec.cs` | 1 | WINDOWS_PHONE | *is* vendored ZLib |
| `Compression/ZLib/ZLibConstants.cs` | 1 | WINDOWS_PHONE | *is* vendored ZLib |
| `Compression/ZLib/ZLibStream.cs` | 2 | WINDOWS_PHONE, NOT | *is* vendored ZLib |
| `Extensions/MonoHttp/HtmlEncoder.cs` | 25 | NET_4_0 | *is* vendored MonoHttp |
| `Extensions/MonoHttp/HttpUtility.cs` | 12 | NET_4_0 | *is* vendored MonoHttp |
| `Extensions/MonoHttp/Helpers.cs` | 0 | – | *is* vendored MonoHttp |
| **Total (40 files, 37 with blocks)** | **121** | | |

---

## 4. `RestSharp/RestSharp.csproj` — conditional / linked `<Compile Include>` entries

- **Platform-conditional `<Compile>` entries: none.** No `<Compile>` element carries a `Condition` attribute; the only `Condition`s in the project are the standard `Configuration`/`Platform` property-group selectors (lines 4, 5, 36, 47).
- **Linked files (`<Link>` child or `..\` path in `<Compile Include>`): none.** All 79 `<Compile Include>` entries (lines 76–160) are relative paths inside `RestSharp/`. The only `..\` path in the file is the `Newtonsoft.Json` `<HintPath>` (`..\packages\Newtonsoft.Json.4.5.1\lib\net35\Newtonsoft.Json.dll`, line 60) — a reference, not a compile item.
- `SharedAssemblyInfo.cs` (line 157) is compiled directly from `RestSharp/` (not linked from the solution root as in upstream RestSharp).
- Relevant properties: `<TargetFrameworkVersion>v3.5</TargetFrameworkVersion>` (line 13), `<TargetFrameworkProfile>Client</TargetFrameworkProfile>` (line 34), `DefineConstants` = `TRACE;DEBUG;FRAMEWORK` / `TRACE;FRAMEWORK` (lines 41, 51); `<RequiredTargetFramework>3.5</RequiredTargetFramework>` on the `System.Core`/`System.Xml.Linq`/`System.Data.DataSetExtensions` references (lines 64, 67, 70).
- Compile entries that become removable once vendored code is dropped: lines 96–104 (`Compression\ZLib\*`, 9 files) and 107–109 (`Extensions\MonoHttp\*`, 3 files). `<None Include="T4Helper\*.tt">` (lines 152–156) are unrelated design-time artifacts.

(Out of scope but noted: the sibling projects `RestSharp.Net2`, `RestSharp.Silverlight`, `RestSharp.WindowsPhone*`, `RestSharp.MonoTouch`, `RestSharp.MonoDroid` are the ones that link `RestSharp/*.cs` files and define the dead symbols; they, not `RestSharp.csproj`, are where platform-conditional compilation lived.)
