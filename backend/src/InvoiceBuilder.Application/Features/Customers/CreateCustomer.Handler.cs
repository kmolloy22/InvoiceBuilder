using InvoiceBuilder.Application.Features.Customers.Models.Create;
using InvoiceBuilder.Application.Shared.Results;
using InvoiceBuilder.Domain.Entities;
using MediatR;

namespace InvoiceBuilder.Application.Features.Customers;

public record CreateCustomerCommand(CreateCustomerDto Dto) : IRequest<CreateCustomerResult>;

internal class CreateCustomerHandler : IRequestHandler<CreateCustomerCommand, CreateCustomerResult>
{
	public async Task<CreateCustomerResult> Handle(CreateCustomerCommand cmd, CancellationToken cancellationToken)
	{
		var entity = Customer.Create(
			cmd.Dto.CompanyName,
			cmd.Dto.CustomerName,
			cmd.Dto.CustomerAddress,
			cmd.Dto.PostalCode,
			cmd.Dto.CustomerEmail,
			cmd.Dto.CustomerTaxVatId);

		return new CreateCustomerResult(entity.Id, entity.CreatedAt);
	}
}
