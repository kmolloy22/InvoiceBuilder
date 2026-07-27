using InvoiceBuilder.Application.Shared.Results;
using MediatR;

namespace InvoiceBuilder.Application.Features.Customers;

public record GetCustomersCommand() : IRequest<GetCustomersResult>;

public class GetCustomersHandler : IRequestHandler<GetCustomersCommand, GetCustomersResult>
{
	public async Task<GetCustomersResult> Handle(GetCustomersCommand cmd, CancellationToken cancellationToken)
	{

		var dto = new GetCustomersResult(
			new List<CustomerListItem>
			{
				new CustomerListItem(
					Guid.NewGuid(),
					"Company 1",
					"Customer 1",
					"customer1@example.com")
			},
			0,
			10,
			1
		);

		return dto;
	}
}
