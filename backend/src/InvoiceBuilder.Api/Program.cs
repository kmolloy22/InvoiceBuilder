using FluentValidation;
using InvoiceBuilder.Api.Features.Customers;
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

await app.InitializeDbAsync();

app.UseExceptionHandler();
await app.RunAsync();
