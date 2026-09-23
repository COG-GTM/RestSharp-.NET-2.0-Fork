# Tier 3 — Shared extensions and validation helpers

Reverse-engineered business specification (read-only analysis). Scope: `RestSharp/Extensions/StringExtensions.cs`, `RestSharp/Extensions/MiscExtensions.cs`, `RestSharp/Extensions/ReflectionExtensions.cs`, `RestSharp/Extensions/XmlExtensions.cs`, `RestSharp/Validation/Validate.cs`, `RestSharp/Validation/Require.cs`, plus the vendored encoder they call into (`RestSharp/Extensions/MonoHttp/*`) where needed to state a rule precisely.

---

## 1. Purpose

This component is the shared "plumbing" layer of the library. It contains no networking logic of its own; instead it supplies the small, reusable rules that every other component depends on:

| Capability | Business meaning | Primary consumers |
| --- | --- | --- |
| Fuzzy name matching (`GetNameVariants`, `ToPascalCase`, `ToCamelCase`, `AddUnderscores`, `AddDashes`, `RemoveUnderscoresAndDashes`) | Lets a caller's C# class property `StartDate` bind to a payload field named `start_date`, `start-date`, `startDate` or `startdate` without any attribute or mapping configuration. This is the headline "fuzzy element name matching" feature advertised in `README.markdown:19`. | `RestSharp/Deserializers/JsonDeserializer.cs — Map()`, `RestSharp/Deserializers/XmlDeserializer.cs — GetElementByName()/GetAttributeByName()/HandleListDerivative()`, `RestSharp/Deserializers/XmlAttributeDeserializer.cs`, `RestSharp/Serializers/SerializeAsAttribute.cs — TransformName()` |
| Lenient JSON date parsing (`ParseJsonDate`) | Accepts the handful of date encodings commonly emitted by REST APIs (Unix seconds, Microsoft `/Date(...)/`, JavaScript `new Date(...)`, ISO-8601 and a few fixed formats) so that callers get a `DateTime` without writing converters. | `RestSharp/Deserializers/JsonDeserializer.cs:146` |
| URL / HTML encode and decode (`UrlEncode`, `UrlDecode`, `HtmlEncode`, `HtmlDecode`, `HtmlAttributeEncode`) | Builds safe query strings and URL segments for outgoing requests; decodes server content. | `RestSharp/Http.cs:268`, `RestSharp/RestClient.cs:299`, `RestSharp/RestClient.cs:344` |
| Stream and byte helpers (`ReadAsBytes`, `CopyTo`, `SaveAs`, `AsString`) | Reads the raw HTTP response body into memory, converts it to text, writes downloaded files to disk, and streams uploaded file content into the request body. | `RestSharp/Http.cs:329-333`, `RestSharp/RestResponse.cs:67`, `RestSharp/HttpResponse.cs:61`, `RestSharp/RestRequest.cs:127` |
| Reflection helpers (`GetAttribute<T>`, `IsSubclassOfRawGeneric`, `ChangeType`, `FindEnumValue`) | Reads the library's own serialization attributes off caller types, detects `List<T>`-derived models, converts payload text to the target CLR type, and resolves enum members by fuzzy name. | Both XML deserializers, `JsonDeserializer`, `RestSharp/Serializers/XmlSerializer.cs` |
| XML namespace helper (`AsNamespaced`) | Turns a plain element name plus an optional namespace into an `XName`, so the same lookup code works for namespaced and non-namespaced documents. | `XmlDeserializer`, `XmlAttributeDeserializer`, `XmlSerializer` |
| Argument validation (`Require.Argument`, `Validate.IsBetween`, `Validate.IsValidLength`) | Intended as guard clauses for public API arguments. | **None in the current code base** — see §8. |

Consumers are (a) the library's own serializers/deserializers and HTTP layer, and (b) third-party application code, because every type here is `public` and ships in the published assembly (`restsharp.nuspec`).

---

## 2. Inputs and outputs

### 2.1 `RestSharp.Extensions.StringExtensions` (public static class, `RestSharp/Extensions/StringExtensions.cs:43`)

All members are extension methods on `string` unless stated otherwise. None accept `null` safely except where noted.

| Member | Signature | Returns | Null input | Meaning |
| --- | --- | --- | --- | --- |
| `UrlDecode` | `string UrlDecode(this string input)` (`:45-48`) | Decoded string | Returns `null` (delegated; `MonoHttp/HttpUtility.cs:134-135`) | Percent/`+` decoding, UTF-8 |
| `UrlEncode` | `string UrlEncode(this string input)` (`:54-57`) | Encoded string | Throws `ArgumentNullException` from `Uri.EscapeDataString` | RFC-style data escaping (see §5.3) |
| `HtmlDecode` | `string HtmlDecode(this string input)` (`:59-62`) | Decoded string | Delegated | HTML entity decoding |
| `HtmlEncode` | `string HtmlEncode(this string input)` (`:64-67`) | Encoded string | Returns `null` (`MonoHttp/HtmlEncoder.cs:260-261`) | HTML entity encoding |
| `HtmlAttributeEncode` | `string HtmlAttributeEncode(this string input)` (`:69-74`) | Encoded string | Delegated | **`FRAMEWORK` builds only** |
| `HasValue` | `bool HasValue(this string input)` (`:81-84`) | `bool` | `false` | `!string.IsNullOrEmpty` |
| `RemoveUnderscoresAndDashes` | `string RemoveUnderscoresAndDashes(this string input)` (`:91-94`) | String without `_` or `-` | `NullReferenceException` | Name normalisation |
| `ParseJsonDate` | `DateTime ParseJsonDate(this string input, CultureInfo culture)` (`:101-128`) | `DateTime`; `default(DateTime)` = `0001-01-01T00:00:00` when nothing matches | `NullReferenceException` | See §5.1 |
| `RemoveSurroundingQuotes` | `string RemoveSurroundingQuotes(this string input)` (`:135-143`) | String without one leading **and** trailing `"` | `NullReferenceException` | Only strips when both ends are quotes |
| `Matches` | `bool Matches(this string input, string pattern)` (`:203-206`) | `bool` | Throws | `Regex.IsMatch`, default options |
| `ToPascalCase` | `string ToPascalCase(this string s, CultureInfo culture)` (`:213-216`) | Pascal-cased string | Returns input (`:226-227`) | Delegates with `removeUnderscores: true` |
| `ToPascalCase` | `string ToPascalCase(this string text, bool removeUnderscores, CultureInfo culture)` (`:224-251`) | Pascal-cased string | Returns input | See §5.2 |
| `ToCamelCase` | `string ToCamelCase(this string s, CultureInfo culture)` (`:258-261`) | Camel-cased string | `ArgumentOutOfRangeException` for `""`/`null` via `MakeInitialLowerCase` | Pascal-case then lower first char |
| `MakeInitialLowerCase` | `string MakeInitialLowerCase(this string word)` (`:268-271`) | String with first char lowered | Throws | **Uses culture-less `ToLower()`** |
| `IsUpperCase` | `bool IsUpperCase(this string inputString)` (`:278-281`) | `bool` | Throws | `^[A-Z]+$` — ASCII only, `""` → `false` |
| `AddUnderscores` | `string AddUnderscores(this string pascalCasedWord)` (`:288-294`) | Snake-ish string | Throws | See §5.2 |
| `AddDashes` | `string AddDashes(this string pascalCasedWord)` (`:301-309`) | Kebab-ish string | Throws | See §5.2 |
| `GetNameVariants` | `IEnumerable<string> GetNameVariants(this string name, CultureInfo culture)` (`:317-341`) | Lazily yielded variants | Yields nothing for `null`/`""` (`:319-320`) | See §3.1 |

