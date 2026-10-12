using AwesomeAssertions;
using HaloPsa.Api.Models.CustomFields;
using System.Globalization;
using System.Text.Json;

namespace HaloPsa.Api.Test.Infrastructure;

/// <summary>
/// Covers <see cref="HaloPsa.Api.Converters.FlexibleStringConverter"/>, which exists because the Halo
/// specification leaves a custom field's <c>display</c> untyped, so one numeric or null display must not fail
/// a whole page of records.
/// </summary>
/// <remarks>
/// These use a plain <see cref="JsonSerializerOptions"/>. The package configures no serializer options of its
/// own - <c>PsaApi</c> builds its <c>RefitSettings</c> with only an exception factory, so Refit's default
/// System.Text.Json options apply. <see cref="Display_ThroughTheRealRefitClient_IsReadWithRefitsDefaultOptions"/>
/// proves the converter also works under those.
/// </remarks>
public class FlexibleStringConverterTests
{
	private static CustomFieldValue ReadField(string fieldJson)
		=> JsonSerializer.Deserialize<CustomFieldValue>(fieldJson)!;

	[Theory]
	[InlineData("\"cisco-sa-aaa\"", "cisco-sa-aaa")]
	[InlineData("\"\"", "")]
	[InlineData("42", "42")]
	[InlineData("21.0", "21.0")]
	[InlineData("-7", "-7")]
	[InlineData("1.5e3", "1.5e3")]
	[InlineData("true", "true")]
	[InlineData("false", "false")]
	[InlineData("null", null)]
	[InlineData("[1,2,3]", "[1,2,3]")]
	[InlineData("""{"a":1}""", """{"a":1}""")]
	public void Display_ReadsEveryJsonType(string rawDisplay, string? expected)
	{
		var field = ReadField($$"""{"id":1,"display":{{rawDisplay}}}""");

		_ = field.Display.Should().Be(expected);
	}

	[Fact]
	public void Display_WhenNumeric_IsNotAffectedByTheCurrentCulture()
	{
		var previous = CultureInfo.CurrentCulture;
		try
		{
			CultureInfo.CurrentCulture = new CultureInfo("de-DE");

			var field = ReadField("""{"id":1,"display":1234.5}""");

			_ = field.Display.Should().Be("1234.5");
		}
		finally
		{
			CultureInfo.CurrentCulture = previous;
		}
	}

	[Fact]
	public void Display_WhenAbsent_IsNull()
	{
		var field = ReadField("""{"id":1}""");

		_ = field.Display.Should().BeNull();
	}

	[Fact]
	public void Value_WhenJsonNull_ReadsAsNull_NotAJsonElementOfKindNull()
	{
		// JsonElement? is a nullable struct, and System.Text.Json maps a JSON null to the nullable's null
		// without asking the element to parse it. So "value": null and an absent value are indistinguishable.
		var field = ReadField("""{"id":1,"value":null,"display":null}""");

		_ = field.Value.Should().BeNull();
		_ = field.Display.Should().BeNull();
	}

	[Fact]
	public void Value_WhenPresent_StaysTheRawJsonElement()
	{
		var field = ReadField("""{"id":1,"value":[7,8],"display":7}""");

		_ = field.Value!.Value.ValueKind.Should().Be(JsonValueKind.Array);
		_ = field.Display.Should().Be("7");
	}

	[Fact]
	public void Display_WhenWritten_IsAJsonStringOrNull()
	{
		var withText = JsonSerializer.Serialize(new CustomFieldValue { Id = 1, Display = "x" });
		var withNull = JsonSerializer.Serialize(new CustomFieldValue { Id = 1, Display = null });

		_ = withText.Should().Contain("\"display\":\"x\"");
		_ = withNull.Should().Contain("\"display\":null");
	}

	[Fact]
	public async Task Display_ThroughTheRealRefitClient_IsReadWithRefitsDefaultOptions()
	{
		const string json = """
			{"tickets":[{"id":1,"summary":"s","status_id":1,"client_id":1,"user_id":1,
			"customfields":[
			  {"id":1,"name":"A","value":"x","display":42},
			  {"id":2,"name":"B","value":null,"display":null},
			  {"id":3,"name":"C","value":5,"display":true}
			]}],"record_count":1}
			""";
		var handler = new CapturingHandler(_ => json);
		var client = Refit.RestService.For<HaloPsa.Api.Interfaces.ITicketsApi>(
			new HttpClient(handler) { BaseAddress = new Uri("https://example.invalid") });

		var response = await client.GetAllAsync(CancellationToken.None);

		var fields = response.Tickets.Single().CustomFields!;
		_ = fields.Select(f => f.Display).Should().Equal("42", null, "true");
		_ = fields[0].Value!.Value.GetString().Should().Be("x");
		_ = fields[1].Value.Should().BeNull();
	}
}
