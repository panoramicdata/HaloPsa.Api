using AwesomeAssertions;
using HaloPsa.Api.Interfaces;

namespace HaloPsa.Api.Test;

/// <summary>
/// Abstract base class for tests that provides common dependencies
/// </summary>
public abstract class TestBase(IntegrationTestFixture fixture)
{
	/// <summary>
	/// Gets the test fixture for creating fresh client instances
	/// </summary>
	protected readonly IntegrationTestFixture _fixture = fixture;

	/// <summary>
	/// Gets the Halo API client for testing
	/// </summary>
	protected IHaloClient HaloClient { get; } = fixture.GetHaloClient();

	/// <summary>
	/// Gets the logger for test output and diagnostics
	/// </summary>
	protected ILogger Logger { get; } = fixture.Logger;

	/// <summary>
	/// Gets a cancellation token for test operations with a reasonable timeout
	/// </summary>
	protected static CancellationToken CancellationToken { get; } = new CancellationTokenSource(TimeSpan.FromMinutes(2)).Token;

	/// <summary>
	/// Asserts that a "get all" endpoint returns a well-formed list, checking the identifier and
	/// name of the first entity when the tenant has any. Every entity's list test asserts exactly
	/// this, so they share one implementation rather than each restating it.
	/// </summary>
	/// <typeparam name="T">The entity type returned by the endpoint.</typeparam>
	/// <param name="getAllAsync">The endpoint's get-all call.</param>
	/// <param name="idSelector">Reads the entity's identifier.</param>
	/// <param name="nameSelector">Reads the entity's name.</param>
	/// <param name="entityName">The entity name, used in assertion messages.</param>
	protected static async Task AssertGetAllReturnsEntityListAsync<T>(
		Func<CancellationToken, Task<IReadOnlyList<T>>> getAllAsync,
		Func<T, int> idSelector,
		Func<T, string?> nameSelector,
		string entityName)
	{
		var result = await getAllAsync(CancellationToken);

		_ = result.Should().NotBeNull();
		_ = result.Should().BeAssignableTo<IReadOnlyList<T>>();

		if (result.Count > 0)
		{
			var first = result[0];
			_ = idSelector(first).Should().BePositive($"{entityName} ID should be positive");
			_ = nameSelector(first).Should().NotBeNullOrEmpty($"{entityName} name should not be null or empty");
		}
	}
}
