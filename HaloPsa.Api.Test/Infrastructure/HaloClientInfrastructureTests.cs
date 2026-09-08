using AwesomeAssertions;

namespace HaloPsa.Api.Test.Infrastructure;

[Collection("Integration Tests")]
public class HaloClientInfrastructureTests(IntegrationTestFixture fixture)
{
	private readonly IntegrationTestFixture _fixture = fixture;

	[Fact]
	public void HaloClient_WithValidOptions_CanBeInstantiated()
	{
		// Arrange
		var options = new HaloClientOptions
		{
			Account = Account,
			ClientId = ClientId,
			ClientSecret = ClientSecret
		};

		// Act
		using var client = new HaloClient(options);

		// Assert
		_ = client.Should().NotBeNull();
		_ = client.Account.Should().Be(options.Account);
		_ = client.BaseUrl.Should().NotBeNullOrEmpty();
		_ = client.Psa.Should().NotBeNull();
		_ = client.ServiceDesk.Should().NotBeNull();
		_ = client.System.Should().NotBeNull();
	}

	[Fact]
	public void HaloClient_WithExtendedOptions_CanBeInstantiated()
	{
		// Arrange
		var options = new HaloClientOptions
		{
			Account = Account,
			ClientId = ClientId,
			ClientSecret = ClientSecret,
			RequestTimeout = TimeSpan.FromSeconds(60),
			MaxRetryAttempts = 5,
			RetryDelay = TimeSpan.FromSeconds(2),
			EnableRequestLogging = true,
			EnableResponseLogging = true,
			Logger = _fixture.Logger
		};

		// Act
		using var client = new HaloClient(options);

		// Assert
		_ = client.Should().NotBeNull();
		_ = client.Account.Should().Be(options.Account);
		_ = client.BaseUrl.Should().Contain(options.Account);
	}

	[Fact]
	public void HaloClientOptions_WithInvalidAccount_ThrowsArgumentException()
		=> AssertValidationFails<ArgumentException>(
			OptionsWith(account: ""),
			"Account cannot be null or empty.*");

	[Fact]
	public void HaloClientOptions_WithInvalidClientId_ThrowsFormatException()
		=> AssertValidationFails<FormatException>(
			OptionsWith(clientId: "invalid-guid"),
			"ClientId must be a valid GUID format*");

	[Fact]
	public void HaloClientOptions_WithInvalidTimeout_ThrowsArgumentException()
		=> AssertValidationFails<ArgumentException>(
			OptionsWith(requestTimeout: TimeSpan.Zero),
			"RequestTimeout must be greater than zero.*");

	private string Account => RequiredSetting("HaloApi:Account");

	private string ClientId => RequiredSetting("HaloApi:ClientId");

	private string ClientSecret => RequiredSetting("HaloApi:ClientSecret");

	/// <summary>
	/// Reads a setting the test cannot run without, failing with the key name rather than a
	/// null-reference further down.
	/// </summary>
	private string RequiredSetting(string key)
		=> _fixture.Configuration[key] ?? throw new InvalidOperationException($"{key} not found");

	/// <summary>
	/// Builds options that pass validation, with one field overridden to whatever the caller wants
	/// to prove invalid. The defaults mirror <see cref="HaloClientOptions"/>'s own, so only the
	/// overridden field is under test.
	/// </summary>
	private static HaloClientOptions OptionsWith(
		string account = "test-account",
		string? clientId = null,
		TimeSpan? requestTimeout = null)
		=> new()
		{
			Account = account,
			ClientId = clientId ?? Guid.NewGuid().ToString(),
			ClientSecret = $"{Guid.NewGuid()}-{Guid.NewGuid()}",
			RequestTimeout = requestTimeout ?? TimeSpan.FromSeconds(30)
		};

	/// <summary>
	/// Asserts that validating <paramref name="options"/> fails in the expected way.
	/// </summary>
	private static void AssertValidationFails<TException>(HaloClientOptions options, string expectedMessagePattern)
		where TException : Exception
		=> _ = ((Action)options.Validate).Should()
			.Throw<TException>()
			.WithMessage(expectedMessagePattern);
}
