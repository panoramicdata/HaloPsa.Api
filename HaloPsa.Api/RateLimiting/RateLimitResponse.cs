using System.Net;
using System.Net.Http.Headers;

namespace HaloPsa.Api.RateLimiting;

/// <summary>A read-only view of the response to an attempt, handed to <see cref="IRateLimiter.OnResponse"/></summary>
/// <remarks>It carries only the status and the Retry-After value, never a header collection or a body.</remarks>
public sealed class RateLimitResponse
{
	/// <summary>Creates a view of a response; public so a limiter's own tests can create one</summary>
	/// <param name="statusCode">The status code of the response</param>
	/// <param name="retryAfter">The Retry-After header value as received, or null when the response had none</param>
	public RateLimitResponse(HttpStatusCode statusCode, RetryConditionHeaderValue? retryAfter)
	{
		StatusCode = statusCode;
		RetryAfter = retryAfter;
	}

	/// <summary>The status code of the response</summary>
	public HttpStatusCode StatusCode { get; }

	/// <summary>The Retry-After header value as received, either a delay or a date; null when the response had none</summary>
	public RetryConditionHeaderValue? RetryAfter { get; }
}
