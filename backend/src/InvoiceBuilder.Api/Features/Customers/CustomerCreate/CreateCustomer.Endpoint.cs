using InvoiceBuilder.Api.Shared.Infrastructure.Validation;
using InvoiceBuilder.Application.Features.Customers;
using InvoiceBuilder.Application.Features.Customers.Models.Create;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceBuilder.Api.Features.Customers.CustomerCreate;

public static class CreateCustomerEndpoint
{
	public static void MapCreateCustomerEndpoint(this IEndpointRouteBuilder app)
	{
		app.MapPost("/", async (
			[FromBody] CreateCustomerDto dto,
			[FromServices] IMediator mediator) =>
		{
			var result = await mediator.Send(new CreateCustomerCommand(dto));

			var idN = result.Id.ToString("N");
			var location = $"/api/customers/{idN}";

			var response = new CreateCustomerResponseDto(
				Id: idN,
				Location: location,
				CreatedAt: result.CreatedAt);

			return Results.Created(location, response);
		})
		.AddEndpointFilter<ValidationFilter<CreateCustomerDto>>()
		.WithName("CreateCustomer")
		.WithSummary("Creates a new customer.")
		.WithDescription("Creates a new customer with the provided details.")
		.Produces<CreateCustomerResponseDto>(StatusCodes.Status201Created)
		.Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
		.ProducesProblem(StatusCodes.Status500InternalServerError);
		
	}
}
