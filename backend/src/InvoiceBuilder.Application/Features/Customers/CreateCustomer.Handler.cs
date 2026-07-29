using InvoiceBuilder.Application.Features.Customers.Models.Create;
using InvoiceBuilder.Application.Shared.Results;
using InvoiceBuilder.Database;
using InvoiceBuilder.Domain.Entities;
using InvoiceBuilder.Domain.Results;
using MediatR;

namespace InvoiceBuilder.Application.Features.Customers;

public record CreateCustomerCommand(CreateCustomerDto Dto) : IRequest<Result<CreateCustomerResult>>;

internal class CreateCustomerHandler : IRequestHandler<CreateCustomerCommand, Result<CreateCustomerResult>>
{
	private readonly InvoiceBuilderContext _dbContext;

	public CreateCustomerHandler(InvoiceBuilderContext dbContext)
	{
		_dbContext = dbContext;
	}

	public async Task<Result<CreateCustomerResult>> Handle(CreateCustomerCommand cmd, CancellationToken cancellationToken)
	{
		var entity = Customer.Create(
			cmd.Dto.CompanyName,
			cmd.Dto.CustomerName,
			cmd.Dto.CustomerAddress,
			cmd.Dto.PostalCode,
			cmd.Dto.CustomerEmail,
			cmd.Dto.CustomerTaxVatId);

		await _dbContext.Customers.AddAsync(entity, cancellationToken);
		await _dbContext.SaveChangesAsync(cancellationToken);

		return Result<CreateCustomerResult>.Success(
			new CreateCustomerResult(entity.Id, entity.CreatedAt));
	}
}
