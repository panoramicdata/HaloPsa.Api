using AwesomeAssertions;
using HaloPsa.Api.Models.Sites;
using System.Text.Json;

namespace HaloPsa.Api.Test.Models;

/// <summary>
/// Covers reading a site's <c>customfields</c> array, in the shape Halo returns it.
/// </summary>
public class CustomFieldValueTests
{
	[Fact]
	public void Site_ReadsCustomFields_FromTheLiveShape()
	{
		const string json = """
			{
			  "id": 3,
			  "name": "Head Office",
			  "client_id": 21.0,
			  "customfields": [
			    {"id":519,"name":"CFAutotaskSiteID","label":"Autotask ID","value":"2187","display":"2187","type":0,"usage":1,"extra":{"a":[1,2]}},
			    {"id":520.0,"name":"CFNumber","label":"A number","value":42,"display":"42"}
			  ]
			}
			""";

		var site = JsonSerializer.Deserialize<Site>(json);

		_ = site.Should().NotBeNull();
		_ = site!.CustomFields.Should().HaveCount(2);

		var first = site.CustomFields![0];
		_ = first.Id.Should().Be(519);
		_ = first.Name.Should().Be("CFAutotaskSiteID");
		_ = first.Label.Should().Be("Autotask ID");
		_ = first.Value!.Value.GetString().Should().Be("2187");
		_ = first.Display.Should().Be("2187");
	}

	[Fact]
	public void CustomFieldValue_WhenTheIdIsADecimal_IsRead()
	{
		var site = JsonSerializer.Deserialize<Site>("""{"id":1,"customfields":[{"id":21.0,"value":7}]}""");

		var field = site!.CustomFields!.Single();
		_ = field.Id.Should().Be(21);
		_ = field.Value!.Value.GetInt32().Should().Be(7);
	}

	[Fact]
	public void Site_WithNoCustomFieldsKey_HasNullCustomFields()
	{
		var site = JsonSerializer.Deserialize<Site>("""{"id":1}""");

		_ = site!.CustomFields.Should().BeNull();
	}
}
