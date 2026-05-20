using AwesomeAssertions;

namespace HaloPsa.Api.Test;

public class ClientTests
{
	[Fact]
	public void CreateClient_ValidCredentials_Succeeds()
		=> _ = new HaloClient(new HaloClientOptions
		{
			Account = "test",
			ClientId = "11111111-1111-1111-1111-111111111111",
			ClientSecret = "11111111-1111-1111-1111-111111111111-11111111-1111-1111-1111-111111111111"
		});

	[Fact]
	public void CreateClient_ValidCredentials_ExposesProperties()
	{
		// Arrange
		var options = new HaloClientOptions
		{
			Account = "test-account",
			ClientId = "22222222-2222-2222-2222-222222222222",
			ClientSecret = "11111111-1111-1111-1111-111111111111-11111111-1111-1111-1111-111111111111"
		};

		// Act
		var client = new HaloClient(options);

		// Assert
		_ = client.Account.Should().Be("test-account");
	}

	[Fact]
	public void HaloClientOptions_Properties_ReturnExpectedValues()
	{
		// Arrange & Act
		var options = new HaloClientOptions
		{
			Account = "my-account",
			ClientId = "33333333-3333-3333-3333-333333333333",
			ClientSecret = "44444444-4444-4444-4444-444444444444-55555555-5555-5555-5555-555555555555"
		};

		// Assert
		_ = options.Account.Should().Be("my-account");
		_ = options.ClientId.Should().Be("33333333-3333-3333-3333-333333333333");
		_ = options.ClientSecret.Should().Be("44444444-4444-4444-4444-444444444444-55555555-5555-5555-5555-555555555555");
	}

	[Fact]
	public void CreateClient_InvalidClientId_Throws()
	{
		Action act = () => _ = new HaloClient(new HaloClientOptions
		{
			Account = "test",
			ClientId = "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
			ClientSecret = "11111111-1111-1111-1111-111111111111-11111111-1111-1111-1111-111111111111"
		});
		_ = act.Should().ThrowExactly<FormatException>()
			.WithMessage("ClientId must be a valid GUID format (xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx).");
	}

	[Fact]
	public void CreateClient_NullOptions_ThrowsArgumentNullException()
	{
		Action act = () => _ = new HaloClient(null!);
		_ = act.Should().ThrowExactly<ArgumentNullException>()
			.WithMessage("Value cannot be null. (Parameter 'options')");
	}

	[Fact]
	public void CreateClient_NullHaloAccount_ThrowsArgumentException()
	{
		Action act = () => _ = new HaloClient(new HaloClientOptions
		{
			Account = null!,
			ClientId = "11111111-1111-1111-1111-111111111111",
			ClientSecret = "11111111-1111-1111-1111-111111111111-11111111-1111-1111-1111-111111111111"
		});
		_ = act.Should().ThrowExactly<ArgumentException>()
			.WithMessage("Account cannot be null or empty. (Parameter 'Account')");
	}

	[Fact]
	public void CreateClient_EmptyHaloAccount_ThrowsArgumentException()
	{
		Action act = () => _ = new HaloClient(new HaloClientOptions
		{
			Account = "",
			ClientId = "11111111-1111-1111-1111-111111111111",
			ClientSecret = "11111111-1111-1111-1111-111111111111-11111111-1111-1111-1111-111111111111"
		});
		_ = act.Should().ThrowExactly<ArgumentException>()
			.WithMessage("Account cannot be null or empty. (Parameter 'Account')");
	}

	[Fact]
	public void CreateClient_WhitespaceHaloAccount_ThrowsArgumentException()
	{
		Action act = () => _ = new HaloClient(new HaloClientOptions
		{
			Account = "   ",
			ClientId = "11111111-1111-1111-1111-111111111111",
			ClientSecret = "11111111-1111-1111-1111-111111111111-11111111-1111-1111-1111-111111111111"
		});
		_ = act.Should().ThrowExactly<ArgumentException>()
			.WithMessage("Account cannot be null or empty. (Parameter 'Account')");
	}

