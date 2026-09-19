using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Text;
using Microsoft.Kiota.Abstractions;
using RoxyApi.Models;

namespace RoxyApi.Tests;

// The hand-written client over a stubbed transport: base URL, the two headers, the error
// mapping and the retry ceiling, all without the network. The live tests at the end hit
// production and are skipped unless ROXY_API_KEY is set, so CI without a key stays green.
public class RoxyClientTests
{
    private const string Key = "not-a-real-key";

    private static string SdkVersion =>
        typeof(RoxyClient).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion.Split('+')[0];

    [Fact]
    public void Constructor_rejects_a_blank_key()
    {
        Assert.Throws<ArgumentException>(() => new RoxyClient(""));
        Assert.Throws<ArgumentException>(() => new RoxyClient("   "));
        Assert.Throws<ArgumentException>(() => new RoxyClient((string)null!));
    }

    [Fact]
    public async Task Sends_the_key_and_sdk_headers_to_the_production_base_url()
    {
        var transport = new StubTransport(_ => Json(HttpStatusCode.OK, "[]"));
        var roxy = new RoxyClient(Key, transport);

        await roxy.Astrology.Signs.GetAsync();

        var request = Assert.Single(transport.Requests);
        Assert.Equal("https://roxyapi.com/api/v2/astrology/signs", request.RequestUri!.ToString());
        Assert.Equal(Key, Assert.Single(request.Headers.GetValues("X-API-Key")));
        Assert.Equal($"roxy-sdk-dotnet/{SdkVersion}", Assert.Single(request.Headers.GetValues("X-SDK-Client")));
        Assert.Matches(@"^roxy-sdk-dotnet/\d+\.\d+\.\d+$", Assert.Single(request.Headers.GetValues("X-SDK-Client")));
    }

    [Fact]
    public async Task A_401_surfaces_as_RoxyError_with_the_code_and_message()
    {
        var transport = new StubTransport(_ => Json(HttpStatusCode.Unauthorized, """{"error":"Invalid API key","code":"invalid_api_key"}"""));
        var roxy = new RoxyClient(Key, transport);

        var ex = await Assert.ThrowsAsync<RoxyError>(() => roxy.Astrology.Signs.GetAsync());

        Assert.Equal(401, ex.ResponseStatusCode);
        Assert.Equal("invalid_api_key", ex.Code);
        Assert.Equal("Invalid API key", ex.Message);
        Assert.Single(transport.Requests);
    }

    [Fact]
    public async Task A_400_carries_the_validation_issues()
    {
        var transport = new StubTransport(_ => Json(HttpStatusCode.BadRequest,
            """{"error":"date: Required","code":"validation_error","issues":[{"path":"date","message":"Required","code":"invalid_type","expected":"string"}]}"""));
        var roxy = new RoxyClient(Key, transport);

        var ex = await Assert.ThrowsAsync<RoxyError>(() => roxy.Astrology.NatalChart.PostAsync(new()));

        Assert.Equal("validation_error", ex.Code);
        var issue = Assert.Single(ex.Issues!);
        Assert.Equal("date", issue.Path);
        Assert.Equal("invalid_type", issue.Code);
    }

    [Fact]
    public async Task A_quota_429_with_a_days_long_retry_after_surfaces_at_once()
    {
        var transport = new StubTransport(_ =>
        {
            var res = Json((HttpStatusCode)429, """{"error":"Rate limit exceeded","code":"rate_limit_exceeded"}""");
            res.Headers.Add("Retry-After", "2592000");
            return res;
        });
        var roxy = new RoxyClient(Key, transport);

        var call = Assert.ThrowsAsync<RoxyError>(() => roxy.Astrology.Signs.GetAsync());
        var finished = await Task.WhenAny(call, Task.Delay(TimeSpan.FromSeconds(10)));

        Assert.True(finished == call, "the client kept retrying instead of surfacing the 429");
        Assert.Equal("rate_limit_exceeded", (await call).Code);
        Assert.Single(transport.Requests);
    }

    [Fact]
    public async Task A_503_is_retried_three_times_honouring_retry_after()
    {
        var transport = new StubTransport(_ =>
        {
            var res = Json(HttpStatusCode.ServiceUnavailable, """{"error":"upstream","code":"unavailable"}""");
            res.Headers.Add("Retry-After", "1");
            return res;
        });
        var roxy = new RoxyClient(Key, transport);

        var clock = Stopwatch.StartNew();
        // Exhausted retries surface as one AggregateException holding every attempt; a status
        // the endpoint does not declare is the base ApiException, never a RoxyError.
        var ex = await Assert.ThrowsAsync<AggregateException>(() => roxy.Astrology.Signs.GetAsync());

        Assert.Equal(4, transport.Requests.Count);
        Assert.True(clock.Elapsed >= TimeSpan.FromSeconds(3), $"retries took {clock.Elapsed}, Retry-After was not honoured");
        Assert.Equal(4, ex.InnerExceptions.Count);
        Assert.All(ex.InnerExceptions, inner => Assert.Equal(503, Assert.IsType<ApiException>(inner).ResponseStatusCode));
    }

    private static HttpResponseMessage Json(HttpStatusCode status, string body) =>
        new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private sealed class StubTransport(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            var response = respond(request);
            response.RequestMessage = request;
            return Task.FromResult(response);
        }
    }

    // Live tests: production, skipped without a key.

    private static string? LiveKey => Environment.GetEnvironmentVariable("ROXY_API_KEY");

    private static RoxyClient Live()
    {
        Skip.If(string.IsNullOrWhiteSpace(LiveKey), "ROXY_API_KEY not set; skipping live API test.");
        return new RoxyClient(LiveKey!);
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
            Latitude = 40.7128,
            Longitude = -74.006,
            Timezone = new() { Double = -5 },
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
        Live();
        var roxy = new RoxyClient("definitely-not-a-valid-key");
        var ex = await Assert.ThrowsAsync<RoxyError>(() => roxy.Astrology.Signs.GetAsync());
        Assert.Equal(401, ex.ResponseStatusCode);
        Assert.False(string.IsNullOrWhiteSpace(ex.Code));
        Assert.False(string.IsNullOrWhiteSpace(ex.Message));
    }
}
