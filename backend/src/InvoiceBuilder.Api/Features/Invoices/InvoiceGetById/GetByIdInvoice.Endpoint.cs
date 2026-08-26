using InvoiceBuilder.Application.Features.Invoices;
using MediatR;

namespace InvoiceBuilder.Api.Features.Invoices.InvoiceGetById;

public static class GetByIdInvoiceEndpoint
{
	public static void MapGetByIdInvoiceEndpoint(this IEndpointRouteBuilder app)
	{
		app.MapGet("/{id:guid}", async (
			string id,
			IMediator mediator) =>

		{
			var result = await mediator.Send(new GetInvoiceByIdCommand(Guid.Parse(id)));

			if (result.IsFailure)
			{
				return Results.NotFound(result.Error);
			}

			return Results.Ok(result.Value);
		})
		.WithName("GetInvoice")
		.WithSummary("Gets an invoice by id.")
		.WithDescription("Returns the invoice if it exists, otherwise 404.");
	}
}
