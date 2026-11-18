using System.Net;
using System.Net.Http.Json;
using InvoiceBuilder.Application.Features.Customers.Models.Create;
using InvoiceBuilder.Application.Features.Invoices.Models.Create;
using InvoiceBuilder.Application.Features.Invoices.Models.Get;
using InvoiceBuilder.Application.Features.Invoices.Models.Update;
using InvoiceBuilder.Application.Features.Senders.Models.Create;
using InvoiceBuilder.Application.Shared.Responses.Customers;
using InvoiceBuilder.Application.Shared.Responses.Invoices;
using InvoiceBuilder.Application.Shared.Responses.Senders;
using InvoiceBuilder.Domain.Results;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace InvoiceBuilder.Api.IntegrationTests;

public sealed class InvoicesEndpointsTests : IClassFixture<CustomerApiFactory>
{
	private const int PageSize = 5;

	private readonly HttpClient _client;

	public InvoicesEndpointsTests(CustomerApiFactory factory)
	{
		_client = factory.CreateClient();
	}

	private async Task<Guid> CreateCustomerAsync()
	{
		var request = new CreateCustomerDto(
			CompanyName: $"Invoice Test Co {Guid.NewGuid():N}",
			CustomerName: "Test Person",
			CustomerAddress: "1 Test Way",
			PostalCode: "10001",
			CustomerEmail: $"test-{Guid.NewGuid():N}@example.com",
			CustomerTaxVatId: "US123456789");

		var response = await _client.PostAsJsonAsync("/api/customers", request);
		Assert.Equal(HttpStatusCode.Created, response.StatusCode);
		var body = await response.Content.ReadFromJsonAsync<CreateCustomerResponseDto>();
		Assert.NotNull(body);
		return Guid.Parse(body!.Id);
	}

	private async Task<Guid> CreateSenderAsync()
	{
		var request = new CreateSenderDto(
			SenderCompanyName: $"Invoice Test Sender {Guid.NewGuid():N}",
			SenderFullName: "Test Sender",
			SenderAddress: "1 Test Way",
			SenderTaxVatId: "US123456789",
			BankDetails: "Bank: Test Bank, IBAN: US00TEST000111, SWIFT: TESTUS33");

		var response = await _client.PostAsJsonAsync("/api/senders", request);
		Assert.Equal(HttpStatusCode.Created, response.StatusCode);
		var body = await response.Content.ReadFromJsonAsync<CreateSenderResponseDto>();
		Assert.NotNull(body);
		return Guid.Parse(body!.Id);
	}

	private static CreateInvoiceDto NewInvoice(Guid customerId, Guid senderId, string? invoiceNumber = null)
	{
		var lineItems = new List<InvoiceLineItemDto>
		{
			new("Consulting", 10m, 100m, 1000m),
		};
		const decimal subtotal = 1000m;
		const decimal taxRate = 0.1m;
		const decimal taxAmount = subtotal * taxRate;
		const decimal total = subtotal + taxAmount;

		return new CreateInvoiceDto(
			InvoiceNumber: invoiceNumber ?? $"INV-TEST-{Guid.NewGuid():N}",
			InvoiceDate: DateTime.UtcNow.Date,
			DueDate: DateTime.UtcNow.Date.AddDays(30),
			Currency: "USD",
			Notes: "Test invoice",
			CustomerId: customerId,
			SenderId: senderId,
			Subtotal: subtotal,
			TaxRate: taxRate,
			TotalAmount: total,
			LineItems: lineItems);
	}

	private async Task<CreateInvoiceDto> NewValidInvoiceAsync(string? invoiceNumber = null)
	{
		var customerId = await CreateCustomerAsync();
		var senderId = await CreateSenderAsync();
		return NewInvoice(customerId, senderId, invoiceNumber);
	}

	private async Task<CreateInvoiceResponseDto> CreateAsync(CreateInvoiceDto request)
	{
		var response = await _client.PostAsJsonAsync("/api/invoices", request);
		Assert.Equal(HttpStatusCode.Created, response.StatusCode);
		var body = await response.Content.ReadFromJsonAsync<CreateInvoiceResponseDto>();
		Assert.NotNull(body);
		return body!;
	}

