namespace InvoiceBuilder.Application.Shared.Responses.Customers;

public sealed record GetCustomersResult(
	List<CustomerListItem> Items,
	Guid? NextCursor,
	Guid? PreviousCursor,
	int PageSize);

public sealed record CustomerListItem(
	Guid Id,
	string CompanyName,
	string CustomerName,
	string CustomerEmail);
