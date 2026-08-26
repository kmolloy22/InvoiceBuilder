namespace InvoiceBuilder.Application.Shared.Responses.Invoices;

public sealed record InvoiceLineItemResult(
	Guid Id,
	string ItemName,
	decimal Quantity,
	decimal UnitPrice,
	decimal Total
);
