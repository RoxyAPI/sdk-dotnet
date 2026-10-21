using System.Globalization;

namespace RoxyApi.Models;

public partial class RoxyError
{
    /// <summary>
    /// How long the server asked the caller to wait before retrying, read from the
    /// <c>Retry-After</c> response header (delta seconds or an HTTP date). <c>null</c> when the
    /// response did not send it. A 429 for an exhausted monthly quota carries the time to the
    /// reset, which can be days.
    /// </summary>
    public TimeSpan? RetryAfter
    {
        get
        {
            if (ResponseHeaders is null || !ResponseHeaders.TryGetValue("Retry-After", out var values)) return null;
            var raw = values.FirstOrDefault()?.Trim();
            if (string.IsNullOrEmpty(raw)) return null;
            if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds))
                return TimeSpan.FromSeconds(Math.Max(0, seconds));
            if (DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var date))
                return date - DateTimeOffset.UtcNow is { } wait && wait > TimeSpan.Zero ? wait : TimeSpan.Zero;
            return null;
        }
    }
}
