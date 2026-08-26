using FluentValidation;
using InvoiceBuilder.Application.Features.Invoices.Models.Create;

namespace InvoiceBuilder.Application.Features.Invoices.Validation;

public class CreateInvoiceDtoValidator : AbstractValidator<CreateInvoiceDto>
{
	public CreateInvoiceDtoValidator()
	{
		RuleFor(invoice => invoice.InvoiceNumber).NotEmpty();
		RuleFor(invoice => invoice.InvoiceDate).NotEmpty();
		RuleFor(invoice => invoice.DueDate).NotEmpty().GreaterThanOrEqualTo(invoice => invoice.InvoiceDate);
		RuleFor(invoice => invoice.Currency).NotEmpty();
		RuleFor(invoice => invoice.Notes).NotEmpty();
		RuleFor(invoice => invoice.CustomerId).NotEmpty();
		RuleFor(invoice => invoice.SenderId).NotEmpty();
		RuleFor(invoice => invoice.Subtotal).GreaterThan(0);
		RuleFor(invoice => invoice.TaxRate).GreaterThanOrEqualTo(0);
		RuleFor(invoice => invoice.TotalAmount).GreaterThan(0);
		RuleFor(invoice => invoice.LineItems).NotEmpty();
	}
}
