using InvoiceBuilder.Application.Features.Invoices;
using InvoiceBuilder.Application.Features.Invoices.Models.Get;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceBuilder.Api.Features.Invoices.InvoicesGet;

public static class GetInvoicesEndpoint
{
	public static void MapGetInvoicesEndpoint(this IEndpointRouteBuilder app)
	{
		app.MapGet("/", async (
			[AsParameters] GetInvoicesDto request,
			[FromServices] IMediator mediator) =>
		{
			var result = await mediator.Send(new GetInvoicesCommand(request));
			if (result.IsFailure)
			{
				return Results.NotFound(result.Error);
			}

			return Results.Ok(result.Value);
		})
		.WithName("GetInvoices")
		.WithSummary("Lists invoices with pagination support.")
		.WithDescription("Returns a list of invoices.")
		.Produces<object>(StatusCodes.Status400BadRequest, "application/json")
		.ProducesProblem(StatusCodes.Status500InternalServerError);
	}
}
