using InvoiceBuilder.Application.Features.Customers.Models.Update;
using InvoiceBuilder.Application.Shared.Results;
using InvoiceBuilder.Domain.Entities;
using MediatR;

namespace InvoiceBuilder.Application.Features.Customers;

public record UpdateCustomerCommand(Guid Id, UpdateCustomerDto Dto) : IRequest<UpdateCustomerResult>;

internal class UpdateCustomerHandler : IRequestHandler<UpdateCustomerCommand, UpdateCustomerResult>
{
	public async Task<UpdateCustomerResult> Handle(UpdateCustomerCommand cmd, CancellationToken cancellationToken)
	{
		var customer = new Customer();

		customer.Update(
			cmd.Dto.CompanyName,
			cmd.Dto.CustomerName,
			cmd.Dto.CustomerAddress,
			cmd.Dto.PostalCode,
			cmd.Dto.CustomerEmail,
			cmd.Dto.CustomerTaxVatId);

		return new UpdateCustomerResult(
			customer.Id,
			customer.CompanyName,
			customer.CustomerName,
			customer.CustomerAddress,
			customer.PostalCode,
			customer.CustomerEmail,
			customer.CustomerTaxVatId);
	}
}
