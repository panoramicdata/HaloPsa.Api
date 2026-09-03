namespace HaloPsa.Api.Test;

[Collection("Integration Tests")]
public class ProjectsIntegrationTest(IntegrationTestFixture fixture) : TestBase(fixture)
{
	[Fact]
	public Task GetProjects_ShouldReturnProjects()
		=> AssertGetAllReturnsEntityListAsync(
			HaloClient.Psa.Projects.GetAllAsync,
			project => project.Id,
			project => project.Name,
			"Project");
}
