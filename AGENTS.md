# RoxyApi .NET SDK - Agent Guide

.NET SDK for RoxyAPI. 12+ domains (Western astrology, Vedic astrology, numerology, tarot, human design, forecast, biorhythm, I Ching, crystals, dreams, angel numbers, location) plus utility namespaces (usage, languages). One API key, fully typed, generated from the OpenAPI spec.

> Before writing code with this SDK, read `docs/llms-full.txt` in this package for the complete method reference with one example per endpoint.

## Install and initialize

```bash
dotnet add package RoxyApi.Sdk
```

```csharp
using RoxyApi;

var roxy = new RoxyClient(Environment.GetEnvironmentVariable("ROXY_API_KEY")!);
```

`new RoxyClient(apiKey)` sets the base URL (`https://roxyapi.com/api/v2`) and the auth and SDK headers automatically. Every call is async and returns the typed response, or throws `RoxyError` on a 4xx or 5xx.

## Five rules to get right

Get these and the generated types do the rest.

- **The fluent path mirrors the URL.** `GET /astrology/horoscope/{sign}/daily` is `roxy.Astrology.Horoscope["aries"].Daily.GetAsync()`. Each path segment is a property; each `{param}` is an indexer. Type `roxy.` and let IntelliSense walk the tree. Never invent a method name.
- **Request bodies use a target-typed `new()`.** `await roxy.Astrology.NatalChart.PostAsync(new() { Date = new Date(1990, 1, 15), ... })`. The type is inferred from the method, so you never need to name it; IntelliSense shows every field on `new() {`.
- **Query parameters use a configuration lambda.** `await roxy.Crystals.Search.GetAsync(c => c.QueryParameters.Q = "amethyst");`. Multiple: `c => { c.QueryParameters.Limit = 20; c.QueryParameters.Offset = 0; }`.
- **Always `await`, and catch `RoxyError`.** There is no result-wrapper object. The call returns the typed response directly and throws `RoxyError` (a subclass of `ApiException`) on failure. Switch on `e.Code`, not `e.Message`.
- **Never hand-roll HttpClient.** `new RoxyClient(key)` injects auth, base URL, retries, and typed responses.

## Critical rule: geocode before any chart endpoint

Every chart, horoscope, panchang, dasha, dosha, navamsa, KP, synastry, compatibility, and natal endpoint needs `Latitude`, `Longitude`, and (for Western) `Timezone`. **Never ask the user for coordinates.** Call `roxy.Location.Search` first.

```csharp
var search = await roxy.Location.Search.GetAsync(c => c.QueryParameters.Q = "Berlin");
var city = search!.Cities![0];
// city.Timezone is the IANA string ("Europe/Berlin"); pass it straight into a chart call and
// the server resolves the DST-correct offset for the chart date. city.UtcOffset (5.5, -5, ...)
// is the decimal equivalent.
```

`Q` accepts a bare city (`"Paris"`), city plus country (`"Berlin Germany"`), or comma-qualified (`"Springfield, Illinois"`). Use the qualified form to disambiguate.

## Domains

<!-- BEGIN:DOMAINS -->
| Accessor | What it covers |
|----------|----------------|
| `roxy.Astrology` | Western astrology API for natal birth charts, daily, weekly, and monthly horoscopes with unique content per sign, syn... |
| `roxy.VedicAstrology` | Vedic astrology (Jyotish) and KP API for kundli generation with 15 divisional charts (D1-D60), Ashtakoot Gun Milan ku... |
| `roxy.Forecast` | Merge upcoming transit aspects, sign ingresses, retrograde stations, new and full moons, biorhythm critical days, and... |
| `roxy.HumanDesign` | Generate the full Human Design bodygraph from a birth moment: type, strategy, inner authority, profile, definition, i... |
| `roxy.Numerology` | Numerology API to calculate life path, expression, soul urge, personality, and maturity numbers, with Pinnacle and Ch... |
| `roxy.Tarot` | Tarot reading API with the complete 78-card Rider-Waite-Smith deck and card meanings for love, career, health, and sp... |
| `roxy.Biorhythm` | The most complete biorhythm API: 10 cycle types across 3 primary (physical, emotional, intellectual), 4 secondary (in... |
| `roxy.Iching` | I-Ching oracle API with all 64 hexagrams, 384 changing lines, 8 trigrams, and modern interpretations for love, career... |
| `roxy.Crystals` | Crystal healing API covering the most popular and widely-searched healing crystals and gemstones, from Amethyst and R... |
| `roxy.Dreams` | Dream interpretation API with a 2,000+ symbol dream dictionary and psychological meanings covering animals, objects,... |
| `roxy.AngelNumbers` | Angel numbers API with meanings for 111, 222, 333, 444, 555, 666, 777, 888, 999, 1111, and 75+ sequences covering eve... |
| `roxy.Location` | City search and geocoding API with 23,000+ cities across 240+ countries, returning latitude, longitude, IANA timezone... |
| `roxy.Usage` | Monitor your API usage, check rate limits, and track request consumption |
| `roxy.Languages` | List the response languages accepted by the `lang` query parameter on every i18n-aware endpoint |
<!-- END:DOMAINS -->

## Critical patterns

### Two-step pattern for coordinate-dependent endpoints

```csharp
var search = await roxy.Location.Search.GetAsync(c => c.QueryParameters.Q = "London");
var city = search!.Cities![0];

var chart = await roxy.Astrology.NatalChart.PostAsync(new()
{
    Date = new Date(1990, 1, 15),
    Time = new Time(14, 30, 0),
    Latitude = city.Latitude,
    Longitude = city.Longitude,
    Timezone = new() { String = city.Timezone },
});
```

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
    Date = new Date(1990, 1, 15), Time = new Time(14, 30, 0),
    Latitude = 28.6139, Longitude = 77.209, Timezone = new() { Double = 5.5 },
});