	private async Task<GetInvoicesResult> GetPageAsync(Guid? cursor = null, bool? isNextPage = null)
	{
		var url = $"/api/invoices?pageSize={PageSize}";
		if (cursor.HasValue)
		{
			url += $"&cursor={cursor.Value}&isNextPage={isNextPage ?? true}";
		}

		var page = await _client.GetFromJsonAsync<GetInvoicesResult>(url);
		Assert.NotNull(page);
		return page!;
	}

	[Fact]
	public async Task Post_creates_invoice_returns_201_and_location()
	{
		var request = await NewValidInvoiceAsync();

		var response = await _client.PostAsJsonAsync("/api/invoices", request);

		Assert.Equal(HttpStatusCode.Created, response.StatusCode);

		var body = await response.Content.ReadFromJsonAsync<CreateInvoiceResponseDto>();
		Assert.NotNull(body);
		Assert.True(Guid.TryParse(body!.Id, out _));
		Assert.Equal($"/api/invoices/{body.Id}", body.Location);
		Assert.Equal(body.Location, response.Headers.Location?.ToString());
	}

	[Fact]
	public async Task Post_invalid_returns_400_with_validation_errors()
	{
		var request = await NewValidInvoiceAsync();
		var invalid = request with { InvoiceNumber = "", Currency = "", LineItems = [] };

		var response = await _client.PostAsJsonAsync("/api/invoices", invalid);

		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

		var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
		Assert.NotNull(problem);
		Assert.Contains("InvoiceNumber", problem!.Errors.Keys);
		Assert.Contains("Currency", problem.Errors.Keys);
		Assert.Contains("LineItems", problem.Errors.Keys);
	}

	[Fact]
	public async Task Post_due_date_before_invoice_date_returns_400()
	{
		var request = await NewValidInvoiceAsync();
		var invalid = request with { DueDate = request.InvoiceDate.AddDays(-1) };

		var response = await _client.PostAsJsonAsync("/api/invoices", invalid);

		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

		var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
		Assert.NotNull(problem);
		Assert.Contains("DueDate", problem!.Errors.Keys);
	}

	[Fact]
	public async Task Post_duplicate_invoice_number_returns_400_with_conflict_error()
	{
		var invoiceNumber = $"INV-TEST-{Guid.NewGuid():N}";
		await CreateAsync(await NewValidInvoiceAsync(invoiceNumber));

		var duplicate = await NewValidInvoiceAsync(invoiceNumber);
		var response = await _client.PostAsJsonAsync("/api/invoices", duplicate);

		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

		var error = await response.Content.ReadFromJsonAsync<ResultError>();
		Assert.NotNull(error);
		Assert.Equal("InvoiceAlreadyExists", error!.Code);
		Assert.Equal(ResultErrorType.Conflict, error.Type);
	}

	[Fact]
	public async Task Post_unknown_customer_returns_400_with_not_found_error()
	{
		var senderId = await CreateSenderAsync();
		var request = NewInvoice(Guid.NewGuid(), senderId);

		var response = await _client.PostAsJsonAsync("/api/invoices", request);

		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

		var error = await response.Content.ReadFromJsonAsync<ResultError>();
		Assert.NotNull(error);
		Assert.Equal("CustomerNotFound", error!.Code);
		Assert.Equal(ResultErrorType.NotFound, error.Type);
	}

	[Fact]
	public async Task Post_unknown_sender_returns_400_with_not_found_error()
	{
		var customerId = await CreateCustomerAsync();
		var request = NewInvoice(customerId, Guid.NewGuid());

		var response = await _client.PostAsJsonAsync("/api/invoices", request);

		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

		var error = await response.Content.ReadFromJsonAsync<ResultError>();
		Assert.NotNull(error);
		Assert.Equal("SenderNotFound", error!.Code);
		Assert.Equal(ResultErrorType.NotFound, error.Type);
	}

