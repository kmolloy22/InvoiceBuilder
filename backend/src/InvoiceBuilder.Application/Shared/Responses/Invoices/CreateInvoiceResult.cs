namespace InvoiceBuilder.Application.Shared.Responses.Invoices;

public sealed record CreateInvoiceResult(
	Guid Id, DateTimeOffset CreatedAt);
