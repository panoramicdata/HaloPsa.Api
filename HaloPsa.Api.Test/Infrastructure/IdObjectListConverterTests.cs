using AwesomeAssertions;
using HaloPsa.Api.Converters;
using HaloPsa.Api.Models.Tickets;
using System.Text;
using System.Text.Json;

namespace HaloPsa.Api.Test.Infrastructure;

/// <summary>
/// Covers <see cref="IdObjectListConverter"/>, which writes ids as <c>[{"id":n}]</c> and must reject anything
/// else when reading rather than quietly returning a partial list.
/// </summary>
public class IdObjectListConverterTests
{
	private static readonly JsonSerializerOptions _options = new() { Converters = { new IdObjectListConverter() } };

	// Reads through the converter directly: the serializer never calls it for a JSON null.
	private static IReadOnlyList<int> Read(string json)
	{
		var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
		_ = reader.Read();

		return new IdObjectListConverter().Read(ref reader, typeof(IReadOnlyList<int>), _options);
	}

	[Fact]
	public void Write_ThenRead_RoundTrips()
	{
		var json = JsonSerializer.Serialize<IReadOnlyList<int>>([3, 1, 2], _options);

		_ = json.Should().Be("""[{"id":3},{"id":1},{"id":2}]""");
		_ = Read(json).Should().Equal(3, 1, 2);
	}

	[Fact]
	public void Read_WhenTheArrayIsEmpty_ReturnsNoIds()
		=> Read("[]").Should().BeEmpty();

	[Theory]
	[InlineData("null", "Null")]
	[InlineData("{}", "StartObject")]
	[InlineData("\"id\"", "String")]
	[InlineData("5", "Number")]
	public void Read_WhenTheTokenIsNotAnArray_Throws(string json, string token)
	{
		var act = () => Read(json);

		_ = act.Should().Throw<JsonException>().WithMessage($"Expected an array of id objects, but found {token}.");
	}

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
	{
		var act = () => Read(json);

		_ = act.Should().Throw<JsonException>().WithMessage("Expected an object with an integer \"id\".");
	}

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
