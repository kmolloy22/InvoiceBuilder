using InvoiceBuilder.Api.Features.Invoices.InvoiceCreate;
using InvoiceBuilder.Api.Features.Invoices.InvoiceDelete;
using InvoiceBuilder.Api.Features.Invoices.InvoiceGetById;
using InvoiceBuilder.Api.Features.Invoices.InvoicesGet;
using InvoiceBuilder.Api.Features.Invoices.InvoiceUpdate;

namespace InvoiceBuilder.Api.Features.Invoices;

public static class InvoiceEndpoint
{
	public static void MapInvoices(this IEndpointRouteBuilder app)
	{
		var group = app.MapGroup("api/invoices").WithTags("Invoices");

		group.MapCreateInvoiceEndpoint();
		group.MapGetInvoicesEndpoint();
		group.MapGetByIdInvoiceEndpoint();
		group.MapUpdateInvoiceEndpoint();
		group.MapDeleteInvoiceEndpoint();
	}
}
