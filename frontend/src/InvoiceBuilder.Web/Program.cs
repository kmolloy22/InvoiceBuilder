using InvoiceBuilder.Web.Components;
using InvoiceBuilder.Web.Services;
using MudBlazor.Services;
using Refit;


var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();

// Configure API Client with service discovery
var apiBaseUrl = builder.Configuration["services__invoicebuilder-api__http__0"]
	?? builder.Configuration["ApiBaseUrl"]
	?? throw new InvalidOperationException("API base URL is not configured. Please set 'ApiBaseUrl' in configuration.");

builder.Services.AddRefitGeneratedClient<ICustomersApiClient>()
	.ConfigureHttpClient(c =>
	{
		c.BaseAddress = new Uri(apiBaseUrl);
		c.Timeout = TimeSpan.FromSeconds(30);
	})
	.AddStandardResilienceHandler();

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

await app.RunAsync();
