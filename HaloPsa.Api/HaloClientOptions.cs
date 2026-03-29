using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace HaloPsa.Api;

/// <summary>
/// Configuration options for the Halo API client
/// </summary>
public partial class HaloClientOptions
{
	private static readonly Regex _guidRegex = GetGuidRegex();
	private static readonly Regex _haloClientSecretRegex = GetHaloClientSecretRegex();

	/// <summary>
	/// Gets or sets the Halo account identifier
	/// </summary>
	public required string Account { get; init; }

	/// <summary>
	/// Gets or sets the Halo client ID (must be in GUID format)
	/// </summary>
	public required string ClientId { get; init; }

	/// <summary>
	/// Gets or sets the Halo client secret (must be in the format of two concatenated GUIDs)
	/// </summary>
	public required string ClientSecret { get; init; }

	/// <summary>
	/// Gets or sets the HTTP request timeout. Default is 30 seconds
	/// </summary>
	public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromSeconds(30);

	/// <summary>
	/// Gets or sets the maximum number of retry attempts for failed requests. Default is 3
	/// </summary>
	public int MaxRetryAttempts { get; init; } = 3;

	/// <summary>
	/// Gets or sets the initial delay between retry attempts. Default is 1 second
	/// </summary>
	public TimeSpan RetryDelay { get; init; } = TimeSpan.FromSeconds(1);

	/// <summary>
	/// Gets or sets the logger instance for HTTP operations. If null, no logging is performed
	/// </summary>
	public ILogger? Logger { get; init; }

	/// <summary>
	/// Gets or sets whether to log HTTP requests
	/// </summary>
	public bool EnableRequestLogging { get; init; }

	/// <summary>
	/// Gets or sets whether to log HTTP responses
	/// </summary>
	public bool EnableResponseLogging { get; init; }

	/// <summary>
	/// Gets or sets additional default headers to include with all requests
	/// </summary>
	public IReadOnlyDictionary<string, string> DefaultHeaders { get; init; } = new Dictionary<string, string>();

	/// <summary>
	/// Gets or sets whether to use exponential back-off for retries. Default is true
	/// </summary>
	public bool UseExponentialBackoff { get; init; } = true;

	/// <summary>
	/// Gets or sets the maximum retry delay when using exponential back-off. Default is 30 seconds
	/// </summary>
	public TimeSpan MaxRetryDelay { get; init; } = TimeSpan.FromSeconds(30);

	/// <summary>
	/// Gets the effective base URL for the Halo API
	/// </summary>
	internal string EffectiveBaseUrl => $"https://{Account}.halopsa.com";

	internal void Validate()
	{
		ValidateRequiredFields();
		ValidateFormats();
		ValidateTimings();
	}

	private void ValidateRequiredFields()
	{
		if (string.IsNullOrWhiteSpace(Account))
		{
			throw new ArgumentException("Account cannot be null or empty.", nameof(Account));
		}

		if (string.IsNullOrWhiteSpace(ClientId))
		{
			throw new ArgumentException("ClientId cannot be null or empty.", nameof(ClientId));
		}

		if (string.IsNullOrWhiteSpace(ClientSecret))
		{
			throw new ArgumentException("ClientSecret cannot be null or empty.", nameof(ClientSecret));
		}
	}

	private void ValidateFormats()
	{
		if (!_guidRegex.IsMatch(ClientId))
		{
			throw new FormatException("ClientId must be a valid GUID format (xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx).");
		}

		if (!_haloClientSecretRegex.IsMatch(ClientSecret))
		{
			throw new FormatException("ClientSecret must be in the format of two concatenated GUIDs (xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx-xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx).");
		}
	}

	private void ValidateTimings()
	{
		if (RequestTimeout <= TimeSpan.Zero)
		{
			throw new ArgumentException("RequestTimeout must be greater than zero.", nameof(RequestTimeout));
		}

		if (MaxRetryAttempts < 0)
		{
			throw new ArgumentException("MaxRetryAttempts cannot be negative.", nameof(MaxRetryAttempts));
		}

		if (RetryDelay < TimeSpan.Zero)
		{
			throw new ArgumentException("RetryDelay cannot be negative.", nameof(RetryDelay));
		}

		if (MaxRetryDelay < RetryDelay)
		{
			throw new ArgumentException("MaxRetryDelay must be greater than or equal to RetryDelay.", nameof(MaxRetryDelay));
		}
	}

	[GeneratedRegex(@"^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$", RegexOptions.Compiled)]
	private static partial Regex GetGuidRegex();

	[GeneratedRegex(@"^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}-[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$", RegexOptions.Compiled)]
	private static partial Regex GetHaloClientSecretRegex();
}