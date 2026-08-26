namespace InvoiceBuilder.Application.Features.Invoices.Models.Create;

public sealed record CreateInvoiceDto(
	string InvoiceNumber,
	DateTime InvoiceDate,
	DateTime DueDate,
	string Currency,
	string Notes,
	Guid CustomerId,
	Guid SenderId,
	decimal Subtotal,
	decimal TaxRate,
	decimal TotalAmount,
	List<InvoiceLineItemDto> LineItems);
