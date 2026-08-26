using InvoiceBuilder.Api.Shared.Infrastructure.Validation;
using InvoiceBuilder.Application.Features.Invoices;
using InvoiceBuilder.Application.Features.Invoices.Models.Create;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceBuilder.Api.Features.Invoices.InvoiceCreate;

public static class CreateInvoiceEndpoint

{
	public static void MapCreateInvoiceEndpoint(this IEndpointRouteBuilder app)
	{
		app.MapPost("/", async (
			[FromBody] CreateInvoiceDto dto,
			[FromServices] IMediator mediator) =>
		{
			var result = await mediator.Send(new CreateInvoiceCommand(dto));
			if (result.IsFailure)
			{
				return Results.BadRequest(result.Error);
			}

			var idN = result.Value!.Id.ToString("N");
			var location = $"/api/invoices/{idN}";

			var response = new CreateInvoiceResponseDto(
				Id: idN,
				Location: location,
				CreatedAt: result.Value.CreatedAt);

			return Results.Created(location, response);
		})
		.AddEndpointFilter<ValidationFilter<CreateInvoiceDto>>()
		.WithName("CreateInvoice")
		.WithSummary("Creates a new invoice.")
		.WithDescription("Creates a new invoice with the provided details.")
		.Produces<CreateInvoiceResponseDto>(StatusCodes.Status201Created)
		.Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
		.ProducesProblem(StatusCodes.Status500InternalServerError);
	}
}
