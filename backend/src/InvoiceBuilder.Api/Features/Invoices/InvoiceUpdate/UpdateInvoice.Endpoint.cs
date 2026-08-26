using InvoiceBuilder.Api.Shared.Infrastructure.Validation;
using InvoiceBuilder.Application.Features.Invoices;
using InvoiceBuilder.Application.Features.Invoices.Models.Update;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceBuilder.Api.Features.Invoices.InvoiceUpdate;

public static class UpdateInvoiceEndpoint
{
	public static void MapUpdateInvoiceEndpoint(this IEndpointRouteBuilder app)
	{
		app.MapPut("/{id:guid}", async (
			string id,
			[FromBody] UpdateInvoiceDto dto,
			[FromServices] IMediator mediator) =>
		{
			var result = await mediator.Send(new UpdateInvoiceCommand(Guid.Parse(id), dto));
			if (result.IsFailure)
			{
				return Results.NotFound(result.Error);
			}

			return Results.Ok(result.Value);
		})
		.AddEndpointFilter<ValidationFilter<UpdateInvoiceDto>>()
		.WithName("UpdateInvoice")
		.WithSummary("Updates an invoice.")
		.WithDescription("Updates the details for the specified invoice.")
		.Produces(StatusCodes.Status204NoContent)
		.Produces(StatusCodes.Status404NotFound)
		.Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
		.ProducesProblem(StatusCodes.Status500InternalServerError);
	}
}
