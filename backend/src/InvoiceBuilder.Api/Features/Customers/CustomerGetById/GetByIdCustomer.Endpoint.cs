using InvoiceBuilder.Application.Features.Customers;
using MediatR;

namespace InvoiceBuilder.Api.Features.Customers.CustomerGetById;

public static class GetByIdCustomerEndpoint
{
	public static void MapGetByIdCustomerEndpoint(this IEndpointRouteBuilder app)
	{
		app.MapGet("/{id:guid}", async (
			string id,
			IMediator mediator) =>

		{
			var result = await mediator.Send(new GetCustomerByIdCommand(Guid.Parse(id)));

			if(result.IsFailure)
			{
				return Results.NotFound(result.Error);
			}

			return Results.Ok(result.Value);
		})
		.WithName("GetCustomer")
		.WithSummary("Gets a customer by id.")
		.WithDescription("Returns the customer if it exists, otherwise 404.");
	}
}
