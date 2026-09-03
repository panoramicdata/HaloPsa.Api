using AwesomeAssertions;

namespace HaloPsa.Api.Test;

[Collection("Integration Tests")]
public class UsersApiUnitTest(IntegrationTestFixture fixture) : TestBase(fixture)
{
	[Fact]
	public Task GetAllUsers_ShouldReturnUsersList()
		=> AssertGetAllReturnsEntityListAsync(
			HaloClient.Psa.Users.GetAllAsync,
			user => user.Id,
			user => user.Name,
			"User");
}

[Collection("Integration Tests")]
public class AssetsApiUnitTest(IntegrationTestFixture fixture) : TestBase(fixture)
{
	[Fact]
	public Task GetAllAssets_ShouldReturnAssetsList()
		=> AssertGetAllReturnsEntityListAsync(
			HaloClient.Psa.Assets.GetAllAsync,
			asset => asset.Id,
			asset => asset.Name,
			"Asset");
}

[Collection("Integration Tests")]
public class ProjectsApiUnitTest(IntegrationTestFixture fixture) : TestBase(fixture)
{
	[Fact]
	public Task GetAllProjects_ShouldReturnProjectsList()
		=> AssertGetAllReturnsEntityListAsync(
			HaloClient.Psa.Projects.GetAllAsync,
			project => project.Id,
			project => project.Name,
			"Project");
}

[Collection("Integration Tests")]
public class ClientsApiUnitTest(IntegrationTestFixture fixture) : TestBase(fixture)
{
	[Fact]
	public Task GetAllClients_ShouldReturnClientsList()
		=> AssertGetAllReturnsEntityListAsync(
			HaloClient.Psa.Clients.GetAllAsync,
			client => client.Id,
			client => client.Name,
			"Client");

	[Fact]
	public async Task GetClientById_WithValidId_ShouldReturnClient()
	{
		// Arrange - First get a client to use for testing
		var allClients = await HaloClient.Psa.Clients.GetAllAsync(CancellationToken);
		_ = allClients.Should().NotBeEmpty("Need at least one client for GetById test");

		var testClientId = allClients[0].Id;

		// Act
		var result = await HaloClient.Psa.Clients.GetByIdAsync(testClientId, CancellationToken);

		// Assert
		_ = result.Should().NotBeNull();
		_ = result.Id.Should().Be(testClientId);
		_ = result.Name.Should().NotBeNullOrEmpty();
	}

	[Fact]
	public async Task GetClientById_WithInvalidId_ShouldThrowException()
	{
		// Arrange
		var invalidClientId = -999999;

		// Act & Assert
		var act = async () => await HaloClient.Psa.Clients.GetByIdAsync(invalidClientId, CancellationToken);
		_ = await act.Should().ThrowAsync<Exception>("Getting non-existent client should throw exception");
	}
}
