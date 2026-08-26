namespace InvoiceBuilder.Application.Features.Invoices.Models.Get;

public sealed record GetInvoicesDto(
	Guid? Cursor,
	bool? IsNextPage,
	int PageSize = 5);
