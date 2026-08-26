using InvoiceBuilder.Application.Features.Invoices.Mapper;
using InvoiceBuilder.Application.Features.Invoices.Models.Create;
using InvoiceBuilder.Application.Shared.Responses.Invoices;
using InvoiceBuilder.Database;
using InvoiceBuilder.Domain.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InvoiceBuilder.Application.Features.Invoices;

public record CreateInvoiceCommand(CreateInvoiceDto dto) : IRequest<Result<CreateInvoiceResult>>;

internal class CreateInvoiceHandler : IRequestHandler<CreateInvoiceCommand, Result<CreateInvoiceResult>>
{
	private readonly InvoiceBuilderContext _dbContext;

	public CreateInvoiceHandler(InvoiceBuilderContext dbContext)
	{
		_dbContext = dbContext;
	}

	public async Task<Result<CreateInvoiceResult>> Handle(CreateInvoiceCommand cmd, CancellationToken cancellationToken)
	{
		var invoiceExists = await _dbContext.Invoices.AnyAsync(x => x.InvoiceNumber == cmd.dto.InvoiceNumber, cancellationToken);
		if (invoiceExists)
		{
			return Result<CreateInvoiceResult>.Failure(
				new ResultError("InvoiceAlreadyExists", $"Invoice with number {cmd.dto.InvoiceNumber} already exists.", ResultErrorType.Conflict));
		}

		var customerExists = await _dbContext.Customers.AnyAsync(x => x.Id == cmd.dto.CustomerId, cancellationToken);
		if (!customerExists)
		{
			return Result<CreateInvoiceResult>.Failure(
				new ResultError("CustomerNotFound", $"Customer with ID {cmd.dto.CustomerId} was not found.", ResultErrorType.NotFound));
		}

		var senderExists = await _dbContext.Senders.AnyAsync(x => x.Id == cmd.dto.SenderId, cancellationToken);
		if (!senderExists)
		{
			return Result<CreateInvoiceResult>.Failure(
				new ResultError("SenderNotFound", $"Sender with ID {cmd.dto.SenderId} was not found.", ResultErrorType.NotFound));
		}

		var invoice = cmd.dto.MapToInvoice();

		await _dbContext.Invoices.AddAsync(invoice, cancellationToken);
		await _dbContext.SaveChangesAsync(cancellationToken);

		return Result<CreateInvoiceResult>.Success(
			new CreateInvoiceResult(invoice.Id, invoice.CreatedAt));
	}
}
