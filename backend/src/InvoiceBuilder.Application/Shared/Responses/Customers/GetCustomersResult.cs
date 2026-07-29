namespace InvoiceBuilder.Application.Shared.Responses.Customers;

public sealed record GetCustomersResult(
	List<CustomerListItem> Items,
	int Offset,
	int Limit,
	int Total);

public sealed record CustomerListItem(
	Guid Id,
	string CompanyName,
	string CustomerName,
	string CustomerEmail);
