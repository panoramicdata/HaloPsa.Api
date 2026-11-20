namespace HaloPsa.Api.RateLimiting;

/// <summary>Paces requests to Halo. Called once per attempt, including retries, the 401 re-send and the token request.</summary>
public interface IRateLimiter
{
	/// <summary>Waits for a free slot before an attempt is sent; may throw HaloRequestNotSentException to give up.</summary>
	/// <param name="request">The request about to be sent</param>
	/// <param name="cancellationToken">Cancellation token</param>
	Task ApplyRateLimitingAsync(HttpRequestMessage request, CancellationToken cancellationToken);

	/// <summary>Called when an attempt's response, body included, has been received.</summary>
	/// <param name="request">The request that was sent</param>
	/// <param name="response">The response that was received</param>
	void OnResponse(HttpRequestMessage request, HttpResponseMessage response);

	/// <summary>Called when an attempt failed or was cancelled before a response arrived, including a cancelled wait for a slot.</summary>
	/// <param name="request">The request that was attempted</param>
	/// <param name="exception">The failure</param>
	void OnSendFailed(HttpRequestMessage request, Exception exception);
}
