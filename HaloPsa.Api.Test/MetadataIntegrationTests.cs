using AwesomeAssertions;

namespace HaloPsa.Api.Test;

/// <summary>
/// Read-only checks of the metadata endpoints against the sandbox.
/// </summary>
[Collection("Integration Tests")]
public class MetadataIntegrationTests(IntegrationTestFixture fixture) : TestBase(fixture)
{
	[Fact]
	public async Task Statuses_GetAllAsync_ReturnsNamedStatuses()
	{
		var statuses = await HaloClient.Psa.Statuses.GetAllAsync(CancellationToken);

		_ = statuses.Should().NotBeEmpty();
		_ = statuses.Should().OnlyContain(s => s.Id > 0 && !string.IsNullOrWhiteSpace(s.Name));
	}

	[Fact]
	public async Task Priorities_GetAllAsync_ReturnsPriorityIdsPerSla()
	{
		var priorities = await HaloClient.Psa.Priorities.GetAllAsync(CancellationToken);

		_ = priorities.Should().NotBeEmpty();
		_ = priorities.Should().OnlyContain(p => p.PriorityId > 0 && p.SlaId > 0);
		_ = priorities.Select(p => Guid.TryParse(p.Id, out var guid) ? guid : Guid.Empty).Should().NotContain(Guid.Empty);
	}

	[Fact]
	public async Task Actions_GetForTicketAsync_ReturnsAResponse()
	{
		var response = await HaloClient.Psa.Actions.GetForTicketAsync(1, CancellationToken);

		_ = response.Should().NotBeNull();
		_ = response.Actions.Should().NotBeNull();
	}
}
