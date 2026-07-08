using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HaloPsa.Api.Infrastructure;

/// <summary>
/// HTTP message handler that manages OAuth2 authentication for Halo API requests using client credentials flow
/// </summary>
internal sealed partial class AuthenticationHandler : DelegatingHandler
{
	private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
	{
		PropertyNameCaseInsensitive = true
	};

	private readonly HaloClientOptions _options;
	private readonly SemaphoreSlim _tokenSemaphore = new(1, 1);
	private readonly HttpClient _authHttpClient; // Separate client for auth requests
	private string? _accessToken;
	private DateTime _tokenExpiry = DateTime.MinValue;

	public AuthenticationHandler(HaloClientOptions options)
	{
		_options = options ?? throw new ArgumentNullException(nameof(options));

		// Create a separate HttpClient for authentication requests
		// This avoids circular dependencies with the main client
		_authHttpClient = new HttpClient
		{
			BaseAddress = new Uri(_options.EffectiveBaseUrl),
			Timeout = _options.RequestTimeout
		};
	}

	protected override async Task<HttpResponseMessage> SendAsync(
		HttpRequestMessage request,
		CancellationToken cancellationToken)
	{
		// Don't intercept auth requests to avoid infinite loops
		if (request.RequestUri?.PathAndQuery.StartsWith("/auth", StringComparison.OrdinalIgnoreCase) == true)
		{
			return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
		}

		// Ensure we have a valid access token
		await EnsureValidTokenAsync(cancellationToken).ConfigureAwait(false);

		// Add the authorization header
		if (!string.IsNullOrEmpty(_accessToken))
		{
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
		}

		var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

		// If we get a 401, try to refresh the token once
		if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized && !string.IsNullOrEmpty(_accessToken))
		{
			if (_options.Logger != null)
			{
				LogUnauthorizedRefresh(_options.Logger);
			}

			// Clear the current token and get a new one
			_accessToken = null;
			_tokenExpiry = DateTime.MinValue;

			await EnsureValidTokenAsync(cancellationToken).ConfigureAwait(false);

			// Create a new request message for retry (HttpRequestMessage can only be sent once)
			if (!string.IsNullOrEmpty(_accessToken))
			{
				var retryRequest = await CloneHttpRequestMessageAsync(request);
				retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

				response.Dispose(); // Dispose the 401 response
				response = await base.SendAsync(retryRequest, cancellationToken).ConfigureAwait(false);
			}
		}