await roxy.Tarot.Spreads.CelticCross.PostAsync(new() { Question = "What should I focus on?" });
await roxy.Numerology.LifePath.PostAsync(new() { Year = 1990, Month = 1, Day = 15 });
```

### Multi-language via the query lambda

Eight languages: `en`, `tr`, `de`, `es`, `fr`, `hi`, `pt`, `ru`. Defaults to `en`.

```csharp
await roxy.Tarot.Daily.PostAsync(new() { Date = new Date(2026, 4, 22) }, c => c.QueryParameters.Lang = "es");
await roxy.Numerology.LifePath.PostAsync(new() { Year = 1990, Month = 1, Day = 15 }, c => c.QueryParameters.Lang = "hi");
```

Supported: astrology, vedicAstrology, numerology, tarot, biorhythm, iching, crystals, angelNumbers. English-only: dreams, location, usage, languages. Call `roxy.Languages.GetAsync()` for the live list.

### Error handling

Calls throw `RoxyError` (extends `ApiException`) on a 4xx or 5xx. `Message` is human-readable and may change; `Code` is stable, switch on it.

```csharp
using RoxyApi.Models;

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
| 404 | `not_found` | Resource not found |
| 429 | `rate_limit_exceeded` | Monthly quota reached |
| 500 | `internal_error` | Server error |

### Reading responses

Responses are fully typed objects, not dictionaries. Discover fields with IntelliSense (`result.`) or the full response JSON on https://roxyapi.com/api-reference. `docs/llms-full.txt` documents the call and inputs for every endpoint; the response shape comes from the type. The success body is nullable (non-null only on a 2xx, since errors throw), so use `result!.Field`.

- **City** (`Location.Search` -> `Cities[i]`): `City` (the name), `Country`, `Province`, `Latitude`, `Longitude`, `Timezone` (IANA), `UtcOffset` (decimal), `Population`.
- **Natal chart**: `Planets`, `Houses`, `Aspects`, `Ascendant`, `Midheaven`, `Summary`. Each planet has `Name`, `Sign`, `Degree`, `House`, `IsRetrograde`. The `Planets` list has more than ten entries (classical bodies plus nodes and key points).

```csharp
var chart = await roxy.Astrology.NatalChart.PostAsync(new() { Date = new Date(1990, 1, 15), Time = new Time(14, 30, 0), Latitude = 40.7128, Longitude = -74.006, Timezone = new() { Double = -5 } });
foreach (var p in chart!.Planets!)
    Console.WriteLine($"{p.Name}: {p.Sign} (house {p.House})");
```

## Common tasks

Ordered by domain priority (Western, Vedic, Numerology, Tarot, Human Design, Forecast, Biorhythm, I Ching, Crystals, Dreams, Angel Numbers, Location).

