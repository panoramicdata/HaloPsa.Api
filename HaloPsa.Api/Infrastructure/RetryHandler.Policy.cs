using System.Net;
using System.Net.Sockets;

namespace HaloPsa.Api.Infrastructure;

internal sealed partial class RetryHandler
{
	private static readonly HttpStatusCode[] _retryableStatusCodes =
	[
		HttpStatusCode.RequestTimeout,
		HttpStatusCode.InternalServerError,
		HttpStatusCode.BadGateway,
		HttpStatusCode.ServiceUnavailable,
		HttpStatusCode.GatewayTimeout
	];

	private bool ShouldRetry(HttpMethod method, HttpResponseMessage response)
		=> !response.IsSuccessStatusCode && MayRetry(method, response.StatusCode);

	private TimeSpan CalculateDelay(int attemptNumber)
	{
		if (!_useExponentialBackoff)
		{
			return _retryDelay;
		}

		// Exponential backoff: delay * 2^attempt
		var exponentialDelay = TimeSpan.FromMilliseconds(_retryDelay.TotalMilliseconds * Math.Pow(2, attemptNumber));
		return exponentialDelay > _maxRetryDelay ? _maxRetryDelay : exponentialDelay;
	}

	// A failed POST may already have been processed, so it is never re-sent; the exception is a 429, which Halo answers unprocessed, when no rate limiter is set.
	private bool MayRetry(HttpMethod method, HttpStatusCode statusCode)
		=> IsRetryableStatusCode(statusCode) && (method != HttpMethod.Post || RetriesTooManyRequests(statusCode));

	private static bool MayRetry(HttpMethod method, Exception ex)
		=> IsRetryableException(ex) && method != HttpMethod.Post;

	private bool IsRetryableStatusCode(HttpStatusCode statusCode)
		=> _retryableStatusCodes.Contains(statusCode) || RetriesTooManyRequests(statusCode);

	// With a limiter, a 429 is left to it: the limit is spent, so an immediate retry cannot succeed.
	private bool RetriesTooManyRequests(HttpStatusCode statusCode)
		=> !_hasRateLimiter && statusCode == HttpStatusCode.TooManyRequests;

	private static bool IsRetryableException(Exception ex)
		=> ex is HttpRequestException or TaskCanceledException or SocketException;
}
