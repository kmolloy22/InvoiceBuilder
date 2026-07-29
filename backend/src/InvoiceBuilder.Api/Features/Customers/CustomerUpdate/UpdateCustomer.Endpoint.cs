using InvoiceBuilder.Api.Shared.Infrastructure.Validation;
using InvoiceBuilder.Application.Features.Customers;
using InvoiceBuilder.Application.Features.Customers.Models.Update;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceBuilder.Api.Features.Customers.CustomerUpdate;

public static class UpdateCustomerEndpoint
{
	public static void MapUpdateCustomerEndpoint(this IEndpointRouteBuilder app)
	{
		app.MapPut("/{id:guid}", async (
			string id,
			[FromBody] UpdateCustomerDto dto,
			[FromServices] IMediator mediator) =>
		{
			var result = await mediator.Send(new UpdateCustomerCommand(Guid.Parse(id), dto));
			if(result.IsFailure)
			{
				return Results.NotFound(result.Error);
			}

			return Results.Ok(result);
		})
		.AddEndpointFilter<ValidationFilter<UpdateCustomerDto>>()
		.WithName("UpdateCustomer")
		.WithSummary("Updates a customer.")
		.WithDescription("Updates first, last name and/or address for the specified customer.")
		.Produces(StatusCodes.Status204NoContent)
		.Produces(StatusCodes.Status404NotFound)
		.Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
		.ProducesProblem(StatusCodes.Status500InternalServerError);
	}
}
