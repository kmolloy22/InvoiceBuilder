using InvoiceBuilder.Application.Features.Customers.Models.Get;
using InvoiceBuilder.Application.Shared.Responses.Customers;
using InvoiceBuilder.Database;
using InvoiceBuilder.Domain.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InvoiceBuilder.Application.Features.Customers;

public record GetCustomersCommand(GetCustomersDto Dto) : IRequest<Result<GetCustomersResult>>;

public class GetCustomersHandler : IRequestHandler<GetCustomersCommand, Result<GetCustomersResult>>
{
	private readonly InvoiceBuilderContext _dbContext;

	public GetCustomersHandler(InvoiceBuilderContext dbContext)
	{
		_dbContext = dbContext;
	}
	public async Task<Result<GetCustomersResult>> Handle(GetCustomersCommand cmd, CancellationToken cancellationToken)
	{
		var pageSize = Math.Clamp(cmd.Dto.PageSize, 1, 5);
		var takeAmount = pageSize + 1;
		var isNextPage = cmd.Dto.IsNextPage ?? true;

		var query = _dbContext.Customers
			.AsNoTracking()
			.AsQueryable();

		if (cmd.Dto.Cursor.HasValue)
		{
			query = isNextPage
				? query.Where(c => c.Id > cmd.Dto.Cursor.Value)
				: query.Where(c => c.Id < cmd.Dto.Cursor.Value);
		}

		query = isNextPage
			? query.OrderBy(c => c.Id)
			: query.OrderByDescending(c => c.Id);

		var items = await query
			.Take(takeAmount)
			.Select(c => new CustomerListItem(
				c.Id,
				c.CompanyName,
				c.CustomerName,
				c.CustomerEmail))
			.ToListAsync(cancellationToken);

		var hasMoreInRequestedDirection = items.Count > pageSize;
		if (hasMoreInRequestedDirection)
		{
			items.RemoveAt(items.Count - 1);
		}

		if(!isNextPage)
		{
			items.Reverse();
		}

		var hasNextPage = isNextPage
			? hasMoreInRequestedDirection
			: cmd.Dto.Cursor.HasValue;

		var hasPreviousPage = isNextPage
			? cmd.Dto.Cursor.HasValue
			: hasMoreInRequestedDirection;

		Guid? nextCursor = hasNextPage && items.Count > 0
			? items[^1].Id
			: null;

		Guid? previousCursor = hasPreviousPage && items.Count > 0
			? items[0].Id
			: null;

		var dto = new GetCustomersResult(
			items,
			nextCursor,
			previousCursor,
			pageSize);

		return Result<GetCustomersResult>.Success(dto);
	}
}
