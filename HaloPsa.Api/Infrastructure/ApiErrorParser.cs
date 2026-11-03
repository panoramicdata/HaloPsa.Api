using HaloPsa.Api.Exceptions;
using System.Collections.ObjectModel;
using System.Net.Http.Headers;
using System.Text.Json;

namespace HaloPsa.Api.Infrastructure;

/// <summary>
/// Turns a failed Halo API response into the matching <see cref="HaloApiException"/>.
///
/// Two call sites need this: the Refit exception factory in PsaApi, which sees an
/// <see cref="HttpResponseMessage"/>, and ErrorHandler, which sees a Refit ApiException. They
/// previously carried their own copies of every method below, so a fix to the URL or JSON parsing
/// had to be made twice and, in practice, drifted.
/// </summary>
internal static class ApiErrorParser
{
	private const string _messageKey = "message";
	private const string _rawContentKey = "rawContent";

	/// <summary>
	/// Parses error details and validation errors out of a JSON error body.
	/// </summary>
	/// <param name="content">The response body, which need not be JSON.</param>
	/// <returns>
	/// The parsed details and validation errors. A body that is absent or empty yields nulls; a JSON
	/// object is read property by property; a JSON string is read, trimmed, under a <c>message</c> key;
	/// any other body is returned intact under a <c>rawContent</c> key, so nothing is lost.
	/// </returns>
	public static (Dictionary<string, object?>? Details, ReadOnlyCollection<string>? ValidationErrors) ParseJsonContent(string? content)
	{
		if (string.IsNullOrEmpty(content))
		{
			return (null, null);
		}

		try
		{
			var jsonDoc = JsonDocument.Parse(content);
			var root = jsonDoc.RootElement;

			if (root.ValueKind == JsonValueKind.Object)
			{
				return (ExtractErrorDetails(root), ExtractValidationErrors(root));
			}

			if (ReadStringMessage(root) is { } message)
			{
				return (new Dictionary<string, object?> { [_messageKey] = message }, null);
			}

			return (new Dictionary<string, object?> { [_rawContentKey] = content }, null);
		}
		catch (JsonException)
		{
			return (new Dictionary<string, object?> { [_rawContentKey] = content }, null);
		}
	}

	/// <summary>
	/// Reads the message from a body that is a bare JSON string, which is how Halo refuses some requests.
	/// </summary>
	/// <param name="content">The response body, which need not be JSON.</param>
	/// <returns>The trimmed string, or null when the body is not a JSON string or the string is blank.</returns>
	public static string? ReadStringBodyMessage(string? content)
	{
		if (string.IsNullOrEmpty(content))
		{
			return null;
		}

		try
		{
			using var jsonDoc = JsonDocument.Parse(content);
			return ReadStringMessage(jsonDoc.RootElement);
		}
		catch (JsonException)
		{
			return null;
		}
	}

	private static string? ReadStringMessage(JsonElement root)
	{
		if (root.ValueKind != JsonValueKind.String)
		{
			return null;
		}

		string? text;
		try
		{
			text = root.GetString()?.Trim();
		}
		catch (InvalidOperationException)
		{
			// GetString throws for an escape that is not valid UTF-16, such as an unpaired surrogate.
			return null;
		}

		return string.IsNullOrEmpty(text) ? null : text;
	}

	/// <summary>
	/// Creates the appropriate HaloApiException subclass based on HTTP status code
	/// </summary>
	/// <param name="statusCode">The HTTP status code of the failed response.</param>
	/// <param name="message">The message that describes the error.</param>
	/// <param name="validationErrors">Validation errors from the API, for a 400 response.</param>
	/// <param name="requestUrl">The request URL, from which a 404's resource type and id are read.</param>
	/// <param name="responseHeaders">The response headers, from which a 429's Retry-After is read.</param>
	/// <param name="errorContext">Error context to attach to the exception. The caller builds this,
	/// because only the caller knows whether there is an inner exception to record.</param>
	public static HaloApiException CreateExceptionForStatusCode(
		int statusCode,
		string message,
		IReadOnlyList<string>? validationErrors,
		string? requestUrl,
		HttpHeaders? responseHeaders,
		HaloApiErrorContext errorContext)
		=> statusCode switch
		{
			400 => new HaloBadRequestException($"Bad request: {message}", validationErrors, errorContext),
			401 => new HaloAuthenticationException($"Authentication failed: {message}", errorContext),
			403 => new HaloAuthorizationException($"Authorization failed: {message}", errorContext),
			404 => new HaloNotFoundException($"Resource not found: {message}",
				ExtractResourceTypeFromUrl(requestUrl), ExtractResourceIdFromUrl(requestUrl), errorContext),
			429 => new HaloRateLimitException($"Rate limit exceeded: {message}",
				ExtractRetryAfterSeconds(responseHeaders), null, null, null, errorContext),
			>= 500 => new HaloServerException($"Server error: {message}", errorContext),
			_ => new HaloApiException($"API error: {message}", errorContext)
		};

	/// <summary>
	/// Extracts validation errors from a JSON element's "errors" property
	/// </summary>
	/// <param name="rootElement">The root JSON element</param>
	/// <returns>A list of validation error strings, or null if none found</returns>
	public static ReadOnlyCollection<string>? ExtractValidationErrors(JsonElement rootElement)
	{
		if (rootElement.ValueKind != JsonValueKind.Object || !rootElement.TryGetProperty("errors", out var errorsElement))
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
	/// Extracts error details from JSON response
	/// </summary>
	/// <param name="element">The JSON element to extract from</param>
	/// <returns>Dictionary of error details</returns>
	public static Dictionary<string, object?> ExtractErrorDetails(JsonElement element)
	{
		var details = new Dictionary<string, object?>();

		if (element.ValueKind != JsonValueKind.Object)
		{
			return details;
		}

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
	public static string? ExtractResourceTypeFromUrl(string? url)
		=> ExtractApiSegment(url, offset: 1);

	/// <summary>
	/// Extracts resource ID from URL (e.g., the ID in /api/Tickets/123)
	/// </summary>
	/// <param name="url">The request URL</param>
	/// <returns>The resource ID, as an int where it parses as one, or null if not found</returns>
	public static object? ExtractResourceIdFromUrl(string? url)
	{
		var idSegment = ExtractApiSegment(url, offset: 2);
		if (idSegment is null)
		{
			return null;
		}

		return int.TryParse(idSegment, out var intId) ? intId : idSegment;
	}

	/// <summary>
	/// Returns the URL segment sitting <paramref name="offset"/> places after the "api/" segment,
	/// which is where Halo puts the resource type (offset 1) and the resource id (offset 2).
	/// </summary>
	private static string? ExtractApiSegment(string? url, int offset)
	{
		if (string.IsNullOrEmpty(url))
		{
			return null;
		}

		var segments = new Uri(url).Segments;

		for (var i = 0; i < segments.Length - offset; i++)
		{
			if (segments[i].Equals("api/", StringComparison.OrdinalIgnoreCase))
			{
				return segments[i + offset].TrimEnd('/');
			}
		}

		return null;
	}

	/// <summary>
	/// Extracts Retry-After header value in seconds
	/// </summary>
	/// <param name="headers">The response headers</param>
	/// <returns>Retry-After value in seconds or null</returns>
	public static int? ExtractRetryAfterSeconds(HttpHeaders? headers)
	{
		if (headers?.TryGetValues("Retry-After", out var values) == true)
		{
			var retryAfter = values.FirstOrDefault();
			if (int.TryParse(retryAfter, out var seconds))
			{
				return seconds;
			}
		}

		return null;
	}
}
