using HaloPsa.Api.Http;
using HaloPsa.Api.Interfaces;

namespace HaloPsa.Api;

/// <summary>
/// Client for interacting with the Halo API
/// </summary>
public class HaloClient : IHaloClient, IDisposable
{
	private readonly HaloClientOptions _options;
	private readonly HttpClient _httpClient;
	private bool _disposed;

	/// <summary>
	/// Initializes a new instance of the HaloClient
	/// </summary>
	/// <param name="options">Configuration options for the client</param>
	public HaloClient(HaloClientOptions options)
	{
		ArgumentNullException.ThrowIfNull(options);
		options.Validate();

		_options = options;
		Account = options.Account;
		_httpClient = DefaultHttpClientFactory.Create(options);

		// Initialize API modules lazily
		Psa = new Lazy<IPsaApi>(() => new PsaApi(_httpClient, _options.IsReadOnly)).Value;
		ServiceDesk = new Lazy<IServiceDeskApi>(() => new ServiceDeskApi(_httpClient)).Value;
		System = new Lazy<ISystemApi>(() => new SystemApi(_httpClient)).Value;
	}

	/// <summary>
	/// Gets the PSA (Professional Services Automation) API module
	/// </summary>
	public IPsaApi Psa { get; private set; }

	/// <summary>
	/// Gets the ServiceDesk API module
	/// </summary>
	public IServiceDeskApi ServiceDesk { get; private set; }

	/// <summary>
	/// Gets the System API module for configuration and administration
	/// </summary>
	public ISystemApi System { get; private set; }

	/// <summary>
	/// Gets the Halo account identifier
	/// </summary>
	public string Account { get; }

	/// <summary>
	/// Gets the base URL for the Halo API
	/// </summary>
	public string BaseUrl => _options.EffectiveBaseUrl;

	internal HttpClient HttpClient => _httpClient;

	/// <summary>
	/// Disposes the HTTP client and releases resources
	/// </summary>
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	/// <summary>
	/// Protected dispose method
	/// </summary>
	/// <param name="disposing">True if disposing managed resources</param>
	protected virtual void Dispose(bool disposing)
	{
		if (!_disposed && disposing)
		{
			_httpClient?.Dispose();
			_disposed = true;
		}
	}
}
