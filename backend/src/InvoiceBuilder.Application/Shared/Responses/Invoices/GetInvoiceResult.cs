using InvoiceBuilder.Application.Shared.Responses.Customers;
using InvoiceBuilder.Application.Shared.Responses.Senders;

namespace InvoiceBuilder.Application.Shared.Responses.Invoices;

public sealed record GetInvoiceResult(
	Guid Id,
	string InvoiceNumber,
	DateTime InvoiceDate,
	DateTime DueDate,
	string Currency,
	string Notes,
	GetCustomerResult Customer,
	GetSenderResult Sender,
	List<InvoiceLineItemResult> LineItems,
	decimal Subtotal,
	decimal TaxRate,
	decimal TotalAmount
);
