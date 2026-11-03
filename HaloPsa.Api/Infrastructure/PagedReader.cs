namespace HaloPsa.Api.Infrastructure;

/// <summary>
/// The limits <see cref="PagedReader"/> reads a list under, and what it calls the items in its error messages
/// </summary>
/// <param name="PageSize">The page size requested from Halo</param>
/// <param name="MaxPages">The most pages read in one pass before giving up</param>
/// <param name="MaxAttempts">How many times the listing is started again because Halo's total moved, before giving up</param>
/// <param name="ItemName">The singular name used in messages, for example <c>site</c></param>
/// <param name="ItemsName">The plural name used in messages, for example <c>sites</c></param>
internal sealed record PagedReadOptions(int PageSize, int MaxPages, int MaxAttempts, string ItemName, string ItemsName);

/// <summary>
/// Reads a Halo list page by page to an empty page, and checks the result against the total Halo reports
/// </summary>
internal static class PagedReader
{
	/// <summary>
	/// Reads every item, restarting the listing if the reported total moves, and throws rather than return a list
	/// it knows is incomplete
	/// </summary>
	/// <param name="readPage">Reads one page by its 1-based number, returning the items and the record count Halo reported</param>
	/// <param name="idOf">Reads an item's id, so an item seen twice is returned once</param>
	/// <param name="options">The limits and message names</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Every item, each once</returns>
	/// <exception cref="InvalidOperationException">Halo is ignoring <c>page_no</c>; the collected items did not match the
	/// reported total; the total kept changing for <see cref="PagedReadOptions.MaxAttempts"/> attempts; or more than
	/// <see cref="PagedReadOptions.MaxPages"/> pages were needed</exception>
	internal static async Task<IReadOnlyList<TItem>> ReadAllAsync<TItem>(
		Func<int, CancellationToken, Task<(IReadOnlyList<TItem> Items, int RecordCount)>> readPage,
		Func<TItem, int> idOf,
		PagedReadOptions options,
		CancellationToken cancellationToken)
	{
		for (var attempt = 1; attempt <= options.MaxAttempts; attempt++)
		{
			var (items, totalMoved) = await ReadEveryPageAsync(readPage, idOf, options, cancellationToken).ConfigureAwait(false);
			if (!totalMoved)
			{
				return items;
			}
		}

		throw new InvalidOperationException(
			$"The {options.ItemName} list kept changing while it was being read ({options.MaxAttempts} attempts); stopping rather than return a list that may be incomplete.");
	}

	private static async Task<(IReadOnlyList<TItem> Items, bool TotalMoved)> ReadEveryPageAsync<TItem>(
		Func<int, CancellationToken, Task<(IReadOnlyList<TItem> Items, int RecordCount)>> readPage,
		Func<TItem, int> idOf,
		PagedReadOptions options,
		CancellationToken cancellationToken)
	{
		var items = new List<TItem>();
		var seenIds = new HashSet<int>();
		int? firstTotal = null;

		for (var pageNo = 1; pageNo <= options.MaxPages; pageNo++)
		{
			var (pageItems, recordCount) = await readPage(pageNo, cancellationToken).ConfigureAwait(false);

			if (TotalHasMoved(recordCount, ref firstTotal))
			{
				return (items, true);
			}

			if (pageItems.Count == 0)
			{
				EnsureTotalReached(firstTotal, items.Count, pageNo, options);
				return (items, false);
			}

			var newItems = pageItems.Where(item => seenIds.Add(idOf(item))).ToList();
			if (newItems.Count == 0)
			{
				throw new InvalidOperationException(
					$"Halo returned page {pageNo} holding only {options.ItemsName} already received; it does not appear to be honouring page_no.");
			}

			items.AddRange(newItems);
		}

		throw new InvalidOperationException(
			$"Halo still had {options.ItemsName} to return after {options.MaxPages} pages of up to {options.PageSize}; stopping at the page limit.");
	}

	/// <summary>
	/// Records the first non-zero total and reports whether this page's total differs from it. A zero total is
	/// Halo's answer past the end of the list and says nothing either way.
	/// </summary>
	private static bool TotalHasMoved(int recordCount, ref int? firstTotal)
	{
		if (recordCount <= 0)
		{
			return false;
		}

		firstTotal ??= recordCount;
		return recordCount != firstTotal;
	}

	private static void EnsureTotalReached(int? total, int collected, int emptyPageNo, PagedReadOptions options)
	{
		if (total is { } expected && collected != expected)
		{
			throw new InvalidOperationException(
				$"Halo reported {expected} {options.ItemsName} but {collected} were returned before the empty page {emptyPageNo}; the pages "
				+ "did not line up, so the list would be incomplete.");
		}
	}
}
