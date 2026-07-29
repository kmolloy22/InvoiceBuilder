using InvoiceBuilder.Application.Shared.Responses.Customers;
using InvoiceBuilder.Database;
using InvoiceBuilder.Domain.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InvoiceBuilder.Application.Features.Customers;

public record GetCustomerByIdCommand(Guid CustomerId) : IRequest<Result<GetCustomerResult>>;

internal class GetCustomerByIdHandler : IRequestHandler<GetCustomerByIdCommand, Result<GetCustomerResult>>
{
	private readonly InvoiceBuilderContext _dbContext;

	public GetCustomerByIdHandler(InvoiceBuilderContext dbContext)
	{
		_dbContext = dbContext;
	}

	public async Task<Result<GetCustomerResult>> Handle(GetCustomerByIdCommand cmd, CancellationToken cancellationToken)
	{
		var customer = await _dbContext.Customers
			.AsNoTracking()
			.FirstOrDefaultAsync(x => x.Id == cmd.CustomerId, cancellationToken);

		if (customer is null)
		{
			return Result<GetCustomerResult>.Failure(
				new ResultError("CustomerNotFound", $"Customer with ID {cmd.CustomerId} was not found.", ResultErrorType.NotFound));
		}

		var result = new GetCustomerResult(
			customer.Id,
			customer.CompanyName,
			customer.CustomerName,
			customer.CustomerAddress,
			customer.PostalCode,
			customer.CustomerEmail,
			customer.CustomerTaxVatId);

		return Result<GetCustomerResult>.Success(result);
	}
}
