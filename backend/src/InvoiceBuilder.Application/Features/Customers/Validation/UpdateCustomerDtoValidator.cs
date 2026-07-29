using FluentValidation;
using InvoiceBuilder.Application.Features.Customers.Models.Update;

namespace InvoiceBuilder.Application.Features.Customers.Validation;

public sealed class UpdateCustomerDtoValidator : AbstractValidator<UpdateCustomerDto>
{
	public UpdateCustomerDtoValidator()
	{
		RuleFor(x => x.CompanyName).NotEmpty();
		RuleFor(x => x.CustomerName).NotEmpty();
		RuleFor(x => x.CustomerAddress).NotEmpty();
		RuleFor(x => x.PostalCode).NotEmpty();
		RuleFor(x => x.CustomerEmail).NotEmpty().EmailAddress();
		RuleFor(x => x.CustomerTaxVatId).NotEmpty();
	}
}
