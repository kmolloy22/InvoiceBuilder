using InvoiceBuilder.Application.Features.Customers.Models;
using InvoiceBuilder.Application.Shared.Results;
using InvoiceBuilder.Domain.Entities;
using MediatR;

namespace InvoiceBuilder.Application.Features.Customers;

public record CreateCustomerCommand(CreateCustomerDto Dto) : IRequest<CreateCustomerResult>;

public class CreateCustomerHandler : IRequestHandler<CreateCustomerCommand, CreateCustomerResult>
{
	public async Task<CreateCustomerResult> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
	{
		var entity = Customer.Create(
			request.Dto.CompanyName,
			request.Dto.CustomerName,
			request.Dto.CustomerAddress,
			request.Dto.PostalCode,
			request.Dto.CustomerEmail,
			request.Dto.CustomerTaxVatId);

		return new CreateCustomerResult(entity.Id, entity.CreatedAt);
	}
}
