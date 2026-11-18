using InvoiceBuilder.Application.Features.Invoices.Mapper;
using InvoiceBuilder.Application.Features.Invoices.Pdf;
using InvoiceBuilder.Application.Shared.Responses.Invoices;
using InvoiceBuilder.Database;
using InvoiceBuilder.Domain.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;

namespace InvoiceBuilder.Application.Features.Invoices;

public record GetInvoicePdfCommand(Guid Id) : IRequest<Result<InvoicePdfResult>>;

internal class GetInvoicePdfHandler : IRequestHandler<GetInvoicePdfCommand, Result<InvoicePdfResult>>
{
	private readonly InvoiceBuilderContext _dbContext;

	public GetInvoicePdfHandler(InvoiceBuilderContext dbContext)
	{
		_dbContext = dbContext;
	}

	public async Task<Result<InvoicePdfResult>> Handle(GetInvoicePdfCommand cmd, CancellationToken cancellationToken)
	{
		var invoice = await _dbContext.Invoices
			.Include(x => x.Customer)
			.Include(x => x.Sender)
			.Include(x => x.Items)
			.FirstOrDefaultAsync(x => x.Id == cmd.Id, cancellationToken);

		if (invoice is null)
		{
			return Result<InvoicePdfResult>.Failure(
				new ResultError("InvoiceNotFound", $"Invoice with ID {cmd.Id} was not found.", ResultErrorType.NotFound));
		}

		var response = invoice.MapToResponse();
		var content = new InvoicePdfDocument(response).GeneratePdf();
		var fileName = $"{invoice.InvoiceNumber}.pdf";

		return Result<InvoicePdfResult>.Success(new InvoicePdfResult(fileName, content));
	}
}
