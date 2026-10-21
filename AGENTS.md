# RoxyApi .NET SDK - Agent Guide

.NET SDK for RoxyAPI. 18+ domains (Western astrology, Vedic astrology, forecast, human design, Chinese astrology, feng shui, Mesoamerican astrology, Vastu, numerology, Kabbalah, tarot, biorhythm, Ayurveda, I Ching, crystals, dreams, angel numbers, location) plus utility namespaces (usage, languages). One API key, fully typed, generated from the OpenAPI spec.

> Before writing code with this SDK, read `docs/llms-full.txt` in this package for the complete method reference with one example per endpoint.

## Install and initialize

```bash
dotnet add package RoxyApi.Sdk
```

```csharp
using RoxyApi;

// Retries 429, 500, 502, 503 and 504 by default. Optional settings, shown with their defaults:
// new RoxyClient(key, new RoxyRetryOptions { MaxRetries = 2, InitialDelay = TimeSpan.FromMilliseconds(500), MaxDelay = TimeSpan.FromSeconds(30) })
var roxy = new RoxyClient(Environment.GetEnvironmentVariable("ROXY_API_KEY")!);
```

`new RoxyClient(apiKey)` sets the base URL (`https://roxyapi.com/api/v2`) and the auth and SDK headers automatically. Every call is async and returns the typed response, or throws `RoxyError` on an error response (see Error handling).

