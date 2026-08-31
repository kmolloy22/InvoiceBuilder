using FluentValidation;
using InvoiceBuilder.Api.Features.Customers;
using InvoiceBuilder.Api.Features.Invoices;
using InvoiceBuilder.Api.Features.Senders;
using InvoiceBuilder.Api.Shared.ErrorHandling;
using InvoiceBuilder.Application.Features.Customers;
using InvoiceBuilder.Application.Features.Customers.Validation;
using InvoiceBuilder.Database;
using InvoiceBuilder.Database.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddProblemDetails()
	.AddExceptionHandler<GlobalExceptionHandler>()
	.AddScoped<GlobalExceptionHandler>();

builder.AddInvoiceBuilderNpgSql<InvoiceBuilderContext>("InvoiceBuilderDB");

// Register MediatR – scan Application assembly for handlers
builder.Services.AddMediatR(cfg =>
{
	cfg.RegisterServicesFromAssembly(typeof(GetCustomersHandler).Assembly);
});

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<CreateCustomerDtoValidator>();

var app = builder.Build();

app.MapCustomers();
app.MapSenders();
app.MapInvoices();

await app.InitializeDbAsync();

app.UseExceptionHandler();
await app.RunAsync();

/// <summary>
/// Exposes the implicit top-level <c>Program</c> class to the integration test
/// project via <c>WebApplicationFactory&lt;Program&gt;</c>. The non-public
/// constructor keeps it non-instantiable.
/// </summary>
public partial class Program
{
	protected Program() { }
}
