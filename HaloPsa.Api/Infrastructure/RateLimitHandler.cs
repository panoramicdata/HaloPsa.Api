using HaloPsa.Api.RateLimiting;
using System.Net;

namespace HaloPsa.Api.Infrastructure;

/// <summary>
/// HTTP message handler that takes a slot from an <see cref="IRateLimiter"/> before every attempt
/// and applies <see cref="HaloClientOptions.RequestTimeout"/> to each attempt separately
/// </summary>
/// <remarks>
/// It sits innermost, directly above the transport, so retries, the 401 re-send and token requests are all paced.
/// The timeout starts once the slot is acquired, so time spent waiting for a slot never counts against it.
/// The response body is buffered inside the timeout so the limiter sees a complete response; a 429 whose body cannot be read is returned with an empty body.
/// </remarks>
internal sealed class RateLimitHandler(IRateLimiter rateLimiter, TimeSpan attemptTimeout) : DelegatingHandler
{
	protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		// A throw here took no slot, so the limiter gets no callback for the attempt.
		await rateLimiter.ApplyRateLimitingAsync(request, cancellationToken).ConfigureAwait(false);

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

	// A 429 whose body cannot be read is still a 429: it is returned with an empty body so the limiter sees it and it is never retried.
	private static bool IsUnreadableTooManyRequests(HttpResponseMessage? response, CancellationToken cancellationToken)
		=> response is { StatusCode: HttpStatusCode.TooManyRequests } && !cancellationToken.IsCancellationRequested;

	private async Task<HttpResponseMessage> SendAttemptAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		using var attemptCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

		HttpResponseMessage? response = null;
		try
		{
			attemptCts.CancelAfter(attemptTimeout);
			response = await base.SendAsync(request, attemptCts.Token).ConfigureAwait(false);
			await response.Content.LoadIntoBufferAsync(attemptCts.Token).ConfigureAwait(false);
			return response;
		}
		catch (Exception) when (IsUnreadableTooManyRequests(response, cancellationToken))
		{
			response!.Content.Dispose();
			response.Content = new ByteArrayContent([]);
			return response;
		}
		catch (Exception ex)
		{
			response?.Dispose();
			rateLimiter.OnSendFailed(request, ex);

			if (ex is OperationCanceledException && attemptCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
			{
				var message = $"The request was canceled due to the configured RequestTimeout of {attemptTimeout.TotalSeconds} seconds elapsing.";
				throw new TaskCanceledException(message, new TimeoutException(message, ex));
			}

			throw;
		}
	}
}
