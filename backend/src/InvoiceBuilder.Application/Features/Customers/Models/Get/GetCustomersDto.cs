namespace InvoiceBuilder.Application.Features.Customers.Models.Get;

public sealed record GetCustomersDto(
	Guid? Cursor,
	bool? IsNextPage,
	int PageSize = 5);
