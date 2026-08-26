using InvoiceBuilder.Application.Features.Invoices;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceBuilder.Api.Features.Invoices.InvoiceDelete;

public static class DeleteInvoiceEndpoint

{
	public static void MapDeleteInvoiceEndpoint(this IEndpointRouteBuilder app)
	{
		app.MapDelete("/{id}", async (
			[FromRoute] string id,
			[FromServices] IMediator mediator) =>
		{
			var result = await mediator.Send(new DeleteInvoiceCommand(Guid.Parse(id)));
			if (result.IsFailure)
			{
				return Results.NotFound(result.Error);
			}

			return Results.NoContent();
		})
		.WithName("DeleteInvoice")
		.WithSummary("Deletes an invoice.")
		.WithDescription("Deletes the specified invoice if it exists. Returns 204 No Content if successful, 404 Not Found if the invoice doesn't exist, or 400 Bad Request for invalid input.")
		.Produces(StatusCodes.Status204NoContent)
		.Produces(StatusCodes.Status404NotFound)
		.Produces<object>(StatusCodes.Status400BadRequest, "application/json")
		.ProducesProblem(StatusCodes.Status500InternalServerError);
	}
}
