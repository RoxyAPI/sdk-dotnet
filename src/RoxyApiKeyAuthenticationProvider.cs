using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;

namespace RoxyApi;

/// <summary>
/// Adds the <c>X-API-Key</c> header to every request. RoxyAPI authenticates with a single
/// API key rather than OAuth, so this is all the authentication the client needs.
/// </summary>
internal sealed class RoxyApiKeyAuthenticationProvider : IAuthenticationProvider
{
    private const string HeaderName = "X-API-Key";
    private readonly string _apiKey;

    public RoxyApiKeyAuthenticationProvider(string apiKey) => _apiKey = apiKey;

    public Task AuthenticateRequestAsync(
        RequestInformation request,
        Dictionary<string, object>? additionalAuthenticationContext = null,
        CancellationToken cancellationToken = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));
        if (!request.Headers.ContainsKey(HeaderName)) request.Headers.Add(HeaderName, _apiKey);
        return Task.CompletedTask;
    }
}
