using InvoiceBuilder.Application.Features.Senders.Models.Update;
using InvoiceBuilder.Application.Shared.Responses.Senders;
using InvoiceBuilder.Database;
using InvoiceBuilder.Domain.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InvoiceBuilder.Application.Features.Senders;

public record UpdateSenderCommand(Guid Id, UpdateSenderDto Dto) : IRequest<Result<UpdateSenderResult>>;

internal class UpdateSenderHandler : IRequestHandler<UpdateSenderCommand, Result<UpdateSenderResult>>
{
	private readonly InvoiceBuilderContext _dbContext;

	public UpdateSenderHandler(InvoiceBuilderContext dbContext)
	{
		_dbContext = dbContext;
	}

	public async Task<Result<UpdateSenderResult>> Handle(UpdateSenderCommand cmd, CancellationToken cancellationToken)
	{
		var sender = await _dbContext.Senders.FirstOrDefaultAsync(x => x.Id == cmd.Id, cancellationToken);
		if (sender is null)
		{
			return Result<UpdateSenderResult>.Failure(
				new ResultError("SenderNotFound", $"Sender with ID {cmd.Id} was not found.", ResultErrorType.NotFound));
		}

		sender.Update(
			cmd.Dto.SenderCompanyName,
			cmd.Dto.SenderFullName,
			cmd.Dto.SenderAddress,
			cmd.Dto.SenderTaxVatId,
			cmd.Dto.BankDetails);

		await _dbContext.SaveChangesAsync(cancellationToken);

		var result = new UpdateSenderResult(
			sender.Id,
			sender.SenderCompanyName,
			sender.SenderFullName,
			sender.SenderAddress,
			sender.SenderTaxVatId,
			sender.BankDetails);

		return Result<UpdateSenderResult>.Success(result);
	}
}
