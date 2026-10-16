using System.Net;
using System.Net.Http.Json;
using InvoiceBuilder.Application.Features.Senders.Models.Create;
using InvoiceBuilder.Application.Features.Senders.Models.Update;
using InvoiceBuilder.Application.Shared.Responses.Senders;
using InvoiceBuilder.Domain.Results;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace InvoiceBuilder.Api.IntegrationTests;

public sealed class SendersEndpointsTests : IClassFixture<CustomerApiFactory>
{
    private const int PageSize = 5;

    private readonly HttpClient _client;

    public SendersEndpointsTests(CustomerApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private static CreateSenderDto NewSender() => new(
        SenderCompanyName: $"Sender Co {Guid.NewGuid():N}",
        SenderFullName: "Test Sender",
        SenderAddress: "1 Test Way",
        SenderTaxVatId: "US123456789",
        BankDetails: "Bank: Test Bank, IBAN: US00TEST000111, SWIFT: TESTUS33");

    private async Task<CreateSenderResponseDto> CreateAsync(CreateSenderDto request)
    {
        var response = await _client.PostAsJsonAsync("/api/senders", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CreateSenderResponseDto>();
        Assert.NotNull(body);
        return body!;
    }

    private async Task<GetSendersResult> GetPageAsync(Guid? cursor = null, bool? isNextPage = null)
    {
        var url = $"/api/senders?pageSize={PageSize}";
        if (cursor.HasValue)
        {
            url += $"&cursor={cursor.Value}&isNextPage={isNextPage ?? true}";
        }

        var page = await _client.GetFromJsonAsync<GetSendersResult>(url);
        Assert.NotNull(page);
        return page!;
    }

    private async Task<Guid> FindSenderIdByCompanyAsync(string companyName)
    {
        var page = await GetPageAsync();
        while (true)
        {
            var match = page.Items.FirstOrDefault(s => s.SenderCompanyName == companyName);
            if (match is not null)
            {
                return match.Id;
            }

            Assert.True(page.NextCursor.HasValue, $"Sender '{companyName}' was not found in any page.");
            page = await GetPageAsync(page.NextCursor, isNextPage: true);
        }
    }

    [Fact]
    public async Task Post_creates_sender_returns_201_and_location()
    {
        var response = await _client.PostAsJsonAsync("/api/senders", NewSender());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<CreateSenderResponseDto>();
        Assert.NotNull(body);
        Assert.True(Guid.TryParse(body!.Id, out _));
        Assert.Equal($"/api/senders/{body.Id}", body.Location);
        Assert.Equal(body.Location, response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Post_invalid_returns_400_with_validation_errors()
    {
        var invalid = NewSender() with { SenderCompanyName = "", BankDetails = "" };

        var response = await _client.PostAsJsonAsync("/api/senders", invalid);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains("SenderCompanyName", problem!.Errors.Keys);
        Assert.Contains("BankDetails", problem.Errors.Keys);
    }

    [Fact]
    public async Task Get_by_id_returns_sender()
    {
        var request = NewSender();
        var created = await CreateAsync(request);

        var sender = await _client.GetFromJsonAsync<GetSenderResult>($"/api/senders/{created.Id}");

        Assert.NotNull(sender);
        Assert.Equal(Guid.Parse(created.Id), sender!.Id);
        Assert.Equal(request.SenderCompanyName, sender.SenderCompanyName);
        Assert.Equal(request.SenderFullName, sender.SenderFullName);
        Assert.Equal(request.SenderAddress, sender.SenderAddress);
        Assert.Equal(request.SenderTaxVatId, sender.SenderTaxVatId);
        Assert.Equal(request.BankDetails, sender.BankDetails);
    }

    [Fact]
    public async Task Get_by_id_unknown_returns_404()
    {
        var response = await _client.GetAsync($"/api/senders/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Put_updates_sender()
    {
        var created = await CreateAsync(NewSender());

        var update = new UpdateSenderDto(
            SenderCompanyName: "Renamed Sender Co",
            SenderFullName: "Renamed Person",
            SenderAddress: "2 New Road",
            SenderTaxVatId: "US999999999",
            BankDetails: "Bank: New Bank, IBAN: US00NEW000999");

        var putResponse = await _client.PutAsJsonAsync($"/api/senders/{created.Id}", update);

        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);

        var reloaded = await _client.GetFromJsonAsync<GetSenderResult>($"/api/senders/{created.Id}");
        Assert.NotNull(reloaded);
        Assert.Equal(update.SenderCompanyName, reloaded!.SenderCompanyName);
        Assert.Equal(update.SenderFullName, reloaded.SenderFullName);
        Assert.Equal(update.SenderAddress, reloaded.SenderAddress);
        Assert.Equal(update.SenderTaxVatId, reloaded.SenderTaxVatId);
        Assert.Equal(update.BankDetails, reloaded.BankDetails);
    }

    [Fact]
    public async Task Put_unknown_returns_404()
    {
        var update = new UpdateSenderDto(
            SenderCompanyName: "Ghost Co",
            SenderFullName: "Ghost",
            SenderAddress: "Nowhere",
            SenderTaxVatId: "US000000000",
            BankDetails: "None");

        var response = await _client.PutAsJsonAsync($"/api/senders/{Guid.NewGuid()}", update);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_removes_sender()
    {
        var created = await CreateAsync(NewSender());

        var deleteResponse = await _client.DeleteAsync($"/api/senders/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/senders/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_unknown_returns_404()
    {
        var response = await _client.DeleteAsync($"/api/senders/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_sender_with_invoices_returns_409_and_keeps_sender()
    {
        // Seeded sender; the data seed attaches invoices to it (Invoice -> Sender is Restrict).
        var seededId = await FindSenderIdByCompanyAsync("Invoice Builder LLC");

        var response = await _client.DeleteAsync($"/api/senders/{seededId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ResultError>();
        Assert.NotNull(error);
        Assert.Equal("SenderInUse", error!.Code);
        Assert.Equal(ResultErrorType.Conflict, error.Type);

        var stillThere = await _client.GetAsync($"/api/senders/{seededId}");
        Assert.Equal(HttpStatusCode.OK, stillThere.StatusCode);
    }

    [Fact]
    public async Task Get_list_pages_forward_and_back_with_cursor()
    {
        // Seed data has 2 senders; make sure there are comfortably more than one page.
        for (var i = 0; i < PageSize + 2; i++)
        {
            await CreateAsync(NewSender());
        }

        var first = await GetPageAsync();
        Assert.Equal(PageSize, first.Items.Count);
        Assert.NotNull(first.NextCursor);
        Assert.Null(first.PreviousCursor);

        var second = await GetPageAsync(first.NextCursor, isNextPage: true);
        Assert.NotEmpty(second.Items);
        Assert.NotNull(second.PreviousCursor);
        Assert.Empty(second.Items.Select(s => s.Id).Intersect(first.Items.Select(s => s.Id)));

        var back = await GetPageAsync(second.PreviousCursor, isNextPage: false);
        Assert.Equal(first.Items.Select(s => s.Id), back.Items.Select(s => s.Id));
    }
}