### 2.2 `RestSharp.Extensions.MiscExtensions` (`RestSharp/Extensions/MiscExtensions.cs:27`)

| Member | Signature | Returns | Notes |
| --- | --- | --- | --- |
| `SaveAs` | `void SaveAs(this byte[] input, string path)` (`:35-38`) | — | `File.WriteAllBytes`; excluded on `WINDOWS_PHONE` (`:29`) |
| `ReadAsBytes` | `byte[] ReadAsBytes(this Stream input)` (`:46-58`) | Whole stream in memory | 16 KiB read buffer; input stream is **not** disposed |
| `CopyTo` | `void CopyTo(this Stream input, Stream output)` (`:65-75`) | — | 32 KiB buffer; no flush/dispose |
| `AsString` | `string AsString(this JToken token)` (`:81-84`) | String token value, else `token.ToString()` | Newtonsoft `JToken` |
| `AsString` | `string AsString(this JToken token, CultureInfo culture)` (`:91-100`) | Culture-converted string | `JValue` path calls `ChangeType(typeof(string), culture)`; `NullReferenceException` if `JValue.Value` is `null` |
| `AsString` | `string AsString(this byte[] buffer)` (`:108-152`) | Decoded text | `null` → `""` (`:110`); encoding rules in §5.4 |

### 2.3 `RestSharp.Extensions.ReflectionExtensions` (`RestSharp/Extensions/ReflectionExtensions.cs:27`)

| Member | Signature | Returns | Notes |
| --- | --- | --- | --- |
| `GetAttribute<T>` | `T GetAttribute<T>(this MemberInfo prop) where T : Attribute` (`:35-37`) | Attribute instance or `null` | `Attribute.GetCustomAttribute` — **inherited attributes included** by that overload's default |
| `GetAttribute<T>` | `T GetAttribute<T>(this Type type) where T : Attribute` (`:45-47`) | Attribute instance or `null` | Same |
| `IsSubclassOfRawGeneric` | `bool IsSubclassOfRawGeneric(this Type toCheck, Type generic)` (`:55-64`) | `bool` | Walks the base chain comparing open generic definitions |
| `ChangeType` | `object ChangeType(this object source, Type newType)` (`:66-73`) | Converted value | `FRAMEWORK`: `Convert.ChangeType(source, newType)` → **current thread culture** |
| `ChangeType` | `object ChangeType(this object source, Type newType, CultureInfo culture)` (`:75-82`) | Converted value | `FRAMEWORK`: honours `culture`; otherwise `culture` is **discarded** (`:80`) |
| `FindEnumValue` | `object FindEnumValue(this Type type, string value, CultureInfo culture)` (`:92-101`) | Enum member | `FRAMEWORK`: fuzzy variant match, throws if none; otherwise `Enum.Parse(..., ignoreCase: true)` |

### 2.4 `RestSharp.Extensions.XmlExtensions` (`RestSharp/Extensions/XmlExtensions.cs:24`)

| Member | Signature | Returns | Notes |
| --- | --- | --- | --- |
| `AsNamespaced` | `XName AsNamespaced(this string name, string @namespace)` (`:32-39`) | `XName` | If `@namespace` is null/empty the plain name is used via implicit `string`→`XName` conversion; otherwise `XName.Get(name, @namespace)` |

### 2.5 Validation helpers

| Member | Signature | Behaviour |
| --- | --- | --- |
| `RestSharp.Validation.Require.Argument` | `static void Argument(string argumentName, object argumentValue)` (`RestSharp/Validation/Require.cs:31-35`) | Throws `ArgumentException("Argument cannot be null.", argumentName)` when the value is `null`; otherwise no-op |
| `RestSharp.Validation.Validate.IsBetween` | `static void IsBetween(int value, int min, int max)` (`RestSharp/Validation/Validate.cs:32-36`) | Throws `ArgumentException` when `value < min || value > max` |
| `RestSharp.Validation.Validate.IsValidLength` | `static void IsValidLength(string value, int maxSize)` (`RestSharp/Validation/Validate.cs:43-50`) | `null` is accepted silently (`:44-45`); throws `ArgumentException` when `value.Length > maxSize` |

Both validation types are plain `public class` (not static, no private constructor), so they can be instantiated.

---

## 3. Behavioral rules

### 3.1 Name-variant generation and its ordering contract

`GetNameVariants` yields up to seven candidates, **in this fixed order** (`RestSharp/Extensions/StringExtensions.cs:317-341`):

| # | Variant | Example for `StartDate` |
| --- | --- | --- |
| 1 | the name as-is | `StartDate` |
| 2 | camel case | `startDate` |
| 3 | lower case (culture-sensitive) | `startdate` |
| 4 | underscored | `Start_Date` |
| 5 | underscored, lower-cased | `start_date` |
| 6 | dashed | `Start-Date` |
| 7 | dashed, lower-cased | `start-date` |

Ordering is a behavioural contract: `JsonDeserializer` takes the **first** variant present in the JSON object (`RestSharp/Deserializers/JsonDeserializer.cs:94`). If a payload contains both `StartDate` and `start_date`, the exact-case name wins. Duplicates are not removed — for an all-lowercase property name several variants are identical, which only costs lookups.

The sequence is lazy (`yield return`), so consumers that stop early never pay for later variants.

Evidence: `RestSharp.Tests/JsonTests.cs:237-263` (underscored payload binds to Pascal properties), `RestSharp.Tests/JsonTests.cs:266-291` (dashed payload), `RestSharp.Tests/JsonTests.cs:202-234` (exact names).

### 3.2 XML element/attribute lookup precedence (the XML side of fuzzy matching)

`XmlDeserializer` does **not** use `GetNameVariants`; it hard-codes its own shorter ladder (`RestSharp/Deserializers/XmlDeserializer.cs:326-365`):

