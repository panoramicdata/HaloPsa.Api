using HaloPsa.Api.Exceptions;

namespace HaloPsa.Api.RateLimiting;

/// <summary>Paces requests to Halo. Called once per attempt, including retries, the 401 re-send and the token request.</summary>
/// <remarks>
/// <list type="bullet">
/// <item>Each <see cref="ApplyRateLimitingAsync"/> that returns is followed by exactly one <see cref="OnResponse"/> or <see cref="OnSendFailed"/> for that attempt.
/// When <see cref="ApplyRateLimitingAsync"/> throws, whatever it throws (a give-up with <see cref="HaloRequestNotSentException"/>, a cancellation by the caller or by the limiter itself, any other exception), the attempt gets neither, because no slot was taken.</item>
/// <item>Every attempt gets a new <see cref="RateLimitAttempt"/>, including each retry, the re-send after a 401 and each token request.
/// The instance passed to <see cref="ApplyRateLimitingAsync"/> is the one passed to that attempt's <see cref="OnResponse"/> or <see cref="OnSendFailed"/>.</item>
/// <item>With a limiter, <see cref="HaloClientOptions.RequestTimeout"/> applies to each attempt and starts once the slot is acquired. A timed-out attempt of a request that is not a POST is retried like other transient failures, taking another slot, and a call has no overall timeout: use the caller's <see cref="CancellationToken"/> to bound it.</item>
/// <item>With a limiter, a 429 Too Many Requests is not retried. It means the shared request limit is already spent, and Halo can keep refusing requests for minutes, often with no Retry-After, so a quick retry only sends another request into a blocked limit. The limiter sees the 429 in <see cref="OnResponse"/> and decides when to send again; a <c>Psa</c> call raises <see cref="HaloRateLimitException"/>.</item>
/// <item>To give up, throw <see cref="HaloRequestNotSentException"/>. Throwing <see cref="TaskCanceledException"/> instead is treated as a transient failure and retried.</item>
/// <item>The limiter sees the method, URI, whether it is the token request, and the response's status and Retry-After, never any other header or a body, so no credentials reach it.</item>
/// </list>
/// </remarks>
public interface IRateLimiter
{
	/// <summary>Waits for a free slot before an attempt is sent; may throw <see cref="HaloRequestNotSentException"/> to give up. A throw takes no slot and gets no <see cref="OnResponse"/> or <see cref="OnSendFailed"/>.</summary>
	/// <param name="attempt">The attempt about to be sent</param>
	/// <param name="cancellationToken">Cancellation token</param>
	Task ApplyRateLimitingAsync(RateLimitAttempt attempt, CancellationToken cancellationToken);

	/// <summary>Called when an attempt's response has been received in full.</summary>
	/// <param name="attempt">The attempt that was sent, the instance passed to <see cref="ApplyRateLimitingAsync"/></param>
	/// <param name="response">The status and Retry-After of the response that was received</param>
	void OnResponse(RateLimitAttempt attempt, RateLimitResponse response);

	/// <summary>Called when an attempt failed or was cancelled before a response arrived, after its slot was taken.</summary>
	/// <param name="attempt">The attempt that failed, the instance passed to <see cref="ApplyRateLimitingAsync"/></param>
	/// <param name="exception">The failure</param>
	void OnSendFailed(RateLimitAttempt attempt, Exception exception);
}
