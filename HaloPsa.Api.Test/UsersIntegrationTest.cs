namespace HaloPsa.Api.Test;

[Collection("Integration Tests")]
public class UsersIntegrationTest(IntegrationTestFixture fixture) : TestBase(fixture)
{
	[Fact]
	public Task GetUsers_ShouldReturnUsers()
		=> AssertGetAllReturnsEntityListAsync(
			HaloClient.Psa.Users.GetAllAsync,
			user => user.Id,
			user => user.Name,
			"User");
}
