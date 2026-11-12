using System.Net;
using System.Net.Sockets;

namespace HaloPsa.Api.Infrastructure;

internal sealed partial class RetryHandler
{
	private static readonly HttpStatusCode[] _retryableStatusCodes =
	[
		HttpStatusCode.RequestTimeout,
		HttpStatusCode.TooManyRequests,
		HttpStatusCode.InternalServerError,
		HttpStatusCode.BadGateway,
		HttpStatusCode.ServiceUnavailable,
		HttpStatusCode.GatewayTimeout
	];

	private static bool ShouldRetry(HttpMethod method, HttpResponseMessage response)
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

	// A failed POST may already have been processed, so only a 429, which Halo answers unprocessed, is safe to re-send.
	private static bool MayRetry(HttpMethod method, HttpStatusCode statusCode)
		=> IsRetryableStatusCode(statusCode) && (method != HttpMethod.Post || statusCode == HttpStatusCode.TooManyRequests);

	private static bool MayRetry(HttpMethod method, Exception ex)
		=> IsRetryableException(ex) && method != HttpMethod.Post;

	private static bool IsRetryableStatusCode(HttpStatusCode statusCode)
		=> _retryableStatusCodes.Contains(statusCode);

	private static bool IsRetryableException(Exception ex)
		=> ex is HttpRequestException or TaskCanceledException or SocketException;
}
