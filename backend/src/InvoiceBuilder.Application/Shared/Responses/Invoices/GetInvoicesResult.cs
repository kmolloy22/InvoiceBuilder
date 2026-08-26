namespace InvoiceBuilder.Application.Shared.Responses.Invoices;

public sealed record GetInvoicesResult(
	List<InvoiceListItem> Items,
	Guid? NextCursor,
	Guid? PreviousCursor,
	int PageSize);

public sealed record InvoiceListItem(
	Guid Id,
	string InvoiceNumber,
	DateTime InvoiceDate,
	DateTime DueDate,
	decimal TotalAmount);
