namespace InvoiceBuilder.Application.Features.Invoices.Models.Update;

public sealed record UpdateInvoiceDto(
	string InvoiceNumber,
	DateTime InvoiceDate,
	DateTime DueDate,
	string Currency,
	string Notes,
	Guid CustomerId,
	Guid SenderId,
	decimal Subtotal,
	decimal TaxRate,
	decimal TotalAmount);