1. exact `XName` (property name + configured namespace);
2. `name.ToLower()` (culture-less);
3. `name.ToCamelCase(Culture)`;
4. special case: a property literally named `Value` maps to the current element itself (`:346-349`);
5. any descendant whose local name, with `_` and `-` removed, equals the property name — ordered by ancestor depth, shallowest first (`:352-354`);
6. same as 5 but compared against the lower-cased property name (`:355-357`);
7. otherwise `null`.

Attributes follow the same ladder minus the `Value` and depth-ordering steps (`:367-395`). `XmlAttributeDeserializer` repeats the ladder with two differences: its `Value` special case compares the `XName` to the string `"Value"` (`RestSharp/Deserializers/XmlAttributeDeserializer.cs:343`) and it has only one sanitized-name pass (`:349`).

Implicit contract: because step 5 searches **all descendants**, a deeply nested element can satisfy a top-level property when no shallower match exists; "shallowest wins" is the only tie-break.

Evidence: `RestSharp.Tests/XmlTests.cs:365-393` (underscored elements), `:397-425` (dashed elements), `:428-456` (lower-cased underscored elements), `RestSharp.Tests/NamespacedXmlTests.cs:133-162` (underscores with an explicit namespace).

### 3.3 List-element name resolution

`XmlDeserializer.HandleListDerivative` tries, in order (`RestSharp/Deserializers/XmlDeserializer.cs:247-265`): the item type name, its `ToLower()` form, its `ToCamelCase(Culture)` form, then any descendant whose local name with underscores/dashes removed equals the type name. First non-empty match wins.

`XmlAttributeDeserializer.HandleListDerivative` evaluates the same three candidates but with `if` statements that **overwrite** rather than stop (`RestSharp/Deserializers/XmlAttributeDeserializer.cs:254-267`), so the effective precedence is reversed: camel case beats lower case beats exact. See §8.

Evidence: `RestSharp.Tests/XmlTests.cs:70-95` (standalone list with and without matching class case).

### 3.4 Serialization name transformation

`SerializeAsAttribute.TransformName` (`RestSharp/Serializers/SerializeAsAttribute.cs:67-79`) applies `Name ?? input` and then the selected `NameStyle`: `CamelCase` → `ToCamelCase(Culture)`, `PascalCase` → `ToPascalCase(Culture)`, `LowerCase` → `name.ToLower()` (culture-less). For `NameStyle.AsIs` the method returns **`input`, not `name`** — an explicit `Name` is ignored on that path (see §8). Default `Culture` is `CultureInfo.InvariantCulture` (`:34`).

### 3.5 Date parsing precedence

`ParseJsonDate` (`RestSharp/Extensions/StringExtensions.cs:101-128`) applies, strictly in order:

1. strip all `\n` and `\r`;
2. strip one pair of surrounding double quotes;
3. if the whole remaining string parses as `Int64` → treat as **Unix seconds since 1970-01-01 UTC**;
4. else if it contains `/Date(` → Microsoft format extraction;
5. else if it contains `new Date(` → strip **all** spaces, then JavaScript format extraction;
6. else → fixed-format parse (§5.1); on failure `default(DateTime)`.

Rule 3 has priority over everything else, so a bare number is always a Unix timestamp regardless of magnitude.

Evidence: `RestSharp.Tests/JsonTests.cs:339-347` with `SampleData` value `1309421746` → `2011-06-30 08:15:46Z`; `:328-336` (JavaScript `new Date(...)`); `:317-325` (ISO).

### 3.6 Deserializer-level date precedence (context for the helper)

`JsonDeserializer` uses `ParseJsonDate` only as the last resort: an explicit `DateFormat` wins, then a Newtonsoft `Date` token (converted with `ToUniversalTime()`), then `ParseJsonDate` (`RestSharp/Deserializers/JsonDeserializer.cs:131-153`). `XmlDeserializer` never calls `ParseJsonDate`; it uses `DateTime.ParseExact` with `DateFormat` or `DateTime.Parse` with `Culture` (`RestSharp/Deserializers/XmlDeserializer.cs:159-171`).

### 3.7 Nullable and enum handling in the reflection helpers

* `Nullable<T>` is unwrapped by the **callers**, not by this component: both deserializers test `type.IsGenericType && GetGenericTypeDefinition() == typeof(Nullable<>)` and substitute the underlying type (`RestSharp/Deserializers/JsonDeserializer.cs:103-106`, `RestSharp/Deserializers/XmlDeserializer.cs:125-134`). `ChangeType` and `FindEnumValue` therefore never see a nullable type. In XML an empty value sets the property to `null` before conversion (`RestSharp/Deserializers/XmlDeserializer.cs:127-132`).
* `FindEnumValue` on `FRAMEWORK` enumerates the enum's members and selects the first whose **name variants contain the payload value**, compared with `StringComparer.Create(culture, ignoreCase: true)` (`RestSharp/Extensions/ReflectionExtensions.cs:95-97`). So member `SoSo` matches `SoSo`, `soSo`, `soso`, `So_So`, `so_so`, `So-So`, `so-so`.
* Matching direction matters: the *enum member* is expanded into variants and the *payload value* must equal one of them. A payload value that is itself an unusual variant of a plain member name (for example `FRIENDLY` for member `Friendly`) matches only because the comparison is case-insensitive.

Evidence: `RestSharp.Tests/JsonTests.cs:158-173` with `RestSharp.Tests/SampleData/jsonenums.txt` covering `FRIENDLY`, `friendly`, `SoSo`, `So_So`, `so_so`, `So-So`.

### 3.8 Generic-type detection

`IsSubclassOfRawGeneric` walks from the supplied type up the base chain, comparing the open generic definition at each level, stopping at `object` (`RestSharp/Extensions/ReflectionExtensions.cs:55-64`). It is used to decide whether a caller model *is* a list (`RestSharp/Deserializers/XmlDeserializer.cs:62`, `:199`), which in turn selects the list-derivative code path that also maps sibling properties (`:271-274`).

### 3.9 XML namespace application

`AsNamespaced` applies the namespace only when it "has value" (`RestSharp/Extensions/XmlExtensions.cs:35-36`). Empty and `null` namespaces are treated identically. When the caller supplies no namespace, `XmlDeserializer` strips namespaces from the whole document first (`RestSharp/Deserializers/XmlDeserializer.cs:54-57`, `RemoveNamespace` at `:74-87`), so subsequent namespace-less lookups succeed. Ordering matters: the `RootElement` lookup happens **before** namespace stripping (`:48-51`), so a `RootElement` in a namespaced document is resolved against the original names.

### 3.10 Stream and content ordering

`Http` reads the response body eagerly into `RawBytes` via `ReadAsBytes`, decompressing first when applicable (`RestSharp/Http.cs:329-333`); `RestResponse.Content` is then a lazily-computed, cached `RawBytes.AsString()` (`RestSharp/RestResponse.cs:63-70`). Consequence: the text decoding rules of §5.4 are applied once per response and cached.