		return response;
	}

	/// <summary>
	/// Clones an HttpRequestMessage for retry purposes since HttpRequestMessage can only be sent once
	/// </summary>
	private static async Task<HttpRequestMessage> CloneHttpRequestMessageAsync(HttpRequestMessage original)
	{
		var clone = new HttpRequestMessage(original.Method, original.RequestUri);

		// Copy headers
		foreach (var header in original.Headers)
		{
			_ = clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
		}

		// Copy content if present
		if (original.Content != null)
		{
			var contentBytes = await original.Content.ReadAsByteArrayAsync();
			clone.Content = new ByteArrayContent(contentBytes);

			// Copy content headers
			foreach (var header in original.Content.Headers)
			{
				_ = clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
			}
		}

		// Copy other properties
		clone.Version = original.Version;
		foreach (var property in original.Options)
		{
			clone.Options.Set(new HttpRequestOptionsKey<object?>(property.Key), property.Value);
		}

		return clone;
	}

	private async Task EnsureValidTokenAsync(CancellationToken cancellationToken)
	{
		// Check if we need a new token (with 5-minute buffer)
		if (!string.IsNullOrEmpty(_accessToken) && _tokenExpiry > DateTime.UtcNow.AddMinutes(5))
		{
			return;
		}

		await _tokenSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
		try
		{
			// Double-check after acquiring the lock
			if (!string.IsNullOrEmpty(_accessToken) && _tokenExpiry > DateTime.UtcNow.AddMinutes(5))
			{
				return;
			}

			await RefreshTokenAsync(cancellationToken).ConfigureAwait(false);
		}
		finally
		{
			_ = _tokenSemaphore.Release();
		}
	}

	private async Task RefreshTokenAsync(CancellationToken cancellationToken)
	{
		if (_options.Logger != null)
		{
			LogRefreshingToken(_options.Logger);
		}

		try
		{
			var tokenResponse = await RequestTokenAsync(cancellationToken).ConfigureAwait(false);
			_accessToken = tokenResponse.AccessToken;
			_tokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn - 60); // 60-second buffer

			if (_options.Logger?.IsEnabled(LogLevel.Debug) == true)
			{
				LogTokenRefreshed(_options.Logger, _tokenExpiry);
			}
		}
		catch (Exception ex) when (ex is not AuthenticationException)
		{
			if (_options.Logger != null)
			{
				LogTokenRefreshFailed(_options.Logger, ex);
			}

			throw new AuthenticationException("Failed to obtain access token from Halo API", ex);
		}
	}

	private async Task<TokenResponse> RequestTokenAsync(CancellationToken cancellationToken)
	{
		var formData = new FormUrlEncodedContent(new Dictionary<string, string>
		{
			["grant_type"] = "client_credentials",
			["client_id"] = _options.ClientId,
			["client_secret"] = _options.ClientSecret,
			["scope"] = "all"
		});

		var response = await _authHttpClient.PostAsync("/auth/token", formData, cancellationToken).ConfigureAwait(false);

		if (!response.IsSuccessStatusCode)
		{
			var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
			if (_options.Logger != null)
			{
				LogAuthFailed(_options.Logger, response.StatusCode, errorContent);
			}

			throw new AuthenticationException(
				$"Failed to obtain access token. Status: {response.StatusCode}, Content: {errorContent}");
		}

		var responseContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

		if (_options.Logger?.IsEnabled(LogLevel.Debug) == true)
		{
			LogTokenResponse(_options.Logger, responseContent);
		}

		TokenResponse? tokenResponse;
		try
		{
			tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseContent, _jsonSerializerOptions);
		}
		catch (JsonException)
		{
			// The token endpoint can return a 2xx with a non-JSON (e.g. HTML login/error page) body.
			// Raise a clear, specific AuthenticationException here (rather than letting the raw JsonException
			// propagate) so RefreshTokenAsync's "ex is not AuthenticationException" filter skips its Error log.
			var excerpt = responseContent.Length > 200 ? responseContent[..200] + "..." : responseContent;
			throw new AuthenticationException(
				$"Failed to obtain access token: the Halo API token endpoint returned a non-JSON response. Status: {response.StatusCode}, Content: {excerpt}");
		}

		return tokenResponse is null || string.IsNullOrEmpty(tokenResponse.AccessToken)
			? throw new AuthenticationException($"Invalid token response received from Halo API. Response: {responseContent}")
			: tokenResponse;
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			_tokenSemaphore.Dispose();
			_authHttpClient.Dispose();
		}

		base.Dispose(disposing);
	}

	private sealed record TokenResponse
	{
		[JsonPropertyName("access_token")]
		public string AccessToken { get; init; } = "";

		[JsonPropertyName("token_type")]
		public string TokenType { get; init; } = "";

		[JsonPropertyName("expires_in")]
		public int ExpiresIn { get; init; }
	}

	[LoggerMessage(LogLevel.Warning, "Received 401 Unauthorized, attempting to refresh token")]
	private static partial void LogUnauthorizedRefresh(ILogger logger);

	[LoggerMessage(LogLevel.Debug, "Refreshing Halo API access token using client credentials")]
	private static partial void LogRefreshingToken(ILogger logger);

	[LoggerMessage(LogLevel.Debug, "Successfully refreshed Halo API access token, expires at {Expiry}")]
	private static partial void LogTokenRefreshed(ILogger logger, DateTime expiry);

	[LoggerMessage(LogLevel.Error, "Failed to refresh Halo API access token")]
	private static partial void LogTokenRefreshFailed(ILogger logger, Exception ex);

	[LoggerMessage(LogLevel.Error, "Authentication failed: {StatusCode} - {Content}")]
	private static partial void LogAuthFailed(ILogger logger, System.Net.HttpStatusCode statusCode, string content);

	[LoggerMessage(LogLevel.Debug, "Token response: {Response}")]
	private static partial void LogTokenResponse(ILogger logger, string response);
}

/// <summary>
/// Exception thrown when authentication with the Halo API fails
/// </summary>
public sealed class AuthenticationException : Exception
{
	/// <summary>
	/// Initializes a new instance of the AuthenticationException class with a specified error message
	/// </summary>
	/// <param name="message">The message that describes the error</param>
	public AuthenticationException(string message) : base(message) { }

	/// <summary>
	/// Initializes a new instance of the AuthenticationException class with a specified error message and a reference to the inner exception
	/// </summary>
	/// <param name="message">The message that describes the error</param>
	/// <param name="innerException">The exception that is the cause of the current exception</param>
	public AuthenticationException(string message, Exception innerException) : base(message, innerException) { }
}