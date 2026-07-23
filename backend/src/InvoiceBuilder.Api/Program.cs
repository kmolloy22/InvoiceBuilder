using InvoiceBuilder.Api.Features.Customers;
using InvoiceBuilder.Application.Features.Customers;

var builder = WebApplication.CreateBuilder(args);

// Register MediatR – scan Application assembly for handlers
builder.Services.AddMediatR(cfg =>
{
	cfg.RegisterServicesFromAssembly(typeof(CreateCustomerHandler).Assembly);
});


var app = builder.Build();

app.MapCustomers();

await app.RunAsync();