---

## 4. Validation and error behavior

| Situation | Behaviour | Citation |
| --- | --- | --- |
| `ParseJsonDate` cannot match any known shape | Returns `default(DateTime)` (`0001-01-01`) — **silently**, no exception, no flag | `RestSharp/Extensions/StringExtensions.cs:159-164` |
| `ExtractDate` regex does not match | Returns `DateTime.MinValue` — silently | `RestSharp/Extensions/StringExtensions.cs:169-194` |
| `ParseJsonDate` on a non-numeric, non-date string | Falls through to `ParseFormattedDate` → `default(DateTime)` | `:127`, `:159-164` |
| `RemoveSurroundingQuotes` on `"` (single quote char) | `StartsWith` and `EndsWith` both true → `Substring(1, -1)` throws `ArgumentOutOfRangeException` | `:137-141` |
| `MakeInitialLowerCase("")` / `ToCamelCase("")` | `Substring(0, 1)` throws `ArgumentOutOfRangeException`; `GetNameVariants` avoids this by short-circuiting on empty input | `:268-271`, `:319-320` |
| Any string helper called on `null` | `NullReferenceException` / `ArgumentNullException` — no guard clauses anywhere in `StringExtensions` | `:91-94`, `:288-294` |
| `FindEnumValue` with no matching member (`FRAMEWORK`) | `First(...)` throws `InvalidOperationException` ("Sequence contains no matching element") | `RestSharp/Extensions/ReflectionExtensions.cs:95-97` |
| `FindEnumValue` with no matching member (non-`FRAMEWORK`) | `Enum.Parse` throws `ArgumentException` | `:99` |
| `ChangeType` with an unconvertible value | Propagates `FormatException` / `InvalidCastException` from `Convert.ChangeType` | `:66-82` |
| `AsString(byte[])` with `null` | Returns `""` | `RestSharp/Extensions/MiscExtensions.cs:110` |
| `AsString(byte[])` with a 1- or 2-byte buffer (non-`FRAMEWORK`) | Indexes `buffer[1]`/`buffer[2]` unconditionally → `IndexOutOfRangeException` | `:129` |
| `AsString(JToken, CultureInfo)` when `JValue.Value` is `null` | `NullReferenceException` on `value.Value.ChangeType(...)` | `:96` |
| `ReadAsBytes` / `CopyTo` | No length cap, no timeout, no cancellation; whole payload is buffered in memory; the source stream is neither disposed nor rewound, so the operations are **not restartable** — a second call returns empty | `:46-75` |
| `SaveAs` | Delegates to `File.WriteAllBytes`; all IO exceptions propagate; overwrites silently | `:35-38` |
| `Require.Argument` | Throws `ArgumentException` (not `ArgumentNullException`) for a null value | `RestSharp/Validation/Require.cs:31-35` |
| `Validate.IsBetween` | Documented as "exclusive of min/max" but implemented as inclusive (`value < min || value > max`) | `RestSharp/Validation/Validate.cs:26-36` |
| `Validate.IsValidLength(null, n)` | Returns without error | `RestSharp/Validation/Validate.cs:44-45` |

Retry/restartability: nothing in this component retries. The date helpers degrade to a sentinel value rather than failing, which means a malformed date is indistinguishable from a real `0001-01-01` downstream.

---

## 5. Core computations

### 5.1 Date formats

| Input shape | Formula / rule | Result kind | Citation |
| --- | --- | --- | --- |
| Any `Int64`-parsable string, e.g. `1309421746` | `1970-01-01T00:00:00Z + value seconds` | `DateTimeKind.Utc` (epoch is constructed as UTC and `AddSeconds` preserves kind) | `RestSharp/Extensions/StringExtensions.cs:108-113` |
| Contains `/Date(` | Regex `\\/Date\((-?\d+)(-|\+)?([0-9]{4})?\)\\/`; group 1 = **milliseconds** since epoch; optional group 3 = `HHmm` offset, added when group 2 is `+`, subtracted otherwise | Utc | `:115-118`, `:167-195` |
| Contains `new Date(` | All spaces removed first, then regex `newDate\((-?\d+)*\)`; group 1 = milliseconds since epoch; no offset group, so the offset adjustment never applies | Utc | `:120-125` |
| Anything else | `DateTime.TryParseExact` against the fixed list below, `DateTimeStyles.None`, supplied culture | Unspecified/Utc per format | `:145-165` |

Fixed format list, in order: `"u"`, `"s"`, `"yyyy'-'MM'-'dd'T'HH':'mm':'ss'Z'"`, `"yyyy-MM-ddTHH:mm:ssZ"`, `"yyyy-MM-dd HH:mm:ssZ"`, `"yyyy-MM-ddTHH:mm:ss"`, `"yyyy-MM-ddTHH:mm:sszzzzzz"`, `"M/d/yyyy h:mm:ss tt"` (`:147-156`). Note that fractional seconds are **not** in the list, and neither is a bare date (`yyyy-MM-dd`).

Worked examples:

| Input | Output |
| --- | --- |
| `"1309421746"` (quoted) | `2011-06-30T08:15:46Z` (`RestSharp.Tests/JsonTests.cs:339-347`) |
| `new Date(-1871248175000)` | `1910-09-25T09:30:25Z` (`RestSharp.Tests/JsonTests.cs:328-336`) |
| `1910-09-25T09:30:25Z` | `1910-09-25T09:30:25` via format 3 (`RestSharp.Tests/JsonTests.cs:317-325`) |
| `2011-06-30 08:15:46.9` | `0001-01-01T00:00:00` — no format has fractional seconds |

### 5.2 Casing algorithms

**`ToPascalCase(text, removeUnderscores, culture)`** (`RestSharp/Extensions/StringExtensions.cs:224-251`):

1. empty/null → returned unchanged;
2. every `_` becomes a space;
3. the join string is `""` when `removeUnderscores` is true, otherwise `"_"`;
4. split on space;
5. if there is more than one word **or** the single word is all-ASCII-uppercase: for each word, upper-case the first char with `char.ToUpper(c, culture)`; if the remainder is all-uppercase, lower-case it with `ToLower(culture)`; join;
6. otherwise: upper-case only the first character (`ToUpper(culture)`) and keep the rest verbatim.

| Input | Output | Why |
| --- | --- | --- |
| `start_date` | `StartDate` | rule 5, two words |
| `startDate` | `StartDate` | rule 6, remainder untouched |
| `ABC` | `Abc` | rule 5, all-upper remainder lower-cased |
| `ABCDef` | `ABCDef` | rule 6 — not all upper, single word, remainder untouched |
| `start_date` with `removeUnderscores: false` | `Start_Date` | join string `_` |
| `` (empty) | `` | rule 1 |

