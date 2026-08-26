using InvoiceBuilder.Database;
using InvoiceBuilder.Domain.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InvoiceBuilder.Application.Features.Invoices;

public record DeleteInvoiceCommand(Guid Id) : IRequest<Result<Success>>;

internal class DeleteInvoiceHandler : IRequestHandler<DeleteInvoiceCommand, Result<Success>>

{
	private readonly InvoiceBuilderContext _dbContext;

	public DeleteInvoiceHandler(InvoiceBuilderContext dbContext)
	{
		_dbContext = dbContext;
	}

	public async Task<Result<Success>> Handle(DeleteInvoiceCommand cmd, CancellationToken cancellationToken)
	{
		var invoice = await _dbContext.Invoices.FirstOrDefaultAsync(c => c.Id == cmd.Id, cancellationToken);
		if (invoice is null)
		{
			return Result<Success>.Failure(
				new ResultError("InvoiceNotFound", $"Invoice with ID {cmd.Id} was not found.", ResultErrorType.NotFound));
		}

		_dbContext.Invoices.Remove(invoice);
		await _dbContext.SaveChangesAsync(cancellationToken);

		return Result<Success>.Success(new Success());
	}
}
