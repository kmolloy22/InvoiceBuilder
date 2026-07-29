using InvoiceBuilder.Application.Features.Senders;
using MediatR;

namespace InvoiceBuilder.Api.Features.Senders.SenderGetById;

public static class GetByIdSenderEndpoint
{
	public static void MapGetByIdSenderEndpoint(this IEndpointRouteBuilder app)
	{
		app.MapGet("/{id:guid}", async (
			string id,
			IMediator mediator) =>

		{
			var dto = await mediator.Send(new GetSenderByIdCommand(Guid.Parse(id)));

			if (dto.IsFailure)
			{
				return Results.NotFound(dto.Error);
			}

			return Results.Ok(dto);
		})
		.WithName("GetSender")
		.WithSummary("Gets a sender by id.")
		.WithDescription("Returns the sender if it exists, otherwise 404.");
	}
}