**`ToCamelCase`** = `ToPascalCase` then lower-case the first character with the culture-less `ToLower()` (`:258-271`). Example: `start_date` → `startDate`.

**`AddUnderscores`** applies three regex replacements in sequence (`:288-294`): `([A-Z]+)([A-Z][a-z])` → `$1_$2`, then `([a-z\d])([A-Z])` → `$1_$2`, then `[-\s]` → `_`. Examples: `StartDate` → `Start_Date`; `HTTPResponse` → `HTTP_Response`; `Order2Id` → `Order2_Id`; `already-dashed name` → `already_dashed_name`.

**`AddDashes`** is the same first two rules with `-`, and a final `[\s]` → `-` (`:301-309`). It does **not** convert existing dashes or underscores. Example: `StartDate` → `Start-Date`; `Start_Date` → `Start_Date` (unchanged).

**`RemoveUnderscoresAndDashes`** is two `String.Replace` calls, deliberately avoiding regex (`:91-94`). Example: `start_date-2` → `startdate2`.

### 5.3 URL and HTML encoding

* `UrlEncode` → `Uri.EscapeDataString` (`:54-57`). The XML doc comment cites the MSDN guidance for choosing it. Space becomes `%20` (not `+`). The unreserved set is framework-version dependent — see §7.
* `UrlDecode` → vendored `HttpUtility.UrlDecode(str, Encoding.UTF8)` (`:45-48`, `MonoHttp/HttpUtility.cs:111-114`). Rules: returns the input unchanged if it contains neither `%` nor `+` (`:137-138`); `+` decodes to space (`:177-178`); `%uXXXX` sequences are supported (`:153-161`); malformed `%` sequences are emitted literally rather than throwing (`:170-173`).
* `HtmlEncode` → vendored encoder (`MonoHttp/HtmlEncoder.cs:258-330`). Escapes `&` → `&amp;`, `<` → `&lt;`, `>` → `&gt;`, `"` → `&quot;`, full-width `＜`/`＞` (U+FF1C/U+FF1E) to numeric entities, and every character in the range 160–255 to `&#nnn;` using the invariant culture. Apostrophes are **not** escaped unless `NET_4_0` is defined, which it is not in any head in this repository. Characters above 255 other than the two full-width ones are emitted verbatim.
* The vendored URL encoder used for byte-array overloads encodes space as `+` and leaves `!()*-._'` unescaped (`MonoHttp/HtmlEncoder.cs:570-575`, `:577-632`) — a different rule set from `Uri.EscapeDataString`. RestSharp's own `string.UrlEncode` does not use it, but it is reachable through the public vendored `HttpUtility`.

Note the asymmetry: `UrlEncode` (RFC-style, `%20`) and `UrlDecode` (form-style, `+` → space) are not exact inverses for the space character.

### 5.4 Byte-array to text

| Build | Rule | Citation |
| --- | --- | --- |
| `FRAMEWORK` (desktop, Net2, MonoTouch, MonoDroid) | Always `Encoding.UTF8.GetString(buffer)`; no BOM detection and no BOM stripping, so a UTF-8 BOM survives as U+FEFF at the start of `Content` | `RestSharp/Extensions/MiscExtensions.cs:113-116` |
| Non-`FRAMEWORK` (Silverlight, Windows Phone) | Sniffs a BOM (`EF BB BF` → UTF-8, `FE FF` → `Encoding.Unicode`), then decodes through a `StreamReader`, which strips the BOM | `:118-151` |

The inline comment "Ansi as default" contradicts the code, which defaults to UTF-8 (`:112-113`). Charset information from the HTTP `Content-Type` header is never consulted by this helper.

### 5.5 Culture sensitivity summary

| Call site | Culture behaviour | Citation |
| --- | --- | --- |
| `ToPascalCase` | Uses the supplied culture for both `char.ToUpper` and `ToLower` | `StringExtensions.cs:242-250` |
| `MakeInitialLowerCase` (and therefore every `ToCamelCase` result) | Culture-less `ToLower()` → **current thread culture** | `:270` |
| `GetNameVariants` variant 3 | `ToLower(culture)` | `:328` |
| `GetNameVariants` variants 5 and 7 | `ToLower(culture)` | `:334`, `:340` |
| `XmlDeserializer` lower-case lookups | Culture-less `ToLower()` | `XmlDeserializer.cs:252`, `:328`, `:357`, `:369` |
| `XmlAttributeDeserializer` lower-case lookups | Culture-less `ToLower()` | `XmlAttributeDeserializer.cs:247`, `:325`, `:360` |
| `SerializeAsAttribute` `LowerCase` style | Culture-less `ToLower()` | `SerializeAsAttribute.cs:75` |
| `IsUpperCase` | Culture-independent ASCII regex | `StringExtensions.cs:280` |
| `FindEnumValue` comparison | `StringComparer.Create(culture, ignoreCase: true)` — culture-sensitive, case-insensitive | `ReflectionExtensions.cs:97` |
| `XmlDeserializer` / `JsonDeserializer` default `Culture` | `CultureInfo.InvariantCulture` | `XmlDeserializer.cs:36-39`, `JsonDeserializer.cs:35-38` |

The mixture is the key risk: name variants are partly invariant and partly thread-culture dependent, so the same payload can bind differently under a Turkish or Azeri locale (`I`/`ı` casing).

---

## 6. Per-target conditional behavior

Symbols per head: `RestSharp.csproj` → `FRAMEWORK` (`RestSharp/RestSharp.csproj:41`, `:51`); `RestSharp.Net2.csproj` → `Net2;FRAMEWORK;NET_2_0` (`RestSharp.Net2/RestSharp.Net2.csproj:22`, `:30`); Silverlight → `SILVERLIGHT` (`RestSharp.Silverlight/RestSharp.Silverlight.csproj:32`, `:43`); Windows Phone → `WINDOWS_PHONE` (`RestSharp.WindowsPhone/RestSharp.WindowsPhone.csproj:27`, `:38`); MonoTouch → `MONOTOUCH;FRAMEWORK` (`RestSharp.MonoTouch/RestSharp.MonoTouch.csproj:22`, `:42`); MonoDroid → `MONODROID;FRAMEWORK` (`RestSharp.MonoDroid/RestSharp.MonoDroid.csproj:22`, `:30`). **`Net2` also defines `FRAMEWORK`**, so every `FRAMEWORK` branch below applies to the .NET 2.0 head as well.

