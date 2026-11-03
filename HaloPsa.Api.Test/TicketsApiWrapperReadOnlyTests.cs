using AwesomeAssertions;
using HaloPsa.Api.Infrastructure;
using HaloPsa.Api.Interfaces;
using HaloPsa.Api.Models.Tickets;

namespace HaloPsa.Api.Test;

/// <summary>
/// Covers the read-only guard on <see cref="TicketsApiWrapper"/>.
/// </summary>
/// <remarks>
/// <para>
/// These need no credentials and reach no network. The stub below throws if any of its members is
/// reached, so a passing test proves the guard refused <i>before</i> a request was composed - not
/// merely that an exception came back from somewhere. A guard that sent the request and then threw
/// would still have written the ticket.
/// </para>
/// <para>
/// The null case matters as much as the true case: null must mean read/write, because that is the
/// state every consumer written before this option existed is in.
/// </para>
/// </remarks>
public class TicketsApiWrapperReadOnlyTests
{
	// Id 0 would fail validation on the list path, so a read-only refusal there proves it comes first.
	private static readonly UpdateTicketRequest _update = new() { Status = 2 };

	// ClientId 0 would also fail validation, so a read-only refusal proves it comes first.
	private static readonly CreateTicketRequest _invalidCreate = new() { Summary = "S", ClientId = 0, UserId = 9 };

	[Fact]
	public void IsReadOnly_WhenTrue_IsReported()
		=> new TicketsApiWrapper(new ThrowingTicketsApi(), readOnly: true)
			.IsReadOnly
			.Should()
			.BeTrue();

	[Fact]
	public void IsReadOnly_WhenNull_IsFalseSoExistingCallersKeepWriting()
		=> new TicketsApiWrapper(new ThrowingTicketsApi())
			.IsReadOnly
			.Should()
			.BeFalse();

	[Fact]
	public void IsReadOnly_WhenFalse_IsFalse()
		=> new TicketsApiWrapper(new ThrowingTicketsApi(), readOnly: false)
			.IsReadOnly
			.Should()
			.BeFalse();

	[Fact]
	public async Task UpdateAsync_WhenReadOnly_RefusesWithoutCallingTheApi()
	{
		var wrapper = new TicketsApiWrapper(new ThrowingTicketsApi(), readOnly: true);

		var act = async () => await wrapper.UpdateAsync(1, _update, TestContext.Current.CancellationToken);

		(await act.Should().ThrowAsync<InvalidOperationException>())
			.WithMessage("*read-only*")
			.WithMessage("*no*request was sent*");
	}

	[Fact]
	public async Task UpdateAsync_ThroughTheInterfaceList_WhenReadOnly_RefusesWithoutCallingTheApi()
	{
		var wrapper = new TicketsApiWrapper(new ThrowingTicketsApi(), readOnly: true);

		var act = async () => await ((ITicketsApi)wrapper).UpdateAsync([_update], TestContext.Current.CancellationToken);

		(await act.Should().ThrowAsync<InvalidOperationException>())
			.WithMessage("*read-only*");
	}

	[Fact]
	public async Task CreateAsync_WhenReadOnly_RefusesWithoutCallingTheApi()
	{
		var wrapper = new TicketsApiWrapper(new ThrowingTicketsApi(), readOnly: true);

		var act = async () => await wrapper.CreateAsync(_invalidCreate, TestContext.Current.CancellationToken);

		(await act.Should().ThrowAsync<InvalidOperationException>())
			.WithMessage("*read-only*");
	}

	[Fact]
	public async Task CreateAsync_WithAList_WhenReadOnly_RefusesWithoutCallingTheApi()
	{
		var wrapper = new TicketsApiWrapper(new ThrowingTicketsApi(), readOnly: true);

		var act = async () => await wrapper.CreateAsync([_invalidCreate], TestContext.Current.CancellationToken);

		(await act.Should().ThrowAsync<InvalidOperationException>())
			.WithMessage("*read-only*");
	}

	[Fact]
	public async Task DeleteAsync_WhenReadOnly_RefusesWithoutCallingTheApi()
	{
		var wrapper = new TicketsApiWrapper(new ThrowingTicketsApi(), readOnly: true);

		var act = async () => await wrapper.DeleteAsync(1, TestContext.Current.CancellationToken);

		await act.Should().ThrowAsync<InvalidOperationException>();
	}

	// An id of 0 and a blank reason would each fail validation, so a read-only refusal proves it comes first.
	[Fact]
	public async Task DeleteAsync_WithAReason_WhenReadOnly_RefusesWithoutCallingTheApi()
	{
		var wrapper = new TicketsApiWrapper(new ThrowingTicketsApi(), readOnly: true);

		var act = async () => await wrapper.DeleteAsync(0, " ", TestContext.Current.CancellationToken);

		(await act.Should().ThrowAsync<InvalidOperationException>())
			.WithMessage("*read-only*DeleteAsync*");
	}

