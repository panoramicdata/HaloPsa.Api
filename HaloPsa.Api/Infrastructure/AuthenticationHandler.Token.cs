using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HaloPsa.Api.Infrastructure;

internal sealed partial class AuthenticationHandler
{
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
		using var formData = new FormUrlEncodedContent(new Dictionary<string, string>
		{
			["grant_type"] = "client_credentials",
			["client_id"] = _options.ClientId,
			["client_secret"] = _options.ClientSecret,
			["scope"] = "all"
		});

		using var response = await _authHttpClient.PostAsync("/auth/token", formData, cancellationToken).ConfigureAwait(false);

		if (!response.IsSuccessStatusCode)
		{
			await ThrowAuthenticationFailureAsync(response, cancellationToken).ConfigureAwait(false);
		}

		var responseContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
		LogTokenResponseIfEnabled(responseContent);
		return DeserializeTokenResponse(responseContent, response.StatusCode);
	}

	private async Task ThrowAuthenticationFailureAsync(
		HttpResponseMessage response,
		CancellationToken cancellationToken)
	{
		var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
		if (_options.Logger != null)
		{
			LogAuthFailed(_options.Logger, response.StatusCode, errorContent);
		}

		throw new AuthenticationException(
			$"Failed to obtain access token. Status: {response.StatusCode}, Content: {errorContent}");
	}

	private void LogTokenResponseIfEnabled(string responseContent)
	{
		if (_options.Logger?.IsEnabled(LogLevel.Debug) == true)
		{
			LogTokenResponse(_options.Logger, responseContent);
		}
	}

	private static TokenResponse DeserializeTokenResponse(
		string responseContent,
		System.Net.HttpStatusCode statusCode)
	{
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
				$"Failed to obtain access token: the Halo API token endpoint returned a non-JSON response. Status: {statusCode}, Content: {excerpt}");
		}

		return tokenResponse is null || string.IsNullOrEmpty(tokenResponse.AccessToken)
			? throw new AuthenticationException($"Invalid token response received from Halo API. Response: {responseContent}")
			: tokenResponse;
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
}
