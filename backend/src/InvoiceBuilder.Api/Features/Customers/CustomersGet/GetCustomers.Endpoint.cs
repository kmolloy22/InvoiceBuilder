using InvoiceBuilder.Application.Features.Customers;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceBuilder.Api.Features.Customers.CustomersGet;

public static class GetCustomersEndpoint
{
	public static void MapGetCustomersEndpoint(this IEndpointRouteBuilder app)
	{
		app.MapGet("/", async (
			[FromServices] IMediator mediator) =>
		{
			var result = await mediator.Send(new GetCustomersCommand());
			if(result.IsFailure)
			{
				return Results.NotFound(result.Error);
			}

			return Results.Ok(result);
		})
		.WithName("GetCustomers")
		.WithSummary("Lists customers with pagination support.")
		.WithDescription("Returns a list of customers.")
		//.Produces<PagedResult<GetCustomerDto>>(StatusCodes.Status200OK, "application/json")
		.Produces<object>(StatusCodes.Status400BadRequest, "application/json")
		.ProducesProblem(StatusCodes.Status500InternalServerError);
	}
}
