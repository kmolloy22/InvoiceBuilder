using InvoiceBuilder.Application.Features.Senders.Models.Get;
using InvoiceBuilder.Application.Shared.Pagination;
using InvoiceBuilder.Application.Shared.Responses.Senders;
using InvoiceBuilder.Database;
using InvoiceBuilder.Domain.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InvoiceBuilder.Application.Features.Senders;

public record GetSendersCommand(GetSendersDto Dto) : IRequest<Result<GetSendersResult>>;

public class GetSendersHandler : IRequestHandler<GetSendersCommand, Result<GetSendersResult>>
{
	private readonly InvoiceBuilderContext _dbContext;

	public GetSendersHandler(InvoiceBuilderContext dbContext)
	{
		_dbContext = dbContext;
	}

	public async Task<Result<GetSendersResult>> Handle(GetSendersCommand cmd, CancellationToken cancellationToken)
	{
		var pageSize = Math.Clamp(cmd.Dto.PageSize, 1, 5);
		var takeAmount = pageSize + 1;
		var isNextPage = cmd.Dto.IsNextPage ?? true;

		var query = _dbContext.Senders
			.AsNoTracking()
			.AsQueryable();

		if (cmd.Dto.Cursor.HasValue)
		{
			query = isNextPage
				? query.Where(s => s.Id > cmd.Dto.Cursor.Value)
				: query.Where(s => s.Id < cmd.Dto.Cursor.Value);
		}

		query = isNextPage
			? query.OrderBy(s => s.Id)
			: query.OrderByDescending(s => s.Id);

		var items = await query
			.Take(takeAmount)
			.Select(s => new SenderListItem(
				s.Id,
				s.SenderCompanyName,
				s.SenderFullName,
				s.SenderTaxVatId))
			.ToListAsync(cancellationToken);

		var page = CursorPagination.CreatePage(
			items,
			pageSize,
			isNextPage,
			cmd.Dto.Cursor.HasValue,
			s => s.Id);

		var dto = new GetSendersResult(
			page.Items,
			page.NextCursor,
			page.PreviousCursor,
			page.PageSize);

		return Result<GetSendersResult>.Success(dto);
	}
}
