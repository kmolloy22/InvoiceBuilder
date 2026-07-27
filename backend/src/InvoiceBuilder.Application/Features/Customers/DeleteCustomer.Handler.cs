using MediatR;

namespace InvoiceBuilder.Application.Features.Customers;

public record DeleteCustomerCommand(Guid Id) : IRequest<bool>;

internal class DeleteCustomerHandler : IRequestHandler<DeleteCustomerCommand, bool>
{
	public async Task<bool> Handle(DeleteCustomerCommand request, CancellationToken cancellationToken)
	{
		return true;
	}
}
