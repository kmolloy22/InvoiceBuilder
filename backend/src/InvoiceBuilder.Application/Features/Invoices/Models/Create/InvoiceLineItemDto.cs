namespace InvoiceBuilder.Application.Features.Invoices.Models.Create;

public sealed record InvoiceLineItemDto(
	string ItemName,
	decimal Quantity,
	decimal UnitPrice,
	decimal Total);
