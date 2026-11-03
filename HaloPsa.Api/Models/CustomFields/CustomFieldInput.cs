using System.Text.Json.Serialization;

namespace HaloPsa.Api.Models.CustomFields;

/// <summary>
/// One custom field value to write, by field id
/// </summary>
public record CustomFieldInput
{
	/// <summary>
	/// The ID of the custom field definition
	/// </summary>
	[JsonPropertyName("id")]
	public required int Id { get; init; }

	/// <summary>
	/// The value to write; its JSON type depends on the field
	/// </summary>
	[JsonPropertyName("value")]
	public object? Value { get; init; }
}
