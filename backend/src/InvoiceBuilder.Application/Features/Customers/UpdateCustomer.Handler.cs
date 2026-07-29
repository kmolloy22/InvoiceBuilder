using InvoiceBuilder.Application.Features.Customers.Models.Update;
using InvoiceBuilder.Application.Shared.Results;
using InvoiceBuilder.Database;
using InvoiceBuilder.Domain.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InvoiceBuilder.Application.Features.Customers;

public record UpdateCustomerCommand(Guid Id, UpdateCustomerDto Dto) : IRequest<Result<UpdateCustomerResult>>;

internal class UpdateCustomerHandler : IRequestHandler<UpdateCustomerCommand, Result<UpdateCustomerResult>>
{
	private readonly InvoiceBuilderContext _dbContext;

	public UpdateCustomerHandler(InvoiceBuilderContext dbContext)
	{
		_dbContext = dbContext;
	}

	public async Task<Result<UpdateCustomerResult>> Handle(UpdateCustomerCommand cmd, CancellationToken cancellationToken)
	{
		var customer = await _dbContext.Customers.FirstOrDefaultAsync(x => x.Id == cmd.Id, cancellationToken);
		if (customer is null)
		{
			return Result<UpdateCustomerResult>.Failure(
				new ResultError("CustomerNotFound", $"Customer with ID {cmd.Id} was not found.", ResultErrorType.NotFound));
		}

		customer.Update(
			cmd.Dto.CompanyName,
			cmd.Dto.CustomerName,
			cmd.Dto.CustomerAddress,
			cmd.Dto.PostalCode,
			cmd.Dto.CustomerEmail,
			cmd.Dto.CustomerTaxVatId);

		await _dbContext.SaveChangesAsync(cancellationToken);

		var result = new UpdateCustomerResult(
			customer.Id,
			customer.CompanyName,
			customer.CustomerName,
			customer.CustomerAddress,
			customer.PostalCode,
			customer.CustomerEmail,
			customer.CustomerTaxVatId);

		return Result<UpdateCustomerResult>.Success(result);
	}
}