	[Fact]
	public async Task Get_by_id_returns_invoice()
	{
		var request = await NewValidInvoiceAsync();
		var created = await CreateAsync(request);

		var invoice = await _client.GetFromJsonAsync<GetInvoiceResult>($"/api/invoices/{created.Id}");

		Assert.NotNull(invoice);
		Assert.Equal(Guid.Parse(created.Id), invoice!.Id);
		Assert.Equal(request.InvoiceNumber, invoice.InvoiceNumber);
		Assert.Equal(request.Currency, invoice.Currency);
		Assert.Equal(request.Notes, invoice.Notes);
		Assert.Equal(request.CustomerId, invoice.Customer.Id);
		Assert.Equal(request.SenderId, invoice.Sender.Id);
		Assert.Equal(request.Subtotal, invoice.Subtotal);
		Assert.Equal(request.TaxRate, invoice.TaxRate);
		Assert.Equal(request.TotalAmount, invoice.TotalAmount);
		Assert.Single(invoice.LineItems);
		Assert.Equal(request.LineItems[0].ItemName, invoice.LineItems[0].ItemName);
		Assert.Equal(request.LineItems[0].Total, invoice.LineItems[0].Total);
	}

	[Fact]
	public async Task Get_by_id_unknown_returns_404()
	{
		var response = await _client.GetAsync($"/api/invoices/{Guid.NewGuid()}");

		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
	}

	[Fact]
	public async Task Put_updates_invoice_returns_200()
	{
		var request = await NewValidInvoiceAsync();
		var created = await CreateAsync(request);
		var newCustomerId = await CreateCustomerAsync();
		var newSenderId = await CreateSenderAsync();

		var update = new UpdateInvoiceDto(
			InvoiceNumber: $"INV-UPDATED-{Guid.NewGuid():N}",
			InvoiceDate: DateTime.UtcNow.Date,
			DueDate: DateTime.UtcNow.Date.AddDays(14),
			Currency: "EUR",
			Notes: "Updated notes",
			CustomerId: newCustomerId,
			SenderId: newSenderId,
			Subtotal: 2000m,
			TaxRate: 0.2m,
			TotalAmount: 2400m);

		var putResponse = await _client.PutAsJsonAsync($"/api/invoices/{created.Id}", update);

		Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);

		var body = await putResponse.Content.ReadFromJsonAsync<GetInvoiceResult>();
		Assert.NotNull(body);
		Assert.Equal(update.InvoiceNumber, body!.InvoiceNumber);
		Assert.Equal(update.Currency, body.Currency);
		Assert.Equal(update.Notes, body.Notes);
		Assert.Equal(newCustomerId, body.Customer.Id);
		Assert.Equal(newSenderId, body.Sender.Id);
		Assert.Equal(update.Subtotal, body.Subtotal);
		Assert.Equal(update.TaxRate, body.TaxRate);
		Assert.Equal(update.TotalAmount, body.TotalAmount);

