using InvoiceBuilder.Application.Features.Customers;
using InvoiceBuilder.Application.Features.Customers.Models;
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
			return Results.Ok(result);
		});
	}
}
