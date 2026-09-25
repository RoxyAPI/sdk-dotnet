# RoxyApi .NET SDK - Agent Guide

.NET SDK for RoxyAPI. 18+ domains (Western astrology, Vedic astrology, forecast, human design, Chinese astrology, feng shui, Mesoamerican astrology, Vastu, numerology, Kabbalah, tarot, biorhythm, Ayurveda, I Ching, crystals, dreams, angel numbers, location) plus utility namespaces (usage, languages). One API key, fully typed, generated from the OpenAPI spec.

> Before writing code with this SDK, read `docs/llms-full.txt` in this package for the complete method reference with one example per endpoint.

## Install and initialize

```bash
dotnet add package RoxyApi.Sdk
```

```csharp
using RoxyApi;

var roxy = new RoxyClient(Environment.GetEnvironmentVariable("ROXY_API_KEY")!);
```

`new RoxyClient(apiKey)` sets the base URL (`https://roxyapi.com/api/v2`) and the auth and SDK headers automatically. Every call is async and returns the typed response, or throws `RoxyError` on an error response (see Error handling).

## Five rules to get right

Get these and the generated types do the rest.

- **The fluent path mirrors the URL.** `GET /astrology/horoscope/{sign}/daily` is `roxy.Astrology.Horoscope["aries"].Daily.GetAsync()`. Each path segment is a property; each `{param}` is an indexer. Type `roxy.` and let IntelliSense walk the tree. Never invent a method name, and never derive one from the `operationId` in the spec: this SDK is path-fluent, not operation-named.
- **Request bodies use a target-typed `new()`.** `await roxy.Astrology.NatalChart.PostAsync(new() { Date = new Date(1990, 1, 15), ... })`. The type is inferred from the method, so you never need to name it; IntelliSense shows every field on `new() {`.
- **Query parameters use a configuration lambda.** `await roxy.Crystals.Search.GetAsync(c => c.QueryParameters.Q = "amethyst");`. Multiple: `c => { c.QueryParameters.Limit = 20; c.QueryParameters.Offset = 0; }`.
- **Always `await`, and catch `RoxyError`.** There is no result-wrapper object. The call returns the typed response directly and throws `RoxyError` (a subclass of `ApiException`) on failure. Switch on `e.Code`, not `e.Message`.
- **Never hand-roll HttpClient.** `new RoxyClient(key)` injects auth, the base URL, typed responses, and a retry with backoff on a 429, 503 or 504: up to three attempts, honouring `Retry-After`, and giving up at once when that header asks for more than thirty seconds. Response field names come from the response schema of the spec and are PascalCase properties; the compiler catches any invented field, so if the build fails on a property, the field does not exist.
- **Look up any operation or field beyond this guide.** Query the combined OpenAPI spec at `https://roxyapi.com/api/v2/openapi.json` with the jq recipe in `https://roxyapi.com/AGENTS.md`, or search the keyless Docs MCP server at `https://roxyapi.com/mcp/docs` (one tool, `search_docs`).

## Critical rule: geocode before any chart endpoint

Every chart, horoscope, panchang, dasha, dosha, navamsa, KP, synastry, compatibility, and natal endpoint needs `Latitude`, `Longitude`, and (for Western) `Timezone`. **Never ask the user for coordinates.** Call `roxy.Location.Search` first.

```csharp
var place = await roxy.Location.Search.GetAsync(c => c.QueryParameters.Q = "New York");
var city = place!.Cities![0];
// city.Timezone is the IANA string ("America/New_York"). Pass it straight into any chart
// endpoint and the server resolves it to the DST-correct decimal offset using the Date of
// the chart itself, so a January 1990 New York chart picks EST (-5) even when you looked
// the city up in July. city.UtcOffset (5.5, -5, 9, ...) also works and produces identical charts.
```

`Q` accepts a bare city (`"Paris"`), city plus country (`"Berlin Germany"`), or comma-qualified (`"Springfield, Illinois"`). Use the qualified form to disambiguate same-named cities.

## Domains