| Behaviour | `FRAMEWORK` (desktop, **Net2**, MonoTouch, MonoDroid) | `SILVERLIGHT` | `WINDOWS_PHONE` |
| --- | --- | --- | --- |
| `HttpUtility` source | Vendored `RestSharp.Contrib` (`StringExtensions.cs:25-27`, `:36-38`; compiled in via `RestSharp.csproj:107-109`, `RestSharp.Net2.csproj:164-171`, MonoTouch `:203-210`, MonoDroid `:159-166`) | `System.Windows.Browser` (`StringExtensions.cs:29-31`) | No `using` is added at all (`:33-34`); the file relies on whatever `HttpUtility` the platform exposes through `System.Net` — Silverlight/WP heads do **not** compile the MonoHttp files (no `MonoHttp` entries in `RestSharp.Silverlight.csproj` / `RestSharp.WindowsPhone.csproj`) |
| `HtmlAttributeEncode` | Present | Absent | Absent (`StringExtensions.cs:69-74`) |
| `MiscExtensions.SaveAs` | Present | Present | **Absent** (`MiscExtensions.cs:29-39`) |
| `AsString(byte[])` | Straight UTF-8, no BOM handling | BOM sniffing + `StreamReader` | BOM sniffing + `StreamReader` (`MiscExtensions.cs:115-151`) |
| `ChangeType(obj, type, culture)` | Culture honoured | Culture **ignored** (passes `null`) | Culture **ignored** (`ReflectionExtensions.cs:75-82`) |
| `ChangeType(obj, type)` | `Convert.ChangeType(source, newType)` (current culture) | `Convert.ChangeType(source, newType, null)` | Same as Silverlight (`:66-73`) |
| `FindEnumValue` | Fuzzy variant matching via LINQ | `Enum.Parse(type, value, ignoreCase: true)` — **no underscore/dash tolerance** | Same as Silverlight (`:92-101`) |
| LINQ availability | Net2 relies on the vendored `RestSharp.Net2/LinqBridge-1.2.cs` (`RestSharp.Net2.csproj:278`); other heads use the BCL | BCL | BCL |
| `System.Xml.Linq` for `AsNamespaced` | Net2 uses the vendored Mono `System.Xml.Linq`; other heads use the BCL | BCL | BCL |
| `NET_4_0`-gated encoder behaviour (apostrophe escaping in `HtmlEncode`, `HttpEncoder.Current` hooks) | Never enabled — no head defines `NET_4_0` (`MonoHttp/HtmlEncoder.cs:271-273`, `:304-308`) | n/a (encoder not compiled) | n/a |

`PocketPC` does not appear anywhere in this component or in the wider `RestSharp` source tree; the only other target-specific symbols used elsewhere are `MONOTOUCH`/`MONODROID` in the HTTP layer (`RestSharp/Http.Sync.cs:21`, `RestSharp/Http.Async.cs:32`).

---

## 7. Target-stack (.NET 8) concerns

| Legacy construct | Modern equivalent | Behavioural risk |
| --- | --- | --- |
| `Uri.EscapeDataString` on .NET 2.0/3.5 (`StringExtensions.cs:56`) | `Uri.EscapeDataString` on .NET 8 | The unreserved character set changed with .NET 4.5 (RFC 2396 → RFC 3986). Characters such as `~`, `!`, `*`, `'`, `(`, `)` are treated differently, so generated query strings and OAuth base strings can differ byte-for-byte. Pre-4.5 also threw `UriFormatException` above ~32 766 characters, a limit .NET 8 does not have. *(Framework-version behaviour stated from documented .NET history, not verified in this repo — confirm with a differential test.)* |
| Vendored `RestSharp.Contrib.HttpUtility` / `HtmlEncoder` (`MonoHttp/*`) | `System.Net.WebUtility` or `System.Web.HttpUtility` | Entity sets differ: the vendored encoder escapes all of 160–255 as numeric entities and never escapes `'`; `WebUtility.HtmlEncode` escapes `'` as `&#39;` and leaves Latin-1 characters intact. Decoding of malformed `%` sequences and `%uXXXX` is also a Mono-specific leniency that the BCL does not reproduce identically. |
| `ToLower()` / `ToUpper()` without culture (`StringExtensions.cs:270`, `XmlDeserializer.cs:252`, `SerializeAsAttribute.cs:75`) | `ToLowerInvariant()` / explicit culture | Current-culture casing under `tr-TR`/`az` maps `I` → `ı`, breaking name matching for properties containing `I` (`Id`, `IsCool`, `UniqueId`). Behaviour is unchanged by the runtime move but becomes easier to trip because .NET 8 on Linux uses ICU, whose casing/collation tables differ from Windows NLS. |
| `StringComparer.Create(culture, true)` in enum matching (`ReflectionExtensions.cs:97`) | `StringComparer.Create(culture, CompareOptions.IgnoreCase)` or `OrdinalIgnoreCase` | ICU vs NLS collation can change equality for non-ASCII member names; ordinal comparison would be deterministic but is a behavioural change. |
| `Convert.ChangeType` (`ReflectionExtensions.cs:66-82`) | `Convert.ChangeType` (unchanged API) | Numeric parsing is culture-driven; the culture-less overload silently uses the ambient culture. Also `double`/`float` **round-tripping and `ToString()` changed in .NET Core 3.0** (shortest round-trippable formatting), so string↔numeric conversions can produce different text than on .NET Framework. |
| `Decimal.Parse` on deserialized values (`JsonDeserializer.cs:156`, `XmlDeserializer.cs:174`) | Same API | `decimal` semantics are stable, but values that arrive as JSON numbers are first materialised by the JSON library; `System.Text.Json` preserves more precision than Newtonsoft 4.5 in some cases and rejects some lenient inputs (trailing commas, comments, `NaN`). |
| Unix-seconds arithmetic (`StringExtensions.cs:108-113`) | `DateTimeOffset.FromUnixTimeSeconds` / `FromUnixTimeMilliseconds` | Same arithmetic, but the BCL helpers validate range and return `DateTimeOffset`. The legacy code accepts any `Int64` and can overflow (`AddSeconds` throws `ArgumentOutOfRangeException` beyond `DateTime.MaxValue`). |
| `DateTime` with implicit kinds (`ParseFormattedDate`, `:145-165`) | `DateTimeOffset` / explicit `DateTimeStyles` | Formats `"u"`, `"s"` and the `Z`-literal formats produce `DateTimeKind.Unspecified` even though the text asserts UTC; the `/Date(...)` and Unix paths produce `Utc`. Downstream `ToUniversalTime()` therefore behaves differently per input shape. Any migration that swaps in `DateTimeOffset` must decide a single kind policy. |
| `(DateTimeOffset)dt` cast on a parsed `DateTime` (`JsonDeserializer.cs:152`) | Explicit offset construction | The cast applies the **local machine offset** for `Unspecified` kinds — server timezone changes the result. Containerised .NET 8 usually runs UTC, silently changing values. |
| Newtonsoft `JToken` / `JValue` (`MiscExtensions.cs:81-100`) | `JsonElement` / `JsonNode` | `System.Text.Json` has no `JTokenType.Date`: Microsoft `/Date(...)/` and `new Date(...)` strings will arrive as plain strings, which *activates* the currently-dormant `ParseJsonDate` branches (§8) and changes observable behaviour. `JToken.ToString()` formatting of numbers/dates also differs. |
| Vendored Mono `System.Xml.Linq` + LinqBridge (`RestSharp.Net2.csproj:278`) | BCL `System.Xml.Linq` and LINQ | `XName.Get` validation, `XDocument.Parse` whitespace/DTD handling and default `XmlReaderSettings` (DTD processing disabled by default in modern .NET) differ from the Mono vendored copy. |
| `Attribute.GetCustomAttribute` (`ReflectionExtensions.cs:36`, `:46`) | `MemberInfo.GetCustomAttribute<T>()` | The legacy overload searches the **inheritance chain** by default; the generic extension's default also inherits for members but not for parameters. Trimming/AOT in .NET 8 can also drop attributes and property metadata unless annotated. |
| Reflection-driven property mapping via `PropertyInfo.SetValue` (callers of this component) | Source-generated `System.Text.Json` serialization | Reflection-free serializers need explicit attributes; the fuzzy name matching implemented here has **no equivalent** in `System.Text.Json` (`PropertyNameCaseInsensitive` and a naming policy cover only part of the variant list) — a custom `JsonNamingPolicy` plus resolver work is required to preserve behaviour. |
| `HttpWebRequest`-era helpers: `ReadAsBytes` on the raw response stream (`MiscExtensions.cs:46-58`, used at `Http.cs:329-333`) | `HttpContent.ReadAsByteArrayAsync` / `ReadAsStreamAsync` | `HttpClient` performs **automatic decompression** when configured, making the explicit `GZipStream` wrapping redundant or double-applied; default headers, connection reuse/pooling, `KeepAlive`, `Expect: 100-continue` (off by default in `HttpClient`), redirect handling, cookie container, proxy defaults and timeout semantics (per-client `Timeout` vs per-request `HttpWebRequest.Timeout`, plus `ReadWriteTimeout` having no equivalent) all change. |
| `Stream.CopyTo` extension (`MiscExtensions.cs:65-75`) | BCL `Stream.CopyTo` / `CopyToAsync` | The BCL instance method **wins over the extension** on modern targets, so buffer size (32 KiB vs 81 920 B) and behaviour change silently the moment the extension is removed or shadowed. |
| Synchronous stream reads inside response handling | `async`/`await` end-to-end | Converting `IAsyncResult`/`Begin*`-`End*` pairs to `Task` risks sync-over-async deadlocks if `.Result`/`.Wait()` is used, and changes exception shape (`AggregateException`/`TaskCanceledException` vs `WebException`). |
| `Encoding.UTF8.GetString` without BOM stripping (`MiscExtensions.cs:116`) | `Encoding.UTF8.GetString` (identical) plus explicit BOM/charset handling | Behaviour is identical, so the existing leading-U+FEFF quirk will persist unless deliberately fixed; note `Encoding.GetEncoding(charset)` for non-UTF-8 charsets requires `System.Text.Encoding.CodePages` on .NET 8. |
| `Regex` with default options (`StringExtensions.cs:205`, `:280`, `:291-308`) | `Regex` / `[GeneratedRegex]` | Patterns are ASCII-only and remain valid; the migration opportunity is compiled/source-generated regexes. `Regex.IsMatch` static overloads have a process-wide cache whose default size differs — performance only, not semantics. |
| `public class Validate` / `Require` with only static members | `static class` | Making them static is a source-breaking change for any external caller that instantiates them. |

