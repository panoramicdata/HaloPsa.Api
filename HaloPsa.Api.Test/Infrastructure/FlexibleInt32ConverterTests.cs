using AwesomeAssertions;
using HaloPsa.Api.Models.Sites;
using System.Text.Json;

namespace HaloPsa.Api.Test.Infrastructure;

/// <summary>
/// Covers <see cref="HaloPsa.Api.Converters.FlexibleInt32Converter"/>, which exists because Halo serialises
/// several integer id fields with a decimal point - <c>GET /api/Site</c> returns <c>"client_id": 21.0</c>.
/// Without it the default int handling rejects the value and the whole response fails to deserialise.
/// </summary>
public class FlexibleInt32ConverterTests
{
	[Theory]
	[InlineData("21", 21)]
	[InlineData("21.0", 21)]
	[InlineData("21.00", 21)]
	[InlineData("0", 0)]
	[InlineData("0.0", 0)]
	public void ClientId_WhenANumber_IsRead(string raw, int expected)
	{
		var site = JsonSerializer.Deserialize<Site>($$"""{"id":1,"client_id":{{raw}}}""");

		_ = site.Should().NotBeNull();
		_ = site!.ClientId.Should().Be(expected);
	}

	[Fact]
	public void ClientId_WhenTheHaloUnsetSentinel_IsPassedThroughUnchanged()
	{
		// Halo writes -1, not 0 or null, when an integration id is not set. Deciding what that means is
		// the caller's job, so the converter must not quietly turn it into 0.
		var site = JsonSerializer.Deserialize<Site>("""{"id":1,"client_id":-1}""");

		_ = site!.ClientId.Should().Be(-1);
	}

	[Theory]
	[InlineData("\"21\"", 21)]
	[InlineData("\"21.0\"", 21)]
	public void ClientId_WhenAString_IsStillRead(string raw, int expected)
	{
		// GET /api/Priority returns its ids as strings, so a string has to be tolerated too.
		var site = JsonSerializer.Deserialize<Site>($$"""{"id":1,"client_id":{{raw}}}""");

		_ = site!.ClientId.Should().Be(expected);
	}

	[Theory]
	[InlineData("null")]
	[InlineData("\"\"")]
	public void ClientId_WhenNullOrEmpty_IsZero(string raw)
	{
		var site = JsonSerializer.Deserialize<Site>($$"""{"id":1,"client_id":{{raw}}}""");

		_ = site!.ClientId.Should().Be(0);
	}

	[Fact]
	public void ClientId_WhenNotANumberAtAll_Throws()
	{
		var act = () => JsonSerializer.Deserialize<Site>("""{"id":1,"client_id":"not-a-number"}""");

		_ = act.Should().Throw<JsonException>();
	}

	[Fact]
	public void ClientId_RoundTrips()
	{
		var json = JsonSerializer.Serialize(new Site { Id = 1, ClientId = 21 });

		_ = json.Should().Contain("\"client_id\":21");
		_ = JsonSerializer.Deserialize<Site>(json)!.ClientId.Should().Be(21);
	}

	[Fact]
	public void SitesResponse_WithDecimalClientIds_Deserialises()
	{
		// The shape GET /api/Site actually returns, which failed outright before the converter.
		const string Json = """{"record_count":2,"sites":[{"id":33,"client_id":21.0},{"id":40,"client_id":22.0}]}""";

		var response = JsonSerializer.Deserialize<SitesResponse>(Json);

		_ = response.Should().NotBeNull();
		_ = response!.Sites.Should().HaveCount(2);
		_ = response.Sites[0].ClientId.Should().Be(21);
		_ = response.Sites[1].ClientId.Should().Be(22);
	}
}
