using InvoiceBuilder.Application.Shared.Results;
using InvoiceBuilder.Database;
using InvoiceBuilder.Domain.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InvoiceBuilder.Application.Features.Customers;

public record GetCustomersCommand() : IRequest<Result<GetCustomersResult>>;

public class GetCustomersHandler : IRequestHandler<GetCustomersCommand, Result<GetCustomersResult>>
{
	private readonly InvoiceBuilderContext _dbContext;

	public GetCustomersHandler(InvoiceBuilderContext dbContext)
	{
		_dbContext = dbContext;
	}
	public async Task<Result<GetCustomersResult>> Handle(GetCustomersCommand cmd, CancellationToken cancellationToken)
	{
		var total = await _dbContext.Customers.AsNoTracking().CountAsync(cancellationToken);

		var items = await _dbContext.Customers
			.AsNoTracking()
			.OrderByDescending(c => c.CreatedAt)
			.Select(c => new CustomerListItem(
				c.Id,
				c.CompanyName,
				c.CustomerName,
				c.CustomerEmail))
			.ToListAsync(cancellationToken);


		var dto = new GetCustomersResult(
			items,
			0,
			10,
			total
		);

		return Result<GetCustomersResult>.Success(dto);
	}
}
