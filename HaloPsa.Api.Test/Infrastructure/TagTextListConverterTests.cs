using AwesomeAssertions;
using HaloPsa.Api.Converters;
using HaloPsa.Api.Models.Tickets;
using System.Text;
using System.Text.Json;

namespace HaloPsa.Api.Test.Infrastructure;

/// <summary>
/// Covers <see cref="TagTextListConverter"/>, which writes tags as <c>[{"text":t}]</c> and must reject anything
/// else when reading rather than quietly returning a partial list.
/// </summary>
public class TagTextListConverterTests
{
	private static readonly JsonSerializerOptions _options = new() { Converters = { new TagTextListConverter() } };

	// Reads through the converter directly: the serializer never calls it for a JSON null.
	private static IReadOnlyList<string> Read(string json)
	{
		var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
		_ = reader.Read();

		return new TagTextListConverter().Read(ref reader, typeof(IReadOnlyList<string>), _options);
	}

	[Fact]
	public void Write_ThenRead_RoundTrips()
	{
		var json = JsonSerializer.Serialize<IReadOnlyList<string>>(["b", "a", "c d"], _options);

		_ = json.Should().Be("""[{"text":"b"},{"text":"a"},{"text":"c d"}]""");
		_ = Read(json).Should().Equal("b", "a", "c d");
	}

	[Fact]
	public void Read_WhenTheArrayIsEmpty_ReturnsNoTags()
		=> Read("[]").Should().BeEmpty();

	[Theory]
	[InlineData("null", "Null")]
	[InlineData("{}", "StartObject")]
	[InlineData("\"text\"", "String")]
	[InlineData("5", "Number")]
	public void Read_WhenTheTokenIsNotAnArray_Throws(string json, string token)
	{
		var act = () => Read(json);

		_ = act.Should().Throw<JsonException>().WithMessage($"Expected an array of tag objects, but found {token}.");
	}

	[Theory]
	[InlineData("[1]")]
	[InlineData("[\"text\"]")]
	[InlineData("[null]")]
	[InlineData("[[]]")]
	[InlineData("[{}]")]
	[InlineData("""[{"id":1}]""")]
	[InlineData("""[{"text":"a"},{}]""")]
	[InlineData("""[{"text":5}]""")]
	[InlineData("""[{"text":null}]""")]
	[InlineData("""[{"text":["a"]}]""")]
	public void Read_WhenAnItemIsNotAnObjectWithAStringText_Throws(string json)
	{
		var act = () => Read(json);

		_ = act.Should().Throw<JsonException>().WithMessage("Expected an object with a string \"text\".");
	}

	[Fact]
	public void Deserialize_ACreateRequestWithTheTagsFirst_ReadsTheTagsAndTheLaterProperties()
	{
		var request = JsonSerializer.Deserialize<CreateTicketRequest>("""{"tags":[{"text":"b"},{"text":"a"}],"summary":"S","client_id":21,"user_id":9}""")!;

		_ = request.Tags.Should().Equal("b", "a");
		_ = request.Summary.Should().Be("S");
		_ = request.ClientId.Should().Be(21);
		_ = request.UserId.Should().Be(9);
	}

	[Fact]
	public void Deserialize_AnUpdateRequestWithTheTagsFirst_ReadsTheTagsAndTheLaterProperties()
	{
		var request = JsonSerializer.Deserialize<UpdateTicketRequest>("""{"tags":[{"text":"b"},{"text":"a"}],"id":7,"summary":"S","status_id":2}""")!;

		_ = request.Tags.Should().Equal("b", "a");
		_ = request.Id.Should().Be(7);
		_ = request.Summary.Should().Be("S");
		_ = request.Status.Should().Be(2);
	}
}
