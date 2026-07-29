using InvoiceBuilder.Application.Features.Senders.Models.Create;
using InvoiceBuilder.Application.Shared.Responses.Senders;
using InvoiceBuilder.Database;
using InvoiceBuilder.Domain.Entities;
using InvoiceBuilder.Domain.Results;
using MediatR;

namespace InvoiceBuilder.Application.Features.Senders;

public record CreateSenderCommand(CreateSenderDto Dto) : IRequest<Result<CreateSenderResult>>;

internal class CreateSenderHandler : IRequestHandler<CreateSenderCommand, Result<CreateSenderResult>>
{
	private readonly InvoiceBuilderContext _dbContext;

	public CreateSenderHandler(InvoiceBuilderContext dbContext)
	{
		_dbContext = dbContext;
	}

	public async Task<Result<CreateSenderResult>> Handle(CreateSenderCommand cmd, CancellationToken cancellationToken)
	{
		var entity = Sender.Create(
			cmd.Dto.SenderCompanyName,
			cmd.Dto.SenderFullName,
			cmd.Dto.SenderAddress,
			cmd.Dto.SenderTaxVatId,
			cmd.Dto.BankDetails);

		await _dbContext.Senders.AddAsync(entity, cancellationToken);
		await _dbContext.SaveChangesAsync(cancellationToken);

		return Result<CreateSenderResult>.Success(
			new CreateSenderResult(entity.Id, entity.CreatedAt));
	}
}