<!-- BEGIN:DOMAINS -->
| Accessor | What it covers |
|----------|----------------|
| `roxy.Astrology` | Western astrology API for natal birth charts, daily, weekly, monthly, and yearly horoscopes with unique content per s... |
| `roxy.VedicAstrology` | Vedic astrology (Jyotish) and KP API for kundli generation with the sixteen Shodasavarga divisional charts (D1 to D60... |
| `roxy.Forecast` | Astrology forecast API that merges upcoming transit aspects, sign ingresses, retrograde stations, new and full moons,... |
| `roxy.HumanDesign` | Human Design API that generates the full bodygraph from a birth moment: type, strategy, inner authority, profile, def... |
| `roxy.ChineseAstrology` | Chinese zodiac and BaZi astrology API: Four Pillars charts, Chinese zodiac signs and the Chinese lunisolar calendar f... |
| `roxy.FengShui` | Compute classical feng shui from one API: Xuan Kong flying star natal charts for any of the nine periods and 24 mount... |
| `roxy.MesoamericanAstrology` | Calculate Mayan astrology day signs, the Tzolkin sacred round, the Haab year, the full Long Count and the Aztec tonal... |
| `roxy.Vastu` | Vastu Shastra API for directional home and plot analysis: entrance padas with the classical effect of each of the 32... |
| `roxy.Numerology` | Numerology API to calculate life path, expression, soul urge, personality, and maturity numbers, with Pinnacle and Ch... |
| `roxy.Kabbalah` | Kabbalah API for gematria, the 72 names, the Tree of Life and the Hebrew birthday, from one key |
| `roxy.Tarot` | Tarot reading API with the complete 78-card Rider-Waite-Smith deck and card meanings for love, career, health, and sp... |
| `roxy.Biorhythm` | The most complete biorhythm API: 10 cycle types across 3 primary (physical, emotional, intellectual), 4 secondary (in... |
| `roxy.Ayurveda` | Ayurveda API for dosha profiles, the dinacharya daily routine and the ritucharya seasonal regimen, with a verse cited... |
| `roxy.Iching` | I-Ching oracle API with all 64 hexagrams, 384 changing lines, 8 trigrams, and modern interpretations for love, career... |
| `roxy.Crystals` | Crystal healing API covering the most popular and widely-searched healing crystals and gemstones, from Amethyst and R... |
| `roxy.Dreams` | Dream interpretation API with a 2,000+ symbol dream dictionary and psychological meanings covering animals, objects,... |
| `roxy.AngelNumbers` | Angel numbers API with meanings for 111, 222, 333, 444, 555, 666, 777, 888, 999, 1111, and 75+ sequences covering eve... |
| `roxy.Location` | Timezone and location API with city search and geocoding across 235,000+ cities in 240+ countries, returning latitude... |
| `roxy.Usage` | Monitor your API usage, check rate limits, and track request consumption |
| `roxy.Languages` | List the response languages accepted by the `lang` query parameter on every i18n-aware endpoint |
<!-- END:DOMAINS -->

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

### Error handling

Calls throw `RoxyError` (in `RoxyApi.Models`, extends `ApiException`) on every error status the endpoint declares. `Message` is human-readable and may change; `Code` is stable, switch on it. A status the endpoint does not declare, such as a 5xx from the edge, throws the base `ApiException` with `ResponseStatusCode` set, and a 503 or 504 still failing after three retries throws an `AggregateException` holding every attempt.

```csharp
try
{
    var horoscope = await roxy.Astrology.Horoscope["aries"].Daily.GetAsync();
    Console.WriteLine(horoscope!.Overview);
}
catch (RoxyError e)
{
    Console.WriteLine($"{e.ResponseStatusCode} {e.Code}: {e.Message}");
    // On a 400, e.Issues lists each field that failed validation.
}
```

| Status | Code | When |
|--------|------|------|
| 400 | `validation_error` | Missing or invalid parameters (see `Issues`) |
| 401 | `api_key_required` | No API key provided |
| 401 | `invalid_api_key` | Key format invalid or tampered |
| 401 | `subscription_not_found` | Key references a non-existent subscription |
| 401 | `subscription_inactive` | Subscription cancelled, expired, or suspended |
| 401 | `api_key_revoked` | Key was deleted from the account |
| 404 | `not_found` | Resource not found |
| 4xx | `bad_request` and other status-derived codes | A client error the endpoint itself detected, such as a date window whose `endDate` precedes `startDate` |
| 429 | `rate_limit_exceeded` | Monthly quota reached |
| 500 | `internal_error` | Server error |

### Reading responses

Responses are fully typed objects, not dictionaries. Discover fields with IntelliSense (`result.`) or the full response JSON on https://roxyapi.com/api-reference. `docs/llms-full.txt` documents the call and inputs for every endpoint; the response shape comes from the type. The success body is nullable (non-null only on a 2xx, since errors throw), so use `result!.Field`.

- **City** (`Location.Search` -> `Cities[i]`): `City` (the name), `Country`, `Province`, `Latitude`, `Longitude`, `Timezone` (IANA), `UtcOffset` (decimal), `Population`.
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

Copy the format column exactly.

| Field | Format | Good | Bad |
|-------|--------|------|-----|
| `Date` (and `BirthDate`, `StartDate`, ...) | The `Date` struct from `Microsoft.Kiota.Abstractions` | `new Date(1990, 1, 15)` | `"1990-01-15"`, `DateTime.Now`, `new DateTime(...)` |
| `Time` | The `Time` struct from `Microsoft.Kiota.Abstractions` | `new Time(14, 30, 0)`, `new Time(9, 0, 0)` | `"14:30:00"` (string), `DateTime.Now`, `new TimeOnly(...)` |
| `Timezone` | Union wrapper: decimal OR IANA | `new() { Double = 5.5 }`, `new() { Double = -5 }` OR `new() { String = "America/New_York" }` | `5.5`, `"5.5"`, `"+0530"` assigned directly |
| `Latitude` / `Longitude` | `double` | `40.7128`, `-74.006` | `"40.7128"`, DMS strings |
| `sign` (horoscope indexer) | Lowercase zodiac name | `["aries"]`, `["scorpio"]` | `["Aries"]`, `["1"]` |
| `chakra` (crystals indexer) | Title-case name | `["Root"]`, `["Heart"]`, `["Third Eye"]` | `["heart"]`, `["third-eye"]` |
| `FullName` (numerology) | Birth-certificate name | `"John William Smith"` | Nickname, partial name |
| `Seed` | Any string (deterministic) | `"user-42"`, `"session-abc"` | numbers, objects |
| `number` (angel numbers indexer) | String | `["1111"]`, `["777"]` | `[1111]` |
| `Lang` (query) | The endpoint enum (`PostLangQueryParameterType` in that endpoint namespace) | `c.QueryParameters.Lang = PostLangQueryParameterType.Hi` | a string such as `"hi"` |
| Enum fields (`Gender`, `Facing`, `Unit`, room `Type`) | The generated enum, which lives in the namespace of its request body | `KuaPostRequestBody_gender.Female` after `using RoxyApi.FengShui.Kua;` | `"female"`, `Gender = "Female"` |

### Timezone cheat sheet (decimal offsets)

| Region | Decimal | Region | Decimal |
|--------|---------|--------|---------|
| UTC / London (winter) | `0` | Delhi (IST) | `5.5` |
| Berlin / Paris | `1` (winter) / `2` (summer) | Kathmandu | `5.75` |
| Istanbul / Moscow | `3` | Dhaka | `6` |
| Dubai | `4` | Bangkok | `7` |
| New York (EST / EDT) | `-5` / `-4` | Singapore / Beijing | `8` |
| Chicago (CST / CDT) | `-6` / `-5` | Tokyo | `9` |
| Los Angeles (PST / PDT) | `-8` / `-7` | Sydney | `10` / `11` (summer) |

DST matters. If the birth date falls inside a daylight-saving window, use the summer / DST offset, or pass the IANA string from the location lookup and let the server resolve it. India observes no DST, so a fixed `5.5` is always right there; anywhere else, a natal chart must carry the offset in force at the time of birth.

## Astrology domain gotchas

LLMs hallucinate confidently in this category. The specific traps:

- **Ayanamsa is server-side in Vedic.** Vedic endpoints apply sidereal Lahiri ayanamsa server-side; KP endpoints take an `AyanamsaValue`. Do not subtract ayanamsa in client code.
- **Tithi count is 30, not 2.** 15 Shukla (waxing) plus 15 Krishna (waning). Panchang responses carry a paksha plus a tithi number.
- **Rahu and Ketu are shadow points, not planets.** They do not appear in a real ephemeris.
- **Nakshatra count is 27.** `roxy.VedicAstrology.Nakshatras.GetAsync()` returns 27 entries.
- **Retrograde is per-planet, not global.** Check the specific planet in the response; never generate "Mercury retrograde globally" copy.
- **Seed-based daily endpoints are deterministic per (seed, date).** Same seed plus same date returns the same reading. By design for push consistency, not a cache bug.
- **Angel number lookup works for any positive integer.** `roxy.AngelNumbers.Lookup` covers non-canonical numbers via digit-root fallback. Do not reject anything but 111 / 222 / 333.

## MCP equivalents

Every method has a matching MCP tool. The remote MCP server per domain is at `https://roxyapi.com/mcp/{domain}` (Streamable HTTP, no stdio, no self-hosting). Tool names follow `{method}_{path_snake_case}`:

- `POST /astrology/natal-chart` -> `post_astrology_natal_chart` on `/mcp/astrology`
- `GET /astrology/horoscope/{sign}/daily` -> `get_astrology_horoscope_sign_daily` on `/mcp/astrology`
- `POST /vedic-astrology/birth-chart` -> `post_vedic_astrology_birth_chart` on `/mcp/vedic-astrology`

Use the SDK for typed .NET apps. Use MCP for AI agents (Claude, Cursor, ChatGPT) where the agent selects tools from user intent.

## Gotchas

- **Geocode first.** Any chart, panchang, synastry, compatibility, or natal endpoint needs coordinates. Call `roxy.Location.Search` before the chart method.
- **Path params are indexers, query params are a lambda.** `roxy.Astrology.Horoscope["aries"].Daily.GetAsync(c => c.QueryParameters.Date = new Date(2026, 4, 3))`. An indexer is typed from the spec: a numeric path param takes a number (`Birthstone[1]`, `Hexagrams[1]`), a string one takes a string (`Numbers["1111"]`, `Calendar.Day["2026-10-01"]`).
- **Do not guess method names.** Type `roxy.Domain.` and let IntelliSense show the fluent tree. It mirrors the URL path, not the `operationId`.
- **Responses are nullable.** Use `result!.Field` or null checks; the success body is non-null only on a 2xx (errors throw).
- **`Timezone` is a union wrapper, never a bare number.** Use `new() { Double = -5 }` or `new() { String = "America/New_York" }`.
- **Switch on `e.Code`, not `e.Message`.** The message may change; the code is stable.
- **List endpoints return a paginated envelope**, `Total`, `Limit`, `Offset` plus a named list (`Cities`, `Crystals`, `Hexagrams`, `Symbols`), never a bare list. Pass `c => c.QueryParameters.Limit = 64` to widen a page; `Iching.Hexagrams` defaults to 20 of 64.
- **Enums live beside their request body.** `Gender`, `Facing`, `Unit` and room `Type` are generated enums in the namespace of the body that uses them (`using RoxyApi.FengShui.Kua;`, `using RoxyApi.Vastu.Rooms;`), so the same member name can exist in several namespaces; the one the body expects is the one to use.
- **Do not expose API keys client-side.** Call Roxy from server, API, or backend code only.

## Dependencies

One runtime dependency: [`Microsoft.Kiota.Bundle`](https://www.nuget.org/packages/Microsoft.Kiota.Bundle) (MIT, by Microsoft), which provides the HTTP, auth, and serialization libraries the generated client uses. Targets `netstandard2.0` and `net8.0`.

## Links

- Full method reference: `docs/llms-full.txt` (bundled in this package)
- Interactive API docs: https://roxyapi.com/api-reference
- Pricing and API keys: https://roxyapi.com/pricing
- MCP for AI agents: https://roxyapi.com/docs/mcp
- TypeScript SDK: https://www.npmjs.com/package/@roxyapi/sdk | Python SDK: https://pypi.org/project/roxy-sdk/
