using InvoiceBuilder.Application.Shared.Results;
using InvoiceBuilder.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InvoiceBuilder.Application.Features.Customers;

public record GetCustomersCommand() : IRequest<GetCustomersResult>;

public class GetCustomersHandler : IRequestHandler<GetCustomersCommand, GetCustomersResult>
{
	private readonly InvoiceBuilderContext _dbContext;

	public GetCustomersHandler(InvoiceBuilderContext dbContext)
	{
		_dbContext = dbContext;
	}
	public async Task<GetCustomersResult> Handle(GetCustomersCommand cmd, CancellationToken cancellationToken)
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

		return dto;
	}
}
