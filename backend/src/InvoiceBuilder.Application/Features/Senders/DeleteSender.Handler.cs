using InvoiceBuilder.Database;
using InvoiceBuilder.Domain.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InvoiceBuilder.Application.Features.Senders;

public record DeleteSenderCommand(Guid Id) : IRequest<Result<Success>>;

internal class DeleteSenderHandler : IRequestHandler<DeleteSenderCommand, Result<Success>>
{
	private readonly InvoiceBuilderContext _dbContext;

	public DeleteSenderHandler(InvoiceBuilderContext dbContext)
	{
		_dbContext = dbContext;
	}

	public async Task<Result<Success>> Handle(DeleteSenderCommand cmd, CancellationToken cancellationToken)
	{
		var sender = await _dbContext.Senders.FirstOrDefaultAsync(c => c.Id == cmd.Id, cancellationToken);
		if (sender is null)
		{
			return Result<Success>.Failure(
				new ResultError("SenderNotFound", $"Sender with ID {cmd.Id} was not found.", ResultErrorType.NotFound));
		}

		_dbContext.Senders.Remove(sender);
		await _dbContext.SaveChangesAsync(cancellationToken);

		return Result<Success>.Success(new Success());
	}
}
