using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;

namespace HaloPsa.Api.Infrastructure;

/// <summary>
/// HTTP message handler that implements retry logic with exponential backoff
/// </summary>
internal sealed class RetryHandler(
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
		for (var attempt = 0; attempt <= _maxRetryAttempts; attempt++)
		{
			try
			{
				var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

				if (!ShouldRetry(response))
				{
					LogSuccessAfterRetry(request, attempt);
					return response;
				}

				if (attempt == _maxRetryAttempts)
				{
					return CreateFinalFailureResponse(request, response);
				}

				LogRetryAttempt(request, attempt, response.StatusCode);
				response.Dispose();
			}
			catch (Exception ex) when (IsRetryableException(ex))
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

	private static bool ShouldRetry(HttpResponseMessage response)
		=> !response.IsSuccessStatusCode && IsRetryableStatusCode(response.StatusCode);

	private void LogSuccessAfterRetry(HttpRequestMessage request, int attempt)
	{
		if (attempt > 0 && _logger?.IsEnabled(LogLevel.Information) == true)
		{
			LogSuccessOnRetry(_logger, attempt + 1, request.Method, request.RequestUri);
		}
	}

	private HttpResponseMessage CreateFinalFailureResponse(HttpRequestMessage request, HttpResponseMessage response)
	{
		var statusCode = response.StatusCode;
		response.Dispose();

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

	private static readonly Action<ILogger, int, HttpMethod, Uri?, Exception?> _logSuccessOnRetry = LoggerMessage.Define<int, HttpMethod, Uri?>(
		LogLevel.Information, new EventId(1, nameof(LogSuccessOnRetry)),
		"HTTP request succeeded on attempt {Attempt} for {Method} {Uri}");

	private static readonly Action<ILogger, int, HttpMethod, Uri?, HttpStatusCode, Exception?> _logFinalFailureStatus = LoggerMessage.Define<int, HttpMethod, Uri?, HttpStatusCode>(
		LogLevel.Warning, new EventId(2, nameof(LogFinalFailureStatus)),
		"HTTP request failed after {MaxAttempts} attempts for {Method} {Uri} with status {StatusCode}");

	private static readonly Action<ILogger, int, double, HttpMethod, Uri?, HttpStatusCode, Exception?> _logRetryAttemptStatus = LoggerMessage.Define<int, double, HttpMethod, Uri?, HttpStatusCode>(
		LogLevel.Warning, new EventId(3, nameof(LogRetryAttemptStatus)),
		"HTTP request failed on attempt {Attempt}, retrying in {Delay}ms for {Method} {Uri} (Status: {StatusCode})");

	private static readonly Action<ILogger, int, HttpMethod, Uri?, Exception?> _logFinalFailureException = LoggerMessage.Define<int, HttpMethod, Uri?>(
		LogLevel.Error, new EventId(4, nameof(LogFinalFailureException)),
		"HTTP request failed after {MaxAttempts} attempts for {Method} {Uri}");

	private static readonly Action<ILogger, int, double, HttpMethod, Uri?, Exception?> _logRetryAfterException = LoggerMessage.Define<int, double, HttpMethod, Uri?>(
		LogLevel.Warning, new EventId(5, nameof(LogRetryAfterException)),
		"HTTP request failed on attempt {Attempt}, retrying in {Delay}ms for {Method} {Uri}");

	private static void LogSuccessOnRetry(ILogger logger, int attempt, HttpMethod method, Uri? uri)
		=> _logSuccessOnRetry(logger, attempt, method, uri, null);

	private static void LogFinalFailureStatus(ILogger logger, int maxAttempts, HttpMethod method, Uri? uri, HttpStatusCode statusCode)
		=> _logFinalFailureStatus(logger, maxAttempts, method, uri, statusCode, null);

	private static void LogRetryAttemptStatus(ILogger logger, int attempt, double delay, HttpMethod method, Uri? uri, HttpStatusCode statusCode)
		=> _logRetryAttemptStatus(logger, attempt, delay, method, uri, statusCode, null);

	private static void LogFinalFailureException(ILogger logger, Exception ex, int maxAttempts, HttpMethod method, Uri? uri)
		=> _logFinalFailureException(logger, maxAttempts, method, uri, ex);

	private static void LogRetryAfterException(ILogger logger, Exception ex, int attempt, double delay, HttpMethod method, Uri? uri)
		=> _logRetryAfterException(logger, attempt, delay, method, uri, ex);
}
