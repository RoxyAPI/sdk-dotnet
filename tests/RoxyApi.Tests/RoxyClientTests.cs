using Microsoft.Kiota.Abstractions;
using RoxyApi;
using RoxyApi.Models;

namespace RoxyApi.Tests;

// Construction tests run everywhere (no network). The live tests hit the production
// API and are skipped unless ROXY_API_KEY is set, so CI without a key stays green.
public class RoxyClientTests
{
    private static string? Key => Environment.GetEnvironmentVariable("ROXY_API_KEY");

    private static RoxyClient Live()
    {
        Skip.If(string.IsNullOrWhiteSpace(Key), "ROXY_API_KEY not set; skipping live API test.");
        return new RoxyClient(Key!);
    }

    [Fact]
    public void Constructor_rejects_a_blank_key()
    {
        Assert.Throws<ArgumentException>(() => new RoxyClient(""));
        Assert.Throws<ArgumentException>(() => new RoxyClient("   "));
        Assert.Throws<ArgumentException>(() => new RoxyClient((string)null!));
    }

    [Fact]
    public void Constructor_exposes_every_domain_accessor()
    {
        var roxy = new RoxyClient("sk_test_not_a_real_key");
        Assert.NotNull(roxy.Astrology);
        Assert.NotNull(roxy.VedicAstrology);
        Assert.NotNull(roxy.Numerology);
        Assert.NotNull(roxy.Tarot);
        Assert.NotNull(roxy.HumanDesign);
        Assert.NotNull(roxy.Iching);
    }

    [SkippableFact]
    public async Task Lists_zodiac_signs()
    {
        var roxy = Live();
        var signs = await roxy.Astrology.Signs.GetAsync();
        Assert.NotNull(signs);
        Assert.Equal(12, signs!.Count);
        Assert.Contains(signs, s => s.Id == "aries");
    }

    [SkippableFact]
    public async Task Gets_a_daily_horoscope_by_path_parameter()
    {
        var roxy = Live();
        var horoscope = await roxy.Astrology.Horoscope["aries"].Daily.GetAsync();
        Assert.NotNull(horoscope);
        Assert.False(string.IsNullOrWhiteSpace(horoscope!.Date));
    }

    [SkippableFact]
    public async Task Generates_a_natal_chart_with_a_decimal_timezone()
    {
        var roxy = Live();
        var chart = await roxy.Astrology.NatalChart.PostAsync(new NatalChartRequest
        {
            Date = new Date(1990, 1, 15),
            Time = new Time(14, 30, 0),
            Latitude = 28.6139,
            Longitude = 77.209,
            Timezone = new() { Double = 5.5 },
        });
        Assert.NotNull(chart);
        Assert.NotNull(chart!.Planets);
        Assert.NotEmpty(chart.Planets!);
    }

    [SkippableFact]
    public async Task Generates_a_natal_chart_with_an_iana_timezone()
    {
        var roxy = Live();
        var chart = await roxy.Astrology.NatalChart.PostAsync(new NatalChartRequest
        {
            Date = new Date(1990, 1, 15),
            Time = new Time(9, 0, 0),
            Latitude = 40.7128,
            Longitude = -74.006,
            Timezone = new() { String = "America/New_York" },
        });
        Assert.NotNull(chart);
        Assert.NotEmpty(chart!.Planets!);
    }

    [SkippableFact]
    public async Task An_invalid_key_throws_a_typed_RoxyError()
    {
        Skip.If(string.IsNullOrWhiteSpace(Key), "ROXY_API_KEY not set; skipping live API test.");
        var roxy = new RoxyClient("sk_live_definitely_invalid_key");
        var ex = await Assert.ThrowsAsync<RoxyError>(() => roxy.Astrology.Signs.GetAsync());
        Assert.Equal(401, ex.ResponseStatusCode);
        Assert.False(string.IsNullOrWhiteSpace(ex.Code));
        Assert.False(string.IsNullOrWhiteSpace(ex.Message));
    }
}
