using FluentValidation;
using InvoiceBuilder.Application.Features.Senders.Models.Create;

namespace InvoiceBuilder.Application.Features.Senders.Validation;

public class CreateSenderDtoValidator : AbstractValidator<CreateSenderDto>
{
	public CreateSenderDtoValidator()
	{
		RuleFor(x => x.SenderCompanyName).NotEmpty();
		RuleFor(x => x.SenderFullName).NotEmpty();
		RuleFor(x => x.SenderAddress).NotEmpty();
		RuleFor(x => x.SenderTaxVatId).NotEmpty();
		RuleFor(x => x.BankDetails).NotEmpty();
	}
}
