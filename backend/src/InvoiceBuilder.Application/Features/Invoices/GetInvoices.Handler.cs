using InvoiceBuilder.Application.Features.Invoices.Models.Get;
using InvoiceBuilder.Application.Shared.Pagination;
using InvoiceBuilder.Application.Shared.Responses.Invoices;
using InvoiceBuilder.Database;
using InvoiceBuilder.Domain.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InvoiceBuilder.Application.Features.Invoices;

public record GetInvoicesCommand(GetInvoicesDto Dto) : IRequest<Result<GetInvoicesResult>>;

internal class GetInvoicesHandler : IRequestHandler<GetInvoicesCommand, Result<GetInvoicesResult>>
{
	private readonly InvoiceBuilderContext _dbContext;

	public GetInvoicesHandler(InvoiceBuilderContext dbContext)
	{
		_dbContext = dbContext;
	}

	public async Task<Result<GetInvoicesResult>> Handle(GetInvoicesCommand cmd, CancellationToken cancellationToken)
	{
		var pageSize = Math.Clamp(cmd.Dto.PageSize, 1, 5);
		var takeAmount = pageSize + 1;
		var isNextPage = cmd.Dto.IsNextPage ?? true;

		var query = _dbContext.Invoices
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
			.Select(i => new InvoiceListItem(
				i.Id,
				i.InvoiceNumber,
				i.InvoiceDate,
				i.DueDate,
				i.TotalAmount))
			.ToListAsync(cancellationToken);

		var page = CursorPagination.CreatePage(
			items,
			pageSize,
			isNextPage,
			cmd.Dto.Cursor.HasValue,
			s => s.Id);

		var dto = new GetInvoicesResult(
			page.Items,
			page.NextCursor,
			page.PreviousCursor,
			page.PageSize);

		return Result<GetInvoicesResult>.Success(dto);
	}
}
