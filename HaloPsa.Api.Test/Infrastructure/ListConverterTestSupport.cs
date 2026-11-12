using AwesomeAssertions;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HaloPsa.Api.Test.Infrastructure;

/// <summary>
/// Shared helpers for the tests of the converters that read and write a list as an array of one-property objects
/// </summary>
internal static class ListConverterTestSupport
{
	// Reads through the converter directly: the serializer never calls it for a JSON null.
	internal static IReadOnlyList<T> ReadWith<T>(JsonConverter<IReadOnlyList<T>> converter, JsonSerializerOptions options, string json)
	{
		var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
		_ = reader.Read();

		return converter.Read(ref reader, typeof(IReadOnlyList<T>), options)!;
	}

	internal static void AssertReadThrows<T>(Func<string, IReadOnlyList<T>> read, string json, string expectedMessage)
	{
		var act = () => read(json);

		_ = act.Should().Throw<JsonException>().WithMessage(expectedMessage);
	}

	internal static void AssertRoundTrips<T>(Func<string, IReadOnlyList<T>> read, JsonSerializerOptions options, IReadOnlyList<T> values, string expectedJson)
	{
		var json = JsonSerializer.Serialize(values, options);

		_ = json.Should().Be(expectedJson);
		_ = read(json).Should().Equal(values);
	}
}
