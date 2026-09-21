[![RoxyAPI .NET SDK, ship in an afternoon. The Spiritual OS layer for agentic AI. One key, flat pricing.](https://raw.githubusercontent.com/RoxyAPI/sdk-dotnet/main/assets/hero.png)](https://roxyapi.com)

# RoxyApi

[![NuGet](https://img.shields.io/nuget/v/RoxyApi.Sdk)](https://www.nuget.org/packages/RoxyApi.Sdk)
[![Docs](https://img.shields.io/badge/docs-roxyapi.com-blue)](https://roxyapi.com/docs/sdk)
[![API Reference](https://img.shields.io/badge/api%20reference-roxyapi.com-blue)](https://roxyapi.com/api-reference)
[![Pricing](https://img.shields.io/badge/pricing-roxyapi.com-blue)](https://roxyapi.com/pricing)

The .NET SDK for astrology, Vedic astrology, numerology, tarot, and more.

One API key. Fully typed. Verified against NASA JPL Horizons.

The fastest way to add natal charts, daily horoscopes, synastry, Vedic kundli, tarot spreads, numerology, human design bodygraphs, and transit forecasts to ASP.NET Core, Blazor, MAUI, Unity, and AI agents. 18+ domains behind a single [Roxy](https://roxyapi.com) subscription, interpretations in 10+ languages, generated from the OpenAPI spec so new endpoints appear the day they ship.

## Install

```bash
dotnet add package RoxyApi.Sdk
```

Or add it to your project file:

```xml
<PackageReference Include="RoxyApi.Sdk" Version="*" />
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
using Microsoft.Kiota.Abstractions; // the Date and Time structs

var roxy = new RoxyClient(Environment.GetEnvironmentVariable("ROXY_API_KEY")!);

// Step 1: geocode the birth city once. Every chart endpoint takes these three values.
var place = await roxy.Location.Search.GetAsync(c => c.QueryParameters.Q = "London");
var city = place!.Cities![0];

// Step 2: a Western natal chart. city.Timezone is the IANA string from the lookup
// ("Europe/London"); the server resolves it to the DST-correct offset for the
// date of the chart.
var chart = await roxy.Astrology.NatalChart.PostAsync(new()
{
    Date = new Date(1990, 1, 15),
    Time = new Time(14, 30, 0),
    Latitude = city.Latitude,
    Longitude = city.Longitude,
    Timezone = new() { String = city.Timezone },
});

// Step 3: the same birth as a Vedic kundli. Same inputs, sidereal zodiac.
var kundli = await roxy.VedicAstrology.BirthChart.PostAsync(new()
{
    Date = new Date(1990, 1, 15),
    Time = new Time(14, 30, 0),
    Latitude = city.Latitude,
    Longitude = city.Longitude,
    Timezone = new() { String = city.Timezone },
});
```

`new RoxyClient(apiKey)` sets the base URL (`https://roxyapi.com/api/v2`) and injects the auth header and SDK identification header on every request. Every call returns the typed response and throws `RoxyError` on an error response (see Error handling).

## Three things to know

These are the only .NET-specific shapes worth learning. The rest is plain typed objects.

- **Dates and times use typed structs.** `Date = new Date(1990, 1, 15)` and `Time = new Time(14, 30, 0)`, both from `Microsoft.Kiota.Abstractions`.
- **`Timezone` is a typed union.** Pass a decimal offset with `new() { Double = -5 }` or an IANA name with `new() { String = "America/New_York" }`. The server resolves an IANA name to the DST-correct offset for the request date.
- **Query parameters use a configuration lambda.** `await roxy.Crystals.Search.GetAsync(c => c.QueryParameters.Q = "amethyst");`

## Domains

Type `roxy.` to see every domain. Type `roxy.Astrology.` to see every endpoint in that domain.

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

## Most-used endpoints

The highest-demand endpoints by domain, in the order you are most likely to ship them. Every example below reads the same birth through a different domain, and every coordinate comes from one location lookup at the top: one API key, one lookup, and eighteen domains that compose into a single product instead of eighteen separate ones. Full catalog in the [API reference](https://roxyapi.com/api-reference).

### Location first: one lookup feeds every chart

Every chart, horoscope, panchang, dasha, dosha, synastry and compatibility endpoint needs `Latitude`, `Longitude` and `Timezone`. Never ask users to type coordinates. Look the city up once and reuse the result in every domain below. Request bodies are distinct generated types, so the values are captured once as locals and assigned into each body.

```csharp
// One lookup feeds every chart below. timezone is the IANA name from the city
// record; the server resolves it to the DST-correct offset for the date of each chart.
var place = await roxy.Location.Search.GetAsync(c => c.QueryParameters.Q = "New York");
var city = place!.Cities![0];
var latitude = city.Latitude;
var longitude = city.Longitude;
var timezone = city.Timezone;
// The birth instant every chart below reads, beside the coordinates from the lookup.
var birthDate = new Date(1990, 1, 15);
var birthTime = new Time(14, 30, 0);

// A second person for the two-chart calls (synastry, Guna Milan, Human Design connection).
var london = await roxy.Location.Search.GetAsync(c => c.QueryParameters.Q = "London");
var partnerCity = london!.Cities![0];
var lat2 = partnerCity.Latitude;
var lon2 = partnerCity.Longitude;
var tz2 = partnerCity.Timezone;
var partnerDate = new Date(1992, 7, 22);
var partnerTime = new Time(9, 0, 0);
```

### 1. Western astrology API (natal chart, daily horoscope, synastry)

Natal chart products, daily horoscope features, dating and compatibility apps, and lunar-cycle wellness apps start here.

```csharp
// Natal chart. The most requested Western call, run once at onboarding.
// The latitude, longitude and timezone come from the location lookup above.
var natal = await roxy.Astrology.NatalChart.PostAsync(new()
{
    Date = birthDate, Time = birthTime,
    Latitude = latitude, Longitude = longitude, Timezone = new() { String = timezone },
});
// natal.Planets[n].Name, .Sign, .House, .Interpretation.Summary; natal.Ascendant.Sign; natal.Aspects

// Daily horoscope. The highest per-user call frequency in the catalog: daily content, streaks, push.
var horoscope = await roxy.Astrology.Horoscope["aries"].Daily.GetAsync();
// horoscope.Overview, horoscope.Love, horoscope.Career, horoscope.Column, horoscope.Events, horoscope.LuckyNumber

// Synastry. Full inter-aspect analysis between two charts, the relationship feature of dating apps.
// Both people come from the lookups above.
var synastry = await roxy.Astrology.Synastry.PostAsync(new()
{
    Person1 = new() { Date = birthDate, Time = birthTime, Latitude = latitude, Longitude = longitude, Timezone = new() { String = timezone } },
    Person2 = new() { Date = partnerDate, Time = partnerTime, Latitude = lat2, Longitude = lon2, Timezone = new() { String = tz2 } },
});
// synastry.CompatibilityScore, synastry.InterAspects, synastry.Analysis.Strengths

// Moon phase. A zero-setup GET for wellness, cycle-tracking and meditation apps.
var moon = await roxy.Astrology.MoonPhase.Current.GetAsync();
// moon.Phase, moon.Illumination, moon.Sign, moon.Meaning.Description
```

### 2. Vedic astrology API (kundli, panchang, dasha, Guna Milan, KP)

Kundli generators, matrimonial matching, muhurta and panchang apps, and KP practitioners. The same birth, read sidereally.

```csharp
// Vedic kundli. The same birth read sidereally, from the same location lookup above.
var kundli = await roxy.VedicAstrology.BirthChart.PostAsync(new()
{
    Date = birthDate, Time = birthTime,
    Latitude = latitude, Longitude = longitude, Timezone = new() { String = timezone },
});
// kundli.Meta.AdditionalData["Moon"] (every planet keyed by name: rashi, nakshatra, longitude), kundli.Houses, kundli.Yogas, kundli.Combustion

// Detailed panchang. Tithi, nakshatra, yoga, karana, rahu kaal and the muhurtas for a date at the place looked up above.
var panchang = await roxy.VedicAstrology.Panchang.Detailed.PostAsync(new()
{
    Date = new Date(2026, 10, 1), Latitude = latitude, Longitude = longitude, Timezone = new() { String = timezone },
});
// panchang.Tithi, panchang.Nakshatra, panchang.RahuKaal, panchang.AbhijitMuhurta

// Vimshottari dasha. The mahadasha, antardasha and pratyantardasha running right now.
var dasha = await roxy.VedicAstrology.Dasha.Current.PostAsync(new()
{
    Date = birthDate, Time = birthTime,
    Latitude = latitude, Longitude = longitude, Timezone = new() { String = timezone },
});
// dasha.Mahadasha, dasha.Antardasha, dasha.RemainingInMahadasha

// Mangal Dosha. The most asked matrimonial check.
var dosha = await roxy.VedicAstrology.Dosha.Manglik.PostAsync(new()
{
    Date = birthDate, Time = birthTime,
    Latitude = latitude, Longitude = longitude, Timezone = new() { String = timezone },
});
// dosha.Present; dosha.Severity and dosha.Remedies are set only when Present is true

// Guna Milan. The 36-point Ashtakoota score behind kundli matching, both people from the lookups above.
var milan = await roxy.VedicAstrology.Compatibility.PostAsync(new()
{
    Person1 = new() { Date = birthDate, Time = birthTime, Latitude = latitude, Longitude = longitude, Timezone = new() { String = timezone } },
    Person2 = new() { Date = partnerDate, Time = partnerTime, Latitude = lat2, Longitude = lon2, Timezone = new() { String = tz2 } },
});
// milan.Total, milan.Percentage, milan.IsCompatible, milan.Breakdown

// KP ruling planets. Horary answers at the moment of the question, for the place looked up above.
var kp = await roxy.VedicAstrology.Kp.RulingPlanets.PostAsync(new()
{
    Latitude = latitude, Longitude = longitude, Timezone = new() { String = timezone },
});
// kp.DayLord, kp.MoonSublord, kp.RulingPlanets
```

### 3. Astrology forecast API (transit forecast, cross-domain timeline)

Forecast feeds, transit alerts and timing tools. One call returns a dated, significance-scored event list; the timeline variant merges Vedic dasha boundaries and biorhythm critical days into the same list, which no single-domain API can do.

```csharp
// Transit forecast. Transit-to-natal aspects, sign ingresses and retrograde stations over a window.
// BirthData is the same birth: date, time, latitude, longitude and timezone from the lookup above.
var transits = await roxy.Forecast.Transits.PostAsync(new()
{
    BirthData = new() { Date = birthDate, Time = birthTime, Latitude = latitude, Longitude = longitude, Timezone = new() { String = timezone } },
    StartDate = new Date(2026, 10, 1), EndDate = new Date(2026, 10, 31),
});
// transits.Count, transits.Events[n].Date, .Type, .Body, .Target, .Aspect, .Significance

// Cross-domain timeline. The same window with Vedic dasha boundaries and biorhythm critical days merged in.
var timeline = await roxy.Forecast.Timeline.PostAsync(new()
{
    BirthData = new() { Date = birthDate, Time = birthTime, Latitude = latitude, Longitude = longitude, Timezone = new() { String = timezone } },
    StartDate = new Date(2026, 10, 1), EndDate = new Date(2026, 10, 31),
});
// timeline.Events[n].Domain (an enum: Western, Vedic, Biorhythm), .Description, .Significance
```

### 4. Human Design API (bodygraph, connection)

Self-discovery apps, coaching bots and compatibility products. The full bodygraph is one call, and the Design side is solved on the exact 88-degree solar arc rather than approximated as calendar days.

```csharp
// Bodygraph. Type, strategy, authority, profile, definition, centers, channels and all 26 gates in one call.
// Human Design needs only the birth instant, so it takes the date, time and timezone from the lookup above.
var hd = await roxy.HumanDesign.Bodygraph.PostAsync(new()
{
    Date = birthDate, Time = birthTime, Timezone = new() { String = timezone },
});
// hd.Type, hd.Strategy, hd.Authority, hd.Profile, hd.Definition, hd.IncarnationCross.Name, hd.Centers, hd.Channels, hd.Gates

// Connection. Two bodygraphs combined, each of the 36 channels classified by how the pair forms it.
var connection = await roxy.HumanDesign.Connection.PostAsync(new()
{
    PersonA = new() { Date = birthDate, Time = birthTime, Timezone = new() { String = timezone } },
    PersonB = new() { Date = partnerDate, Time = partnerTime, Timezone = new() { String = tz2 } },
});
// connection.TotalChannels, connection.Summary.Electromagnetic, connection.CombinedDefinition
```

### 5. Chinese zodiac API (BaZi four pillars, zodiac animal, almanac)

BaZi readings, zodiac content and Tong Shu date pages. The school splits that make two calculators disagree (`DayBoundary`, `YearBoundary`, `HourClock`) are typed request parameters with named defaults.

```csharp
// BaZi Four Pillars. The anchor call of the domain, from the same birth instant as every chart above.
// Each response echoes the Conventions it was computed under, so a chart can be reproduced, not guessed.
var bazi = await roxy.ChineseAstrology.Bazi.Chart.PostAsync(new()
{
    Date = birthDate, Time = birthTime, Timezone = new() { String = timezone },
});
// bazi.Pillars[n].Position ("year" | "month" | "day" | "hour"), .Stem.Element, .Branch.Animal, .TenGod.Name
// bazi.DayMaster.Element, bazi.ZodiacAnimal, bazi.FiveElements, bazi.Conventions

// Chinese zodiac animal. Defaults YearBoundary to the Lunar New Year, the folk rule people mean
// when they ask which animal they are. Pass LiChun for the classical BaZi boundary.
var animal = await roxy.ChineseAstrology.Zodiac.Sign.PostAsync(new() { Date = birthDate });
// animal.Animal.Name, animal.Animal.Element, animal.Element (the year stem element), animal.Interpretation

// Almanac day. The Tong Shu view of a date: day officer, mansion, clash animal, favours and avoids.
var almanac = await roxy.ChineseAstrology.Calendar.Day[new Date(2026, 10, 1)].GetAsync();
// almanac.DayPillar, almanac.DayOfficer, almanac.ClashAnimal, almanac.Favours, almanac.Avoids
```

### 6. Feng shui API (Kua number, flying star chart)

Kua numbers with the Eight Mansions map, Xuan Kong flying star charts for any of the nine periods and 24 mountains, annual and monthly star plates, and the annual afflictions.

```csharp
// Kua number. One birth date and a gender give the personal directions everything else reads off.
// The enums live with their request bodies: using RoxyApi.FengShui.Kua; and using RoxyApi.FengShui.FlyingStars.Natal;
var kua = await roxy.FengShui.Kua.PostAsync(new() { Date = birthDate, Gender = KuaPostRequestBody_gender.Female });
// kua.Kua, kua.Group ("east" | "west"), kua.Trigram.English, kua.Sectors[n].Direction, .Nature, .Rank

// Flying star natal chart. Period plus facing gives the nine palaces with base, mountain and water stars.
// Send Facing (a mountain id like Bing or a compass label like S2) or FacingDegrees, not neither.
var stars = await roxy.FengShui.FlyingStars.Natal.PostAsync(new() { Period = 9, Facing = NatalPostRequestBody_facing.S2 });
// stars.Facing.Label, stars.Sitting.Label, stars.Structure.Name, stars.Palaces[n].Palace, .Base, .Mountain, .Water, .Reading
```

### 7. Mayan astrology API (Tzolkin day sign, full Maya chart)

Maya day signs, the Haab and Long Count, and the Aztec tonalpohualli, every value a function of the date under a typed `Correlation` convention echoed back in `Conventions`.

```csharp
// Tzolkin day sign. The most asked Maya question, answered from a date alone.
var tzolkin = await roxy.MesoamericanAstrology.Mayan.Tzolkin.PostAsync(new() { Date = birthDate });
// tzolkin.DaySign, tzolkin.DaySignName, tzolkin.Number, tzolkin.Trecena, tzolkin.Reading

// Full Maya chart. Tzolkin, Haab, Long Count, Calendar Round, Lord of the Night, Year Bearer and the Cruz Maya.
var maya = await roxy.MesoamericanAstrology.Mayan.Chart.PostAsync(new() { Date = birthDate });
// maya.Tzolkin, maya.Haab, maya.LongCount, maya.CalendarRound, maya.YearBearer, maya.Cross, maya.Conventions.Correlation
```

### 8. Vastu Shastra API (entrance analysis, room compliance)

Home and plot analysis from typed geometry. Every verdict carries a `Source` object naming the text, chapter and verse it rests on, or a convention label where the texts are silent.

```csharp
// Entrance analysis. Plot, facing and door in; the pada, its devata, the classical effect and the recommended padas out.
// The enums live with their request bodies: using RoxyApi.Vastu.Entrance; and using RoxyApi.Vastu.Rooms;
var entrance = await roxy.Vastu.Entrance.PostAsync(new()
{
    Plot = new() { Width = 30, Depth = 40, Unit = EntrancePostRequestBody_plot_unit.Feet },
    Facing = EntrancePostRequestBody_facing.North, DoorPosition = 0.4,
});
// entrance.Pada, entrance.Devata, entrance.Effect, entrance.Auspiciousness, entrance.RecommendedPadas, entrance.Source

// Room compliance. A verdict per room with the verse or the convention it rests on, and a scored composite.
var rooms = await roxy.Vastu.Rooms.PostAsync(new()
{
    Plot = new() { Width = 30, Depth = 40, Unit = RoomsPostRequestBody_plot_unit.Feet },
    Facing = RoomsPostRequestBody_facing.North,
    Rooms =
    [
        new() { Type = RoomsPostRequestBody_rooms_type.Kitchen, Direction = RoomsPostRequestBody_rooms_direction.Southeast },
        new() { Type = RoomsPostRequestBody_rooms_type.MasterBedroom, Direction = RoomsPostRequestBody_rooms_direction.Southwest },
        new() { Type = RoomsPostRequestBody_rooms_type.Puja, Direction = RoomsPostRequestBody_rooms_direction.Northeast },
    ],
});
// rooms.Score, rooms.Rooms[n].Type, .Verdict, .IdealDirections, .Source
```

### 9. Numerology API (life path, full chart, personal year)

Works from the birth date and name alone, no coordinates, which makes it the easiest domain to integrate.

```csharp
// Life Path. The most searched numerology number, from the birth date alone.
var lifePath = await roxy.Numerology.LifePath.PostAsync(new() { Year = 1990, Month = 1, Day = 15 });
// lifePath.Number, lifePath.Type (an enum: Single, Master), lifePath.Meaning

// Full numerology chart. All six core numbers plus karmic lessons, pinnacles and the personal year in one call.
var numerology = await roxy.Numerology.Chart.PostAsync(new() { FullName = "Jane Smith", Year = 1990, Month = 1, Day = 15 });
// numerology.CoreNumbers.LifePath, .Expression, .SoulUrge, numerology.AdditionalInsights.PersonalYear

// Personal Year. The annual theme, the January feature of every numerology app.
var personalYear = await roxy.Numerology.PersonalYear.PostAsync(new() { Month = 1, Day = 15, Year = 2026 });
// personalYear.PersonalYear, personalYear.Theme, personalYear.Advice
```

### 10. Kabbalah API (gematria, birth profile)

Gematria of a Latin name under a declared transliteration convention, the 72 names, the Tree of Life, and a Hebrew birthday computed from the same birth instant as every chart above.

```csharp
// Gematria. A Latin name transliterated under a declared convention, ten ciphers, each with its tradition and source.
var gematria = await roxy.Kabbalah.Gematria.PostAsync(new() { Text = "Sarah" });
// gematria.Chosen.Hebrew, gematria.Values[n].Id, .Name, .Value, .Tradition; gematria.Matches, gematria.Conventions

// Birth profile. The Hebrew date and birthday, the three birth angels and the birth sephirah from the instant above.
var kabbalah = await roxy.Kabbalah.BirthProfile.PostAsync(new()
{
    Date = birthDate, Time = birthTime, Timezone = new() { String = timezone },
});
// kabbalah.HebrewDate, kabbalah.HebrewBirthday, kabbalah.Angels, kabbalah.Sephirah
```

### 11. Tarot API (daily card, three-card, Celtic Cross, yes or no)

The complete 78-card deck with meanings for love, career, health and spirit. Pass a `Seed` per user for deterministic once-per-day draws.

```csharp
// Daily card. Deterministic per (seed, date), so one user sees one card per day.
var card = await roxy.Tarot.Daily.PostAsync(new() { Seed = "user-42" });
// card.Card.Name, card.Card.Reversed, card.Card.ImageUrl, card.DailyMessage

// Three-card spread. Past, present, future: the most drawn spread on every tarot platform.
var three = await roxy.Tarot.Spreads.ThreeCard.PostAsync(new() { Question = "My next quarter", Seed = "user-42" });
// three.Positions[n].Name, .Card.Name, .Interpretation; three.Summary

// Celtic Cross. The ten-position professional reading.
var celtic = await roxy.Tarot.Spreads.CelticCross.PostAsync(new() { Question = "What should I focus on?", Seed = "user-42" });
// celtic.Positions[n].Name, .Card.Name, .Interpretation; celtic.Summary

// Yes or no. One card, one answer, with its strength.
var answer = await roxy.Tarot.YesNo.PostAsync(new() { Question = "Should I take the offer?" });
// answer.Answer (an enum: Yes, No, Maybe), answer.Strength, answer.Card.Name
```

### 12. Biorhythm API (reading, forecast)

Ten cycle types across primary, secondary and extended cycles, for wellness, productivity, sports and couples apps.

```csharp
// Biorhythm reading. All ten cycles for a date, from the same birth date as every chart above.
var bio = await roxy.Biorhythm.Reading.PostAsync(new() { BirthDate = birthDate, TargetDate = new Date(2026, 10, 1) });
// bio.Cycles.AdditionalData["physical"] (every cycle keyed by name: value, rawValue, phase); bio.EnergyRating, bio.OverallPhase, bio.CriticalAlerts, bio.Interpretation

// Forecast. Every cycle for every day of a window, with the best and worst days named.
var bioForecast = await roxy.Biorhythm.Forecast.PostAsync(new()
{
    BirthDate = birthDate, StartDate = new Date(2026, 10, 1), EndDate = new Date(2026, 10, 31),
});
// bioForecast.Summary.BestDay, .WorstDay, .AverageEnergy; bioForecast.Days[n].Date, .Physical, .Emotional, .Intellectual, .IsCritical
```

### 13. Ayurveda API (dosha constitution, dinacharya)

The dosha profile read from a verified sidereal chart with the verse on each factor, a daily routine anchored on the local sunrise, and the six seasons from real solar ingresses. Every response carries `Meta.Disclaimer`.

```csharp
// Constitution. The dosha profile read from the sidereal chart of the same birth, each factor with its verse.
var constitution = await roxy.Ayurveda.Constitution.PostAsync(new()
{
    Date = birthDate, Time = birthTime,
    Latitude = latitude, Longitude = longitude, Timezone = new() { String = timezone },
});
// constitution.Composite.Dominant, .Type; constitution.Factors[n].Id, .Input, .Doshas, .Source; constitution.Meta.Disclaimer

// Dinacharya. Brahma muhurta, the dosha periods and the routine for a date at the place looked up above.
var dinacharya = await roxy.Ayurveda.Dinacharya.PostAsync(new()
{
    Date = new Date(2026, 10, 1), Latitude = latitude, Longitude = longitude, Timezone = new() { String = timezone },
});
// dinacharya.BrahmaMuhurta, dinacharya.DoshaPeriods, dinacharya.Routine
```

### 14. I Ching API (cast a reading, hexagram catalog)

All 64 hexagrams, 384 changing lines and 8 trigrams, for meditation apps, decision tools and wisdom chatbots.

```csharp
// Cast a reading. Three coins six times: the primary hexagram, the changing lines and the resulting hexagram.
var reading = await roxy.Iching.Cast.GetAsync(c => c.QueryParameters.Seed = "user-42");
// reading.Hexagram.Number, reading.Hexagram.English, reading.Lines, reading.ChangingLinePositions, reading.ResultingHexagram

// Hexagram catalog. Paginated, 20 per page by default; ask for all 64 once and cache them.
var hexagrams = await roxy.Iching.Hexagrams.GetAsync(c => c.QueryParameters.Limit = 64);
// hexagrams.Total, hexagrams.Hexagrams[n].Number, .English, .Pinyin; fetch roxy.Iching.Hexagrams[number].GetAsync() for the judgment and lines
```

### 15. Crystal healing API (by zodiac, by chakra, birthstone)

Crystal retail and metaphysical content: "crystals for [sign]" and "[chakra] chakra stones" pages, plus the birthstone for each month.

```csharp
// By zodiac. The most searched crystal query pattern.
var bySign = await roxy.Crystals.Zodiac["scorpio"].GetAsync();
// bySign.Crystals[n].Id, .Name, .ImageUrl, .Colors; fetch roxy.Crystals[id].GetAsync() for full properties

// By chakra. Wellness and yoga content pages.
var byChakra = await roxy.Crystals.Chakra["Heart"].GetAsync();
// byChakra.Crystals[n].Name, .Colors

// Birthstone. Evergreen gift and jewelry pages.
var birthstone = await roxy.Crystals.Birthstone[1].GetAsync();
```

### 16. Dream interpretation API (symbol dictionary, search)

A 2,000+ symbol dream dictionary for journal apps, AI companions and self-discovery products.

```csharp
// Symbol detail. Every "what does it mean to dream about X" page lands here.
var symbol = await roxy.Dreams.Symbols["flying"].GetAsync();
// symbol.Id, symbol.Name, symbol.Meaning

// Symbol search. Chatbots fetch the dictionary once and keep it locally.
var symbols = await roxy.Dreams.Symbols.GetAsync(c => c.QueryParameters.Q = "water");
// symbols.Symbols[n].Id, .Name
```

### 17. Angel numbers API (1111, 222, 333 meanings plus universal lookup)

Meanings for every common sequence, and a lookup that answers any positive integer through its digit root.

```csharp
// By number. Every "meaning of 1111" page is backed by this. The path param is a string.
var angel = await roxy.AngelNumbers.Numbers["1111"].GetAsync();
// angel.Title, angel.CoreMessage, angel.Meaning.Spiritual, angel.Meaning.Love, angel.Affirmation

// Universal lookup. Any positive integer, with the digit root carrying the answer when no curated entry exists.
var sequence = await roxy.AngelNumbers.Lookup.GetAsync(c => c.QueryParameters.Number = "4242");
// sequence.DigitRoot, sequence.IsRepeating, sequence.KnownMeaning (null when not curated), sequence.DigitRootMeaning.Title
```

## Built for AI agents (Cursor, Claude Code, Copilot, Codex, Gemini CLI)

![Your coding agent already knows the API. Built for AI agents, Remote MCP, no local setup.](https://raw.githubusercontent.com/RoxyAPI/sdk-dotnet/main/assets/agents.png)

This package ships documentation that AI coding agents read directly from the restored NuGet package:

- `AGENTS.md` for quick start, patterns, gotchas, and a common-tasks reference
- `docs/llms-full.txt` for the complete method reference with one example per endpoint

Agents that support `AGENTS.md` (Claude Code, Cursor, GitHub Copilot, OpenAI Codex, Gemini CLI) pick it up automatically. For other tools, point your agent at the restored package under `~/.nuget/packages/roxyapi.sdk/<version>/`.

Prefer MCP? Every domain has a [Remote MCP server](https://roxyapi.com/docs/mcp) at `https://roxyapi.com/mcp/{domain}` (Streamable HTTP, no stdio, no self-hosting). One-line Claude Code setup:

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

Interpretations and editorial text are available in 10 languages: English (`en`), Turkish (`tr`), German (`de`), Spanish (`es`), French (`fr`), Hindi (`hi`), Portuguese (`pt`), Russian (`ru`), Chinese Simplified (`zh-Hans`), Chinese Traditional (`zh-Hant`). Pass `Lang` on any supported endpoint through the query configuration:

```csharp
var card = await roxy.Tarot.Daily.PostAsync(
    new() { Date = new Date(2026, 4, 22) },
    c => c.QueryParameters.Lang = "es");
```

Supported: astrology, Vedic astrology, forecast, human design, Chinese astrology, feng shui, Mesoamerican astrology, vastu, numerology, kabbalah, tarot, biorhythm, ayurveda, I Ching, crystals, angel numbers. English-only: dreams, location. The two Chinese scripts (zh-Hans, zh-Hant) currently ship on Chinese astrology and feng shui; every other domain answers those codes in English per field. Untranslated fields fall back to English. Call `roxy.Languages.GetAsync()` for the live list.

## Error handling

Every endpoint throws a typed `RoxyError` (in `RoxyApi.Models`, a subclass of `ApiException`) on every error status it declares. The message is human-readable; switch on `Code` for programmatic handling. A status the endpoint does not declare, such as a 5xx from the edge, throws the base `ApiException` with `ResponseStatusCode` set, and a 503 or 504 still failing after three retries throws an `AggregateException` holding every attempt.

```csharp
try
{
    var daily = await roxy.Astrology.Horoscope["aries"].Daily.GetAsync();
    Console.WriteLine(daily!.Overview);
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
| 401 | `api_key_revoked` | Key was deleted from the account |
| 404 | `not_found` | Resource not found |
| 4xx | `bad_request` and other status-derived codes | A client error the endpoint itself detected, such as a date window whose `endDate` precedes `startDate` |
| 429 | `rate_limit_exceeded` | Monthly quota reached |
| 500 | `internal_error` | Server error |

## Frequently asked questions

### How do I discover the fields on a response?

Every response is a fully typed object, so your IDE autocompletes every field. There is no untyped dictionary to guess against. The complete response JSON for every endpoint, with real production data, is on the live [API reference playground](https://roxyapi.com/api-reference). You rarely write a response type name yourself: capture results with `var` and let IntelliSense show the shape (the generated type names such as `SearchGetResponse_cities` are internal plumbing).

### What does a city lookup return?

`roxy.Location.Search` returns `Cities`, a list where each item has `City` (the name), `Country`, `Province`, `Latitude`, `Longitude`, `Timezone` (IANA string), `UtcOffset` (decimal), and `Population`.

```csharp
var search = await roxy.Location.Search.GetAsync(c => c.QueryParameters.Q = "London, UK");
var city = search!.Cities![0];
Console.WriteLine($"{city.City}, {city.Country} at {city.Latitude}, {city.Longitude} ({city.Timezone})");
```

### What does a natal chart return?

`Planets`, `Houses`, `Aspects`, `Ascendant`, `Midheaven`, `PartOfFortune`, `Patterns`, and a `Summary`. Each planet carries `Name`, `Sign`, `Degree`, `House`, `IsRetrograde`, and `Speed`. The `Planets` list covers the full set used in modern astrology (the ten classical bodies plus the lunar nodes and key points), so expect more than ten entries.

```csharp
var chart = await roxy.Astrology.NatalChart.PostAsync(new()
{
    Date = new Date(1990, 1, 15), Time = new Time(14, 30, 0),
    Latitude = 40.7128, Longitude = -74.006, Timezone = new() { Double = -5 },
});

foreach (var planet in chart!.Planets!)
    Console.WriteLine($"{planet.Name}: {planet.Sign} {planet.Degree:F2} (house {planet.House})");
```

### Do calls return an error object or throw?

They throw. On success a call returns the typed response (a nullable reference, so use `!` or a null check after you have handled errors); on an error status the endpoint declares it throws `RoxyError`. Wrap calls in `try`/`catch (RoxyError e)` and switch on `e.Code`; add `catch (ApiException e)` for a status the endpoint does not declare (see Error handling).

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
- [Templates](https://roxyapi.com/templates)
- [TypeScript SDK](https://www.npmjs.com/package/@roxyapi/sdk) | [Python SDK](https://pypi.org/project/roxy-sdk/) | [PHP SDK](https://packagist.org/packages/roxyapi/sdk) | [Go SDK](https://pkg.go.dev/github.com/RoxyAPI/sdk-go)
- [Issues](https://github.com/RoxyAPI/sdk-dotnet/issues)

## License

MIT
