using System.Net;
using System.Reflection;
using System.Text;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Serialization;
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
    public async Task A_quota_429_exposes_code_doc_url_and_retry_after()
    {
        var transport = new StubTransport(_ =>
        {
            var res = Json((HttpStatusCode)429, """{"error":"Monthly quota reached","code":"rate_limit_exceeded","doc_url":"https://roxyapi.com/docs/errors#rate_limit_exceeded"}""");
            res.Headers.Add("Retry-After", "86400");
            return res;
        });
        var roxy = new RoxyClient(Key, transport);

        var ex = await Assert.ThrowsAsync<RoxyError>(() => roxy.Astrology.Signs.GetAsync());

        Assert.Equal(429, ex.ResponseStatusCode);
        Assert.Equal("rate_limit_exceeded", ex.Code);
        Assert.Equal("https://roxyapi.com/docs/errors#rate_limit_exceeded", ex.DocUrl);
        Assert.Equal(TimeSpan.FromDays(1), ex.RetryAfter);
        Assert.Single(transport.Requests);
    }

    [Fact]
    public async Task A_500_is_retried_then_surfaces_the_error_without_a_retry_after()
    {
        var transport = new StubTransport(_ => Json(HttpStatusCode.InternalServerError, """{"error":"boom","code":"internal_error"}"""));
        var roxy = new RoxyClient(Key, transport, null, () => 0, new Sleeps().Sleep);

        var ex = await Assert.ThrowsAsync<RoxyError>(() => roxy.Astrology.Signs.GetAsync());

        Assert.Equal("internal_error", ex.Code);
        Assert.Null(ex.RetryAfter);
        Assert.Equal(3, transport.Requests.Count);
    }

    [Fact]
    public async Task A_500_then_a_success_returns_the_success()
    {
        var transport = Scripted(Json(HttpStatusCode.InternalServerError, """{"error":"boom","code":"internal_error"}"""), Json(HttpStatusCode.OK, "[]"));
        var roxy = new RoxyClient(Key, transport, null, () => 0, new Sleeps().Sleep);

        await roxy.Astrology.Signs.GetAsync();

        Assert.Equal(2, transport.Requests.Count);
    }

    [Fact]
    public async Task A_502_is_retried_then_succeeds()
    {
        var transport = Scripted(Json(HttpStatusCode.BadGateway, "bad gateway"), Json(HttpStatusCode.OK, "[]"));
        var sleeps = new Sleeps();
        var roxy = new RoxyClient(Key, transport, null, () => 0.5, sleeps.Sleep);

        await roxy.Astrology.Signs.GetAsync();

        Assert.Equal(2, transport.Requests.Count);
        Assert.Equal([TimeSpan.FromMilliseconds(250)], sleeps.Delays);
    }

    [Fact]
    public async Task A_per_minute_429_with_retry_after_one_retries_once_then_succeeds()
    {
        var limited = Json((HttpStatusCode)429, """{"error":"slow down","code":"rate_limit_per_minute"}""");
        limited.Headers.Add("Retry-After", "1");
        var transport = Scripted(limited, Json(HttpStatusCode.OK, "[]"));
        var sleeps = new Sleeps();
        var roxy = new RoxyClient(Key, transport, null, () => 0.5, sleeps.Sleep);

        await roxy.Astrology.Signs.GetAsync();

        Assert.Equal(2, transport.Requests.Count);
        Assert.Equal([TimeSpan.FromMilliseconds(1250)], sleeps.Delays);
    }

    [Fact]
    public async Task The_default_is_two_retries_then_the_last_error_surfaces()
    {
        var transport = new StubTransport(_ => Json(HttpStatusCode.ServiceUnavailable, """{"error":"busy","code":"compute_saturated"}"""));
        var sleeps = new Sleeps();
        var roxy = new RoxyClient(Key, transport, null, () => 1.0, sleeps.Sleep);

        var ex = await Assert.ThrowsAsync<ApiException>(() => roxy.Astrology.Signs.GetAsync());

        Assert.Equal(503, ex.ResponseStatusCode);
        Assert.Equal(3, transport.Requests.Count);
        Assert.Equal([TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(1000)], sleeps.Delays);
    }

    [Fact]
    public async Task Jitter_stays_inside_zero_and_the_capped_exponential_ceiling()
    {
        var options = new RoxyRetryOptions { MaxRetries = 6, InitialDelay = TimeSpan.FromSeconds(1), MaxDelay = TimeSpan.FromSeconds(5) };
        foreach (var draw in new[] { 0.0, 0.5, 0.999 })
        {
            var transport = new StubTransport(_ => Json(HttpStatusCode.ServiceUnavailable, "{}"));
            var sleeps = new Sleeps();
            var roxy = new RoxyClient(Key, transport, options, () => draw, sleeps.Sleep);

            await Assert.ThrowsAsync<ApiException>(() => roxy.Astrology.Signs.GetAsync());

            Assert.Equal(6, sleeps.Delays.Count);
            for (var attempt = 0; attempt < 6; attempt++)
                Assert.Equal(draw * Math.Min(5000, 1000 * Math.Pow(2, attempt)), sleeps.Delays[attempt].TotalMilliseconds, 3);
        }
    }

    [Fact]
    public async Task A_retry_after_inside_the_max_delay_is_padded_by_jitter_and_capped()
    {
        var options = new RoxyRetryOptions { InitialDelay = TimeSpan.FromSeconds(1), MaxDelay = TimeSpan.FromSeconds(10) };
        var busy = Json(HttpStatusCode.ServiceUnavailable, "{}");
        busy.Headers.Add("Retry-After", "10");
        var transport = Scripted(busy, Json(HttpStatusCode.OK, "[]"));
        var sleeps = new Sleeps();
        var roxy = new RoxyClient(Key, transport, options, () => 0.9, sleeps.Sleep);

        await roxy.Astrology.Signs.GetAsync();

        Assert.Equal([TimeSpan.FromSeconds(10)], sleeps.Delays);
    }

    [Fact]
    public async Task A_retry_after_past_the_max_delay_is_not_retried()
    {
        var transport = new StubTransport(_ =>
        {
            var res = Json((HttpStatusCode)429, """{"error":"Rate limit exceeded","code":"rate_limit_exceeded"}""");
            res.Headers.Add("Retry-After", "31");
            return res;
        });
        var sleeps = new Sleeps();
        var roxy = new RoxyClient(Key, transport, null, () => 0, sleeps.Sleep);

        var ex = await Assert.ThrowsAsync<RoxyError>(() => roxy.Astrology.Signs.GetAsync());

        Assert.Equal(TimeSpan.FromSeconds(31), ex.RetryAfter);
        Assert.Single(transport.Requests);
        Assert.Empty(sleeps.Delays);
    }

    [Fact]
    public async Task A_post_body_is_replayed_on_every_retry()
    {
        var bodies = new List<string>();
        var transport = new StubTransport(request =>
        {
            bodies.Add(request.Content!.ReadAsStringAsync().Result);
            return bodies.Count == 1 ? Json(HttpStatusCode.ServiceUnavailable, "{}") : Json(HttpStatusCode.OK, "{}");
        });
        var roxy = new RoxyClient(Key, transport, null, () => 0, new Sleeps().Sleep);

        await roxy.Astrology.NatalChart.PostAsync(new() { Latitude = 40.7, Longitude = -74 });

        Assert.Equal(2, bodies.Count);
        Assert.NotEmpty(bodies[0]);
        Assert.Equal(bodies[0], bodies[1]);
        Assert.Equal(Key, Assert.Single(transport.Requests[1].Headers.GetValues("X-API-Key")));
    }

    [Fact]
    public async Task A_refused_connection_is_retried_but_a_read_failure_is_not()
    {
        var calls = 0;
        var refused = new StubTransport(_ => calls++ == 0
            ? throw new HttpRequestException("refused", new System.Net.Sockets.SocketException((int)System.Net.Sockets.SocketError.ConnectionRefused))
            : Json(HttpStatusCode.OK, "[]"));
        await new RoxyClient(Key, refused, null, () => 0, new Sleeps().Sleep).Astrology.Signs.GetAsync();
        Assert.Equal(2, refused.Requests.Count);

        var reset = new StubTransport(_ => throw new HttpRequestException("reset", new System.Net.Sockets.SocketException((int)System.Net.Sockets.SocketError.ConnectionReset)));
        await Assert.ThrowsAsync<HttpRequestException>(() => new RoxyClient(Key, reset, null, () => 0, new Sleeps().Sleep).Astrology.Signs.GetAsync());
        Assert.Single(reset.Requests);
    }

    [Fact]
    public async Task Retries_are_off_when_MaxRetries_is_zero()
    {
        var transport = new StubTransport(_ => Json(HttpStatusCode.ServiceUnavailable, """{"error":"busy","code":"compute_saturated"}"""));
        var sleeps = new Sleeps();
        var roxy = new RoxyClient(Key, transport, new RoxyRetryOptions { MaxRetries = 0 }, () => 0, sleeps.Sleep);

        await Assert.ThrowsAsync<ApiException>(() => roxy.Astrology.Signs.GetAsync());

        Assert.Single(transport.Requests);
        Assert.Empty(sleeps.Delays);
    }

    [Fact]
    public async Task A_cancelled_token_stops_the_retry_sleep()
    {
        var transport = new StubTransport(_ => Json(HttpStatusCode.ServiceUnavailable, "{}"));
        using var cts = new CancellationTokenSource();
        var roxy = new RoxyClient(Key, transport, null, () => 1, (_, token) => { cts.Cancel(); token.ThrowIfCancellationRequested(); return Task.CompletedTask; });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => roxy.Astrology.Signs.GetAsync(cancellationToken: cts.Token));

        Assert.Single(transport.Requests);
    }

    [Fact]
    public async Task A_retry_never_sleeps_past_the_call_timeout()
    {
        var transport = new StubTransport(_ => Json(HttpStatusCode.ServiceUnavailable, "{}"));
        var sleeps = new Sleeps();
        var handler = new RoxyRetryHandler(new RoxyRetryOptions(), () => 1.0, sleeps.Sleep) { InnerHandler = transport, OverallTimeout = TimeSpan.FromMilliseconds(1200) };
        using var invoker = new HttpMessageInvoker(handler);

        using var response = await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://roxyapi.com/api/v2/astrology/signs"), default);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal([TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(1000)], sleeps.Delays);
        Assert.Equal(3, transport.Requests.Count);
    }

    [Fact]
    public void Invalid_retry_options_are_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RoxyClient(Key, new RoxyRetryOptions { MaxRetries = -1 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RoxyClient(Key, new RoxyRetryOptions { InitialDelay = TimeSpan.FromSeconds(-1) }));
    }

    private sealed class Sleeps
    {
        public List<TimeSpan> Delays { get; } = [];

        public Task Sleep(TimeSpan delay, CancellationToken _)
        {
            Delays.Add(delay);
            return Task.CompletedTask;
        }
    }

    private static StubTransport Scripted(params HttpResponseMessage[] responses)
    {
        var next = 0;
        return new StubTransport(_ => responses[next++]);
    }

    [Fact]
    public async Task Open_key_maps_unwrap_through_UntypedObject_and_UntypedString()
    {
        var transport = new StubTransport(_ => Json(HttpStatusCode.OK,
            """{"meta":{"Moon":{"rashi":"Taurus","nakshatra":{"name":"Rohini","pada":2,"lord":"Moon"}}}}"""));
        var roxy = new RoxyClient(Key, transport);

        var kundli = await roxy.VedicAstrology.BirthChart.PostAsync(new());

        var moon = ((UntypedObject)kundli!.Meta!.AdditionalData["Moon"]).GetValue();
        Assert.Equal("Taurus", ((UntypedString)moon["rashi"]).GetValue());
        Assert.Equal("Rohini", ((UntypedString)((UntypedObject)moon["nakshatra"]).GetValue()["name"]).GetValue());
    }

    [Fact]
    public async Task Validation_issues_expose_path_message_and_code()
    {
        var transport = new StubTransport(_ => Json(HttpStatusCode.BadRequest,
            """{"error":"time: Required","code":"validation_error","issues":[{"path":"time","message":"Required","code":"invalid_type"}]}"""));
        var roxy = new RoxyClient(Key, transport);

        var ex = await Assert.ThrowsAsync<RoxyError>(() => roxy.Astrology.NatalChart.PostAsync(new()));

        var issue = Assert.Single(ex.Issues ?? []);
        Assert.Equal("time", issue.Path);
        Assert.Equal("Required", issue.Message);
        Assert.Equal(ErrorCodes.ValidationError, ex.Code);
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
