using InvoiceBuilder.Application.Features.Senders;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceBuilder.Api.Features.Senders.SenderDelete;

public static class DeleteSenderEndpoint
{
	public static void MapDeleteSenderEndpoint(this IEndpointRouteBuilder app)
	{
		app.MapDelete("/{id}", async (
			[FromRoute] string id,
			[FromServices] IMediator mediator) =>
		{
			var result = await mediator.Send(new DeleteSenderCommand(Guid.Parse(id)));
			if (result.IsFailure)
			{
				return Results.NotFound(result.Error);
			}

			return Results.NoContent();
		})
		.WithName("DeleteSender")
		.WithSummary("Deletes a sender.")
		.WithDescription("Deletes the specified sender if it exists. Returns 204 No Content if successful, 404 Not Found if the sender doesn't exist, or 400 Bad Request for invalid input.")
		.Produces(StatusCodes.Status204NoContent)
		.Produces(StatusCodes.Status404NotFound)
		.Produces<object>(StatusCodes.Status400BadRequest, "application/json")
		.ProducesProblem(StatusCodes.Status500InternalServerError);
	}
}
