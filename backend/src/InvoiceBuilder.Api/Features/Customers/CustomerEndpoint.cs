using InvoiceBuilder.Api.Features.Customers.CustomerCreate;
using InvoiceBuilder.Api.Features.Customers.CustomersGet;

namespace InvoiceBuilder.Api.Features.Customers;

public static class CustomerEndpoint
{
	public static void MapCustomers(this IEndpointRouteBuilder app)
	{
		var group = app.MapGroup("api/customers").WithTags("Customers");

		group.MapCreateCustomerEndpoint();
		group.MapGetCustomersEndpoint();
	}
}
