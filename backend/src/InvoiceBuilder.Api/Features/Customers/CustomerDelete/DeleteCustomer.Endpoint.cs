using InvoiceBuilder.Application.Features.Customers;
using InvoiceBuilder.Domain.Results;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceBuilder.Api.Features.Customers.CustomerDelete;

public static class DeleteCustomerEndpoint
{
	public static void MapDeleteCustomerEndpoint(this IEndpointRouteBuilder app)
	{
		app.MapDelete("/{id}", async (
			[FromRoute] string id,
			[FromServices] IMediator mediator) =>
		{
			var result = await mediator.Send(new DeleteCustomerCommand(Guid.Parse(id)));
			if(result.IsFailure)
			{
				return result.Error is { Type: ResultErrorType.Conflict }
					? Results.Conflict(result.Error)
					: Results.NotFound(result.Error);
			}

			return Results.NoContent();
		})
		.WithName("DeleteCustomer")
		.WithSummary("Deletes a customer.")
		.WithDescription("Deletes the specified customer if it exists. Returns 204 No Content if successful, 404 Not Found if the customer doesn't exist, 409 Conflict if the customer is still referenced by invoices, or 400 Bad Request for invalid input.")
		.Produces(StatusCodes.Status204NoContent)
		.Produces(StatusCodes.Status404NotFound)
		.Produces<object>(StatusCodes.Status409Conflict, "application/json")
		.Produces<object>(StatusCodes.Status400BadRequest, "application/json")
		.ProducesProblem(StatusCodes.Status500InternalServerError);
	}
}
