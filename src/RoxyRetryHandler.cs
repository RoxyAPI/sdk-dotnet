using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace RoxyApi;

/// <summary>
/// Retries 429, 500, 502, 503 and 504 responses and connection failures where the request was never
/// sent, as described on <see cref="RoxyRetryOptions"/>. Replaces Kiota's default retry handler
/// at the same place in the pipeline.
/// </summary>
internal sealed class RoxyRetryHandler(
    RoxyRetryOptions options,
    Func<double> random,
    Func<TimeSpan, CancellationToken, Task> sleep) : DelegatingHandler
{
    private static readonly Random Shared = new();

    /// <summary>The <c>HttpClient.Timeout</c> of the call; a retry never sleeps past it. Infinite when unset.</summary>
    public TimeSpan OverallTimeout { get; set; } = Timeout.InfiniteTimeSpan;

    public RoxyRetryHandler(RoxyRetryOptions options)
        : this(options, () => { lock (Shared) { return Shared.NextDouble(); } }, Task.Delay) { }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (options.MaxRetries <= 0) return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        var clock = Stopwatch.StartNew();
        var replay = await Replay.CaptureAsync(request).ConfigureAwait(false);
        var current = request;
        for (var attempt = 0; ; attempt++)
        {
            HttpResponseMessage? response = null;
            HttpRequestException? failure = null;
            try
            {
                response = await base.SendAsync(current, cancellationToken).ConfigureAwait(false);
            }
            catch (HttpRequestException e) when (NeverSent(e))
            {
                failure = e;
            }

            if (response is not null && !IsRetryable(response.StatusCode)) return response;

            var wait = attempt < options.MaxRetries ? NextDelay(attempt, response) : null;
            var deadline = OverallTimeout > TimeSpan.Zero ? OverallTimeout : TimeSpan.MaxValue;
            if (wait is null || clock.Elapsed + wait.Value >= deadline)
            {
                if (response is not null) return response;
                throw failure!;
            }

            response?.Dispose();
            await sleep(wait.Value, cancellationToken).ConfigureAwait(false);
            current = replay.Clone(request);
        }
    }

    private static bool IsRetryable(HttpStatusCode status) => (int)status is 429 or 500 or 502 or 503 or 504;

    // Refused and unresolvable hosts fail before a byte is written, so replaying cannot duplicate a request.
    private static bool NeverSent(HttpRequestException e) =>
        e.InnerException is SocketException { SocketErrorCode: SocketError.ConnectionRefused or SocketError.HostNotFound };

    private TimeSpan? NextDelay(int attempt, HttpResponseMessage? response)
    {
        if (response?.Headers.RetryAfter is { } retryAfter)
        {
            var stated = retryAfter.Delta ?? (retryAfter.Date.HasValue ? retryAfter.Date.Value - DateTimeOffset.UtcNow : TimeSpan.Zero);
            if (stated < TimeSpan.Zero) stated = TimeSpan.Zero;
            if (stated > options.MaxDelay) return null;
            return Min(options.MaxDelay, stated + Scale(options.InitialDelay, random()));
        }

        var ceiling = Math.Min(options.MaxDelay.TotalMilliseconds, options.InitialDelay.TotalMilliseconds * Math.Pow(2, attempt));
        return TimeSpan.FromMilliseconds(ceiling * random());
    }

    private static TimeSpan Scale(TimeSpan span, double factor) => TimeSpan.FromMilliseconds(span.TotalMilliseconds * factor);

    private static TimeSpan Min(TimeSpan a, TimeSpan b) => a < b ? a : b;

    // An HttpRequestMessage cannot be sent twice and its content stream is consumed by the first
    // send, so the body is buffered once and every retry is a fresh message over the same bytes.
    private sealed class Replay
    {
        private byte[]? _body;
        private List<KeyValuePair<string, string[]>> _contentHeaders = [];

        public static async Task<Replay> CaptureAsync(HttpRequestMessage request)
        {
            var replay = new Replay();
            if (request.Content is { } content)
            {
                replay._body = await content.ReadAsByteArrayAsync().ConfigureAwait(false);
                replay._contentHeaders = content.Headers.Select(h => new KeyValuePair<string, string[]>(h.Key, h.Value.ToArray())).ToList();
                request.Content = replay.NewContent();
            }
            return replay;
        }

        public HttpRequestMessage Clone(HttpRequestMessage original)
        {
            var clone = new HttpRequestMessage(original.Method, original.RequestUri) { Version = original.Version, Content = NewContent() };
#if NET5_0_OR_GREATER
            clone.VersionPolicy = original.VersionPolicy;
            foreach (var option in (IDictionary<string, object?>)original.Options)
                ((IDictionary<string, object?>)clone.Options)[option.Key] = option.Value;
#else
            foreach (var property in original.Properties) clone.Properties[property.Key] = property.Value;
#endif
            foreach (var header in original.Headers) clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            return clone;
        }

        private HttpContent? NewContent()
        {
            if (_body is null) return null;
            var content = new ByteArrayContent(_body);
            content.Headers.Clear();
            foreach (var header in _contentHeaders) content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            return content;
        }
    }
}
