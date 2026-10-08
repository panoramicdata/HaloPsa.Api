using AwesomeAssertions;
using HaloPsa.Api.Interfaces;
using HaloPsa.Api.Test.Infrastructure;
using Refit;

namespace HaloPsa.Api.Test.Models;

/// <summary>
/// Payloads are trimmed from the live panoramicdlsandbox responses (2026-10-08): both endpoints
/// return a plain array, and a priority's "id" is a GUID with the filterable id in "priorityid".
/// </summary>
public class MetadataApiTests
{
	private const string StatusArray = """
		[ { "id": 1, "name": "New", "shortname": "New", "type": 0, "sequence": 1, "colour": "#fcc400" },
		  { "id": 9, "name": "Closed", "shortname": "Closed", "type": 0, "sequence": 9, "colour": "#000000" } ]
		""";

	private const string PriorityArray = """
		[ { "id": "c183eb27-0a8a-4380-59fa-08d5c0811dad", "slaid": 2, "priorityid": 1, "name": "Urgent", "ishidden": false, "colour": "#f44e3b" },
		  { "id": "0b4f9a3e-1111-4380-59fa-08d5c0811dad", "slaid": 1, "priorityid": 1, "name": "Critical", "ishidden": true, "colour": "#f44e3b" } ]
		""";

	[Fact]
	public async Task Statuses_AreRequestedUnderApi_AndReadFromAPlainArray()
	{
		var handler = new CapturingHandler(StatusArray);

		var response = await RestService.For<IStatusesApi>(handler.CreateClient())
			.GetAllResponseAsync(TestContext.Current.CancellationToken);

		_ = handler.RequestUri!.AbsolutePath.Should().Be("/api/Status");
		_ = response.Statuses.Select(s => s.Name).Should().Equal("New", "Closed");
		_ = response.RecordCount.Should().Be(2);
	}

	[Fact]
	public async Task Statuses_StillReadTheWrappedShape()
	{
		var handler = new CapturingHandler("""{ "record_count": 1, "statuses": [ { "id": 1, "name": "New" } ] }""");

		var response = await RestService.For<IStatusesApi>(handler.CreateClient())
			.GetAllResponseAsync(TestContext.Current.CancellationToken);

		_ = response.Statuses.Should().ContainSingle().Which.Name.Should().Be("New");
	}

	[Fact]
	public async Task Priorities_AreRequestedUnderApi_AndReadTheLiveShape()
	{
		var handler = new CapturingHandler(PriorityArray);

		var response = await RestService.For<IPrioritiesApi>(handler.CreateClient())
			.GetAllResponseAsync(TestContext.Current.CancellationToken);

		_ = handler.RequestUri!.AbsolutePath.Should().Be("/api/Priority");
		_ = response.Priorities.Should().HaveCount(2);

		var urgent = response.Priorities[0];
		_ = urgent.Id.Should().Be("c183eb27-0a8a-4380-59fa-08d5c0811dad");
		_ = urgent.PriorityId.Should().Be(1);
		_ = urgent.SlaId.Should().Be(2);
		_ = urgent.Name.Should().Be("Urgent");
		_ = response.Priorities[1].IsHidden.Should().BeTrue();
	}

	[Theory]
	[InlineData(typeof(ICategoriesApi), "/api/Category")]
	[InlineData(typeof(ISitesApi), "/api/Site")]
	[InlineData(typeof(ITeamsApi), "/api/Team")]
	[InlineData(typeof(IStatusesApi), "/api/Status")]
	[InlineData(typeof(IPrioritiesApi), "/api/Priority")]
	public void EveryMetadataRoute_IsUnderApi(Type api, string expectedListRoute)
	{
		var routes = api.GetMethods()
			.SelectMany(m => m.GetCustomAttributes(typeof(HttpMethodAttribute), inherit: false).Cast<HttpMethodAttribute>())
			.Select(a => a.Path)
			.ToList();

		_ = routes.Should().Contain(expectedListRoute);
		_ = routes.Should().OnlyContain(r => r.StartsWith("/api/", StringComparison.Ordinal));
	}
}
