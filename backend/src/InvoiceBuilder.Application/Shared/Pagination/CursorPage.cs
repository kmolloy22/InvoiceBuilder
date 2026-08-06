namespace InvoiceBuilder.Application.Shared.Pagination;

public sealed record CursorPage<T>(
	List<T> Items,
	Guid? NextCursor,
	Guid? PreviousCursor,
	int PageSize);
