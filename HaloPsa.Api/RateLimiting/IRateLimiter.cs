using HaloPsa.Api.Exceptions;

namespace HaloPsa.Api.RateLimiting;

/// <summary>Paces requests to Halo. Called once per attempt, including retries, the 401 re-send and the token request.</summary>
/// <remarks>
/// <list type="bullet">
/// <item>Each <see cref="ApplyRateLimitingAsync"/> that returns is followed by exactly one <see cref="OnResponse"/> or <see cref="OnSendFailed"/> for that attempt.
/// When <see cref="ApplyRateLimitingAsync"/> throws, whatever it throws (a give-up with <see cref="HaloRequestNotSentException"/>, a cancellation by the caller or by the limiter itself, any other exception), the attempt gets neither, because no slot was taken.</item>
/// <item>Retries re-send the same <see cref="HttpRequestMessage"/> instance, so <see cref="ApplyRateLimitingAsync"/> can see one instance several times, one attempt after another.
/// The re-send after a 401 and each token request are different instances.</item>
/// <item>With a limiter, <see cref="HaloClientOptions.RequestTimeout"/> applies to each attempt and starts once the slot is acquired. A timed-out attempt of a request that is not a POST is retried like other transient failures, taking another slot, and a call has no overall timeout: use the caller's <see cref="CancellationToken"/> to bound it.</item>
/// <item>With a limiter, a 429 Too Many Requests is not retried. It means the shared request limit is already spent, and Halo can keep refusing requests for minutes, often with no Retry-After, so a quick retry only sends another request into a blocked limit. The limiter sees the 429 in <see cref="OnResponse"/> and decides when to send again; a <c>Psa</c> call raises <see cref="HaloRateLimitException"/>.</item>
/// <item>To give up, throw <see cref="HaloRequestNotSentException"/>. Throwing <see cref="TaskCanceledException"/> instead is treated as a transient failure and retried.</item>
/// <item>Security: an implementation is handed the full request and response, which carry the <c>Authorization</c> bearer header on API requests, <c>client_id</c> and <c>client_secret</c> in the token request's form body, and <c>access_token</c> in the token response. It must not log or store request or response contents or headers.</item>
/// </list>
/// </remarks>
public interface IRateLimiter
{
	/// <summary>Waits for a free slot before an attempt is sent; may throw <see cref="HaloRequestNotSentException"/> to give up. A throw takes no slot and gets no <see cref="OnResponse"/> or <see cref="OnSendFailed"/>.</summary>
	/// <param name="request">The request about to be sent</param>
	/// <param name="cancellationToken">Cancellation token</param>
	Task ApplyRateLimitingAsync(HttpRequestMessage request, CancellationToken cancellationToken);

	/// <summary>Called when an attempt's response, body included, has been received.</summary>
	/// <param name="request">The request that was sent</param>
	/// <param name="response">The response that was received</param>
	void OnResponse(HttpRequestMessage request, HttpResponseMessage response);

	/// <summary>Called when an attempt failed or was cancelled before a response arrived, after its slot was taken.</summary>
	/// <param name="request">The request that was attempted</param>
	/// <param name="exception">The failure</param>
	void OnSendFailed(HttpRequestMessage request, Exception exception);
}
