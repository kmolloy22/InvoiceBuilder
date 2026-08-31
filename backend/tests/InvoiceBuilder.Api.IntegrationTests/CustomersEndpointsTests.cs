using System.Net;
using System.Net.Http.Json;
using InvoiceBuilder.Application.Features.Customers.Models.Create;
using InvoiceBuilder.Application.Features.Customers.Models.Update;
using InvoiceBuilder.Application.Shared.Responses.Customers;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace InvoiceBuilder.Api.IntegrationTests;

public sealed class CustomersEndpointsTests : IClassFixture<CustomerApiFactory>
{
    private readonly HttpClient _client;

    public CustomersEndpointsTests(CustomerApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private static CreateCustomerDto NewCustomer() => new(
        CompanyName: $"Acme {Guid.NewGuid():N}",
        CustomerName: "Test Person",
        CustomerAddress: "1 Test Way",
        PostalCode: "10001",
        CustomerEmail: $"test-{Guid.NewGuid():N}@example.com",
        CustomerTaxVatId: "US123456789");

    private async Task<CreateCustomerResponseDto> CreateAsync(CreateCustomerDto request)
    {
        var response = await _client.PostAsJsonAsync("/api/customers", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CreateCustomerResponseDto>();
        Assert.NotNull(body);
        return body!;
    }

    [Fact]
    public async Task Post_creates_customer_returns_201_and_location()
    {
        var response = await _client.PostAsJsonAsync("/api/customers", NewCustomer());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<CreateCustomerResponseDto>();
        Assert.NotNull(body);
        Assert.True(Guid.TryParse(body!.Id, out _));
        Assert.Equal($"/api/customers/{body.Id}", body.Location);
        Assert.Equal(body.Location, response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Post_invalid_returns_400_with_validation_errors()
    {
        var invalid = NewCustomer() with { CompanyName = "", CustomerEmail = "not-an-email" };

        var response = await _client.PostAsJsonAsync("/api/customers", invalid);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains("CompanyName", problem!.Errors.Keys);
        Assert.Contains("CustomerEmail", problem.Errors.Keys);
    }

    [Fact]
    public async Task Get_by_id_returns_customer()
    {
        var request = NewCustomer();
        var created = await CreateAsync(request);

        var customer = await _client.GetFromJsonAsync<GetCustomerResult>($"/api/customers/{created.Id}");

        Assert.NotNull(customer);
        Assert.Equal(Guid.Parse(created.Id), customer!.Id);
        Assert.Equal(request.CompanyName, customer.CompanyName);
        Assert.Equal(request.CustomerName, customer.CustomerName);
        Assert.Equal(request.CustomerAddress, customer.CustomerAddress);
        Assert.Equal(request.PostalCode, customer.PostalCode);
        Assert.Equal(request.CustomerEmail, customer.CustomerEmail);
        Assert.Equal(request.CustomerTaxVatId, customer.CustomerTaxVatId);
    }

    [Fact]
    public async Task Get_by_id_unknown_returns_404()
    {
        var response = await _client.GetAsync($"/api/customers/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Put_updates_customer()
    {
        var created = await CreateAsync(NewCustomer());

        var update = new UpdateCustomerDto(
            CompanyName: "Renamed Co",
            CustomerName: "Renamed Person",
            CustomerAddress: "2 New Road",
            PostalCode: "20002",
            CustomerEmail: "renamed@example.com",
            CustomerTaxVatId: "US999999999");

        var putResponse = await _client.PutAsJsonAsync($"/api/customers/{created.Id}", update);

        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);

        var reloaded = await _client.GetFromJsonAsync<GetCustomerResult>($"/api/customers/{created.Id}");
        Assert.NotNull(reloaded);
        Assert.Equal(update.CompanyName, reloaded!.CompanyName);
        Assert.Equal(update.CustomerName, reloaded.CustomerName);
        Assert.Equal(update.CustomerAddress, reloaded.CustomerAddress);
        Assert.Equal(update.PostalCode, reloaded.PostalCode);
        Assert.Equal(update.CustomerEmail, reloaded.CustomerEmail);
        Assert.Equal(update.CustomerTaxVatId, reloaded.CustomerTaxVatId);
    }

    [Fact]
    public async Task Put_unknown_returns_404()
    {
        var update = new UpdateCustomerDto(
            CompanyName: "Ghost Co",
            CustomerName: "Ghost",
            CustomerAddress: "Nowhere",
            PostalCode: "00000",
            CustomerEmail: "ghost@example.com",
            CustomerTaxVatId: "US000000000");

        var response = await _client.PutAsJsonAsync($"/api/customers/{Guid.NewGuid()}", update);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_removes_customer()
    {
        var created = await CreateAsync(NewCustomer());

        var deleteResponse = await _client.DeleteAsync($"/api/customers/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/customers/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_unknown_returns_404()
    {
        var response = await _client.DeleteAsync($"/api/customers/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
