using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace HaloPsa.Api.Test.Infrastructure;

public class AuthenticationTests
{
	[Fact]
	public void HaloClientOptions_WithValidCredentials_ValidatesSuccessfully()
	{
		// Arrange
		var options = new HaloClientOptions
		{
			Account = "testaccount",
			ClientId = "550e8400-e29b-41d4-a716-446655440000",
			ClientSecret = "550e8400-e29b-41d4-a716-446655440000-123e4567-e89b-12d3-a456-426614174000"
		};

		// Act & Assert - Should not throw
		options.Validate();
	}

	[Fact]
	public void HaloClientOptions_WithInvalidClientId_ThrowsFormatException()
	{
		// Arrange
		var options = new HaloClientOptions
		{
			Account = "testaccount",
			ClientId = "invalid-guid",
			ClientSecret = "550e8400-e29b-41d4-a716-446655440000-123e4567-e89b-12d3-a456-426614174000"
		};

		// Act & Assert
		var act = options.Validate;
		_ = act.Should().Throw<FormatException>()
			.WithMessage("*ClientId must be a valid GUID format*");
	}

	[Fact]
	public void AuthenticationHandler_WithValidCredentials_ShouldValidateOptions()
	{
		// Arrange
		var options = new HaloClientOptions
		{
			Account = "testaccount",
			ClientId = "550e8400-e29b-41d4-a716-446655440000",
			ClientSecret = "550e8400-e29b-41d4-a716-446655440000-123e4567-e89b-12d3-a456-426614174000",
			Logger = NullLogger.Instance
		};

		// Act & Assert - Verify the options are valid
		options.Validate();
		_ = options.Should().NotBeNull();
		_ = options.Account.Should().Be("testaccount");
	}
}