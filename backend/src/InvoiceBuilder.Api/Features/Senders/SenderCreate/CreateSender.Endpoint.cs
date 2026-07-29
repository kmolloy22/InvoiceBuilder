using InvoiceBuilder.Api.Shared.Infrastructure.Validation;
using InvoiceBuilder.Application.Features.Senders;
using InvoiceBuilder.Application.Features.Senders.Models.Create;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceBuilder.Api.Features.Senders.SenderCreate;

public static class CreateSenderEndpoint
{
	public static void MapCreateSenderEndpoint(this IEndpointRouteBuilder app)
	{
		app.MapPost("/", async (
			[FromBody] CreateSenderDto dto,
			[FromServices] IMediator mediator) =>
		{
			var result = await mediator.Send(new CreateSenderCommand(dto));
			if (result.IsFailure)
			{
				return Results.BadRequest(result.Error);
			}

			var idN = result.Value!.Id.ToString("N");
			var location = $"/api/senders/{idN}";

			var response = new CreateSenderResponseDto(
				Id: idN,
				Location: location,
				CreatedAt: result.Value.CreatedAt);

			return Results.Created(location, response);
		})
		.AddEndpointFilter<ValidationFilter<CreateSenderDto>>()
		.WithName("CreateSender")
		.WithSummary("Creates a new sender.")
		.WithDescription("Creates a new sender with the provided details.")
		.Produces<CreateSenderResponseDto>(StatusCodes.Status201Created)
		.Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
		.ProducesProblem(StatusCodes.Status500InternalServerError);
	}
}
