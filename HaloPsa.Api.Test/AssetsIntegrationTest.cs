namespace HaloPsa.Api.Test;

[Collection("Integration Tests")]
public class AssetsIntegrationTest(IntegrationTestFixture fixture) : TestBase(fixture)
{
	[Fact]
	public Task GetAssets_ShouldReturnAssets()
		=> AssertGetAllReturnsEntityListAsync(
			HaloClient.Psa.Assets.GetAllAsync,
			asset => asset.Id,
			asset => asset.Name,
			"Asset");
}