| Task | Code |
|------|------|
| Daily horoscope | `roxy.Astrology.Horoscope["aries"].Daily.GetAsync()` |
| Natal chart (Western) | `roxy.Astrology.NatalChart.PostAsync(new() { Date, Time, Latitude, Longitude, Timezone })` |
| Synastry | `roxy.Astrology.Synastry.PostAsync(new() { Person1, Person2 })` |
| Compatibility score | `roxy.Astrology.CompatibilityScore.PostAsync(new() { Person1, Person2 })` |
| Current moon phase | `roxy.Astrology.MoonPhase.Current.GetAsync()` |
| Kundli (Vedic birth chart) | `roxy.VedicAstrology.BirthChart.PostAsync(new() { Date, Time, Latitude, Longitude, Timezone })` |
| Panchang (detailed) | `roxy.VedicAstrology.Panchang.Detailed.PostAsync(new() { Date, Latitude, Longitude, Timezone })` |
| Current dasha | `roxy.VedicAstrology.Dasha.Current.PostAsync(new() { Date, Time, Latitude, Longitude, Timezone })` |
| Mangal Dosha | `roxy.VedicAstrology.Dosha.Manglik.PostAsync(new() { Date, Time, Latitude, Longitude, Timezone })` |
| Guna Milan (matching) | `roxy.VedicAstrology.Compatibility.PostAsync(new() { Person1, Person2 })` |
| Navamsa (D9) | `roxy.VedicAstrology.Navamsa.PostAsync(new() { Date, Time, Latitude, Longitude, Timezone })` |
| KP ruling planets | `roxy.VedicAstrology.Kp.RulingPlanets.PostAsync(new() { Latitude, Longitude, Timezone, Datetime })` |
| Nakshatra detail | `roxy.VedicAstrology.Nakshatras["ashwini"].GetAsync()` |
| Life path number | `roxy.Numerology.LifePath.PostAsync(new() { Year, Month, Day })` |
| Full numerology chart | `roxy.Numerology.Chart.PostAsync(new() { FullName, Year, Month, Day })` |
| Personal year | `roxy.Numerology.PersonalYear.PostAsync(new() { Month, Day, Year })` |
| Daily tarot card | `roxy.Tarot.Daily.PostAsync(new() { Seed })` |
| Three-card spread | `roxy.Tarot.Spreads.ThreeCard.PostAsync(new() { Question })` |
| Celtic Cross | `roxy.Tarot.Spreads.CelticCross.PostAsync(new() { Question })` |
| Yes / no tarot | `roxy.Tarot.YesNo.PostAsync(new() { Question })` |
| Human Design bodygraph | `roxy.HumanDesign.Bodygraph.PostAsync(new() { Date, Time, Latitude, Longitude, Timezone })` |
| Forecast timeline | `roxy.Forecast.Timeline.PostAsync(new() { BirthData, StartDate, EndDate })` |
| Daily biorhythm | `roxy.Biorhythm.Daily.PostAsync(new() { Seed })` |
| Cast I Ching reading | `roxy.Iching.Cast.GetAsync(c => c.QueryParameters.Seed = "user-42")` |
| Crystal by zodiac | `roxy.Crystals.Zodiac["scorpio"].GetAsync()` |
| Crystal by chakra | `roxy.Crystals.Chakra["Heart"].GetAsync()` |
| Dream symbol lookup | `roxy.Dreams.Symbols["snake"].GetAsync()` |
| Angel number meaning | `roxy.AngelNumbers.Numbers["1111"].GetAsync()` |
| Find city coordinates | `roxy.Location.Search.GetAsync(c => c.QueryParameters.Q = "Berlin")` |
| Check API usage | `roxy.Usage.GetAsync()` |

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
| `Lang` (query) | Lowercase code | `c.QueryParameters.Lang = "hi"` | `"Hindi"`, `"HI"` |

### Timezone cheat sheet (decimal offsets)

| Region | Decimal | Region | Decimal |
|--------|---------|--------|---------|
| UTC / London (winter) | `0` | Delhi / Kolkata (IST) | `5.5` |
| Berlin / Paris | `1` (winter) / `2` (summer) | Kathmandu | `5.75` |
| Istanbul / Moscow | `3` | Dhaka | `6` |
| Dubai | `4` | Bangkok | `7` |
| New York (EST / EDT) | `-5` / `-4` | Singapore / Beijing | `8` |
| Chicago (CST / CDT) | `-6` / `-5` | Tokyo | `9` |
| Los Angeles (PST / PDT) | `-8` / `-7` | Sydney | `10` / `11` (summer) |

DST matters for Western charts: use the summer offset if the birth date falls in a daylight-saving window, or pass the IANA name and let the server resolve it. Vedic endpoints default to IST (`5.5`), which is DST-free.

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
- **Path params are indexers, query params are a lambda.** `roxy.Astrology.Horoscope["aries"].Daily.GetAsync(c => c.QueryParameters.Date = new Date(2026, 4, 3))`.
- **Do not guess method names.** Type `roxy.Domain.` and let IntelliSense show the fluent tree. It mirrors the URL path.
- **Responses are nullable.** Use `result!.Field` or null checks; the success body is non-null only on a 2xx (errors throw).
- **`Timezone` is a union wrapper, never a bare number.** Use `new() { Double = -5 }` or `new() { String = "America/New_York" }`.
- **Do not expose API keys client-side.** Call Roxy from server, API, or backend code only.

## Dependencies

One runtime dependency: [`Microsoft.Kiota.Bundle`](https://www.nuget.org/packages/Microsoft.Kiota.Bundle) (MIT, by Microsoft), which provides the HTTP, auth, and serialization libraries the generated client uses. Targets `netstandard2.0` and `net8.0`.

## Links

- Full method reference: `docs/llms-full.txt` (bundled in this package)
- Interactive API docs: https://roxyapi.com/api-reference
- Pricing and API keys: https://roxyapi.com/pricing
- MCP for AI agents: https://roxyapi.com/docs/mcp
- TypeScript SDK: https://www.npmjs.com/package/@roxyapi/sdk | Python SDK: https://pypi.org/project/roxy-sdk/
