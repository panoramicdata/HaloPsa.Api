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
			Account = _fixture.Configuration["HaloApi:Account"] ?? throw new InvalidOperationException("HaloApi:Account not found"),
			ClientId = _fixture.Configuration["HaloApi:ClientId"] ?? throw new InvalidOperationException("HaloApi:ClientId not found"),
			ClientSecret = _fixture.Configuration["HaloApi:ClientSecret"] ?? throw new InvalidOperationException("HaloApi:ClientSecret not found")
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
			Account = _fixture.Configuration["HaloApi:Account"] ?? throw new InvalidOperationException("HaloApi:Account not found"),
			ClientId = _fixture.Configuration["HaloApi:ClientId"] ?? throw new InvalidOperationException("HaloApi:ClientId not found"),
			ClientSecret = _fixture.Configuration["HaloApi:ClientSecret"] ?? throw new InvalidOperationException("HaloApi:ClientSecret not found"),
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
	{
		// Arrange & Act & Assert
		Action act = () => new HaloClientOptions
		{
			Account = "",
			ClientId = Guid.NewGuid().ToString(),
			ClientSecret = $"{Guid.NewGuid()}-{Guid.NewGuid()}"
		}.Validate();

		_ = act.Should().Throw<ArgumentException>()
			.WithMessage("Account cannot be null or empty.*");
	}

	[Fact]
	public void HaloClientOptions_WithInvalidClientId_ThrowsFormatException()
	{
		// Arrange & Act & Assert
		Action act = () => new HaloClientOptions
		{
			Account = "test-account",
			ClientId = "invalid-guid",
			ClientSecret = $"{Guid.NewGuid()}-{Guid.NewGuid()}"
		}.Validate();

		_ = act.Should().Throw<FormatException>()
			.WithMessage("ClientId must be a valid GUID format*");
	}

	[Fact]
	public void HaloClientOptions_WithInvalidTimeout_ThrowsArgumentException()
	{
		// Arrange & Act & Assert
		Action act = () => new HaloClientOptions
		{
			Account = "test-account",
			ClientId = Guid.NewGuid().ToString(),
			ClientSecret = $"{Guid.NewGuid()}-{Guid.NewGuid()}",
			RequestTimeout = TimeSpan.Zero
		}.Validate();

		_ = act.Should().Throw<ArgumentException>()
			.WithMessage("RequestTimeout must be greater than zero.*");
	}
}