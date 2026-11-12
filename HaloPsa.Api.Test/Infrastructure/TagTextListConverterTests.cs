using AwesomeAssertions;
using HaloPsa.Api.Converters;
using HaloPsa.Api.Models.Tickets;
using System.Text.Json;
using static HaloPsa.Api.Test.Infrastructure.ListConverterTestSupport;

namespace HaloPsa.Api.Test.Infrastructure;

/// <summary>
/// Covers <see cref="TagTextListConverter"/>, which writes tags as <c>[{"text":t}]</c> and must reject anything
/// else when reading rather than quietly returning a partial list.
/// </summary>
public class TagTextListConverterTests
{
	private static readonly JsonSerializerOptions _options = new() { Converters = { new TagTextListConverter() } };

	private static IReadOnlyList<string> Read(string json) => ReadWith(new TagTextListConverter(), _options, json);

	[Fact]
	public void Write_ThenRead_RoundTrips()
		=> AssertRoundTrips<string>(Read, _options, ["b", "a", "c d"], """[{"text":"b"},{"text":"a"},{"text":"c d"}]""");

	[Fact]
	public void Read_WhenTheArrayIsEmpty_ReturnsNoTags()
		=> Read("[]").Should().BeEmpty();

	[Theory]
	[InlineData("null", "Null")]
	[InlineData("{}", "StartObject")]
	[InlineData("\"text\"", "String")]
	[InlineData("5", "Number")]
	public void Read_WhenTheTokenIsNotAnArray_Throws(string json, string token)
		=> AssertReadThrows(Read, json, $"Expected an array of tag objects, but found {token}.");

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
		=> AssertReadThrows(Read, json, "Expected an object with a string \"text\".");

	[Fact]
	public void Deserialize_ACreateRequestWithTheTagsFirst_ReadsTheTagsAndTheLaterProperties()
		=> AssertCreateReadsLaterProperties("""
			"tags":[{"text":"b"},{"text":"a"}]
			""", request => _ = request.Tags.Should().Equal("b", "a"));

	[Fact]
	public void Deserialize_AnUpdateRequestWithTheTagsFirst_ReadsTheTagsAndTheLaterProperties()
		=> AssertUpdateReadsLaterProperties("""
			"tags":[{"text":"b"},{"text":"a"}]
			""", request => _ = request.Tags.Should().Equal("b", "a"));
}
