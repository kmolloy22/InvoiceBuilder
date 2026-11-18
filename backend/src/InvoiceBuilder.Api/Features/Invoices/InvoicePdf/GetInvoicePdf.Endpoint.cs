using InvoiceBuilder.Application.Features.Invoices;
using MediatR;

namespace InvoiceBuilder.Api.Features.Invoices.InvoicePdf;

public static class GetInvoicePdfEndpoint
{
	public static void MapGetInvoicePdfEndpoint(this IEndpointRouteBuilder app)
	{
		app.MapGet("/{id:guid}/pdf", async (
			string id,
			IMediator mediator) =>
		{
			var result = await mediator.Send(new GetInvoicePdfCommand(Guid.Parse(id)));

			if (result.IsFailure)
			{
				return Results.NotFound(result.Error);
			}

			return Results.File(result.Value!.Content, "application/pdf", result.Value.FileName);
		})
		.WithName("GetInvoicePdf")
		.WithSummary("Downloads an invoice as a PDF.")
		.WithDescription("Returns the invoice rendered as a PDF file, or 404 if it doesn't exist.");
	}
}
