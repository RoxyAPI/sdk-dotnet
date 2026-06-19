using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Http.HttpClientLibrary;

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
    public RoxyClient(string apiKey) : this(BuildRequestAdapter(apiKey)) { }

    private static IRequestAdapter BuildRequestAdapter(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new System.ArgumentException(
                "A RoxyAPI key is required. Create one at https://roxyapi.com/pricing.", nameof(apiKey));
        }

        // Reuse Kiota's default middleware (retry with backoff, redirect, compression) and append
        // the SDK identification header. Auth is a separate concern handled by the auth provider.
        var handlers = KiotaClientFactory.CreateDefaultHandlers();
        handlers.Add(new SdkClientHandler());
        var pipeline = KiotaClientFactory.ChainHandlersCollectionAndGetFirstLink(
            KiotaClientFactory.GetDefaultHttpMessageHandler(), handlers.ToArray());
        var httpClient = new System.Net.Http.HttpClient(pipeline!);

        return new HttpClientRequestAdapter(new RoxyApiKeyAuthenticationProvider(apiKey), httpClient: httpClient);
    }
}
