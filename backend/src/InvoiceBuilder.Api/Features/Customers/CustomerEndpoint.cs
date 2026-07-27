using InvoiceBuilder.Api.Features.Customers.CustomerCreate;
using InvoiceBuilder.Api.Features.Customers.CustomerDelete;
using InvoiceBuilder.Api.Features.Customers.CustomerGetById;
using InvoiceBuilder.Api.Features.Customers.CustomersGet;
using InvoiceBuilder.Api.Features.Customers.CustomerUpdate;

namespace InvoiceBuilder.Api.Features.Customers;

public static class CustomerEndpoint
{
	public static void MapCustomers(this IEndpointRouteBuilder app)
	{
		var group = app.MapGroup("api/customers").WithTags("Customers");

		group.MapCreateCustomerEndpoint();
		group.MapGetCustomersEndpoint();
		group.MapGetByIdCustomerEndpoint();
		group.MapUpdateCustomerEndpoint();
		group.MapDeleteCustomerEndpoint();
	}
}
