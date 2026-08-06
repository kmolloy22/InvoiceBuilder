using InvoiceBuilder.Application.Features.Senders;
using InvoiceBuilder.Application.Features.Senders.Models.Get;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceBuilder.Api.Features.Senders.SendersGet;

public static class GetSendersEndpoint
{
	public static void MapGetSendersEndpoint(this IEndpointRouteBuilder app)
	{
		app.MapGet("/", async (
			[AsParameters] GetSendersDto request,
			[FromServices] IMediator mediator) =>
		{
			var result = await mediator.Send(new GetSendersCommand(request));
			if (result.IsFailure)
			{
				return Results.NotFound(result.Error);
			}

			return Results.Ok(result);
		})
		.WithName("GetSenders")
		.WithSummary("Lists senders with pagination support.")
		.WithDescription("Returns a list of senders.")
		.Produces<object>(StatusCodes.Status400BadRequest, "application/json")
		.ProducesProblem(StatusCodes.Status500InternalServerError);
	}
}
