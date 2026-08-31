using InvoiceBuilder.Application.Features.Customers.Models.Create;
using InvoiceBuilder.Application.Features.Customers.Models.Get;
using InvoiceBuilder.Application.Features.Customers.Models.Update;
using InvoiceBuilder.Application.Shared.Responses.Customers;
using Refit;

namespace InvoiceBuilder.Web.Services;

public interface ICustomersApiClient
{
	[Get("/api/customers")]
	Task<GetCustomersResult> GetCustomersAsync([Query] GetCustomersDto request);

	[Post("/api/customers")]
	Task<CreateCustomerResponseDto> CreateCustomerAsync([Body] CreateCustomerDto request);

	[Get("/api/customers/{id}")]
	Task<GetCustomerResult> GetCustomerByIdAsync(Guid id);

	[Put("/api/customers/{id}")]
	Task<UpdateCustomerResult> UpdateCustomerAsync(Guid id, [Body] UpdateCustomerDto request);

	[Delete("/api/customers/{id}")]
	Task DeleteCustomerAsync(Guid id);
}
