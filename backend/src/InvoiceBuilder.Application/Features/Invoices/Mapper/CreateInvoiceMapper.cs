using InvoiceBuilder.Application.Features.Invoices.Models.Create;
using InvoiceBuilder.Domain.Entities;

namespace InvoiceBuilder.Application.Features.Invoices.Mapper;

internal static class CreateInvoiceMapper
{
	public static Invoice MapToInvoice(this CreateInvoiceDto request)
		=> Invoice.Create(
			request.InvoiceNumber,
			request.InvoiceDate,
			request.DueDate,
			request.Currency,
			request.Notes,
			request.CustomerId,
			request.SenderId,
			request.Subtotal,
			request.TaxRate,
			request.TotalAmount,
			request.LineItems
				.Select(x => new InvoiceLineItem
				{
					Id = Guid.NewGuid(),
					ItemName = x.ItemName,
					Quantity = x.Quantity,
					UnitPrice = x.UnitPrice,
					Total = x.Total
				}).ToList()
		);
}
