using InvoiceBuilder.Application.Shared.Responses.Senders;
using InvoiceBuilder.Database;
using InvoiceBuilder.Domain.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InvoiceBuilder.Application.Features.Senders;

public record GetSendersCommand() : IRequest<Result<GetSendersResult>>;

public class GetSendersHandler : IRequestHandler<GetSendersCommand, Result<GetSendersResult>>
{
	private readonly InvoiceBuilderContext _dbContext;

	public GetSendersHandler(InvoiceBuilderContext dbContext)
	{
		_dbContext = dbContext;
	}

	public async Task<Result<GetSendersResult>> Handle(GetSendersCommand cmd, CancellationToken cancellationToken)
	{
		var total = await _dbContext.Senders.AsNoTracking().CountAsync(cancellationToken);

		var items = await _dbContext.Senders
			.AsNoTracking()
			.OrderByDescending(c => c.CreatedAt)
			.Select(c => new SenderListItem(
				c.Id,
				c.SenderCompanyName,
				c.SenderFullName,
				c.SenderTaxVatId))
			.ToListAsync(cancellationToken);

		var dto = new GetSendersResult(
			items,
			0,
			10,
			total
		);

		return Result<GetSendersResult>.Success(dto);
	}
}
