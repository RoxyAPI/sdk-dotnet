using System.Net.Http;
using System.Reflection;

namespace RoxyApi;

/// <summary>
/// Stamps every request with <c>X-SDK-Client: roxy-sdk-dotnet/{version}</c> so RoxyAPI can
/// see which SDK and version a call came from. The version is read from the assembly so it
/// tracks the published package version with no separate constant to keep in sync.
/// </summary>
internal sealed class SdkClientHandler : DelegatingHandler
{
    private const string HeaderName = "X-SDK-Client";
    private static readonly string HeaderValue = $"roxy-sdk-dotnet/{ResolveVersion()}";

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!request.Headers.Contains(HeaderName)) request.Headers.TryAddWithoutValidation(HeaderName, HeaderValue);
        return base.SendAsync(request, cancellationToken);
    }

    private static string ResolveVersion()
    {
        var informational = typeof(SdkClientHandler).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        // Informational version may carry a build metadata suffix (1.2.3+sha); keep the semver.
        return informational?.Split('+')[0] ?? "0.0.0";
    }
}
