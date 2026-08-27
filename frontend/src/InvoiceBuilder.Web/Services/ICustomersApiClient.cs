using InvoiceBuilder.Application.Features.Customers.Models.Get;
using InvoiceBuilder.Application.Shared.Responses.Customers;
using Refit;

namespace InvoiceBuilder.Web.Services;

public interface ICustomersApiClient
{
	[Get("/api/customers")]
	Task<GetCustomersResult> GetCustomersAsync([Query] GetCustomersDto request);
}