		var reloaded = await _client.GetFromJsonAsync<GetInvoiceResult>($"/api/invoices/{created.Id}");
		Assert.NotNull(reloaded);
		Assert.Equal(update.InvoiceNumber, reloaded!.InvoiceNumber);
		Assert.Equal(newCustomerId, reloaded.Customer.Id);
		Assert.Equal(newSenderId, reloaded.Sender.Id);
	}

	[Fact]
	public async Task Put_unknown_returns_404()
	{
		var request = await NewValidInvoiceAsync();
		var update = new UpdateInvoiceDto(
			InvoiceNumber: request.InvoiceNumber,
			InvoiceDate: request.InvoiceDate,
			DueDate: request.DueDate,
			Currency: request.Currency,
			Notes: request.Notes,
			CustomerId: request.CustomerId,
			SenderId: request.SenderId,
			Subtotal: request.Subtotal,
			TaxRate: request.TaxRate,
			TotalAmount: request.TotalAmount);

		var response = await _client.PutAsJsonAsync($"/api/invoices/{Guid.NewGuid()}", update);

		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
	}

	[Fact]
	public async Task Put_duplicate_invoice_number_returns_404_with_conflict_error()
	{
		var existingNumber = $"INV-TEST-{Guid.NewGuid():N}";
		await CreateAsync(await NewValidInvoiceAsync(existingNumber));

		var request = await NewValidInvoiceAsync();
		var created = await CreateAsync(request);

		var update = new UpdateInvoiceDto(
			InvoiceNumber: existingNumber,
			InvoiceDate: request.InvoiceDate,
			DueDate: request.DueDate,
			Currency: request.Currency,
			Notes: request.Notes,
			CustomerId: request.CustomerId,
			SenderId: request.SenderId,
			Subtotal: request.Subtotal,
			TaxRate: request.TaxRate,
			TotalAmount: request.TotalAmount);

		var response = await _client.PutAsJsonAsync($"/api/invoices/{created.Id}", update);

		// UpdateInvoiceEndpoint maps every handler failure to 404, including this Conflict
		// case - same status-code-vs-ResultErrorType mismatch already noted for Create.
		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

		var error = await response.Content.ReadFromJsonAsync<ResultError>();
		Assert.NotNull(error);
		Assert.Equal("InvoiceAlreadyExists", error!.Code);
		Assert.Equal(ResultErrorType.Conflict, error.Type);
	}

	[Fact]
	public async Task Put_same_invoice_number_is_allowed()
	{
		var request = await NewValidInvoiceAsync();
		var created = await CreateAsync(request);

		var update = new UpdateInvoiceDto(
			InvoiceNumber: request.InvoiceNumber,
			InvoiceDate: request.InvoiceDate,
			DueDate: request.DueDate,
			Currency: "EUR",
			Notes: request.Notes,
			CustomerId: request.CustomerId,
			SenderId: request.SenderId,
			Subtotal: request.Subtotal,
			TaxRate: request.TaxRate,
			TotalAmount: request.TotalAmount);

		var response = await _client.PutAsJsonAsync($"/api/invoices/{created.Id}", update);

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
	}

	[Fact]
	public async Task Put_unknown_customer_returns_404()
	{
		var created = await CreateAsync(await NewValidInvoiceAsync());
		var senderId = await CreateSenderAsync();

		var update = new UpdateInvoiceDto(
			InvoiceNumber: $"INV-UPDATED-{Guid.NewGuid():N}",
			InvoiceDate: DateTime.UtcNow.Date,
			DueDate: DateTime.UtcNow.Date.AddDays(14),
			Currency: "USD",
			Notes: "Notes",
			CustomerId: Guid.NewGuid(),
			SenderId: senderId,
			Subtotal: 1000m,
			TaxRate: 0.1m,
			TotalAmount: 1100m);

		var response = await _client.PutAsJsonAsync($"/api/invoices/{created.Id}", update);

		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

		var error = await response.Content.ReadFromJsonAsync<ResultError>();
		Assert.NotNull(error);
		Assert.Equal("CustomerNotFound", error!.Code);
	}

	[Fact]
	public async Task Delete_removes_invoice()
	{
		var created = await CreateAsync(await NewValidInvoiceAsync());

		var deleteResponse = await _client.DeleteAsync($"/api/invoices/{created.Id}");
		Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

		var getResponse = await _client.GetAsync($"/api/invoices/{created.Id}");
		Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
	}

	[Fact]
	public async Task Delete_unknown_returns_404()
	{
		var response = await _client.DeleteAsync($"/api/invoices/{Guid.NewGuid()}");

		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
	}

	[Fact]
	public async Task Get_list_pages_forward_and_back_with_cursor()
	{
		for (var i = 0; i < PageSize + 2; i++)
		{
			await CreateAsync(await NewValidInvoiceAsync());
		}

		var first = await GetPageAsync();
		Assert.Equal(PageSize, first.Items.Count);
		Assert.NotNull(first.NextCursor);
		Assert.Null(first.PreviousCursor);

		var second = await GetPageAsync(first.NextCursor, isNextPage: true);
		Assert.NotEmpty(second.Items);
		Assert.NotNull(second.PreviousCursor);
		Assert.Empty(second.Items.Select(i => i.Id).Intersect(first.Items.Select(i => i.Id)));

		var back = await GetPageAsync(second.PreviousCursor, isNextPage: false);
		Assert.Equal(first.Items.Select(i => i.Id), back.Items.Select(i => i.Id));
	}
}
