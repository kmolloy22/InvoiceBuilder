using InvoiceBuilder.Application.Features.Invoices.Mapper;
using InvoiceBuilder.Application.Features.Invoices.Models.Update;
using InvoiceBuilder.Application.Shared.Responses.Invoices;
using InvoiceBuilder.Database;
using InvoiceBuilder.Domain.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InvoiceBuilder.Application.Features.Invoices;

public record UpdateInvoiceCommand(Guid Id, UpdateInvoiceDto Dto) : IRequest<Result<GetInvoiceResult>>;

public class UpdateInvoiceHandler : IRequestHandler<UpdateInvoiceCommand, Result<GetInvoiceResult>>
{
	private readonly InvoiceBuilderContext _dbContext;

	public UpdateInvoiceHandler(InvoiceBuilderContext dbContext)
	{
		_dbContext = dbContext;
	}

	public async Task<Result<GetInvoiceResult>> Handle(UpdateInvoiceCommand cmd, CancellationToken cancellationToken)
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

		var customerExists = await _dbContext.Customers.AnyAsync(x => x.Id == cmd.Dto.CustomerId, cancellationToken);
		if (!customerExists)
		{
			return Result<GetInvoiceResult>.Failure(
				new ResultError("CustomerNotFound", $"Customer with ID {cmd.Dto.CustomerId} was not found.", ResultErrorType.NotFound));
		}

		var senderExists = await _dbContext.Senders.AnyAsync(x => x.Id == cmd.Dto.SenderId, cancellationToken);
		if (!senderExists)
		{
			return Result<GetInvoiceResult>.Failure(
				new ResultError("SenderNotFound", $"Sender with ID {cmd.Dto.SenderId} was not found.", ResultErrorType.NotFound));
		}

		if (cmd.Dto.InvoiceNumber != invoice.InvoiceNumber)
		{
			var invoiceNumberTaken = await _dbContext.Invoices.AnyAsync(
				x => x.InvoiceNumber == cmd.Dto.InvoiceNumber && x.Id != invoice.Id, cancellationToken);
			if (invoiceNumberTaken)
			{
				return Result<GetInvoiceResult>.Failure(
					new ResultError("InvoiceAlreadyExists", $"Invoice with number {cmd.Dto.InvoiceNumber} already exists.", ResultErrorType.Conflict));
			}
		}

		invoice.Update(
			cmd.Dto.InvoiceNumber,
			cmd.Dto.InvoiceDate,
			cmd.Dto.DueDate,
			cmd.Dto.Currency,
			cmd.Dto.Notes,
			cmd.Dto.CustomerId,
			cmd.Dto.SenderId,
			cmd.Dto.Subtotal,
			cmd.Dto.TaxRate,
			cmd.Dto.TotalAmount);

		await _dbContext.SaveChangesAsync(cancellationToken);

		// Customer/Sender may have changed; the navigation properties loaded via the
		// initial Include() still point at the previous related entities (or are nulled
		// by EF's FK-mismatch fixup), so reload them before mapping the response.
		await _dbContext.Entry(invoice).Reference(x => x.Customer).LoadAsync(cancellationToken);
		await _dbContext.Entry(invoice).Reference(x => x.Sender).LoadAsync(cancellationToken);

		var result = invoice.MapToResponse();

		return Result<GetInvoiceResult>.Success(result);
	}
}
