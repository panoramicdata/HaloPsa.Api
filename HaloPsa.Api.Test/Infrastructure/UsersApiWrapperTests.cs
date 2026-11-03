using AwesomeAssertions;
using HaloPsa.Api.Infrastructure;
using HaloPsa.Api.Interfaces;
using HaloPsa.Api.Models.Users;
using Refit;

namespace HaloPsa.Api.Test.Infrastructure;

/// <summary>
/// Covers how <see cref="User"/> is read from Halo's response and the query parameter names <see cref="UserFilter"/>
/// puts on the wire. These tests build the real Refit client over a capturing handler, so they need no network and
/// no credentials.
/// </summary>
public class UsersApiWrapperTests
{
	private const string Jane = """
		{"id":42,"name":"Jane Smith","emailaddress":"","site":"Head Office","department":"Sales","client_id":21,"client_name":"Example Ltd",
		 "site_id":3.0,"login":"jane.smith@example.com","firstname":"Jane","surname":"Smith","email2":"jane@example.org","email3":null,"linked_agent_id":-1}
		""";

	private static UsersApiWrapper CreateWrapper(CapturingHandler handler)
		=> new(CreateClient(handler));

	private static IUsersRefitApi CreateClient(CapturingHandler handler)
		=> RestService.For<IUsersRefitApi>(handler.CreateClient());

	[Fact]
	public async Task GetByIdAsync_ReadsTheExistingFields()
	{
		var wrapper = CreateWrapper(new CapturingHandler(Jane));

		var user = await wrapper.GetByIdAsync(42, CancellationToken.None);

		_ = user.Id.Should().Be(42);
		_ = user.Name.Should().Be("Jane Smith");
		_ = user.Site.Should().Be("Head Office");
		_ = user.Department.Should().Be("Sales");
		_ = user.ClientId.Should().Be(21);
		_ = user.ClientName.Should().Be("Example Ltd");
	}

	[Fact]
	public async Task GetByIdAsync_ReadsLoginNamesAndExtraAddresses()
	{
		var wrapper = CreateWrapper(new CapturingHandler(Jane));

		var user = await wrapper.GetByIdAsync(42, CancellationToken.None);

		_ = user.Login.Should().Be("jane.smith@example.com");
		_ = user.FirstName.Should().Be("Jane");
		_ = user.Surname.Should().Be("Smith");
		_ = user.EmailAddress.Should().Be("");
		_ = user.EmailAddress2.Should().Be("jane@example.org");
		_ = user.EmailAddress3.Should().BeNull();
		_ = user.LinkedAgentId.Should().Be(-1);
	}

	[Fact]
	public async Task GetByIdAsync_WhenOptionalFieldsAreMissing_ReadsThemAsNull()
	{
		var wrapper = CreateWrapper(CapturingHandler.Returning("""{"id":43,"name":"No Login"}"""));

		var user = await wrapper.GetByIdAsync(43, CancellationToken.None);

		_ = user.Login.Should().BeNull();
		_ = user.FirstName.Should().BeNull();
		_ = user.Surname.Should().BeNull();
		_ = user.EmailAddress.Should().BeNull();
		_ = user.EmailAddress2.Should().BeNull();
		_ = user.EmailAddress3.Should().BeNull();
		_ = user.LinkedAgentId.Should().BeNull();
	}

	[Fact]
	public async Task Username_ReturnsLogin()
	{
		// Halo's Users record has no username field, so a stray one must not be read.
		var wrapper = CreateWrapper(new CapturingHandler(Jane.Replace("\"id\":42,", "\"id\":42,\"username\":\"ignored\",")));

		var user = await wrapper.GetByIdAsync(42, CancellationToken.None);

#pragma warning disable CS0618 // Username is obsolete on purpose
		_ = user.Username.Should().Be("jane.smith@example.com");
#pragma warning restore CS0618
	}

	[Fact]
	public async Task UserFilter_SendsHalosOwnParameterNames()
	{
		var handler = CapturingHandler.Returning("""{"users":[],"record_count":0}""");
		var wrapper = CreateWrapper(handler);
		var filter = new UserFilter
		{
			Paginate = true,
			PageNo = 2,
			PageSize = 50,
			Search = "jsmith",
			ClientId = 21,
			IncludeInactive = false,
			ExcludeAgents = true
		};

		_ = await wrapper.GetResponseAsync(filter, CancellationToken.None);

		_ = handler.LastRequest.AbsolutePath.Should().Be("/api/Users");
		_ = handler.LastRequest.Query.Should().Be(
			"?pageinate=True&page_no=2&page_size=50&search=jsmith&client_id=21&includeinactive=False&exclude_agents=True");
	}

	[Fact]
	public async Task GetAllAsync_WithAFilter_ReturnsTheUsersHaloSent()
	{
		var handler = CapturingHandler.Returning($$"""{"users":[{{Jane}},{"id":43,"name":"No Login"}],"record_count":2}""");
		var wrapper = CreateWrapper(handler);

		var users = await wrapper.GetAllAsync(new UserFilter { Search = "smith" }, CancellationToken.None);

		_ = users.Select(u => u.Id).Should().Equal(42, 43);
		_ = users[0].Login.Should().Be("jane.smith@example.com");
	}
}
