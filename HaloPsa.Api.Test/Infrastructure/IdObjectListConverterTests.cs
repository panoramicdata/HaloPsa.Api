using AwesomeAssertions;
using HaloPsa.Api.Converters;
using HaloPsa.Api.Models.Tickets;
using System.Text.Json;
using static HaloPsa.Api.Test.Infrastructure.ListConverterTestSupport;

namespace HaloPsa.Api.Test.Infrastructure;

/// <summary>
/// Covers <see cref="IdObjectListConverter"/>, which writes ids as <c>[{"id":n}]</c> and must reject anything
/// else when reading rather than quietly returning a partial list.
/// </summary>
public class IdObjectListConverterTests
{
	private static readonly JsonSerializerOptions _options = new() { Converters = { new IdObjectListConverter() } };

	private static IReadOnlyList<int> Read(string json) => ReadWith(new IdObjectListConverter(), _options, json);

	[Fact]
	public void Write_ThenRead_RoundTrips()
		=> AssertRoundTrips<int>(Read, _options, [3, 1, 2], """[{"id":3},{"id":1},{"id":2}]""");

	[Fact]
	public void Read_WhenTheArrayIsEmpty_ReturnsNoIds()
		=> Read("[]").Should().BeEmpty();

	[Theory]
	[InlineData("null", "Null")]
	[InlineData("{}", "StartObject")]
	[InlineData("\"id\"", "String")]
	[InlineData("5", "Number")]
	public void Read_WhenTheTokenIsNotAnArray_Throws(string json, string token)
		=> AssertReadThrows(Read, json, $"Expected an array of id objects, but found {token}.");

	[Theory]
	[InlineData("[1]")]
	[InlineData("[\"id\"]")]
	[InlineData("[null]")]
	[InlineData("[[]]")]
	[InlineData("[{}]")]
	[InlineData("""[{"text":"a"}]""")]
	[InlineData("""[{"id":1},{}]""")]
	[InlineData("""[{"id":"1"}]""")]
	[InlineData("""[{"id":1.5}]""")]
	[InlineData("""[{"id":null}]""")]
	public void Read_WhenAnItemIsNotAnObjectWithAnIntegerId_Throws(string json)
		=> AssertReadThrows(Read, json, "Expected an object with an integer \"id\".");

	[Fact]
	public void Deserialize_ACreateRequestWithTheAssetsFirst_ReadsTheAssetsAndTheLaterProperties()
	{
		var request = JsonSerializer.Deserialize<CreateTicketRequest>("""{"assets":[{"id":3},{"id":1}],"summary":"S","client_id":21,"user_id":9}""")!;

		_ = request.AssetIds.Should().Equal(3, 1);
		_ = request.Summary.Should().Be("S");
		_ = request.ClientId.Should().Be(21);
		_ = request.UserId.Should().Be(9);
	}

	[Fact]
	public void Deserialize_AnUpdateRequestWithTheAssetsFirst_ReadsTheAssetsAndTheLaterProperties()
	{
		var request = JsonSerializer.Deserialize<UpdateTicketRequest>("""{"assets":[{"id":3},{"id":1}],"id":7,"summary":"S","status_id":2}""")!;

		_ = request.AssetIds.Should().Equal(3, 1);
		_ = request.Id.Should().Be(7);
		_ = request.Summary.Should().Be("S");
		_ = request.Status.Should().Be(2);
	}
}
