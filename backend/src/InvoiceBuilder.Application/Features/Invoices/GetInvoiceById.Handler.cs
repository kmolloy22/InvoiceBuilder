using InvoiceBuilder.Application.Features.Invoices.Mapper;
using InvoiceBuilder.Application.Shared.Responses.Invoices;
using InvoiceBuilder.Database;
using InvoiceBuilder.Domain.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InvoiceBuilder.Application.Features.Invoices;

public record GetInvoiceByIdCommand(Guid Id) : IRequest<Result<GetInvoiceResult>>;

internal class GetInvoiceByIdHandler : IRequestHandler<GetInvoiceByIdCommand, Result<GetInvoiceResult>>
{
	private readonly InvoiceBuilderContext _dbContext;

	public GetInvoiceByIdHandler(InvoiceBuilderContext dbContext)
	{
		_dbContext = dbContext;
	}

	public async Task<Result<GetInvoiceResult>> Handle(GetInvoiceByIdCommand cmd, CancellationToken cancellationToken)
	{
		var invoice = await _dbContext.Invoices
			.Include(x => x.Customer)
			.Include(x => x.Sender)
			.Include(x => x.Items)
			.FirstOrDefaultAsync(x => x.Id == cmd.Id, cancellationToken);

		if (invoice is null)
		{
			return Result<GetInvoiceResult>.Failure(
				new ResultError("InvoiceNotFound", $"Invoice with ID {cmd.Id} was not found.", ResultErrorType.NotFound));
		}

		var result = invoice.MapToResponse();

		return Result<GetInvoiceResult>.Success(result);
	}
}
