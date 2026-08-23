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
	private static readonly UpdateTicketRequest _update = new() { Notes = "note" };

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
	public async Task DeleteAsync_WhenReadOnly_RefusesWithoutCallingTheApi()
	{
		var wrapper = new TicketsApiWrapper(new ThrowingTicketsApi(), readOnly: true);

		var act = async () => await wrapper.DeleteAsync(1, TestContext.Current.CancellationToken);

		await act.Should().ThrowAsync<InvalidOperationException>();
	}

	[Fact]
	public async Task CloseAsync_WhenReadOnly_RefusesWithoutCallingTheApi()
	{
		var wrapper = new TicketsApiWrapper(new ThrowingTicketsApi(), readOnly: true);

		var act = async () => await wrapper.CloseAsync(1, "done", TestContext.Current.CancellationToken);

		await act.Should().ThrowAsync<InvalidOperationException>();
	}

	[Fact]
	public async Task AssignAsync_WhenReadOnly_RefusesWithoutCallingTheApi()
	{
		var wrapper = new TicketsApiWrapper(new ThrowingTicketsApi(), readOnly: true);

		var act = async () => await wrapper.AssignAsync(1, 2, TestContext.Current.CancellationToken);

		await act.Should().ThrowAsync<InvalidOperationException>();
	}

	[Fact]
	public async Task ReopenAsync_WhenReadOnly_RefusesWithoutCallingTheApi()
	{
		var wrapper = new TicketsApiWrapper(new ThrowingTicketsApi(), readOnly: true);

		var act = async () => await wrapper.ReopenAsync(1, "again", TestContext.Current.CancellationToken);

		await act.Should().ThrowAsync<InvalidOperationException>();
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
			=> throw new NotSupportedException();

		public Task<TicketsResponse> GetAllAsync(CancellationToken cancellationToken)
			=> throw new NotSupportedException();

		public Task<Ticket> GetByIdAsync(int id, bool includeDetails, CancellationToken cancellationToken)
			=> throw new NotSupportedException();

		public Task<Ticket> GetByIdAsync(int id, CancellationToken cancellationToken)
			=> throw new NotSupportedException();

		public Task<CreateTicketResponse> CreateAsync(CreateTicketRequest request, CancellationToken cancellationToken)
			=> throw new NotSupportedException();

		public Task<UpdateTicketResponse> UpdateAsync(int id, UpdateTicketRequest request, CancellationToken cancellationToken)
			=> throw new NotSupportedException();

		public Task DeleteAsync(int id, CancellationToken cancellationToken)
			=> throw new NotSupportedException();

		public Task<UpdateTicketResponse> CloseAsync(int id, string resolution, CancellationToken cancellationToken)
			=> throw new NotSupportedException();

		public Task<UpdateTicketResponse> CloseAsync(int id, CancellationToken cancellationToken)
			=> throw new NotSupportedException();

		public Task<UpdateTicketResponse> ReopenAsync(int id, string reason, CancellationToken cancellationToken)
			=> throw new NotSupportedException();

		public Task<UpdateTicketResponse> ReopenAsync(int id, CancellationToken cancellationToken)
			=> throw new NotSupportedException();

		public Task<UpdateTicketResponse> AssignAsync(int id, int agentId, CancellationToken cancellationToken)
			=> throw new NotSupportedException();

		public Task<UpdateTicketResponse> AssignAsync(int id, int agentId, int teamId, CancellationToken cancellationToken)
			=> throw new NotSupportedException();
	}
}
