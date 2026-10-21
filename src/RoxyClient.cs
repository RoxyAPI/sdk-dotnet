using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Http.HttpClientLibrary;
using Microsoft.Kiota.Http.HttpClientLibrary.Middleware;

namespace RoxyApi;

/// <summary>
/// The RoxyAPI client. The fluent surface (<c>roxy.Astrology</c>, <c>roxy.VedicAstrology</c>, ...)
/// is generated from the OpenAPI specification; this partial adds the one-line convenience
/// constructor most callers want.
/// </summary>
/// <remarks>
/// <para>Get an API key at <see href="https://roxyapi.com/pricing">roxyapi.com/pricing</see>, then:</para>
/// <code>
/// var roxy = new RoxyClient(Environment.GetEnvironmentVariable("ROXY_API_KEY")!);
/// var horoscope = await roxy.Astrology.Horoscope["aries"].Daily.GetAsync();
/// </code>
/// <para>Keep the key server side. Never ship it in a desktop, mobile, or browser client.</para>
/// </remarks>
public partial class RoxyClient
{
    /// <summary>
    /// Creates a client pointed at the RoxyAPI production endpoint with the API key, base URL,
    /// and SDK identification header wired in. This is the constructor to use for almost every app.
    /// </summary>
    /// <param name="apiKey">Your RoxyAPI key. Create one at <see href="https://roxyapi.com/pricing">roxyapi.com/pricing</see>.</param>
    /// <exception cref="System.ArgumentException">Thrown when <paramref name="apiKey"/> is null, empty, or whitespace.</exception>
    public RoxyClient(string apiKey) : this(apiKey, new RoxyRetryOptions()) { }

    /// <summary>
    /// Creates a client like <see cref="RoxyClient(string)"/> with your own retry settings.
    /// </summary>
    /// <param name="apiKey">Your RoxyAPI key. Create one at <see href="https://roxyapi.com/pricing">roxyapi.com/pricing</see>.</param>
    /// <param name="retry">When and how long to retry; <c>MaxRetries = 0</c> turns retries off.</param>
    /// <exception cref="System.ArgumentException">Thrown when <paramref name="apiKey"/> is null, empty, or whitespace.</exception>
    public RoxyClient(string apiKey, RoxyRetryOptions retry) : this(BuildRequestAdapter(apiKey, KiotaClientFactory.GetDefaultHttpMessageHandler(), retry, null, null)) { }

    // The test seam: the same pipeline over a stubbed transport, with the retry randomness and
    // sleep injectable, so the headers, the base URL, the error mapping and the retry schedule
    // are asserted without the network or the wall clock.
    internal RoxyClient(string apiKey, HttpMessageHandler transport, RoxyRetryOptions? retry = null, Func<double>? random = null, Func<TimeSpan, CancellationToken, Task>? sleep = null)
        : this(BuildRequestAdapter(apiKey, transport, retry ?? new RoxyRetryOptions(), random, sleep)) { }

    private static IRequestAdapter BuildRequestAdapter(string apiKey, HttpMessageHandler transport, RoxyRetryOptions retry, Func<double>? random, Func<TimeSpan, CancellationToken, Task>? sleep)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentException(
                "A RoxyAPI key is required. Create one at https://roxyapi.com/pricing.", nameof(apiKey));
        }
        if (retry is null) throw new ArgumentNullException(nameof(retry));
        retry.Validate();

        // Kiota's default middleware (redirect, parameter name decoding, user agent and the two
        // inspection handlers) over a transport with automatic decompression, with RoxyRetryHandler
        // standing where Kiota's own retry handler sits. The SDK identification header goes last;
        // the API key is added by the authentication provider, which the request adapter runs first.
        var handlers = KiotaClientFactory.CreateDefaultHandlers();
        var retryHandler = random is null || sleep is null ? new RoxyRetryHandler(retry) : new RoxyRetryHandler(retry, random, sleep);
        var slot = handlers.ToList().FindIndex(h => h is RetryHandler);
        if (slot < 0) throw new InvalidOperationException("Kiota's default handler chain has no retry handler to replace.");
        handlers[slot] = retryHandler;
        handlers.Add(new SdkClientHandler());
        var pipeline = KiotaClientFactory.ChainHandlersCollectionAndGetFirstLink(transport, handlers.ToArray());
        var httpClient = new HttpClient(pipeline!);
        retryHandler.OverallTimeout = httpClient.Timeout;

        return new HttpClientRequestAdapter(new RoxyApiKeyAuthenticationProvider(apiKey), httpClient: httpClient);
    }
}
