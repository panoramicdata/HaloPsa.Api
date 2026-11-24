using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;

namespace HaloPsa.Api.Infrastructure;

/// <summary>
/// HTTP message handler that implements retry logic with exponential backoff
/// </summary>
internal sealed partial class RetryHandler(
	int maxRetryAttempts,
	TimeSpan retryDelay,
	bool useExponentialBackoff,
	TimeSpan maxRetryDelay,
	bool hasRateLimiter,
	ILogger? logger) : DelegatingHandler
{
	private readonly int _maxRetryAttempts = maxRetryAttempts;
	private readonly TimeSpan _retryDelay = retryDelay;
	private readonly bool _useExponentialBackoff = useExponentialBackoff;
	private readonly TimeSpan _maxRetryDelay = maxRetryDelay;
	private readonly bool _hasRateLimiter = hasRateLimiter;
	private readonly ILogger? _logger = logger;

	protected override async Task<HttpResponseMessage> SendAsync(
		HttpRequestMessage request,
		CancellationToken cancellationToken)
	{
		for (var attempt = 0; attempt <= _maxRetryAttempts; attempt++)
		{
			try
			{
				var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

				if (!ShouldRetry(request.Method, response))
				{
					LogSuccessAfterRetry(request, response, attempt);
					return response;
				}

				if (attempt == _maxRetryAttempts)
				{
					return CreateFinalFailureResponse(request, response);
				}

				LogRetryAttempt(request, attempt, response.StatusCode);
				response.Dispose();
			}
			catch (Exception ex) when (MayRetry(request.Method, ex))
			{
				if (attempt == _maxRetryAttempts)
				{
					LogFinalFailure(request, ex);
					throw;
				}

				LogRetryAfterException(request, attempt, ex);
			}

			await Task.Delay(CalculateDelay(attempt), cancellationToken).ConfigureAwait(false);
		}

		throw new InvalidOperationException("The retry loop ended unexpectedly.");
	}

	private HttpResponseMessage CreateFinalFailureResponse(HttpRequestMessage request, HttpResponseMessage response)
	{
		var statusCode = response.StatusCode;

		if (_logger?.IsEnabled(LogLevel.Debug) == true)
		{
			LogFinalFailureStatus(_logger, _maxRetryAttempts + 1, request.Method, request.RequestUri, statusCode);
		}

		// A limiter needs the real final response to see a 429 and its Retry-After; other clients keep the bare one for compatibility
		if (_hasRateLimiter)
		{
			return response;
		}

		response.Dispose();

		return new HttpResponseMessage(statusCode)
		{
			RequestMessage = request,
			ReasonPhrase = $"Failed after {_maxRetryAttempts + 1} attempts"
		};
	}
}
