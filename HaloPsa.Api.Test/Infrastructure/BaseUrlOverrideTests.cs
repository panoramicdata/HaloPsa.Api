using AwesomeAssertions;

namespace HaloPsa.Api.Test.Infrastructure;

/// <summary>
/// Covers <see cref="HaloClientOptions.BaseUrl"/>, which lets the client reach a Halo instance that is not at
/// the default <c>https://{Account}.halopsa.com</c> - an ITSM-branded tenant on <c>haloitsm.com</c>, or a
/// self-hosted instance on an arbitrary hostname.
/// </summary>
public class BaseUrlOverrideTests
{
	private const string ValidClientId = "00000000-0000-0000-0000-000000000001";
	private const string ValidClientSecret = "not-a-real-secret";

	[Fact]
	public void BaseUrl_WhenNotSet_DerivesTheHaloPsaAddressFromAccount()
	{
		var options = new HaloClientOptions
		{
			Account = "contoso",
			ClientId = ValidClientId,
			ClientSecret = ValidClientSecret
		};

		using var client = new HaloClient(options);

		_ = client.BaseUrl.Should().Be("https://contoso.halopsa.com");
	}

	[Fact]
	public void BaseUrl_WhenSet_IsUsedInsteadOfTheDerivedAddress()
	{
		var options = new HaloClientOptions
		{
			Account = "contosoitsm",
			BaseUrl = "https://contosoitsm.haloitsm.com",
			ClientId = ValidClientId,
			ClientSecret = ValidClientSecret
		};

		using var client = new HaloClient(options);

		_ = client.BaseUrl.Should().Be("https://contosoitsm.haloitsm.com");
	}

	[Fact]
	public void BaseUrl_WhenSetOnASelfHostedInstance_IsUsedVerbatim()
	{
		var options = new HaloClientOptions
		{
			Account = "internal",
			BaseUrl = "https://halo.internal.contoso.local",
			ClientId = ValidClientId,
			ClientSecret = ValidClientSecret
		};

		using var client = new HaloClient(options);

		_ = client.BaseUrl.Should().Be("https://halo.internal.contoso.local");
	}

	[Fact]
	public void BaseUrl_WithATrailingSlash_IsTrimmed()
	{
		// The API paths carry their own /api prefix and auth posts to /auth/token, both relative to this,
		// so a trailing slash would otherwise produce a doubled separator.
		var options = new HaloClientOptions
		{
			Account = "contosoitsm",
			BaseUrl = "https://contosoitsm.haloitsm.com/",
			ClientId = ValidClientId,
			ClientSecret = ValidClientSecret
		};

		using var client = new HaloClient(options);

		_ = client.BaseUrl.Should().Be("https://contosoitsm.haloitsm.com");
	}

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	public void BaseUrl_WhenBlank_FallsBackToTheDerivedAddress(string blank)
	{
		var options = new HaloClientOptions
		{
			Account = "contoso",
			BaseUrl = blank,
			ClientId = ValidClientId,
			ClientSecret = ValidClientSecret
		};

		using var client = new HaloClient(options);

		_ = client.BaseUrl.Should().Be("https://contoso.halopsa.com");
	}

	[Theory]
	[InlineData("contosoitsm.haloitsm.com")]
	[InlineData("/api")]
	public void BaseUrl_WhenNotAbsolute_Throws(string notAbsolute)
	{
		var options = new HaloClientOptions
		{
			Account = "contosoitsm",
			BaseUrl = notAbsolute,
			ClientId = ValidClientId,
			ClientSecret = ValidClientSecret
		};

		var act = () => new HaloClient(options);

		_ = act.Should().Throw<FormatException>().WithMessage("*absolute URL*");
	}

	[Fact]
	public void BaseUrl_WhenNotHttpOrHttps_Throws()
	{
		var options = new HaloClientOptions
		{
			Account = "contosoitsm",
			BaseUrl = "ftp://contosoitsm.haloitsm.com",
			ClientId = ValidClientId,
			ClientSecret = ValidClientSecret
		};

		var act = () => new HaloClient(options);

		_ = act.Should().Throw<FormatException>().WithMessage("*http or https*");
	}
}