	// A status, agent or team id of 0 and a blank note would each fail validation, so a read-only refusal in the next five proves it comes first.
	[Fact]
	public async Task CloseAsync_WhenReadOnly_RefusesWithoutCallingTheApi()
	{
		var wrapper = new TicketsApiWrapper(new ThrowingTicketsApi(), readOnly: true);

		var act = async () => await wrapper.CloseAsync(1, 0, TestContext.Current.CancellationToken);

		(await act.Should().ThrowAsync<InvalidOperationException>())
			.WithMessage("*read-only*CloseAsync*");
	}

	[Fact]
	public async Task CloseAsync_WithANote_WhenReadOnly_RefusesWithoutCallingTheApi()
	{
		var wrapper = new TicketsApiWrapper(new ThrowingTicketsApi(), readOnly: true);

		var act = async () => await wrapper.CloseAsync(1, 0, " ", TestContext.Current.CancellationToken);

		(await act.Should().ThrowAsync<InvalidOperationException>())
			.WithMessage("*read-only*CloseAsync*");
	}

	[Fact]
	public async Task AssignAsync_WhenReadOnly_RefusesWithoutCallingTheApi()
	{
		var wrapper = new TicketsApiWrapper(new ThrowingTicketsApi(), readOnly: true);

		var act = async () => await wrapper.AssignAsync(1, 0, TestContext.Current.CancellationToken);

		(await act.Should().ThrowAsync<InvalidOperationException>())
			.WithMessage("*read-only*AssignAsync*");
	}

	[Fact]
	public async Task AssignAsync_WithATeam_WhenReadOnly_RefusesWithoutCallingTheApi()
	{
		var wrapper = new TicketsApiWrapper(new ThrowingTicketsApi(), readOnly: true);

		var act = async () => await wrapper.AssignAsync(1, 0, 0, TestContext.Current.CancellationToken);

		(await act.Should().ThrowAsync<InvalidOperationException>())
			.WithMessage("*read-only*AssignAsync*");
	}

	[Fact]
	public async Task ReopenAsync_WhenReadOnly_RefusesWithoutCallingTheApi()
	{
		var wrapper = new TicketsApiWrapper(new ThrowingTicketsApi(), readOnly: true);

		var act = async () => await wrapper.ReopenAsync(1, 0, TestContext.Current.CancellationToken);

		(await act.Should().ThrowAsync<InvalidOperationException>())
			.WithMessage("*read-only*ReopenAsync*");
	}

	[Fact]
	public async Task UpdateAsync_WhenNotReadOnly_ReachesTheApi()
	{
		// The stub throws NotSupportedException when reached. Seeing that - rather than
		// InvalidOperationException - is what proves the guard let the call through.
		var wrapper = new TicketsApiWrapper(new ThrowingTicketsApi(), readOnly: false);

		var act = async () => await wrapper.UpdateAsync(1, _update, TestContext.Current.CancellationToken);

		await act.Should().ThrowAsync<NotSupportedException>();
	}

	[Fact]
	public async Task UpdateAsync_WhenReadOnlyIsNull_ReachesTheApi()
	{
		var wrapper = new TicketsApiWrapper(new ThrowingTicketsApi());

		var act = async () => await wrapper.UpdateAsync(1, _update, TestContext.Current.CancellationToken);

		await act.Should().ThrowAsync<NotSupportedException>();
	}

	/// <summary>
	/// An <see cref="ITicketsApi"/> that fails loudly if anything reaches it.
	/// </summary>
	private sealed class ThrowingTicketsApi : ITicketsApi
	{
		public Task<TicketsResponse> GetAllAsync(TicketFilter? filter, CancellationToken cancellationToken)
			=> ThrowIfReached<Task<TicketsResponse>>(filter, cancellationToken);

		public Task<TicketsResponse> GetAllAsync(CancellationToken cancellationToken)
			=> ThrowIfReached<Task<TicketsResponse>>(cancellationToken);

		public Task<Ticket> GetByIdAsync(int id, bool includeDetails, CancellationToken cancellationToken)
			=> ThrowIfReached<Task<Ticket>>(id, includeDetails, cancellationToken);

		public Task<Ticket> GetByIdAsync(int id, CancellationToken cancellationToken)
			=> ThrowIfReached<Task<Ticket>>(id, cancellationToken);

		public Task<CreateTicketResponse> CreateAsync(CreateTicketRequest request, CancellationToken cancellationToken)
			=> ThrowIfReached<Task<CreateTicketResponse>>(request, cancellationToken);

		public Task<CreateTicketResponse> CreateAsync(IReadOnlyList<CreateTicketRequest> requests, CancellationToken cancellationToken)
			=> ThrowIfReached<Task<CreateTicketResponse>>(requests, cancellationToken);

		public Task<UpdateTicketResponse> UpdateAsync(IReadOnlyList<UpdateTicketRequest> requests, CancellationToken cancellationToken)
			=> ThrowIfReached<Task<UpdateTicketResponse>>(requests, cancellationToken);

		public Task DeleteAsync(int id, CancellationToken cancellationToken)
			=> ThrowIfReached<Task>(id, cancellationToken);

		public Task DeleteAsync(int id, string reason, CancellationToken cancellationToken)
			=> ThrowIfReached<Task>(id, reason, cancellationToken);

		private static T ThrowIfReached<T>(params object?[] arguments)
		{
			GC.KeepAlive(arguments);
			throw new NotSupportedException();
		}
	}
}