One runtime dependency: [`Microsoft.Kiota.Bundle`](https://www.nuget.org/packages/Microsoft.Kiota.Bundle) (MIT, by Microsoft), which provides the HTTP, auth, and serialization libraries the generated client uses. Targets `netstandard2.0` and `net8.0`.

## Critical rule: geocode before any chart endpoint

Every chart, horoscope, panchang, dasha, dosha, navamsa, KP, synastry, compatibility, and natal endpoint needs `Latitude`, `Longitude`, and (for Western) `Timezone`. **Never ask the user for coordinates.** Always call `roxy.Location.Search` first.

```csharp
var place = await roxy.Location.Search.GetAsync(c => c.QueryParameters.Q = "New York");
var city = place!.Cities![0];
// `city.Timezone` is the IANA string ("America/New_York"). Pass it directly to any chart endpoint.
// The server resolves it to the DST-correct decimal offset using the date of the chart itself,
// so a January 1990 New York chart gets EST (-5) even when you looked the city up in July.
// `city.UtcOffset` is the offset in force today, not on the birth date, so pass the IANA `timezone` string.
```

`Q` accepts a bare city (`"Paris"`), city plus country (`"Berlin Germany"`), or comma-qualified (`"Springfield, Illinois"`). Use the qualified form to disambiguate same-named cities.

## Domains

<!-- BEGIN:DOMAINS -->
| Accessor | What it covers |
|----------|----------------|
| [`roxy.Astrology`](https://roxyapi.com/api-reference/western-astrology) | Western astrology API for natal birth charts, daily, weekly, monthly, and yearly horoscopes with unique content per s... |
| [`roxy.VedicAstrology`](https://roxyapi.com/api-reference/vedic-astrology) | Vedic astrology (Jyotish) and KP API for kundli generation with the sixteen Shodasavarga divisional charts (D1 to D60... |
| [`roxy.Forecast`](https://roxyapi.com/api-reference/forecast) | Astrology forecast API that merges upcoming transit aspects, sign ingresses, retrograde stations, new and full moons,... |
| [`roxy.HumanDesign`](https://roxyapi.com/api-reference/human-design) | Human Design API that generates the full bodygraph from a birth moment: type, strategy, inner authority, profile, def... |
| [`roxy.ChineseAstrology`](https://roxyapi.com/api-reference/chinese-astrology) | Chinese zodiac and BaZi astrology API: Four Pillars charts, Chinese zodiac signs and the Chinese lunisolar calendar f... |
| [`roxy.FengShui`](https://roxyapi.com/api-reference/feng-shui) | Compute classical feng shui from one API: Xuan Kong flying star natal charts for any of the nine periods and 24 mount... |
| [`roxy.MesoamericanAstrology`](https://roxyapi.com/api-reference/mesoamerican-astrology) | Calculate Mayan astrology day signs, the Tzolkin sacred round, the Haab year, the full Long Count and the Aztec tonal... |
| [`roxy.Vastu`](https://roxyapi.com/api-reference/vastu) | Vastu Shastra API for directional home and plot analysis: entrance padas with the classical effect of each of the 32... |
| [`roxy.Numerology`](https://roxyapi.com/api-reference/numerology) | Numerology API for life path numbers, complete numerology charts and compatibility, in both Pythagorean and Chaldean... |
| [`roxy.Kabbalah`](https://roxyapi.com/api-reference/kabbalah) | Kabbalah API for gematria, the 72 names, the Tree of Life and the Hebrew birthday |
| [`roxy.Tarot`](https://roxyapi.com/api-reference/tarot) | Tarot reading API with the complete 78-card Rider-Waite-Smith deck and card meanings for love, career, health, and sp... |
| [`roxy.Biorhythm`](https://roxyapi.com/api-reference/biorhythm) | Biorhythm API for entertainment and reflection, with 10 model cycle types across 3 primary (physical, emotional, inte... |
| [`roxy.Ayurveda`](https://roxyapi.com/api-reference/ayurveda) | Ayurveda API for dosha profiles, the dinacharya daily routine and the ritucharya seasonal regimen, with a verse cited... |
| [`roxy.Iching`](https://roxyapi.com/api-reference/i-ching) | I-Ching oracle API with all 64 hexagrams, 384 changing lines, 8 trigrams, and modern interpretations for love, career... |
| [`roxy.Crystals`](https://roxyapi.com/api-reference/crystals-and-healing-stones) | Crystal healing API covering the most popular and widely-searched healing crystals and gemstones, from Amethyst and R... |
| [`roxy.Dreams`](https://roxyapi.com/api-reference/dreams) | Dream interpretation API with a 2,200+ symbol dream dictionary and psychological meanings covering animals, objects,... |
| [`roxy.AngelNumbers`](https://roxyapi.com/api-reference/angel-numbers) | Angel numbers API with meanings for 111, 222, 333, 444, 555, 666, 777, 888, 999, 1111, and 75+ sequences covering eve... |
| [`roxy.Location`](https://roxyapi.com/api-reference/location-and-timezone) | Timezone and location API with city search and geocoding across 235,000+ cities in 240+ countries, returning latitude... |
| [`roxy.Usage`](https://roxyapi.com/api-reference/usage) | Monitor your API usage, check rate limits, and track request consumption |
| [`roxy.Languages`](https://roxyapi.com/api-reference/languages) | List the response languages accepted by the `lang` query parameter on every i18n-aware endpoint |
<!-- END:DOMAINS -->

## Quality guidelines for agents

The rules to follow when writing any call with this SDK. Get these right and the generated types do the rest.

- **The fluent path mirrors the URL.** `GET /astrology/horoscope/{sign}/daily` is `roxy.Astrology.Horoscope["aries"].Daily.GetAsync()`. Each path segment is a property; each `{param}` is an indexer. Type `roxy.` and let IntelliSense walk the tree. Never invent a method name, and never derive one from the `operationId` in the spec: this SDK is path-fluent, not operation-named.
- **Request bodies use a target-typed `new()`.** `await roxy.Astrology.NatalChart.PostAsync(new() { Date = new Date(1990, 1, 15), ... })`. The type is inferred from the method, so you never need to name it; IntelliSense shows every field on `new() {`.
- **Query parameters use a configuration lambda.** `await roxy.Crystals.Search.GetAsync(c => c.QueryParameters.Q = "amethyst");`. Multiple: `c => { c.QueryParameters.Limit = 20; c.QueryParameters.Offset = 0; }`.
- **Always `await`, and catch `RoxyError`.** There is no result-wrapper object. The call returns the typed response directly and throws `RoxyError` (a subclass of `ApiException`) on failure. Switch on `e.Code`, not `e.Message`.
- **Do not hand-roll requests.** No raw `HttpClient` calls. `new RoxyClient(key)` injects auth, the base URL, typed responses, and retries twice by default (see Retries). Response field names come from the response schema of the spec and are PascalCase properties; the compiler catches any invented field, so if the build fails on a property, the field does not exist.
- **Look up any operation or field beyond this guide.** Query the combined OpenAPI spec at `https://roxyapi.com/api/v2/openapi.json` with the jq recipe in `https://roxyapi.com/AGENTS.md`, or search the keyless Docs MCP server at `https://roxyapi.com/mcp/docs` (one tool, `search_docs`).

## Critical patterns

### Two-step pattern for coordinate-dependent endpoints

```csharp
var place = await roxy.Location.Search.GetAsync(c => c.QueryParameters.Q = "London");
var city = place!.Cities![0];
var latitude = city.Latitude;
var longitude = city.Longitude;
var timezone = city.Timezone;
var birthDate = new Date(1990, 1, 15);
var birthTime = new Time(14, 30, 0);

var chart = await roxy.Astrology.NatalChart.PostAsync(new()
{
    Date = birthDate, Time = birthTime,
    Latitude = latitude, Longitude = longitude, Timezone = new() { String = timezone },
});
```

One lookup feeds every domain. Request bodies are distinct generated types, so capture the five values once as locals and assign them into each body. The same `Date`, `Time`, `Latitude`, `Longitude` and `Timezone` are the body for `Astrology.NatalChart`, `VedicAstrology.BirthChart`, `VedicAstrology.Dasha.Current`, `Ayurveda.Constitution` and the `BirthData` of `Forecast.Transits`; the instant alone (`Date`, `Time`, `Timezone`) is the body for `HumanDesign.Bodygraph`, `ChineseAstrology.Bazi.Chart` and `Kabbalah.BirthProfile`. Never look the city up twice for one person.

### GET endpoints: path params are indexers, query params use the lambda

```csharp
await roxy.Astrology.Horoscope["aries"].Daily.GetAsync();
await roxy.Crystals.Zodiac["leo"].GetAsync();
await roxy.Crystals.Search.GetAsync(c => c.QueryParameters.Q = "amethyst");
```

### POST endpoints: body via target-typed new()

Most valuable endpoints (charts, spreads, calculations) are POST:

```csharp
await roxy.VedicAstrology.BirthChart.PostAsync(new()
{
    Date = birthDate, Time = birthTime,
    Latitude = latitude, Longitude = longitude, Timezone = new() { String = timezone },
});

await roxy.Tarot.Spreads.CelticCross.PostAsync(new() { Question = "What should I focus on?" });
await roxy.Numerology.LifePath.PostAsync(new() { Year = 1990, Month = 1, Day = 15 });
```

### Multi-language via the query lambda

Ten languages: `en`, `tr`, `de`, `es`, `fr`, `hi`, `pt`, `ru`, `zh-Hans`, `zh-Hant`. Defaults to `en`.

```csharp
await roxy.Tarot.Daily.PostAsync(new() { Date = new Date(2026, 4, 22) }, c => c.QueryParameters.Lang = RoxyApi.Tarot.Daily.PostLangQueryParameterType.Es);
await roxy.Numerology.LifePath.PostAsync(new() { Year = 1990, Month = 1, Day = 15 }, c => c.QueryParameters.Lang = RoxyApi.Numerology.LifePath.PostLangQueryParameterType.Hi);
```

Supported: astrology, vedicAstrology, forecast, humanDesign, chineseAstrology, fengShui, mesoamericanAstrology, vastu, numerology, kabbalah, tarot, biorhythm, ayurveda, iching, crystals, angelNumbers. English-only: dreams, location, usage, languages. The two Chinese scripts (zh-Hans, zh-Hant) currently ship on chineseAstrology and fengShui; every other domain answers those codes in English per field. Call `roxy.Languages.GetAsync()` for the live list.

### Reading responses

Responses are fully typed objects, not dictionaries. Discover fields with IntelliSense (`result.`) or the full response JSON on https://roxyapi.com/api-reference. `docs/llms-full.txt` documents the call and inputs for every endpoint; the response shape comes from the type. The success body is nullable (non-null only on a 2xx, since errors throw), so use `result!.Field`.

- **City** (`Location.Search` -> `Cities[i]`): `City` (the name, never `Name`), `Province`, `Country`, `Iso2`, `Latitude`, `Longitude`, `Timezone` (IANA), `UtcOffset` (decimal, the offset in force today), `Population`.
- **Natal chart**: `Planets`, `Houses`, `Aspects`, `Ascendant`, `Midheaven`, `Summary`. Each planet has `Name`, `Sign`, `Degree`, `House`, `IsRetrograde`. The `Planets` list has more than ten entries (classical bodies plus nodes and key points).
- **Maps keyed by name** arrive in `AdditionalData`, not as properties. The Vedic kundli `Meta` (one entry per planet: `rashi`, `nakshatra`, `longitude`) and the biorhythm reading `Cycles` (one entry per cycle: `value`, `rawValue`, `phase`) are objects whose keys the spec leaves open, so the generated class has no fields of its own: read `kundli.Meta!.AdditionalData["Moon"]`, which is an `UntypedObject` from `Microsoft.Kiota.Abstractions.Serialization`, and call `GetValue()` on it for the inner dictionary.

```csharp
var chart = await roxy.Astrology.NatalChart.PostAsync(new() { Date = birthDate, Time = birthTime, Latitude = latitude, Longitude = longitude, Timezone = new() { String = timezone } });
foreach (var p in chart!.Planets!)
    Console.WriteLine($"{p.Name}: {p.Sign} (house {p.House})");
```

## Common tasks

In the catalog order (Western astrology, Vedic astrology, forecast, Human Design, Chinese astrology, feng shui, Mesoamerican astrology, Vastu, numerology, Kabbalah, tarot, biorhythm, Ayurveda, I Ching, crystals, dreams, angel numbers, location, usage, languages). `Date, Time, Latitude, Longitude, Timezone` are the five values from the two-step pattern above; `Person1`, `Person2`, `PersonA`, `PersonB` and `BirthData` are nested bodies built from them.

| Task | Code |
|------|------|
| Find city coordinates (do this first) | `roxy.Location.Search.GetAsync(c => c.QueryParameters.Q = "Berlin")` |
| Daily horoscope | `roxy.Astrology.Horoscope[sign].Daily.GetAsync()` |
| Natal chart (Western) | `roxy.Astrology.NatalChart.PostAsync(new() { Date, Time, Latitude, Longitude, Timezone })` |
| Synastry | `roxy.Astrology.Synastry.PostAsync(new() { Person1, Person2 })` |
| Compatibility score | `roxy.Astrology.CompatibilityScore.PostAsync(new() { Person1, Person2 })` |
| Current moon phase | `roxy.Astrology.MoonPhase.Current.GetAsync()` |
| Transits | `roxy.Astrology.Transits.PostAsync(new() { NatalChart })` |
| Kundli (Vedic birth chart) | `roxy.VedicAstrology.BirthChart.PostAsync(new() { Date, Time, Latitude, Longitude, Timezone })` |
| Panchang (detailed) | `roxy.VedicAstrology.Panchang.Detailed.PostAsync(new() { Date, Latitude, Longitude, Timezone })` |
| Choghadiya | `roxy.VedicAstrology.Panchang.Choghadiya.PostAsync(new() { Date, Latitude, Longitude, Timezone })` |
| Current dasha | `roxy.VedicAstrology.Dasha.Current.PostAsync(new() { Date, Time, Latitude, Longitude, Timezone })` |
| Mangal Dosha | `roxy.VedicAstrology.Dosha.Manglik.PostAsync(new() { Date, Time, Latitude, Longitude, Timezone })` |
| Guna Milan (matching) | `roxy.VedicAstrology.Compatibility.PostAsync(new() { Person1, Person2 })` |
| Navamsa (D9) | `roxy.VedicAstrology.Navamsa.PostAsync(new() { Date, Time, Latitude, Longitude, Timezone })` |
| KP chart | `roxy.VedicAstrology.Kp.Chart.PostAsync(new() { Date, Time, Latitude, Longitude, Timezone })` |
| KP ruling planets | `roxy.VedicAstrology.Kp.RulingPlanets.PostAsync(new() { Latitude, Longitude, Timezone })` |
| Nakshatra detail | `roxy.VedicAstrology.Nakshatras["ashwini"].GetAsync()` |
| Transit forecast | `roxy.Forecast.Transits.PostAsync(new() { BirthData, StartDate, EndDate })` |
| Cross-domain timeline | `roxy.Forecast.Timeline.PostAsync(new() { BirthData, StartDate, EndDate })` |
| Human Design bodygraph | `roxy.HumanDesign.Bodygraph.PostAsync(new() { Date, Time, Timezone })` |
| Human Design connection | `roxy.HumanDesign.Connection.PostAsync(new() { PersonA, PersonB })` |
| BaZi Four Pillars | `roxy.ChineseAstrology.Bazi.Chart.PostAsync(new() { Date, Time, Timezone })` |
| Chinese zodiac animal | `roxy.ChineseAstrology.Zodiac.Sign.PostAsync(new() { Date })` |
| Almanac day (Tong Shu) | `roxy.ChineseAstrology.Calendar.Day[new Date(2026, 10, 1)].GetAsync()` |
| Kua number | `roxy.FengShui.Kua.PostAsync(new() { Date, Gender })` |
| Flying star natal chart | `roxy.FengShui.FlyingStars.Natal.PostAsync(new() { Period, Facing })` |
| Tzolkin day sign | `roxy.MesoamericanAstrology.Mayan.Tzolkin.PostAsync(new() { Date })` |
| Full Maya chart | `roxy.MesoamericanAstrology.Mayan.Chart.PostAsync(new() { Date })` |
| Vastu entrance | `roxy.Vastu.Entrance.PostAsync(new() { Plot, Facing, DoorPosition })` |
| Vastu room compliance | `roxy.Vastu.Rooms.PostAsync(new() { Plot, Facing, Rooms })` |
| Life path number | `roxy.Numerology.LifePath.PostAsync(new() { Year, Month, Day })` |
| Full numerology chart | `roxy.Numerology.Chart.PostAsync(new() { FullName, Year, Month, Day })` |
| Personal year | `roxy.Numerology.PersonalYear.PostAsync(new() { Month, Day })` |
| Gematria | `roxy.Kabbalah.Gematria.PostAsync(new() { Text })` |
| Kabbalah birth profile | `roxy.Kabbalah.BirthProfile.PostAsync(new() { Date, Time, Timezone })` |
| Daily tarot card | `roxy.Tarot.Daily.PostAsync(new() { Seed })` |
| Three-card spread | `roxy.Tarot.Spreads.ThreeCard.PostAsync(new() { Question })` |
| Celtic Cross | `roxy.Tarot.Spreads.CelticCross.PostAsync(new() { Question })` |
| Yes / no tarot | `roxy.Tarot.YesNo.PostAsync(new() { Question })` |
| Biorhythm reading | `roxy.Biorhythm.Reading.PostAsync(new() { BirthDate })` |
| Daily biorhythm (seeded) | `roxy.Biorhythm.Daily.PostAsync(new() { Seed })` |
| Biorhythm forecast | `roxy.Biorhythm.Forecast.PostAsync(new() { BirthDate })` |
| Biorhythm compatibility | `roxy.Biorhythm.Compatibility.PostAsync(new() { Person1, Person2 })` |
| Ayurvedic constitution | `roxy.Ayurveda.Constitution.PostAsync(new() { Date, Time, Latitude, Longitude, Timezone })` |
| Dinacharya | `roxy.Ayurveda.Dinacharya.PostAsync(new() { Date, Latitude, Longitude, Timezone })` |
| Daily hexagram | `roxy.Iching.Daily.PostAsync(new() { Seed })` |
| Cast I Ching reading | `roxy.Iching.Cast.GetAsync()` |
| Hexagram detail | `roxy.Iching.Hexagrams[1].GetAsync()` |
| Crystal by zodiac | `roxy.Crystals.Zodiac[sign].GetAsync()` |
| Crystal by chakra | `roxy.Crystals.Chakra[chakra].GetAsync()` |
| Dream symbol lookup | `roxy.Dreams.Symbols["flying"].GetAsync()` |
| Angel number meaning | `roxy.AngelNumbers.Numbers["1111"].GetAsync()` |
| Universal number lookup | `roxy.AngelNumbers.Lookup.GetAsync(c => c.QueryParameters.Number = "1234")` |
| Check API usage | `roxy.Usage.GetAsync()` |
| List supported languages | `roxy.Languages.GetAsync()` |

## Field formats that trip agents

These are the fields AI agents most often get wrong. Copy the format column exactly.

| Field | Format | Good | Bad |
|-------|--------|------|-----|
| `Date` (and `BirthDate`, `StartDate`, ...) | The `Date` struct from `Microsoft.Kiota.Abstractions` | `new Date(1990, 1, 15)` | `"1990-01-15"`, `DateTime.Now`, `new DateTime(...)` |
| `Time` | The `Time` struct from `Microsoft.Kiota.Abstractions` | `new Time(14, 30, 0)`, `new Time(9, 0, 0)` | `"14:30:00"` (string), `DateTime.Now`, `new TimeOnly(...)` |
| `Timezone` | Union wrapper: decimal OR IANA | `new() { Double = 5.5 }`, `new() { Double = -5 }` OR `new() { String = "America/New_York" }` | `5.5`, `"5.5"`, `"+0530"` assigned directly |
| `Latitude` / `Longitude` | `double` | `40.7128`, `-74.006` | `"40.7128"`, DMS strings |
| `sign` (horoscope indexer) | Zodiac name; matched case-insensitively, lowercase is canonical | `["aries"]`, `["scorpio"]` | `["1"]`, `["ram"]` |
| `chakra` (crystals indexer) | Title-case name | `["Root"]`, `["Heart"]`, `["Third Eye"]` | `["heart"]`, `["third-eye"]` |
| `FullName` (numerology) | Birth-certificate name | `"John William Smith"` | Nickname, partial name |
| `Seed` | Any string (deterministic) | `"user-42"`, `"session-abc"` | numbers, objects |
| `number` (angel numbers indexer) | String | `["1111"]`, `["777"]` | `[1111]` |
| `Lang` (query) | The endpoint enum (`PostLangQueryParameterType` in that endpoint namespace) | `c.QueryParameters.Lang = PostLangQueryParameterType.Hi` | a string such as `"hi"` |
| Enum fields (`Gender`, `Facing`, `Unit`, room `Type`) | The generated enum, which lives in the namespace of its request body | `KuaPostRequestBody_gender.Female` after `using RoxyApi.FengShui.Kua;` | `"female"`, `Gender = "Female"` |

### Timezone cheat sheet (most-asked locations)

| Region | Decimal | Region | Decimal |
|--------|---------|--------|---------|
| UTC / London (winter) | `0` | Dubai | `4` |
| London (summer, BST) | `1` | Karachi | `5` |
| Berlin / Paris | `1` (winter) / `2` (summer) | Delhi (IST) | `5.5` |
| Istanbul | `3` | Kathmandu (NPT) | `5.75` |
| Moscow | `3` | Dhaka | `6` |
| Tehran | `3.5` (winter) / `4.5` (summer) | Bangkok | `7` |
| Adelaide | `9.5` (winter) / `10.5` (summer) | Singapore / Beijing | `8` |
| New York (EST / EDT) | `-5` / `-4` | Tokyo | `9` |
| Chicago (CST / CDT) | `-6` / `-5` | Sydney | `10` (winter) / `11` (summer) |
| Denver (MST / MDT) | `-7` / `-6` | Auckland | `12` (winter) / `13` (summer) |
| Los Angeles (PST / PDT) | `-8` / `-7` | Honolulu | `-10` |

DST matters. If the birth date falls inside a daylight-saving window, use the summer / DST offset, or pass the IANA string from the location lookup and let the server resolve it. India observes no DST, so a fixed `5.5` is always right there; anywhere else, a natal chart must carry the offset in force at the time of birth.

## Astrology domain gotchas for LLMs

LLMs hallucinate confidently in this category. These are the specific traps you will hit when writing client code:

- **Ayanamsa is server-side in Vedic.** LLMs default to tropical / Western math. Vedic endpoints apply sidereal Lahiri ayanamsa server-side. KP endpoints accept `Ayanamsa` of `kp-newcomb` (default), `kp-old`, `lahiri`, or `custom` (your own degrees in `AyanamsaValue`). Do not try to "correct" server output by subtracting ayanamsa in client code.
- **Tithi count is 30, not 2.** 15 Shukla (waxing) plus 15 Krishna (waning). Older LLM training data conflates Purnima and Amavasya as single tithis. Our panchang response carries a `paksha` field (`"Shukla"` or `"Krishna"`) plus a tithi number, so there are 30 distinct tithis in a lunar month.
- **Rahu and Ketu are shadow points, not planets.** They do not appear in a real ephemeris. Endpoints accept `NodeType` of `"mean"` (smooth mean node, traditional Vedic default) or `"true"` (osculating node with perturbation corrections) to select which calculation to use.
- **Nakshatra count is 27.** Abhijit is sometimes treated as a 28th in some schools, but this API uses the standard 27. `roxy.VedicAstrology.Nakshatras.GetAsync()` returns a list of length 27.
- **Retrograde is per-planet, not global.** Natal chart planets and Vedic `meta` include `IsRetrograde` (a boolean) per planet. KP planet lists use `retrograde`. Never generate "Mercury retrograde globally" UI copy, check the specific planet in the response.
- **Tarot reversals are a product choice.** `AllowReversals = false` on a tarot draw means no reversed cards in that draw, period. It is not cosmically meaningful, it is a config flag.
- **Angel number lookup works for any positive integer.** Digit-root fallback covers non-canonical numbers. Do not generate validation logic that rejects anything other than `111` / `222` / `333`.
- **Seed-based daily endpoints are DETERMINISTIC per `(seed, date)` pair.** Same seed plus same date returns the same reading. This is by design for push-notification consistency. Do not describe it as "cached" or retry on stale responses.
- **Timezone affects Western calculations more than Vedic.** Western natal charts must respect DST at the time of birth. Vedic endpoints default to IST (`5.5`) which is DST-free. Pass the IANA `Timezone` string from the location lookup (`"America/New_York"`, `"Asia/Kolkata"`) directly to chart calls and the server resolves the DST-correct offset for the chart `Date`. Never pass the `UtcOffset` of the city record (the offset in force today) or the clock of the user right now.

## Error handling

Calls throw `RoxyError` (in `RoxyApi.Models`, extends `ApiException`) on every error status the endpoint declares. `Message` is human-readable and may change; `Code` is stable, switch on it, comparing against the `ErrorCodes` constants. `DocUrl` and `RetryAfter` (a nullable `TimeSpan`) carry the documentation link and the `Retry-After` wait. A status the endpoint does not declare, such as a 5xx from the edge, throws the base `ApiException` with `ResponseStatusCode` set. A request that still fails after the last retry throws the error of the final response.

```csharp
try
{
    var horoscope = await roxy.Astrology.Horoscope["aries"].Daily.GetAsync();
    Console.WriteLine(horoscope!.Overview);
}
catch (RoxyError e) when (e.Code == ErrorCodes.RateLimitPerMinute)
{
    await Task.Delay(e.RetryAfter ?? TimeSpan.FromSeconds(1)); // then repeat the call
}
catch (RoxyError e)
{
    Console.WriteLine($"{e.ResponseStatusCode} {e.Code}: {e.Message} ({e.DocUrl})");
    // On a 400, e.Issues lists each field that failed validation.
}
```

<!-- BEGIN:ERRORS -->
Every error returns `{ error, code, doc_url }`. Switch on `code`; `error` is readable text and may change. Each meaning below says whether and when to retry, and `Retry-After` carries the wait when the response sends it. Every code is documented at https://roxyapi.com/docs/errors.

| Status | Code | Meaning |
|--------|------|---------|
| 400 | `validation_error` | A field is missing or invalid. The body lists every failing field at once in issues, each with its path and the expected shape. |
| 400 | `bad_request` | The request was rejected as a whole, for example a body that is not valid JSON or a date range that ends before it starts. |
| 400 | `date_out_of_range` | A date lies outside the span the planetary positions are served for. The error names the supported dates. |
| 401 | `api_key_required` | No API key was sent. |
| 401 | `invalid_api_key` | The key sent is malformed or cannot be verified. |
| 401 | `api_key_revoked` | The key was valid once and has since been revoked from the account page. |
| 401 | `subscription_not_found` | The key verifies, but the subscription behind it no longer exists. |
| 401 | `subscription_inactive` | The subscription behind the key is cancelled, expired or suspended, or a trial is past its end date. |
| 401 | `publishable_key_not_allowed_on_mcp` | A publishable key was used on a Remote MCP server, which takes a secret key. |
| 401 | `invalid_client_ip` | A keyless request could not be attributed to a caller. |
| 401 | `unauthorized` | Authentication failed with no more specific reason to give. |
| 403 | `forbidden` | The key is valid, but this request is not permitted. |
| 403 | `origin_required` | A publishable key with an origin allowlist was used with no Origin header, as from a server or a native app. |
| 403 | `origin_not_allowed` | The calling site is not on the origin allowlist of this publishable key. |
| 404 | `not_found` | Nothing is served at this path, or the identifier in it does not exist. A path miss adds suggestions, the closest real endpoints. |
| 405 | `method_not_allowed` | The path exists for a different HTTP method. The body lists the accepted methods in allow, as does the Allow header. |
| 409 | `conflict` | The request cannot be applied in the current state. |
| 410 | `gone` | The path was removed permanently. |
| 413 | `payload_too_large` | The request body is larger than the API accepts. A body far over the cap is refused before it reaches the API and returns a bare 413 with no JSON body. |
| 415 | `unsupported_media_type` | A request with a body was sent without Content-Type: application/json. |
| 422 | `unprocessable_entity` | Every field is valid, but the combination cannot be computed. |
| 429 | `rate_limit_exceeded` | The monthly request allowance is used up. Retry-After gives the seconds to the reset. |
| 429 | `rate_limit_per_minute` | The per-minute limit of the plan was reached. Retry-After gives the seconds to the next minute, and the monthly allowance is not charged. |
| 429 | `rate_limited` | A rate limit was reached, reported without the plan detail. |
| 429 | `free_tier_exhausted` | A keyless request has used up its trial allowance. |
| 500 | `internal_error` | The request failed inside our service. Retry with exponential backoff. |
| 503 | `compute_saturated` | Heavy calculations are briefly at capacity. Send the same request again after Retry-After seconds. |
| Any | `error` | A fallback for a status with no more specific code. The status and the error field carry the signal. |
<!-- END:ERRORS -->

### Retries

The client retries twice by default on a 429, 500, 502, 503 or 504 and on a refused or unresolvable connection, with full jitter, honouring `Retry-After`, and gives up at once when that header asks for more than thirty seconds. A read timeout is not retried, and an exhausted retry throws the final `RoxyError` or `ApiException`. `new RoxyClient(key, new RoxyRetryOptions { MaxRetries = 0 })` turns retries off.

## MCP equivalents

Every method has a matching MCP tool. The remote MCP server per domain is at `https://roxyapi.com/mcp/{domain}` (Streamable HTTP, no stdio / no self-hosting). Tool names follow `{method}_{path_snake_case}`, for example:

- `POST /astrology/natal-chart` -> `post_astrology_natal_chart` on `/mcp/astrology`
- `GET /astrology/horoscope/{sign}/daily` -> `get_astrology_horoscope_sign_daily` on `/mcp/astrology`
- `POST /vedic-astrology/birth-chart` -> `post_vedic_astrology_birth_chart` on `/mcp/vedic-astrology`
- `POST /tarot/spreads/celtic-cross` -> `post_tarot_spreads_celtic_cross` on `/mcp/tarot`

Use the SDK for typed .NET apps. Use MCP for AI agents (Claude Desktop, Cursor MCP, OpenAI agents) where the agent selects tools based on user intent.

## Gotchas

- **Geocode first.** Any chart, panchang, synastry, compatibility, or natal endpoint needs coordinates. Call `roxy.Location.Search` before the chart method.
- **Path params are indexers, query params are a lambda.** `roxy.Astrology.Horoscope["aries"].Daily.GetAsync(c => c.QueryParameters.Date = new Date(2026, 4, 3))`. An indexer is typed from the spec: a numeric path param takes a number (`Birthstone[1]`, `Hexagrams[1]`), a string one takes a string (`Numbers["1111"]`, `Calendar.Day["2026-10-01"]`).
- **Do not guess method names.** Type `roxy.Domain.` and let IntelliSense show the fluent tree. It mirrors the URL path, not the `operationId`.
- **Responses are nullable.** Use `result!.Field` or null checks; the success body is non-null only on a 2xx (errors throw).
- **`Timezone` is a union wrapper, never a bare number.** Use `new() { Double = -5 }` or `new() { String = "America/New_York" }`.
- **Switch on `e.Code`, not `e.Message`.** The message may change; the code is stable. Compare against `ErrorCodes`, and `using RoxyApi.Models;` for `RoxyError` (`ErrorCodes` and `RoxyRetryOptions` are in `RoxyApi`).
- **A `validation_error` carries `Issues`, each with `Path` (the dot-separated field, such as `time` or `person1.date`), `Message` and `Code`.** Loop over them to show every failing field. `Path` is `(root)` for the whole body, and all three members are nullable strings:
  ```csharp
  foreach (var i in e.Issues ?? [])
      Console.WriteLine($"{i.Path}: {i.Message} ({i.Code})");
  ```
- **`Lang` is a per-endpoint enum.** A GET enum is `GetLangQueryParameterType` in the endpoint namespace, which gains an `Item` segment per indexer: `using RoxyApi.Astrology.Horoscope.Item.Daily;` then `c => c.QueryParameters.Lang = GetLangQueryParameterType.Es`. A POST enum is `PostLangQueryParameterType` in its own namespace.
- **A city record has `City`, `Province`, `Country`, `Iso2`, `Latitude`, `Longitude`, `Timezone`, `UtcOffset` and `Population`.** The name is `City`, never `Name`.
- **`UtcOffset` on a city record is the offset in force today.** Pass the IANA `Timezone` string to chart calls so the server resolves the offset for the birth date.
- **A natal chart `Planets` is a list of objects.** Find a body by `Name`: `chart!.Planets!.First(p => p.Name == "Moon")`.
- **In a Vedic chart `meta.Moon.rashi` is a string and `meta.Moon.nakshatra` is an object** (`name`, `pada`, `lord`). `Meta` values are untyped: `((UntypedObject)kundli!.Meta!.AdditionalData["Moon"]).GetValue()` is a dictionary of `UntypedNode` (`rashi` an `UntypedString`, `nakshatra` an `UntypedObject`). Unwrap with `((UntypedString)node).GetValue()` (`using Microsoft.Kiota.Abstractions.Serialization;`) and display the nakshatra `name`.
- **Usage figures are nullable doubles.** `Usage.GetAsync()` returns `UsedThisMonth`, `RemainingThisMonth` and `RequestsPerMonth` as `double?`.
- **`sign` is matched case-insensitively.** Lowercase is canonical.
- **Total requests on a persistent failure = 1 + retries.** The default `MaxRetries` of 2 makes 3 requests; `MaxRetries = 0` makes exactly one.
- **Most list endpoints return a paginated envelope**, `Total`, `Limit`, `Offset` plus a named list (`Cities`, `Crystals`, `Hexagrams`, `Symbols`). A few catalogs return a bare `List<...>` instead (`Astrology.Signs`, `Astrology.PlanetMeanings`, `VedicAstrology.Avasthas`). Tell them apart by the spec: `jq -r '.paths | to_entries[] | select(.value.get.responses["200"].content["application/json"].schema.type == "array") | .key' specs/openapi.json`, or by the return type of `GetAsync` (`List<...>?` versus an object). Pass `c => c.QueryParameters.Limit = 64` to widen a page; `Iching.Hexagrams` defaults to 20 of 64.
- **Enums live beside their request body.** `Gender`, `Facing`, `Unit` and room `Type` are generated enums in the namespace of the body that uses them (`using RoxyApi.FengShui.Kua;`, `using RoxyApi.Vastu.Rooms;`), so the same member name can exist in several namespaces; the one the body expects is the one to use.
- **Do not expose API keys client-side.** Call Roxy from server code only.

## Links

- Full method reference: `docs/llms-full.txt` (bundled in this package)
- Documentation: https://roxyapi.com/docs
- Interactive API docs: https://roxyapi.com/api-reference
- Pricing and API keys: https://roxyapi.com/pricing
- MCP for AI agents: https://roxyapi.com/docs/mcp
- Templates: https://roxyapi.com/templates
- Source and issues: https://github.com/RoxyAPI/sdk-dotnet
- Other SDKs: TypeScript https://www.npmjs.com/package/@roxyapi/sdk, Python https://pypi.org/project/roxy-sdk/, PHP https://packagist.org/packages/roxyapi/sdk, Go https://pkg.go.dev/github.com/RoxyAPI/sdk-go, Rust https://crates.io/crates/roxyapi
