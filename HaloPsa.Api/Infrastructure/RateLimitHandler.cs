using HaloPsa.Api.RateLimiting;

namespace HaloPsa.Api.Infrastructure;

/// <summary>
/// HTTP message handler that takes a slot from an <see cref="IRateLimiter"/> before every attempt
/// and applies <see cref="HaloClientOptions.RequestTimeout"/> to each attempt separately
/// </summary>
/// <remarks>
/// It sits innermost, directly above the transport, so retries, the 401 re-send and token requests are all paced.
/// The timeout starts once the slot is acquired, so time spent waiting for a slot never counts against it.
/// The response body is buffered inside the timeout so the limiter sees a complete response.
/// </remarks>
internal sealed class RateLimitHandler(IRateLimiter rateLimiter, TimeSpan attemptTimeout) : DelegatingHandler
{
	protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		await WaitForSlotAsync(request, cancellationToken).ConfigureAwait(false);

		var response = await SendAttemptAsync(request, cancellationToken).ConfigureAwait(false);

		try
		{
			rateLimiter.OnResponse(request, response);
		}
		catch
		{
			response.Dispose();
			throw;
		}

		return response;
	}

	private async Task WaitForSlotAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		try
		{
			await rateLimiter.ApplyRateLimitingAsync(request, cancellationToken).ConfigureAwait(false);
		}
		catch (OperationCanceledException ex)
		{
			rateLimiter.OnSendFailed(request, ex);
			throw;
		}
	}

	private async Task<HttpResponseMessage> SendAttemptAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		using var attemptCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		attemptCts.CancelAfter(attemptTimeout);

		HttpResponseMessage? response = null;
		try
		{
			response = await base.SendAsync(request, attemptCts.Token).ConfigureAwait(false);
			await response.Content.LoadIntoBufferAsync(attemptCts.Token).ConfigureAwait(false);
			return response;
		}
		catch (Exception ex)
		{
			response?.Dispose();
			rateLimiter.OnSendFailed(request, ex);

			if (ex is OperationCanceledException && attemptCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
			{
				var message = $"The request was canceled due to the configured RequestTimeout of {attemptTimeout.TotalSeconds} seconds elapsing.";
				throw new TaskCanceledException(message, new TimeoutException(message));
			}

			throw;
		}
	}
}
