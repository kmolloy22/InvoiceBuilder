using InvoiceBuilder.Application.Shared.Responses.Customers;
using InvoiceBuilder.Application.Shared.Responses.Invoices;
using InvoiceBuilder.Application.Shared.Responses.Senders;
using InvoiceBuilder.Domain.Entities;

namespace InvoiceBuilder.Application.Features.Invoices.Mapper;

internal static class GetInvoiceByIdMapper
{
	public static GetInvoiceResult MapToResponse(this Invoice invoice)
		=> new(
			invoice.Id,
			invoice.InvoiceNumber,
			invoice.InvoiceDate,
			invoice.DueDate,
			invoice.Currency,
			invoice.Notes,
			new GetCustomerResult(
				invoice.Customer.Id,
				invoice.Customer.CompanyName,
				invoice.Customer.CustomerName,
				invoice.Customer.CustomerAddress,
				invoice.Customer.PostalCode,
				invoice.Customer.CustomerEmail,
				invoice.Customer.CustomerTaxVatId
			),
			new GetSenderResult(
				invoice.Sender.Id,
				invoice.Sender.SenderCompanyName,
				invoice.Sender.SenderFullName,
				invoice.Sender.SenderAddress,
				invoice.Sender.SenderTaxVatId,
				invoice.Sender.BankDetails
			),
			invoice.Items
				.Select(x => new InvoiceLineItemResult(
					x.Id,
					x.ItemName,
					x.Quantity,
					x.UnitPrice,
					x.Total
				))
				.ToList(),
			invoice.Subtotal,
			invoice.TaxRate,
			invoice.TotalAmount
		);
}
