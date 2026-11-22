namespace HaloPsa.Api.RateLimiting;

/// <summary>A read-only view of one attempt to send a request, handed to an <see cref="IRateLimiter"/></summary>
/// <remarks>
/// A new instance is created for every attempt: each retry, the re-send after a 401 and each token request.
/// The instance passed to <see cref="IRateLimiter.ApplyRateLimitingAsync"/> is the one passed to that attempt's <see cref="IRateLimiter.OnResponse"/> or <see cref="IRateLimiter.OnSendFailed"/>,
/// so a limiter can use it as a correlation key by reference.
/// It carries no headers and no body, so no credentials reach the limiter.
/// </remarks>
public sealed class RateLimitAttempt
{
	internal RateLimitAttempt(HttpMethod method, Uri? requestUri, bool isTokenRequest)
	{
		Method = method;
		RequestUri = requestUri;
		IsTokenRequest = isTokenRequest;
	}

	/// <summary>The HTTP method of the request</summary>
	public HttpMethod Method { get; }

	/// <summary>The URI the request is sent to</summary>
	public Uri? RequestUri { get; }

	/// <summary>True when the attempt is a request for an access token rather than an API request</summary>
	public bool IsTokenRequest { get; }
}
