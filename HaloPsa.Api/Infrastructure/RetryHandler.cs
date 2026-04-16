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
	ILogger? logger) : DelegatingHandler
{
	private readonly int _maxRetryAttempts = maxRetryAttempts;
	private readonly TimeSpan _retryDelay = retryDelay;
	private readonly bool _useExponentialBackoff = useExponentialBackoff;
	private readonly TimeSpan _maxRetryDelay = maxRetryDelay;
	private readonly ILogger? _logger = logger;

	private static readonly HttpStatusCode[] _retryableStatusCodes =
	[
		HttpStatusCode.RequestTimeout,
		HttpStatusCode.TooManyRequests,
		HttpStatusCode.InternalServerError,
		HttpStatusCode.BadGateway,
		HttpStatusCode.ServiceUnavailable,
		HttpStatusCode.GatewayTimeout
	];

	protected override async Task<HttpResponseMessage> SendAsync(
		HttpRequestMessage request,
		CancellationToken cancellationToken)
	{
		var attempt = 0;
		Exception? lastException = null;

		while (attempt <= _maxRetryAttempts)
		{
			try
			{
				var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

				if (response.IsSuccessStatusCode || !IsRetryableStatusCode(response.StatusCode))
				{
					LogSuccessAfterRetry(request, attempt);
					return response;
				}

				response.Dispose();

				if (attempt == _maxRetryAttempts)
				{
					return CreateFinalFailureResponse(request, response.StatusCode);
				}

				LogRetryAttempt(request, attempt, response.StatusCode);
			}
			catch (Exception ex) when (IsRetryableException(ex))
			{
				lastException = ex;

				if (attempt == _maxRetryAttempts)
				{
					LogFinalFailure(request, ex);
					throw;
				}

				LogRetryAfterException(request, attempt, ex);
			}

			if (attempt < _maxRetryAttempts)
			{
				await Task.Delay(CalculateDelay(attempt), cancellationToken).ConfigureAwait(false);
			}

			attempt++;
		}

		throw lastException ?? new HttpRequestException($"Request failed after {_maxRetryAttempts + 1} attempts");
	}

	private void LogSuccessAfterRetry(HttpRequestMessage request, int attempt)
	{
		if (attempt > 0 && _logger?.IsEnabled(LogLevel.Information) == true)
		{
			LogSuccessOnRetry(_logger, attempt + 1, request.Method, request.RequestUri);
		}
	}

	private HttpResponseMessage CreateFinalFailureResponse(HttpRequestMessage request, HttpStatusCode statusCode)
	{
		if (_logger?.IsEnabled(LogLevel.Warning) == true)
		{
			LogFinalFailureStatus(_logger, _maxRetryAttempts + 1, request.Method, request.RequestUri, statusCode);
		}

		return new HttpResponseMessage(statusCode)
		{
			RequestMessage = request,
			ReasonPhrase = $"Failed after {_maxRetryAttempts + 1} attempts"
		};
	}

	private void LogRetryAttempt(HttpRequestMessage request, int attempt, HttpStatusCode statusCode)
	{
		if (_logger?.IsEnabled(LogLevel.Warning) == true)
		{
			LogRetryAttemptStatus(_logger, attempt + 1, CalculateDelay(attempt).TotalMilliseconds, request.Method, request.RequestUri, statusCode);
		}
	}

	private void LogFinalFailure(HttpRequestMessage request, Exception ex)
	{
		if (_logger?.IsEnabled(LogLevel.Error) == true)
		{
			LogFinalFailureException(_logger, ex, _maxRetryAttempts + 1, request.Method, request.RequestUri);
		}
	}

	private void LogRetryAfterException(HttpRequestMessage request, int attempt, Exception ex)
	{
		if (_logger?.IsEnabled(LogLevel.Warning) == true)
		{
			LogRetryAfterException(_logger, ex, attempt + 1, CalculateDelay(attempt).TotalMilliseconds, request.Method, request.RequestUri);
		}
	}

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

	private static bool IsRetryableStatusCode(HttpStatusCode statusCode)
		=> _retryableStatusCodes.Contains(statusCode);

	private static bool IsRetryableException(Exception ex)
		=> ex is HttpRequestException or TaskCanceledException or SocketException;

	[LoggerMessage(LogLevel.Information, "HTTP request succeeded on attempt {Attempt} for {Method} {Uri}")]
	private static partial void LogSuccessOnRetry(ILogger logger, int attempt, HttpMethod method, Uri? uri);

	[LoggerMessage(LogLevel.Warning, "HTTP request failed after {MaxAttempts} attempts for {Method} {Uri} with status {StatusCode}")]
	private static partial void LogFinalFailureStatus(ILogger logger, int maxAttempts, HttpMethod method, Uri? uri, HttpStatusCode statusCode);

	[LoggerMessage(LogLevel.Warning, "HTTP request failed on attempt {Attempt}, retrying in {Delay}ms for {Method} {Uri} (Status: {StatusCode})")]
	private static partial void LogRetryAttemptStatus(ILogger logger, int attempt, double delay, HttpMethod method, Uri? uri, HttpStatusCode statusCode);

	[LoggerMessage(LogLevel.Error, "HTTP request failed after {MaxAttempts} attempts for {Method} {Uri}")]
	private static partial void LogFinalFailureException(ILogger logger, Exception ex, int maxAttempts, HttpMethod method, Uri? uri);

	[LoggerMessage(LogLevel.Warning, "HTTP request failed on attempt {Attempt}, retrying in {Delay}ms for {Method} {Uri}")]
	private static partial void LogRetryAfterException(ILogger logger, Exception ex, int attempt, double delay, HttpMethod method, Uri? uri);
}