---

## 8. Gaps, stubs, dead code and discrepancies

1. **The validation component is entirely unused.** A repository-wide search finds no call to `Require.Argument`, `Validate.IsBetween` or `Validate.IsValidLength` and no `using RestSharp.Validation` outside the files themselves; the files are nonetheless compiled into every head (`RestSharp/RestSharp.csproj:158-159` and equivalents). It is public API surface with zero internal consumers.
2. **`Validate.IsBetween` contradicts its own documentation.** The XML comment says "exclusive of min/max" (`RestSharp/Validation/Validate.cs:27-31`) but the check is inclusive (`:33`).
3. **`Require.Argument` throws the wrong exception type** for a null argument (`ArgumentException` rather than `ArgumentNullException`) (`RestSharp/Validation/Require.cs:32-34`).
4. **The `/Date(...)` regex requires literal backslashes.** The pattern is `@"\\/Date\((-?\d+)(-|\+)?([0-9]{4})?\)\\/"` (`RestSharp/Extensions/StringExtensions.cs:117`), i.e. it only matches text containing `\/Date(...)\/`. The guard that selects this branch tests for `/Date(` without backslashes (`:115`). With Newtonsoft's default date handling the value normally arrives as a `Date` token and never reaches this code (`RestSharp/Deserializers/JsonDeserializer.cs:139-142`), which is why `RestSharp.Tests/JsonTests.cs:363-372` passes despite the `SampleData/datetimes.txt` payload using unescaped `/Date(...)/`. The branch appears effectively unreachable today and would start executing under a JSON stack that does not pre-parse dates.
5. **Double timezone adjustment in `ExtractDate`.** The epoch milliseconds in a Microsoft `/Date(ms+offset)/` value are already UTC, yet the offset is added or subtracted on top (`:180-191`). Also, `match.Groups.Count > 2` is always true for this pattern, so the guard is redundant.
6. **`ExtractDate`'s offset branch is unreachable from the `new Date(` path**, whose pattern has only one capture group (`:124`), so `Groups[3]` never has a value.
7. **`ParseFormattedDate` swallows failures**, returning `default(DateTime)` (`:159-164`), so callers cannot distinguish "absent/invalid" from the year 1 (`JsonDeserializer` has no fallback handling).
8. **Unix-seconds detection is over-eager.** Any all-digit string becomes a date (`:109-113`); a millisecond epoch (13 digits) yields a date around the year 45 000.
9. **`RemoveSurroundingQuotes` mishandles the single-character input `"`** — both `StartsWith` and `EndsWith` succeed and `Substring(1, -1)` throws (`:135-143`).
10. **`MakeInitialLowerCase` ignores the culture parameter chain.** `ToCamelCase` accepts a `CultureInfo` and passes it to `ToPascalCase`, then lower-cases the first character with the culture-less `ToLower()` (`:260`, `:270`) — the supplied culture is partially honoured.
11. **`SerializeAsAttribute.TransformName` ignores `Name` for `NameStyle.AsIs`.** The local `name` variable resolves `Name ?? input`, but the fall-through returns `input` (`RestSharp/Serializers/SerializeAsAttribute.cs:68`, `:78`).
12. **`AsString(byte[])` has a dead branch and a null check that contradicts itself.** The `0xfe 0xff` condition is repeated, so the `Encoding.BigEndianUnicode` branch can never execute, and the little-endian comparison does not match the UTF-16LE BOM (`FF FE`) it claims to detect (`RestSharp/Extensions/MiscExtensions.cs:133-140`). The function already returned `""` for `null` at `:110`, making the second null check at `:118` dead. The `Encoding` variable is also commented "Ansi as default" while being assigned UTF-8 (`:112-113`). Buffers shorter than three bytes will throw before any of this (`:129`).
13. **`XmlAttributeDeserializer.HandleListDerivative` uses overwriting `if`s instead of short-circuits** (`RestSharp/Deserializers/XmlAttributeDeserializer.cs:254-267`), inverting the intended exact → lower → camel precedence; it also mixes `name` (possibly overridden by `DeserializeAsAttribute`) into the existence test but re-queries with `t.Name` (`:254-256`). If none of the three matches, `elements` stays `null` and `PopulateListFromElements` throws a `NullReferenceException` (`:252`, `:269`), whereas `XmlDeserializer` has a fourth sanitized-name fallback (`RestSharp/Deserializers/XmlDeserializer.cs:262-265`).
14. **`XmlAttributeDeserializer`'s `Value` special case compares an `XName` to the raw string `"Value"`** (`:343`), which only succeeds for a namespace-less name, unlike `XmlDeserializer`'s namespaced comparison (`RestSharp/Deserializers/XmlDeserializer.cs:346`).
15. **`GetNameVariants` yields duplicates** for already-lowercase or single-word names (e.g. `name` yields `name` four times), costing redundant lookups.
16. **`ReadAsBytes` and `CopyTo` never dispose or rewind the source stream** (`RestSharp/Extensions/MiscExtensions.cs:46-75`); `CopyTo` also shadows the BCL method of the same name on modern targets.
17. **The test suite contains a commented-out assertion** for dashed date binding (`RestSharp.Tests/JsonTests.cs:274`), i.e. a known behaviour gap in date handling for dashed payload names was parked rather than fixed.
18. **The enum sample data is internally inconsistent**: `RestSharp.Tests/SampleData/jsonenums.txt` maps `lower_dashes` to the value `so_so` (underscored, not dashed) and the file contains a trailing comma, so the "lower dashes" case is not actually exercised by `RestSharp.Tests/JsonTests.cs:172`.
19. **`README.markdown:14` advertises support for "Silverlight 4, Windows Phone 7, Mono, MonoTouch"** while those heads silently lose culture-aware conversion and fuzzy enum matching (§6) — documentation and behaviour diverge.
20. **`HtmlAttributeEncode` is exposed only on `FRAMEWORK`** (`RestSharp/Extensions/StringExtensions.cs:69-74`), so the public surface of the assembly differs per head, which the README does not mention.
21. **No head defines `NET_4_0`**, so all `NET_4_0` blocks inside the vendored encoder are dead code in this repository (`RestSharp/Extensions/MonoHttp/HtmlEncoder.cs`, `RestSharp/Extensions/MonoHttp/HttpUtility.cs`).

