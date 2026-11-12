using HaloPsa.Api.Converters;
using System.Text.Json.Serialization;

namespace HaloPsa.Api.Models.Categories;

/// <summary>
/// Response wrapper for category list operations
/// </summary>
[JsonConverter(typeof(CategoriesResponseConverter))]
public record CategoriesResponse
{
	/// <summary>
	/// The list of categories
	/// </summary>
	[JsonPropertyName("categories")]
	public IReadOnlyList<Category> Categories { get; init; } = [];

	/// <summary>
	/// The total number of categories in the system
	/// </summary>
	[JsonPropertyName("record_count")]
	public int RecordCount { get; init; }
}

internal sealed class CategoriesResponseConverter() : ArrayOrWrapperConverter<CategoriesResponse, Category>("categories")
{
	protected override CategoriesResponse Create(IReadOnlyList<Category> items, int recordCount)
		=> new() { Categories = items, RecordCount = recordCount };

	protected override IReadOnlyList<Category> GetItems(CategoriesResponse value) => value.Categories;

	protected override int GetRecordCount(CategoriesResponse value) => value.RecordCount;
}
