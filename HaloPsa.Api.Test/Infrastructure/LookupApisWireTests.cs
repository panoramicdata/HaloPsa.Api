using AwesomeAssertions;
using HaloPsa.Api.Infrastructure;
using HaloPsa.Api.Interfaces;
using Refit;

namespace HaloPsa.Api.Test.Infrastructure;

/// <summary>
/// Pins the Statuses, Teams, Priorities and Categories clients to what a live Halo tenant answers (issue 64):
/// every route carries the /api prefix, and each list endpoint returns a bare JSON array, not a wrapper object.
/// </summary>
public class LookupApisWireTests
{
	private const string BareArray = """[{"id":1,"name":"One"},{"id":2,"name":"Two"}]""";

	private static T CreateClient<T>(CapturingHandler handler)
		=> RestService.For<T>(handler.CreateClient());

	[Fact]
	public async Task Statuses_GetAll_UsesTheApiPrefixAndReadsABareArray()
	{
		var handler = CapturingHandler.Returning(BareArray);

		var statuses = await new StatusesApiWrapper(CreateClient<IStatusesApi>(handler)).GetAllAsync(CancellationToken.None);

		_ = handler.LastRequest.AbsolutePath.Should().Be("/api/Status");
		_ = statuses.Select(s => s.Name).Should().Equal("One", "Two");
	}

	[Fact]
	public async Task Statuses_GetById_UsesTheApiPrefix()
	{
		var handler = CapturingHandler.Returning("""{"id":7,"name":"Seven"}""");

		var status = await new StatusesApiWrapper(CreateClient<IStatusesApi>(handler)).GetByIdAsync(7, CancellationToken.None);

		_ = handler.LastRequest.AbsolutePath.Should().Be("/api/Status/7");
		_ = status.Name.Should().Be("Seven");
	}

	[Fact]
	public async Task Priorities_GetAll_UsesTheApiPrefixAndReadsABareArrayWithStringIds()
	{
		var handler = CapturingHandler.Returning("""[{"id":"1","priorityid":1,"name":"Low"},{"id":"2","priorityid":2,"name":"High"}]""");

		var priorities = await new PrioritiesApiWrapper(CreateClient<IPrioritiesApi>(handler)).GetAllAsync(CancellationToken.None);

		_ = handler.LastRequest.AbsolutePath.Should().Be("/api/Priority");
		_ = priorities.Select(p => p.Id).Should().Equal("1", "2");
		_ = priorities.Select(p => p.Name).Should().Equal("Low", "High");
	}

	[Fact]
	public async Task Priorities_GetById_UsesTheApiPrefix()
	{
		var handler = CapturingHandler.Returning("""{"id":"3","priorityid":3,"name":"Urgent"}""");

		var priority = await new PrioritiesApiWrapper(CreateClient<IPrioritiesApi>(handler)).GetByIdAsync("3", CancellationToken.None);

		_ = handler.LastRequest.AbsolutePath.Should().Be("/api/Priority/3");
		_ = priority.Name.Should().Be("Urgent");
	}

	[Fact]
	public async Task Teams_GetAll_UsesTheApiPrefixAndReadsABareArray()
	{
		var handler = CapturingHandler.Returning(BareArray);

		var teams = await new TeamsApiWrapper(CreateClient<ITeamsApi>(handler)).GetAllAsync(CancellationToken.None);

		_ = handler.LastRequest.AbsolutePath.Should().Be("/api/Team");
		_ = teams.Select(t => t.Name).Should().Equal("One", "Two");
	}

	[Fact]
	public async Task Teams_GetById_UsesTheApiPrefix()
	{
		var handler = CapturingHandler.Returning("""{"id":4,"name":"Four"}""");

		var team = await new TeamsApiWrapper(CreateClient<ITeamsApi>(handler)).GetByIdAsync(4, CancellationToken.None);

		_ = handler.LastRequest.AbsolutePath.Should().Be("/api/Team/4");
		_ = team.Name.Should().Be("Four");
	}

	[Fact]
	public async Task Categories_GetAll_UsesTheApiPrefixAndReadsABareArray()
	{
		var handler = CapturingHandler.Returning(BareArray);

		var categories = await new CategoriesApiWrapper(CreateClient<ICategoriesApi>(handler)).GetAllAsync(CancellationToken.None);

		_ = handler.LastRequest.AbsolutePath.Should().Be("/api/Category");
		_ = categories.Select(c => c.Name).Should().Equal("One", "Two");
	}

	[Fact]
	public async Task Categories_GetById_UsesTheApiPrefix()
	{
		var handler = CapturingHandler.Returning("""{"id":5,"name":"Five"}""");

		var category = await new CategoriesApiWrapper(CreateClient<ICategoriesApi>(handler)).GetByIdAsync(5, CancellationToken.None);

		_ = handler.LastRequest.AbsolutePath.Should().Be("/api/Category/5");
		_ = category.Name.Should().Be("Five");
	}

	[Fact]
	public async Task Teams_GetAllResponse_StillReadsAWrapperObject()
	{
		var handler = CapturingHandler.Returning("""{"teams":[{"id":1,"name":"One"}],"record_count":9}""");

		var teams = await new TeamsApiWrapper(CreateClient<ITeamsApi>(handler)).GetAllResponseAsync(CancellationToken.None);

		_ = teams.Teams.Should().ContainSingle().Which.Name.Should().Be("One");
		_ = teams.RecordCount.Should().Be(9);
	}

	[Fact]
	public async Task Categories_GetAllResponse_StillReadsAWrapperObject()
	{
		var handler = CapturingHandler.Returning("""{"categories":[{"id":1,"name":"One"}],"record_count":9}""");

		var categories = await new CategoriesApiWrapper(CreateClient<ICategoriesApi>(handler)).GetAllResponseAsync(null, null, CancellationToken.None);

		_ = categories.Categories.Should().ContainSingle().Which.Name.Should().Be("One");
		_ = categories.RecordCount.Should().Be(9);
	}

	[Fact]
	public async Task PsaApi_ExposesTeamsAndCategories()
	{
		var handler = CapturingHandler.Returning(BareArray);
		IPsaApi psa = new PsaApi(handler.CreateClient());

		var teams = await psa.Teams.GetAllAsync(CancellationToken.None);
		var categories = await psa.Categories.GetAllAsync(CancellationToken.None);

		_ = handler.Requests.Select(r => r.AbsolutePath).Should().Equal("/api/Team", "/api/Category");
		_ = teams.Should().HaveCount(2);
		_ = categories.Should().HaveCount(2);
	}
}