---

## 9. Open questions for the human reviewer

1. **Fuzzy name matching on .NET 8** — `System.Text.Json` cannot reproduce the seven-variant ladder out of the box. Do we (a) preserve exact legacy behaviour with a custom naming policy plus a contract resolver, (b) narrow to case-insensitive plus snake/kebab policies and accept that some payloads stop binding, or (c) require explicit `[JsonPropertyName]` attributes on caller models?
2. **Variant precedence** — if we keep fuzzy matching, must the "first variant wins" order (exact → camel → lower → underscored → …) be preserved exactly, or may we normalise names once and match on the normalised form (which changes which field wins when several variants are present)?
3. **Culture policy** — do we standardise every casing and comparison on invariant/ordinal (deterministic, but changes behaviour for non-ASCII names under non-invariant cultures), or keep the current mixture of culture-sensitive and culture-less calls bug-for-bug?
4. **Silent date failures** — should an unparsable date keep returning `0001-01-01`, or throw / return `null`? If it should throw, that is a breaking change for callers relying on the sentinel.
5. **Date format coverage** — do we keep exactly the eight accepted formats, or extend to full ISO-8601 (fractional seconds, date-only, offsets) as `DateTimeOffset.TryParse` would? Extending silently accepts payloads that previously produced `0001-01-01`.
6. **`/Date(...)` and `new Date(...)` support** — with `System.Text.Json` these values arrive as plain strings, activating currently-dormant code. Do we (a) port the paths as-is including the double-offset bug, (b) port them corrected, or (c) drop Microsoft/JavaScript date support entirely?
7. **Unix-epoch heuristic** — keep "any integer is Unix seconds", restrict it to `DateTime`-typed targets only, or add a magnitude heuristic to distinguish seconds from milliseconds?
8. **`DateTime` vs `DateTimeOffset`** — should the modern API return `DateTimeOffset` with an explicit UTC policy, and what should happen to the current mix of `Utc` and `Unspecified` kinds (notably the local-offset `(DateTimeOffset)dt` cast)?
9. **URL encoding parity** — `Uri.EscapeDataString` output differs between .NET 2.0/3.5 and .NET 8 for `~`, `!`, `*`, `'`, `(`, `)`. Do we accept the modern RFC 3986 output (and re-baseline OAuth signature tests), or add a compatibility encoder that reproduces the legacy set?
10. **HTML encode/decode parity** — replace the vendored Mono encoder with `WebUtility` (apostrophes now escaped, Latin-1 no longer numeric-escaped), or port the vendored rules verbatim to preserve byte-for-byte output?
11. **Validation helpers** — delete `Require`/`Validate` as dead code, keep them as-is for binary/source compatibility, or fix the inclusive/exclusive documentation mismatch and the exception type (a behavioural change for any external caller)?
12. **Public surface** — several helpers are incidental (`MakeInitialLowerCase`, `IsUpperCase`, `Matches`, `CopyTo`, `AsString`). Should they become `internal` in the modern package, or stay public for downstream compatibility?
13. **Known defects** — for each item in §8 (double timezone adjustment, `XmlAttributeDeserializer` precedence inversion, dead BOM branch, `TransformName` ignoring `Name`), should the port reproduce the legacy behaviour exactly, or fix it and record the change in release notes?
14. **BOM handling** — should the modern reader strip a UTF-8 BOM from response text (a change from today's `FRAMEWORK` behaviour) and honour the `Content-Type` charset instead of assuming UTF-8?
15. **Dropped heads** — with Silverlight/WP/MonoTouch/MonoDroid removed, the non-`FRAMEWORK` code paths disappear. Confirm that no shipped behaviour depends on them (e.g. the BOM-sniffing `AsString`, the culture-ignoring `ChangeType`) before they are deleted.
