namespace InvoiceBuilder.Application.Shared.Results;

public sealed record CreateCustomerResult(
	Guid Id, DateTimeOffset CreatedAt);
