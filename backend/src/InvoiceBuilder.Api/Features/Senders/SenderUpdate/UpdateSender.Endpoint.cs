using InvoiceBuilder.Api.Shared.Infrastructure.Validation;
using InvoiceBuilder.Application.Features.Senders;
using InvoiceBuilder.Application.Features.Senders.Models.Update;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceBuilder.Api.Features.Senders.SenderUpdate;

public static class UpdateSenderEndpoint
{
	public static void MapUpdateSenderEndpoint(this IEndpointRouteBuilder app)
	{
		app.MapPut("/{id:guid}", async (
			string id,
			[FromBody] UpdateSenderDto dto,
			[FromServices] IMediator mediator) =>
		{
			var result = await mediator.Send(new UpdateSenderCommand(Guid.Parse(id), dto));
			if (result.IsFailure)
			{
				return Results.NotFound(result.Error);
			}

			return Results.Ok(result);
		})
		.AddEndpointFilter<ValidationFilter<UpdateSenderDto>>()
		.WithName("UpdateSender")
		.WithSummary("Updates a sender.")
		.WithDescription("Updates first, last name and/or address for the specified sender.")
		.Produces(StatusCodes.Status204NoContent)
		.Produces(StatusCodes.Status404NotFound)
		.Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
		.ProducesProblem(StatusCodes.Status500InternalServerError);
	}
}
