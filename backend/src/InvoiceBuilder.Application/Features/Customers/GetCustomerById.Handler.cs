using InvoiceBuilder.Application.Shared.Results;
using MediatR;

namespace InvoiceBuilder.Application.Features.Customers;

public record GetCustomerByIdCommand(Guid CustomerId) : IRequest<GetCustomerResult>;

internal class GetCustomerByIdHandler : IRequestHandler<GetCustomerByIdCommand, GetCustomerResult>
{
	public async Task<GetCustomerResult> Handle(GetCustomerByIdCommand cmd, CancellationToken cancellationToken)
	{
		return new GetCustomerResult(
			Guid.NewGuid(),
			"Company 1",
			"Customer 1",
			"Customer Address 1",
			"Postal Code 1",
			"customer1@example.com",
			"TaxVatId 1"
		);
	}
}
