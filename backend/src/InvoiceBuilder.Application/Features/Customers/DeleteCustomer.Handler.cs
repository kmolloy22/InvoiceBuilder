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

		// Invoice -> Customer is DeleteBehavior.Restrict; report it as a conflict instead of letting the FK violation surface as a 500.
		var invoiceCount = await _dbContext.Invoices.CountAsync(i => i.CustomerId == cmd.Id, cancellationToken);
		if (invoiceCount > 0)
		{
			return Result<Success>.Failure(
				new ResultError(
					"CustomerInUse",
					$"Customer \"{customer.CompanyName}\" is used by {invoiceCount} invoice(s) and cannot be deleted.",
					ResultErrorType.Conflict));
		}

		_dbContext.Customers.Remove(customer);
		await _dbContext.SaveChangesAsync(cancellationToken);

		return Result<Success>.Success(new Success());
	}
}
