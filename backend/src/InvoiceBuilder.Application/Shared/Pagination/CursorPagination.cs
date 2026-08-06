namespace InvoiceBuilder.Application.Shared.Pagination;

public static class CursorPagination
{
	public static CursorPage<T> CreatePage<T>(
		List<T> items,
		int pageSize,
		bool isNextPage,
		bool hasInputCursor,
		Func<T, Guid> getCursor)
	{
		var hasMoreInRequestedDirection = items.Count > pageSize;
		if (hasMoreInRequestedDirection)
		{
			items.RemoveAt(items.Count - 1);
		}

		if (!isNextPage)
		{
			items.Reverse();
		}

		var hasNextPage = isNextPage
			? hasMoreInRequestedDirection
			: hasInputCursor;

		var hasPreviousPage = isNextPage
			? hasInputCursor
			: hasMoreInRequestedDirection;

		Guid? nextCursor = hasNextPage && items.Count > 0
			? getCursor(items[^1])
			: null;

		Guid? previousCursor = hasPreviousPage && items.Count > 0
			? getCursor(items[0])
			: null;

		return new CursorPage<T>(
			items,
			nextCursor,
			previousCursor,
			pageSize);
	}
}