	[Fact]
	public void CreateClient_NullHaloClientId_ThrowsArgumentException()
	{
		Action act = () => _ = new HaloClient(new HaloClientOptions
		{
			Account = "test",
			ClientId = null!,
			ClientSecret = "11111111-1111-1111-1111-111111111111-11111111-1111-1111-1111-111111111111"
		});
		_ = act.Should().ThrowExactly<ArgumentException>()
			.WithMessage("ClientId cannot be null or empty. (Parameter 'ClientId')");
	}

	[Fact]
	public void CreateClient_EmptyHaloClientId_ThrowsArgumentException()
	{
		Action act = () => _ = new HaloClient(new HaloClientOptions
		{
			Account = "test",
			ClientId = "",
			ClientSecret = "11111111-1111-1111-1111-111111111111-11111111-1111-1111-1111-111111111111"
		});
		_ = act.Should().ThrowExactly<ArgumentException>()
			.WithMessage("ClientId cannot be null or empty. (Parameter 'ClientId')");
	}

	[Fact]
	public void CreateClient_InvalidHaloClientIdFormat_NoHyphens_ThrowsFormatException()
	{
		Action act = () => _ = new HaloClient(new HaloClientOptions
		{
			Account = "test",
			ClientId = "111111111111111111111111111111111111",
			ClientSecret = "11111111-1111-1111-1111-111111111111-11111111-1111-1111-1111-111111111111"
		});
		_ = act.Should().ThrowExactly<FormatException>()
			.WithMessage("ClientId must be a valid GUID format (xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx).");
	}

	[Fact]
	public void CreateClient_InvalidHaloClientIdFormat_WrongLength_ThrowsFormatException()
	{
		Action act = () => _ = new HaloClient(new HaloClientOptions
		{
			Account = "test",
			ClientId = "1111-1111-1111-1111-111111111111",
			ClientSecret = "11111111-1111-1111-1111-111111111111-11111111-1111-1111-1111-111111111111"
		});
		_ = act.Should().ThrowExactly<FormatException>()
			.WithMessage("ClientId must be a valid GUID format (xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx).");
	}

	[Fact]
	public void CreateClient_NullHaloClientSecret_ThrowsArgumentException()
	{
		Action act = () => _ = new HaloClient(new HaloClientOptions
		{
			Account = "test",
			ClientId = "11111111-1111-1111-1111-111111111111",
			ClientSecret = null!
		});
		_ = act.Should().ThrowExactly<ArgumentException>()
			.WithMessage("ClientSecret cannot be null or empty. (Parameter 'ClientSecret')");
	}

	[Fact]
	public void CreateClient_EmptyHaloClientSecret_ThrowsArgumentException()
	{
		Action act = () => _ = new HaloClient(new HaloClientOptions
		{
			Account = "test",
			ClientId = "11111111-1111-1111-1111-111111111111",
			ClientSecret = ""
		});
		_ = act.Should().ThrowExactly<ArgumentException>()
			.WithMessage("ClientSecret cannot be null or empty. (Parameter 'ClientSecret')");
	}

	[Fact]
	public void CreateClient_ValidCredentialsWithMixedCase_Succeeds()
		=> _ = new HaloClient(new HaloClientOptions
		{
			Account = "test",
			ClientId = "AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE",
			ClientSecret = "AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE-FFFFFFFF-1111-2222-3333-444444444444"
		});

	[Fact]
	public void CreateClient_ValidCredentialsWithLowerCase_Succeeds()
		=> _ = new HaloClient(new HaloClientOptions
		{
			Account = "test",
			ClientId = "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
			ClientSecret = "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee-ffffffff-1111-2222-3333-444444444444"
		});

	[Fact]
	public void CreateClient_ValidCredentialsWithBase64Secret_Succeeds()
		=> _ = new HaloClient(new HaloClientOptions
		{
			Account = "test",
			ClientId = "608b2936-4f6a-45c1-b95f-ec8613d571d9",
			ClientSecret = "FWEcJZhDzmRvp-OoIVCQV9zO0bTsLai7c0fdS4KyDak"
		});
}
