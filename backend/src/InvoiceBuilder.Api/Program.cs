using FluentValidation;
using InvoiceBuilder.Api.Features.Customers;
using InvoiceBuilder.Application.Features.Customers;
using InvoiceBuilder.Application.Features.Customers.Validation;

var builder = WebApplication.CreateBuilder(args);

// Register MediatR – scan Application assembly for handlers
builder.Services.AddMediatR(cfg =>
{
	cfg.RegisterServicesFromAssembly(typeof(CreateCustomerHandler).Assembly);
	cfg.RegisterServicesFromAssembly(typeof(GetCustomersHandler).Assembly);
});

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<CreateCustomerDtoValidator>();

var app = builder.Build();

app.MapCustomers();

await app.RunAsync();
