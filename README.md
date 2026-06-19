<p align="center">
  <a href="https://roxyapi.com">
    <img src="https://raw.githubusercontent.com/RoxyAPI/sdk-dotnet/main/assets/hero.png" alt="Roxy .NET SDK. Astrology, Vedic, numerology, tarot, and more behind one API key." width="100%">
  </a>
</p>

# RoxyApi

[![NuGet](https://img.shields.io/nuget/v/RoxyApi)](https://www.nuget.org/packages/RoxyApi)
[![Docs](https://img.shields.io/badge/docs-roxyapi.com-blue)](https://roxyapi.com/docs/sdk)
[![API Reference](https://img.shields.io/badge/api%20reference-roxyapi.com-blue)](https://roxyapi.com/api-reference)
[![Pricing](https://img.shields.io/badge/pricing-roxyapi.com-blue)](https://roxyapi.com/pricing)

The .NET SDK for astrology, Vedic astrology, numerology, tarot, and more.

One API key. Fully typed. Verified against NASA JPL Horizons.

The fastest way to add natal charts, kundli matching, daily horoscopes, tarot readings, and spiritual insights to ASP.NET Core, Blazor, MAUI, Unity, and AI agents. Twelve domains behind a single [Roxy](https://roxyapi.com) subscription, interpretations in eight languages, generated from the OpenAPI spec so new endpoints appear the day they ship.

## Install

```bash
dotnet add package RoxyApi
```

Or add it to your project file:

```xml
<PackageReference Include="RoxyApi" Version="*" />
```

Targets `netstandard2.0` and `net8.0`, so it runs on .NET 8 and later, .NET Framework 4.6.1 and later, Unity, and Mono.

## Start with one call

Get real product value with a single typed call. No setup beyond your API key.

```csharp
using RoxyApi;

var roxy = new RoxyClient(Environment.GetEnvironmentVariable("ROXY_API_KEY")!);

var horoscope = await roxy.Astrology.Horoscope["aries"].Daily.GetAsync();
Console.WriteLine($"{horoscope!.Overview}\n{horoscope.Love}\nLucky number: {horoscope.LuckyNumber}");
```

Then expand into charts, compatibility, numerology, tarot, and more.

## Quick start

```csharp
using RoxyApi;
using RoxyApi.Models;
using Microsoft.Kiota.Abstractions; // for the Date type

var roxy = new RoxyClient(Environment.GetEnvironmentVariable("ROXY_API_KEY")!);

// Step 1: geocode the birth city (required for any chart endpoint).
var search = await roxy.Location.Search.GetAsync(c => c.QueryParameters.Q = "Mumbai, India");
var city = search!.Cities![0];

// Step 2: Western natal chart. Pass the IANA timezone string and the server
// resolves it to the DST-correct offset for the chart's own date.
var chart = await roxy.Astrology.NatalChart.PostAsync(new()
{
    Date = new Date(1990, 1, 15),
    Time = "14:30:00",
    Latitude = city.Latitude,
    Longitude = city.Longitude,
    Timezone = new() { String = city.Timezone },
});

// Vedic kundli takes the same inputs (timezone optional, defaults to 5.5 IST).
var kundli = await roxy.VedicAstrology.BirthChart.PostAsync(new()
{
    Date = new Date(1990, 1, 15),
    Time = "14:30:00",
    Latitude = city.Latitude,
    Longitude = city.Longitude,
    Timezone = new() { String = city.Timezone },
});
```

`new RoxyClient(apiKey)` sets the base URL (`https://roxyapi.com/api/v2`) and injects the auth header and SDK identification header on every request.

## Three things to know

These are the only .NET-specific shapes worth learning. The rest is plain typed objects.

- **Dates use the `Date` type.** `Date = new Date(1990, 1, 15)` (from `Microsoft.Kiota.Abstractions`). Times stay strings: `Time = "14:30:00"`.
- **`Timezone` is a typed union.** Pass a decimal offset with `new() { Double = 5.5 }` or an IANA name with `new() { String = "Asia/Kolkata" }`. The server resolves an IANA name to the DST-correct offset for the request date.
- **Query parameters use a configuration lambda.** `await roxy.Crystals.Search.GetAsync(c => c.QueryParameters.Q = "amethyst");`

## Location first

Every chart, horoscope, panchang, dasha, dosha, navamsa, KP, synastry, compatibility, and natal endpoint needs `Latitude`, `Longitude`, and (for Western) `Timezone`. **Never ask users to type coordinates.** Call `roxy.Location.Search` first, then feed the result into the chart method.

```csharp
var search = await roxy.Location.Search.GetAsync(c => c.QueryParameters.Q = "Tokyo");
var city = search!.Cities![0];
// city.Timezone is the IANA string ("Asia/Tokyo"). Pass it straight into any chart
// endpoint. city.UtcOffset (a decimal like 9 or 5.5) also works.
```

`Q` accepts a bare city (`"Mumbai"`), city plus country (`"Berlin Germany"`), or comma-qualified (`"Springfield, Illinois"`). Use the qualified form to disambiguate same-named cities.

## Domains

Type `roxy.` to see every domain. Type `roxy.Astrology.` to see every endpoint in that domain.

<!-- BEGIN:DOMAINS -->
| Accessor | Endpoints | What it covers |
|----------|-----------|----------------|
| `roxy.Astrology` | 23 | Production-ready Western astrology API + remote MCP for AI agents and developers |
| `roxy.VedicAstrology` | 43 | Production-grade Vedic (Jyotish) and KP astrology API + remote MCP for AI agents and developers |
| `roxy.Numerology` | 20 | Production-ready Pythagorean numerology API + hosted MCP for AI agents and developers |
| `roxy.Tarot` | 10 | Production-ready tarot card reading API + hosted MCP for AI agents and developers |
| `roxy.HumanDesign` | 12 | Generate the full Human Design bodygraph from a birth moment: type, strategy, inner authority, profile, definition, i... |
| `roxy.Forecast` | 5 | Merge upcoming transit aspects, sign ingresses, retrograde stations, new and full moons, biorhythm critical days, and... |
| `roxy.Biorhythm` | 6 | The most complete biorhythm API + remote MCP for AI agents and developers |
| `roxy.Iching` | 9 | I-Ching oracle API + hosted MCP for AI agents and developers |
| `roxy.Crystals` | 12 | Production-ready crystal healing API + hosted MCP for AI agents and developers |
| `roxy.Dreams` | 5 | Dream interpretation API + hosted MCP for AI agents and developers |
| `roxy.AngelNumbers` | 4 | Production-ready angel numbers API + hosted MCP for AI agents and developers |
| `roxy.Location` | 3 | City search and geocoding API + hosted MCP for AI agents and astrology apps |
| `roxy.Usage` | 1 | Monitor your API usage, check rate limits, and track request consumption |
| `roxy.Languages` | 1 | List the response languages accepted by the `lang` query parameter on every i18n-aware endpoint |
<!-- END:DOMAINS -->

## Most-used endpoints

The highest-demand endpoints by domain, in the order you are most likely to ship them. Full endpoint catalog in the [API reference](https://roxyapi.com/api-reference), complete method list in [`docs/llms-full.txt`](https://github.com/RoxyAPI/sdk-dotnet/blob/main/docs/llms-full.txt).

### 1. Western astrology API (natal chart, daily horoscope, synastry)

The global astrology app market is $6.27B and almost entirely Western. These endpoints power zodiac dating apps, Co-Star-style natal chart products, daily horoscope features, and lunar-cycle wellness apps.

```csharp
// Natal chart. The number-one Western query, called on every onboarding.
var natal = await roxy.Astrology.NatalChart.PostAsync(new()
{
    Date = new Date(1990, 1, 15), Time = "14:30:00",
    Latitude = 28.6139, Longitude = 77.209, Timezone = new() { Double = 5.5 },
});

// Daily horoscope. Highest per-user call frequency in the catalog, drives DAUs and push.
var horoscope = await roxy.Astrology.Horoscope["aries"].Daily.GetAsync();
// horoscope.Overview, horoscope.Love, horoscope.Career, horoscope.LuckyNumber

// Synastry. The dating-app pro-tier feature, full inter-aspect analysis between two charts.
var synastry = await roxy.Astrology.Synastry.PostAsync(new()
{
    Person1 = new() { Date = new Date(1990, 1, 15), Time = "14:30:00", Latitude = 28.61, Longitude = 77.20, Timezone = new() { Double = 5.5 } },
    Person2 = new() { Date = new Date(1992, 7, 22), Time = "09:00:00", Latitude = 19.07, Longitude = 72.87, Timezone = new() { Double = 5.5 } },
});

// Moon phase. Viral for wellness, cycle-tracking, and meditation apps.
var moon = await roxy.Astrology.MoonPhase.Current.GetAsync();
```

### 2. Vedic astrology API (kundli, panchang, dasha, Guna Milan, KP)

The depth moat. India astrology market: $163M in 2024, projected $1.8B by 2030. Kundli, panchang, dasha, dosha, and KP are the five Google-dominant queries for every matrimonial platform, kundli generator, and muhurat app.

```csharp
// Vedic kundli. Top India astrology keyword. Entry point for every Jyotish product.
var kundli = await roxy.VedicAstrology.BirthChart.PostAsync(new()
{
    Date = new Date(1990, 1, 15), Time = "14:30:00",
    Latitude = 28.6139, Longitude = 77.209, Timezone = new() { Double = 5.5 },
});

// Panchang. Tithi, nakshatra, yoga, karana, rahu kaal, abhijit muhurta in one call.
var panchang = await roxy.VedicAstrology.Panchang.Detailed.PostAsync(new()
{
    Date = new Date(2026, 4, 22), Latitude = 28.6139, Longitude = 77.209, Timezone = new() { Double = 5.5 },
});

// Vimshottari dasha. Highest-value single-shot Vedic query.
var dasha = await roxy.VedicAstrology.Dasha.Current.PostAsync(new()
{
    Date = new Date(1990, 1, 15), Time = "14:30:00",
    Latitude = 28.6139, Longitude = 77.209, Timezone = new() { Double = 5.5 },
});

// Mangal Dosha. Most-asked matrimonial question in India.
var manglik = await roxy.VedicAstrology.Dosha.Manglik.PostAsync(new()
{
    Date = new Date(1990, 1, 15), Time = "14:30:00",
    Latitude = 28.6139, Longitude = 77.209, Timezone = new() { Double = 5.5 },
});

// Guna Milan. 36-point Ashtakoota matrimonial compatibility score.
var milan = await roxy.VedicAstrology.Compatibility.PostAsync(new()
{
    Person1 = new() { Date = new Date(1990, 1, 15), Time = "14:30:00", Latitude = 28.61, Longitude = 77.20, Timezone = new() { Double = 5.5 } },
    Person2 = new() { Date = new Date(1992, 7, 22), Time = "09:00:00", Latitude = 19.07, Longitude = 72.87, Timezone = new() { Double = 5.5 } },
});

// KP ruling planets. Horary answers for "will X happen" questions in real time.
var kp = await roxy.VedicAstrology.Kp.RulingPlanets.PostAsync(new()
{
    Latitude = 28.6139, Longitude = 77.209, Timezone = new() { Double = 5.5 },
    Datetime = "2026-04-22T10:30:00Z",
});
```

### 3. Numerology API (life path, full chart, personal year)

Commodity content with durable demand. `life path number calculator` is among the highest-volume spiritual searches globally. Works without birth time, the easiest domain to integrate.

```csharp
// Life Path. The number-one numerology keyword, every calculator page starts here.
var lifePath = await roxy.Numerology.LifePath.PostAsync(new() { Year = 1990, Month = 1, Day = 15 });
// lifePath.Number, lifePath.Type ("single" or "master"), lifePath.Meaning

// Full numerology chart. Premium one-shot: all core numbers plus karmic and personal year.
var chart = await roxy.Numerology.Chart.PostAsync(new()
{
    FullName = "Jane Smith", Year = 1990, Month = 1, Day = 15,
});

// Personal Year. Annual forecast, drives January traffic spikes.
var personalYear = await roxy.Numerology.PersonalYear.PostAsync(new() { Month = 1, Day = 15, Year = 2026 });
```

### 4. Tarot API (daily card, Celtic Cross, three-card, yes / no)

High search volume, evergreen. Apps fetch the card database once and cache it, then draw on demand.

```csharp
// Daily card. Stickiest tarot feature. Seed per user for deterministic once-per-day behavior.
var daily = await roxy.Tarot.Daily.PostAsync(new() { Seed = "user-42" });
// daily.Card.Name, daily.Card.ImageUrl, daily.DailyMessage

// Celtic Cross. Professional-reader spread. Premium-tier, ten positions.
var celtic = await roxy.Tarot.Spreads.CelticCross.PostAsync(new() { Question = "What should I focus on?" });

// Three-card past-present-future. Most-drawn spread on every tarot platform.
var three = await roxy.Tarot.Spreads.ThreeCard.PostAsync(new() { Question = "My next quarter" });

// Yes / No. Impulse micro-query, highest conversion-to-first-call on tarot surfaces.
var answer = await roxy.Tarot.YesNo.PostAsync(new() { Question = "Should I take the offer?" });
// answer.Answer ("Yes", "No", "Maybe"), answer.Strength
```

### 5. Human Design API (bodygraph in one call)

The breakout 2026 self-discovery category. One call returns the full bodygraph from a birth moment: energy type, strategy, authority, profile, definition, incarnation cross, the nine centers, defined channels, and all gate activations. The Design side is solved on the exact 88-degree solar arc, not approximated as calendar days.

```csharp
var bodygraph = await roxy.HumanDesign.Bodygraph.PostAsync(new()
{
    Date = new Date(1990, 7, 4), Time = "10:12:00",
    Latitude = 28.6139, Longitude = 77.209, Timezone = new() { Double = 5.5 },
});
// bodygraph.Type, bodygraph.Strategy, bodygraph.Profile, bodygraph.Definition
// bodygraph.Centers, bodygraph.Channels, bodygraph.Gates, bodygraph.IncarnationCross
```

### 6. Forecast API (cross-domain timeline)

The first cross-domain, stateless forecast in the catalog. One call merges Western transit-to-natal aspects, sign ingresses, retrograde stations, Vedic Vimshottari dasha boundaries, and biorhythm critical days into a single significance-scored, time-ordered timeline.

```csharp
var timeline = await roxy.Forecast.Timeline.PostAsync(new()
{
    BirthData = new() { Date = new Date(1990, 7, 4), Time = "10:12:00", Latitude = 28.6139, Longitude = 77.209, Timezone = new() { Double = 5.5 } },
    StartDate = new Date(2026, 6, 1),
    EndDate = new Date(2026, 6, 30),
});
// timeline.Count, timeline.Events[0].Date, timeline.Events[0].Domain, timeline.Events[0].Significance
```

### 7. Biorhythm API (daily check-in, forecast, compatibility)

Zero competition domain. Steady search volume with the top Google result being a static calculator page. Pure land-grab for wellness, productivity, sports, and couples apps.

```csharp
// Daily biorhythm. Physical, emotional, intellectual, intuitive, plus extended cycles.
var bio = await roxy.Biorhythm.Daily.PostAsync(new() { Seed = "user-1", Date = new Date(2026, 4, 23) });

// Multi-day forecast. Best-day and worst-day planner for calendar and coaching products.
var forecast = await roxy.Biorhythm.Forecast.PostAsync(new()
{
    BirthDate = new Date(1990, 1, 15), StartDate = new Date(2026, 4, 1), EndDate = new Date(2026, 4, 30),
});
```

### 8. I Ching API (cast a reading, 64-hexagram catalog)

Meditation apps, decision-making tools, and wisdom chatbots. `i ching API` and `hexagram API` are the keywords.

```csharp
// Cast a reading. Active divination: primary hexagram plus changing lines and transformed hexagram.
var reading = await roxy.Iching.Cast.GetAsync(c => c.QueryParameters.Seed = "user-42");
// reading.Hexagram, reading.ChangingLinePositions, reading.ResultingHexagram

// Hexagram catalog. Cache once for all 64 hexagrams.
var hexagrams = await roxy.Iching.Hexagrams.GetAsync();
```

### 9. Crystals API (by zodiac, by chakra, birthstone)

Crystal retail and metaphysical shops use these to build "crystals for [sign]" and "[chakra] chakra stones" pages.

```csharp
// By zodiac. Highest-search crystal query pattern.
var bySign = await roxy.Crystals.Zodiac["scorpio"].GetAsync();

// By chakra. Second-highest crystal query pattern.
var byChakra = await roxy.Crystals.Chakra["Heart"].GetAsync();

// Birthstone by month. Evergreen gift and jewelry SEO.
var birthstone = await roxy.Crystals.Birthstone[4].GetAsync();
```

### 10. Dream interpretation API (symbol dictionary, search)

Thousands of dream symbols. `dream meaning` is among the highest-volume spiritual searches on Google. Journal apps, AI therapy chatbots, and self-discovery products are the buyers.

```csharp
// Symbol detail. Every "what does it mean to dream about X" page lands here.
var symbol = await roxy.Dreams.Symbols["snake"].GetAsync();
// symbol.Name, symbol.Meaning

// Symbol search. Chatbots cache the dictionary locally after one call.
var results = await roxy.Dreams.Symbols.GetAsync(c => c.QueryParameters.Q = "water");
```

### 11. Angel Numbers API (111, 222, 333 meanings plus universal lookup)

Gen Z spiritual-tok fuel. `111 meaning`, `222 meaning`, `333 angel number` are evergreen viral queries with massive shareability.

```csharp
// By number. Every "meaning of 1111" page is backed by this.
var angel = await roxy.AngelNumbers.Numbers["1111"].GetAsync();
// angel.Meaning.Spiritual, angel.Meaning.Love, angel.Affirmation

// Universal lookup. Works for any positive integer via digit-root fallback.
var any = await roxy.AngelNumbers.Lookup.GetAsync(c => c.QueryParameters.Number = "4242");
```

## Built for AI agents (Cursor, Claude Code, Copilot, Codex, Gemini CLI)

<p align="center">
  <img src="https://raw.githubusercontent.com/RoxyAPI/sdk-dotnet/main/assets/agents.png" alt="Built for Cursor, Claude Code, Copilot, Codex. AGENTS.md ships in the package, remote MCP, no local setup." width="100%">
</p>

This package ships documentation that AI coding agents read directly from the restored NuGet package:

- `AGENTS.md` for quick start, patterns, gotchas, and a common-tasks reference
- `docs/llms-full.txt` for the complete method reference with one example per endpoint

Agents that support `AGENTS.md` (Claude Code, Cursor, GitHub Copilot, OpenAI Codex, Gemini CLI) pick it up automatically. For other tools, point your agent at the restored package under `~/.nuget/packages/roxyapi/<version>/`.

Prefer MCP? Every domain has a [remote MCP server](https://roxyapi.com/docs/mcp) at `https://roxyapi.com/mcp/{domain}` (Streamable HTTP, no stdio, no self-hosting). One-line Claude Code setup:

```bash
claude mcp add-json --scope user roxy-astrology \
  '{"type":"http","url":"https://roxyapi.com/mcp/astrology","headers":{"X-API-Key":"YOUR_KEY"}}'
```

## Authentication

Get your API key at [roxyapi.com/pricing](https://roxyapi.com/pricing). Instant delivery after checkout.

```csharp
var roxy = new RoxyClient(Environment.GetEnvironmentVariable("ROXY_API_KEY")!);
```

Never expose your API key in a desktop, mobile, browser, or Unity client. Call Roxy from your server, API, or backend only.

For advanced use (a custom `HttpClient`, a proxy, or your own middleware), build the client with a Kiota request adapter:

```csharp
using Microsoft.Kiota.Http.HttpClientLibrary;

var adapter = new HttpClientRequestAdapter(authProvider, httpClient: yourHttpClient);
var roxy = new RoxyClient(adapter);
```

## Multi-language responses

Interpretations and editorial text are available in eight languages: English (`en`), Turkish (`tr`), German (`de`), Spanish (`es`), French (`fr`), Hindi (`hi`), Portuguese (`pt`), Russian (`ru`). Pass `Lang` on any supported endpoint through the query configuration:

```csharp
var card = await roxy.Tarot.Daily.PostAsync(
    new() { Date = new Date(2026, 4, 22) },
    c => c.QueryParameters.Lang = "es");
```

Supported: astrology, Vedic astrology, numerology, tarot, biorhythm, I Ching, crystals, angel numbers. English-only: dreams, location. Untranslated fields fall back to English. Call `roxy.Languages.GetAsync()` for the live list.

## Error handling

Every endpoint throws a typed `RoxyError` (which extends `ApiException`) on a 4xx or 5xx response. The message is human-readable; switch on `Code` for programmatic handling.

```csharp
using RoxyApi.Models;

try
{
    var horoscope = await roxy.Astrology.Horoscope["aries"].Daily.GetAsync();
    Console.WriteLine(horoscope!.Overview);
}
catch (RoxyError e)
{
    // e.Code is stable; e.Message may change wording. e.ResponseStatusCode is the HTTP status.
    // On a 400, e.Issues lists every field that failed validation.
    Console.WriteLine($"{e.ResponseStatusCode} {e.Code}: {e.Message}");
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

## Frequently asked questions

### How do I discover the fields on a response?

Every response is a fully typed object, so your IDE autocompletes every field. There is no untyped dictionary to guess against. The complete response JSON for every endpoint, with real production data, is on the live [API reference playground](https://roxyapi.com/api-reference). You rarely write a response type name yourself: capture results with `var` and let IntelliSense show the shape (the generated type names such as `SearchGetResponse_cities` are internal plumbing).

### What does a city lookup return?

`roxy.Location.Search` returns `Cities`, a list where each item has `City` (the name), `Country`, `Province`, `Latitude`, `Longitude`, `Timezone` (IANA string), `UtcOffset` (decimal), and `Population`.

```csharp
var search = await roxy.Location.Search.GetAsync(c => c.QueryParameters.Q = "Mumbai, India");
var city = search!.Cities![0];
Console.WriteLine($"{city.City}, {city.Country} at {city.Latitude}, {city.Longitude} ({city.Timezone})");
```

### What does a natal chart return?

`Planets`, `Houses`, `Aspects`, `Ascendant`, `Midheaven`, `PartOfFortune`, `Patterns`, and a `Summary`. Each planet carries `Name`, `Sign`, `Degree`, `House`, `IsRetrograde`, and `Speed`. The `Planets` list covers the full set used in modern astrology (the ten classical bodies plus the lunar nodes and key points), so expect more than ten entries.

```csharp
var chart = await roxy.Astrology.NatalChart.PostAsync(new()
{
    Date = new Date(1990, 1, 15), Time = "14:30:00",
    Latitude = 28.6139, Longitude = 77.209, Timezone = new() { Double = 5.5 },
});

foreach (var planet in chart!.Planets!)
    Console.WriteLine($"{planet.Name}: {planet.Sign} {planet.Degree:F2} (house {planet.House})");
```

### Do calls return an error object or throw?

They throw. On success a call returns the typed response (a nullable reference, so use `!` or a null check after you have handled errors); on a 4xx or 5xx it throws `RoxyError`. Wrap calls in `try`/`catch (RoxyError e)` and switch on `e.Code`.

### Which .NET versions are supported?

.NET 8 and later, plus anything that consumes `netstandard2.0`: .NET Framework 4.6.1+, Unity, Xamarin, MAUI, and Mono. Language version 12 or later is recommended for the `new()` and collection-expression syntax in these examples.

## Requirements and dependencies

- **.NET 8 or later, or any runtime supporting `netstandard2.0`** (.NET Framework 4.6.1+, Unity, Mono). Language version 12 or later is recommended for the collection-expression and target-typed `new()` syntax shown above.
- **[`Microsoft.Kiota.Bundle`](https://www.nuget.org/packages/Microsoft.Kiota.Bundle)** is the only runtime dependency. It pulls in the Kiota HTTP, authentication, and serialization libraries (all MIT licensed, maintained by Microsoft) that power the generated client.

The typed client is generated from the public OpenAPI specification with [Kiota](https://learn.microsoft.com/openapi/kiota/), so the SDK stays in lockstep with the API and new endpoints arrive automatically.

## Links

- [Documentation](https://roxyapi.com/docs)
- [API Reference](https://roxyapi.com/api-reference)
- [Pricing](https://roxyapi.com/pricing)
- [MCP setup for AI agents](https://roxyapi.com/docs/mcp)
- [Templates](https://roxyapi.com/starters)
- [TypeScript SDK](https://www.npmjs.com/package/@roxyapi/sdk) | [Python SDK](https://pypi.org/project/roxy-sdk/) | [PHP SDK](https://packagist.org/packages/roxyapi/sdk)
- [Issues](https://github.com/RoxyAPI/sdk-dotnet/issues)

## License

MIT
