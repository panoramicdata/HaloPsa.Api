using AwesomeAssertions;
using HaloPsa.Api.Models.Tickets;
using System.Text.Json;

namespace HaloPsa.Api.Test.Models.Tickets;

/// <summary>
/// Covers reading a ticket's <c>customfields</c> array, in the shape a live Halo tenant returns it.
/// </summary>
public class TicketCustomFieldsTests
{
	[Fact]
	public void Ticket_ReadsCustomFields_FromTheLiveShape()
	{
		const string json = """
			{
			  "id": 1234,
			  "summary": "Cisco advisory",
			  "status_id": 1,
			  "client_id": 11,
			  "user_id": 22,
			  "customfields": [
			    {
			      "id": 518,
			      "name": "CFAppPortalCiscoSecurityAdvisoryId",
			      "value": "cisco-sa-aaa-Yx47ZT8Q",
			      "display": "cisco-sa-aaa-Yx47ZT8Q",
			      "label": "AppPortal_CiscoSecurityAdvisoryId",
			      "type": 0,
			      "usage": 1,
			      "mandatory": false,
			      "options": [],
			      "extra": {"a": [1, 2]}
			    }
			  ]
			}
			""";

		var ticket = JsonSerializer.Deserialize<Ticket>(json);

		_ = ticket.Should().NotBeNull();
		var field = ticket!.CustomFields.Should().ContainSingle().Subject;
		_ = field.Id.Should().Be(518);
		_ = field.Name.Should().Be("CFAppPortalCiscoSecurityAdvisoryId");
		_ = field.Label.Should().Be("AppPortal_CiscoSecurityAdvisoryId");
		_ = field.Value!.Value.GetString().Should().Be("cisco-sa-aaa-Yx47ZT8Q");
		_ = field.Display.Should().Be("cisco-sa-aaa-Yx47ZT8Q");
	}

	[Fact]
	public void Ticket_WithNoCustomFieldsKey_HasNullCustomFields()
	{
		var ticket = JsonSerializer.Deserialize<Ticket>("""{"id":1,"summary":"s","status_id":1,"client_id":1,"user_id":1}""");

		_ = ticket!.CustomFields.Should().BeNull();
	}

	[Fact]
	public void Ticket_WithAnEmptyCustomFieldsArray_HasAnEmptyList()
	{
		var ticket = JsonSerializer.Deserialize<Ticket>("""{"id":1,"summary":"s","status_id":1,"client_id":1,"user_id":1,"customfields":[]}""");

		_ = ticket!.CustomFields.Should().BeEmpty();
	}

	[Fact]
	public void Ticket_ExtensionData_DoesNotSwallowCustomFields()
	{
		// [JsonExtensionData] only takes keys the model does not declare, so "customfields" must be read into
		// the typed property while an unmodelled key still lands in AdditionalProperties.
		const string json = """
			{
			  "id": 1,
			  "summary": "s",
			  "status_id": 1,
			  "client_id": 1,
			  "user_id": 1,
			  "timetaken": 1.25,
			  "customfields": [ { "id": 518, "name": "CFRef", "value": "ABC" } ]
			}
			""";

		var ticket = JsonSerializer.Deserialize<Ticket>(json);

		_ = ticket!.CustomFields.Should().ContainSingle().Which.Id.Should().Be(518);
		_ = ticket.AdditionalProperties.Should().NotBeNull();
		_ = ticket.AdditionalProperties!.Keys.Should().Contain("timetaken").And.NotContain("customfields");
	}
}
