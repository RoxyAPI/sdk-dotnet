namespace RoxyApi;

/// <summary>
/// How <see cref="RoxyClient"/> retries a request that failed in a way a second attempt can fix:
/// an HTTP 429, 500, 502, 503 or 504, or a connection that was refused or could not resolve the host
/// (the request never left the machine). Everything else, including a read timeout,
/// surfaces at once.
/// </summary>
/// <remarks>
/// <para>Without a <c>Retry-After</c> header the wait is a random value between zero and
/// <c>min(MaxDelay, InitialDelay * 2^attempt)</c>. With one, the wait is that value plus a random
/// padding of up to <see cref="InitialDelay"/>, capped at <see cref="MaxDelay"/>. A
/// <c>Retry-After</c> longer than <see cref="MaxDelay"/> is never waited out: the call throws at once
/// and <see cref="Models.RoxyError.RetryAfter"/> carries the server-stated wait.</para>
/// <para>A retry never sleeps past the <see cref="System.Net.Http.HttpClient"/> timeout of the call.</para>
/// </remarks>
public sealed class RoxyRetryOptions
{
    /// <summary>Retries after the first attempt. The default is 2; 0 turns retries off.</summary>
    public int MaxRetries { get; set; } = 2;

    /// <summary>Base of the exponential backoff and the padding added to a <c>Retry-After</c>. The default is 500 ms.</summary>
    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromMilliseconds(500);

    /// <summary>Longest wait before a retry, and the largest <c>Retry-After</c> that is honoured. The default is 30 s.</summary>
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(30);

    internal void Validate()
    {
        if (MaxRetries < 0) throw new ArgumentOutOfRangeException(nameof(MaxRetries), "MaxRetries cannot be negative.");
        if (InitialDelay < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(InitialDelay), "InitialDelay cannot be negative.");
        if (MaxDelay < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(MaxDelay), "MaxDelay cannot be negative.");
    }
}
