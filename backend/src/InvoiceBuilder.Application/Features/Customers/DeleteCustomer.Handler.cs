using InvoiceBuilder.Database;
using InvoiceBuilder.Domain.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InvoiceBuilder.Application.Features.Customers;

public record DeleteCustomerCommand(Guid Id) : IRequest<Result<Success>>;

internal class DeleteCustomerHandler : IRequestHandler<DeleteCustomerCommand, Result<Success>>
{
	private readonly InvoiceBuilderContext _dbContext;

	public DeleteCustomerHandler(InvoiceBuilderContext dbContext)
	{
		_dbContext = dbContext;
	}

	public async Task<Result<Success>> Handle(DeleteCustomerCommand cmd, CancellationToken cancellationToken)
	{
		var customer = await _dbContext.Customers.FirstOrDefaultAsync(c => c.Id == cmd.Id, cancellationToken);
		if (customer is null)
		{
			return Result<Success>.Failure(
				new ResultError("CustomerNotFound", $"Customer with ID {cmd.Id} was not found.", ResultErrorType.NotFound));
		}

		_dbContext.Customers.Remove(customer);
		await _dbContext.SaveChangesAsync(cancellationToken);

		return Result<Success>.Success(new Success());
	}
}
