using AwesomeAssertions;
using HaloPsa.Api.Interfaces;
using Refit;

namespace HaloPsa.Api.Test.Infrastructure;

/// <summary>
/// Pins the query parameter names the category, priority, status, team and ticket clients put on the wire. Halo
/// matches parameter names case-insensitively but not across underscores, so a .NET name such as <c>teamId</c>
/// is silently ignored where <c>team_id</c> filters, and the request still succeeds - which is why only the URL
/// proves it. Names are as declared in Specification/swagger.json. These tests build the real Refit clients over a
/// capturing handler, so they need no network and no credentials.
/// </summary>
public class QueryParameterNameTests
{
	private static readonly Uri _baseAddress = new("https://example.invalid");

	private static T CreateClient<T>(CapturingHandler handler)
		=> RestService.For<T>(new HttpClient(handler) { BaseAddress = _baseAddress });

	[Fact]
	public async Task CategoriesGetAll_SendsTicketTypeAndTeamInHalosSpelling()
	{
		var handler = CapturingHandler.Returning("""{"categories":[],"record_count":0}""");
		var client = CreateClient<ICategoriesApi>(handler);

		_ = await client.GetAllResponseAsync(ticketTypeId: 4, teamId: 9, CancellationToken.None);

		_ = handler.LastRequest.AbsolutePath.Should().Be("/api/Category");
		_ = handler.LastRequest.Query.Should().Be("?tickettype_id=4&team_id=9");
	}

	[Fact]
	public async Task CategoriesGetById_SendsIncludeDetailsInHalosSpelling()
	{
		var handler = CapturingHandler.Returning("""{"id":7}""");
		var client = CreateClient<ICategoriesApi>(handler);

		_ = await client.GetByIdAsync(7, includeDetails: true, CancellationToken.None);

		_ = handler.LastRequest.AbsolutePath.Should().Be("/api/Category/7");
		_ = handler.LastRequest.Query.Should().Be("?includedetails=True");
	}

	[Fact]
	public async Task PrioritiesGetById_SendsIncludeDetailsInHalosSpelling()
	{
		var handler = CapturingHandler.Returning("""{"id":"7"}""");
		var client = CreateClient<IPrioritiesApi>(handler);

		_ = await client.GetByIdAsync("7", includeDetails: true, CancellationToken.None);

		_ = handler.LastRequest.AbsolutePath.Should().Be("/api/Priority/7");
		_ = handler.LastRequest.Query.Should().Be("?includedetails=True");
	}

	[Fact]
	public async Task StatusesGetById_SendsIncludeDetailsInHalosSpelling()
	{
		var handler = CapturingHandler.Returning("""{"id":7}""");
		var client = CreateClient<IStatusesApi>(handler);

		_ = await client.GetByIdAsync(7, includeDetails: true, CancellationToken.None);

		_ = handler.LastRequest.AbsolutePath.Should().Be("/api/Status/7");
		_ = handler.LastRequest.Query.Should().Be("?includedetails=True");
	}

	[Fact]
	public async Task TeamsGetById_SendsIncludeDetailsInHalosSpelling()
	{
		var handler = CapturingHandler.Returning("""{"id":7}""");
		var client = CreateClient<ITeamsApi>(handler);

		_ = await client.GetByIdAsync(7, includeDetails: true, CancellationToken.None);

		_ = handler.LastRequest.AbsolutePath.Should().Be("/api/Team/7");
		_ = handler.LastRequest.Query.Should().Be("?includedetails=True");
	}

	[Fact]
	public async Task TicketsGetById_SendsIncludeDetailsInHalosSpelling()
	{
		var handler = CapturingHandler.Returning("""{"id":7,"summary":"s","status_id":1,"client_id":2,"user_id":3}""");
		var client = CreateClient<ITicketsApi>(handler);

		_ = await client.GetByIdAsync(7, includeDetails: true, CancellationToken.None);

		_ = handler.LastRequest.AbsolutePath.Should().Be("/api/Tickets/7");
		_ = handler.LastRequest.Query.Should().Be("?includedetails=True");
	}
}
