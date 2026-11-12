using AwesomeAssertions;
using HaloPsa.Api.Models.Tickets;
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

	internal static void AssertCreateReadsLaterProperties(string firstProperty, Action<CreateTicketRequest> assertFirst)
	{
		var request = JsonSerializer.Deserialize<CreateTicketRequest>($$"""{{{firstProperty}},"summary":"S","client_id":21,"user_id":9}""")!;

		assertFirst(request);
		_ = request.Summary.Should().Be("S");
		_ = request.ClientId.Should().Be(21);
		_ = request.UserId.Should().Be(9);
	}

	internal static void AssertUpdateReadsLaterProperties(string firstProperty, Action<UpdateTicketRequest> assertFirst)
	{
		var request = JsonSerializer.Deserialize<UpdateTicketRequest>($$"""{{{firstProperty}},"id":7,"summary":"S","status_id":2}""")!;

		assertFirst(request);
		_ = request.Id.Should().Be(7);
		_ = request.Summary.Should().Be("S");
		_ = request.Status.Should().Be(2);
	}
}
