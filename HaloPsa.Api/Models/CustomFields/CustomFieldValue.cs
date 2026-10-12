using HaloPsa.Api.Converters;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HaloPsa.Api.Models.CustomFields;

/// <summary>
/// The value of one custom field on a Halo record, as returned in the record's <c>customfields</c> array
/// </summary>
/// <remarks>
/// Halo returns many more keys per custom field (the field's definition); only the ones needed to
/// identify the field and read its value are modelled, and the rest are ignored.
/// </remarks>
public record CustomFieldValue
{
	/// <summary>
	/// The ID of the custom field definition
	/// </summary>
	/// <remarks>
	/// Halo sometimes serialises this as a decimal (<c>21.0</c>), so it needs <see cref="FlexibleInt32Converter"/>.
	/// </remarks>
	[JsonPropertyName("id")]
	[JsonConverter(typeof(FlexibleInt32Converter))]
	public int Id { get; init; }

	/// <summary>
	/// The internal name of the custom field
	/// </summary>
	[JsonPropertyName("name")]
	public string? Name { get; init; }

	/// <summary>
	/// The label shown to users for the custom field
	/// </summary>
	[JsonPropertyName("label")]
	public string? Label { get; init; }

	/// <summary>
	/// The raw value of the custom field
	/// </summary>
	/// <remarks>
	/// The Halo specification gives <c>value</c> no type, and its JSON type depends on the field
	/// (a string, a number, an array of ids), so it is kept as a <see cref="JsonElement"/>.
	/// A JSON <c>null</c> reads as <see langword="null"/> rather than a <see cref="JsonElement"/> of kind
	/// <see cref="JsonValueKind.Null"/>, because that is how System.Text.Json treats a nullable struct.
	/// </remarks>
	[JsonPropertyName("value")]
	public JsonElement? Value { get; init; }

	/// <summary>
	/// The display text of the custom field value
	/// </summary>
	/// <remarks>
	/// The Halo specification does not give <c>display</c> a type, so it is read with
	/// <see cref="FlexibleStringConverter"/>: one numeric, boolean or null display does not fail a whole page.
	/// </remarks>
	[JsonPropertyName("display")]
	[JsonConverter(typeof(FlexibleStringConverter))]
	public string? Display { get; init; }
}
