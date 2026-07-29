using InvoiceBuilder.Application.Shared.Responses.Senders;
using InvoiceBuilder.Database;
using InvoiceBuilder.Domain.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InvoiceBuilder.Application.Features.Senders;

public record GetSenderByIdCommand(Guid SenderId) : IRequest<Result<GetSenderResult>>;

internal class GetSenderByIdHandler : IRequestHandler<GetSenderByIdCommand, Result<GetSenderResult>>
{
	private readonly InvoiceBuilderContext _dbContext;

	public GetSenderByIdHandler(InvoiceBuilderContext dbContext)
	{
		_dbContext = dbContext;
	}

	public async Task<Result<GetSenderResult>> Handle(GetSenderByIdCommand cmd, CancellationToken cancellationToken)
	{
		var sender = await _dbContext.Senders
			.AsNoTracking()
			.FirstOrDefaultAsync(x => x.Id == cmd.SenderId, cancellationToken);

		if (sender is null)
		{
			return Result<GetSenderResult>.Failure(
				new ResultError("SenderNotFound", $"Sender with ID {cmd.SenderId} was not found.", ResultErrorType.NotFound));
		}

		var result = new GetSenderResult(
			sender.Id,
			sender.SenderCompanyName,
			sender.SenderFullName,
			sender.SenderAddress,
			sender.SenderTaxVatId,
			sender.BankDetails
		);
		return Result<GetSenderResult>.Success(result);
	}
}
