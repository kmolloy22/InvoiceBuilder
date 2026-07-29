namespace InvoiceBuilder.Application.Shared.Responses.Customers;

public sealed record CreateCustomerResult(
	Guid Id, DateTimeOffset CreatedAt);
