using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Http.HttpClientLibrary;
using Microsoft.Kiota.Http.HttpClientLibrary.Middleware.Options;

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
    public RoxyClient(string apiKey) : this(BuildRequestAdapter(apiKey, KiotaClientFactory.GetDefaultHttpMessageHandler())) { }

    // The test seam: the same pipeline over a stubbed transport, so the headers, the base
    // URL, the error mapping and the retry ceiling are asserted without the network.
    internal RoxyClient(string apiKey, HttpMessageHandler transport) : this(BuildRequestAdapter(apiKey, transport)) { }

    /// <summary>
    /// The default retry handler waits <c>Retry-After</c> (capped at 180 seconds) up to three times
    /// before surfacing a 429, 503 or 504. Its own backoff without the header sums to 21 seconds
    /// (3, 6, 12), which this ceiling keeps; a longer server-stated wait, such as a monthly quota
    /// that resets in days, surfaces as <c>RoxyError</c> at once instead of blocking the caller.
    /// </summary>
    private static readonly TimeSpan RetryCeiling = TimeSpan.FromSeconds(30);

    private static IRequestAdapter BuildRequestAdapter(string apiKey, HttpMessageHandler transport)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentException(
                "A RoxyAPI key is required. Create one at https://roxyapi.com/pricing.", nameof(apiKey));
        }

        // Kiota's default middleware: retry (429, 503, 504, honouring Retry-After), redirect,
        // parameter name decoding, user agent and the two inspection handlers, over a transport
        // with automatic decompression. The SDK identification header goes last; the API key is
        // added by the authentication provider, which the request adapter runs first.
        var handlers = KiotaClientFactory.CreateDefaultHandlers([new RetryHandlerOption { RetriesTimeLimit = RetryCeiling }]);
        handlers.Add(new SdkClientHandler());
        var pipeline = KiotaClientFactory.ChainHandlersCollectionAndGetFirstLink(transport, handlers.ToArray());
        var httpClient = new HttpClient(pipeline!);

        return new HttpClientRequestAdapter(new RoxyApiKeyAuthenticationProvider(apiKey), httpClient: httpClient);
    }
}
