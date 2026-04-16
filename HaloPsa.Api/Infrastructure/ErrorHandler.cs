using HaloPsa.Api.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Refit;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace HaloPsa.Api.Infrastructure;

/// <summary>
/// HTTP message handler that converts Refit API exceptions to HaloApiExceptions
/// </summary>
internal sealed partial class ErrorHandler(ILogger? logger) : DelegatingHandler
{
	private readonly ILogger _logger = logger ?? NullLogger.Instance;

	/// <summary>
	/// Processes HTTP requests and converts any API exceptions to HaloApiExceptions
	/// </summary>
	/// <param name="request">The HTTP request message</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>The HTTP response message</returns>
	protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		try
		{
			var response = await base.SendAsync(request, cancellationToken);
			return response;
		}
		catch (ApiException apiException)
		{
			LogApiException(_logger, apiException, apiException.StatusCode, apiException.ReasonPhrase);

			// Convert Refit ApiException to appropriate HaloApiException
			var haloException = ConvertToHaloApiException(apiException, request);
			throw haloException;
		}
		catch (Exception ex)
		{
			LogUnexpectedError(_logger, ex, request.RequestUri);
			throw;
		}
	}

	/// <summary>
	/// Converts a Refit ApiException to the appropriate HaloApiException type
	/// </summary>
	/// <param name="apiException">The Refit API exception</param>
	/// <param name="request">The original HTTP request</param>
	/// <returns>The appropriate HaloApiException</returns>
	private static HaloApiException ConvertToHaloApiException(ApiException apiException, HttpRequestMessage request)
	{
		var statusCode = (int)apiException.StatusCode;
		var message = apiException.ReasonPhrase ?? $"API request failed with status {statusCode}";
		var requestUrl = request.RequestUri?.ToString();
		var requestMethod = request.Method.Method;

		var (details, validationErrors) = ParseResponseContent(apiException.Content);

		return CreateExceptionForStatusCode(
			statusCode, message, details, validationErrors,
			requestUrl, requestMethod, apiException);
	}

	/// <summary>
	/// Parses error details and validation errors from response content JSON
	/// </summary>
	/// <param name="content">The response content string</param>
	/// <returns>A tuple of error details and validation errors</returns>
	private static (Dictionary<string, object?>? Details, IReadOnlyList<string>? ValidationErrors) ParseResponseContent(string? content)
	{
		if (string.IsNullOrEmpty(content))
		{
			return (null, null);
		}

		try
		{
			var jsonDoc = JsonDocument.Parse(content);
			var details = ExtractErrorDetails(jsonDoc.RootElement);
			var validationErrors = ExtractValidationErrors(jsonDoc.RootElement);
			return (details, validationErrors);
		}
		catch (JsonException)
		{
			return (new Dictionary<string, object?> { ["rawContent"] = content }, null);
		}
	}

	/// <summary>
	/// Extracts validation errors from a JSON element's "errors" property
	/// </summary>
	/// <param name="rootElement">The root JSON element</param>
	/// <returns>A list of validation error strings, or null if none found</returns>
	private static ReadOnlyCollection<string>? ExtractValidationErrors(JsonElement rootElement)
	{
		if (!rootElement.TryGetProperty("errors", out var errorsElement))
		{
			return null;
		}

		if (errorsElement.ValueKind != JsonValueKind.Array)
		{
			return null;
		}

		var errorsList = new List<string>();
		foreach (var error in errorsElement.EnumerateArray())
		{
			if (error.ValueKind == JsonValueKind.String)
			{
				errorsList.Add(error.GetString() ?? string.Empty);
			}
		}

		return errorsList.AsReadOnly();
	}

	/// <summary>
	/// Creates the appropriate HaloApiException subclass based on HTTP status code
	/// </summary>
	private static HaloApiException CreateExceptionForStatusCode(
		int statusCode,
		string message,
		Dictionary<string, object?>? details,
		IReadOnlyList<string>? validationErrors,
		string? requestUrl,
		string? requestMethod,
		Exception? innerException)
	{
		var errorContext = new HaloApiErrorContext
		{
			StatusCode = statusCode,
			Details = details,
			RequestUrl = requestUrl,
			RequestMethod = requestMethod,
			InnerException = innerException
		};

		return statusCode switch
		{
			400 => new HaloBadRequestException($"Bad request: {message}", validationErrors, errorContext),
			401 => new HaloAuthenticationException($"Authentication failed: {message}", errorContext),
			403 => new HaloAuthorizationException($"Authorization failed: {message}", errorContext),
			404 => new HaloNotFoundException($"Resource not found: {message}",
				ExtractResourceTypeFromUrl(requestUrl), ExtractResourceIdFromUrl(requestUrl), errorContext),
			429 => new HaloRateLimitException($"Rate limit exceeded: {message}",
				innerException is ApiException apiEx ? ExtractRetryAfterSeconds(apiEx) : null, null, null, null, errorContext),
			>= 500 => new HaloServerException($"Server error: {message}", errorContext),
			_ => new HaloApiException($"API error: {message}", errorContext)
		};
	}

	/// <summary>
	/// Extracts error details from JSON response
	/// </summary>
	/// <param name="element">The JSON element to extract from</param>
	/// <returns>Dictionary of error details</returns>
	private static Dictionary<string, object?> ExtractErrorDetails(JsonElement element)
	{
		var details = new Dictionary<string, object?>();

		foreach (var property in element.EnumerateObject())
		{
			details[property.Name] = property.Value.ValueKind switch
			{
				JsonValueKind.String => property.Value.GetString(),
				JsonValueKind.Number => property.Value.TryGetInt32(out var intVal) ? intVal : property.Value.GetDouble(),
				JsonValueKind.True => true,
				JsonValueKind.False => false,
				JsonValueKind.Null => null,
				_ => property.Value.GetRawText()
			};
		}

		return details;
	}

	/// <summary>
	/// Extracts resource type from URL (e.g., "Tickets", "Users", "Clients")
	/// </summary>
	/// <param name="url">The request URL</param>
	/// <returns>The resource type or null if not found</returns>
	private static string? ExtractResourceTypeFromUrl(string? url)
	{
		if (string.IsNullOrEmpty(url))
		{
			return null;
		}

		var uri = new Uri(url);
		var segments = uri.Segments;

		// Look for /api/{resourceType} pattern
		for (var i = 0; i < segments.Length - 1; i++)
		{
			if (segments[i].Equals("api/", StringComparison.OrdinalIgnoreCase))
			{
				var resourceSegment = segments[i + 1].TrimEnd('/');
				return resourceSegment;
			}
		}

		return null;
	}

	/// <summary>
	/// Extracts resource ID from URL (e.g., the ID in /api/Tickets/123)
	/// </summary>
	/// <param name="url">The request URL</param>
	/// <returns>The resource ID or null if not found</returns>
	private static object? ExtractResourceIdFromUrl(string? url)
	{
		if (string.IsNullOrEmpty(url))
		{
			return null;
		}

		var uri = new Uri(url);
		var segments = uri.Segments;

		// Look for /api/{resourceType}/{id} pattern
		for (var i = 0; i < segments.Length - 2; i++)
		{
			if (segments[i].Equals("api/", StringComparison.OrdinalIgnoreCase))
			{
				var idSegment = segments[i + 2].TrimEnd('/');
				return int.TryParse(idSegment, out var intId) ? intId : idSegment;
			}
		}

		return null;
	}

	/// <summary>
	/// Extracts Retry-After header value in seconds
	/// </summary>
	/// <param name="apiException">The API exception</param>
	/// <returns>Retry-After value in seconds or null</returns>
	private static int? ExtractRetryAfterSeconds(ApiException apiException)
	{
		if (apiException.Headers?.TryGetValues("Retry-After", out var values) == true)
		{
			var retryAfter = values.FirstOrDefault();
			if (int.TryParse(retryAfter, out var seconds))
			{
				return seconds;
			}
		}

		return null;
	}

	[LoggerMessage(LogLevel.Error, "API exception occurred: {StatusCode} {ReasonPhrase}")]
	private static partial void LogApiException(ILogger logger, Exception ex, System.Net.HttpStatusCode statusCode, string? reasonPhrase);

	[LoggerMessage(LogLevel.Error, "Unexpected error occurred during HTTP request to {RequestUri}")]
	private static partial void LogUnexpectedError(ILogger logger, Exception ex, Uri? requestUri);
}