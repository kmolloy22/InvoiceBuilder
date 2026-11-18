using InvoiceBuilder.Application.Features.Invoices;
using InvoiceBuilder.Application.Features.Invoices.Models.Create;
using InvoiceBuilder.Application.Features.Invoices.Models.Get;
using InvoiceBuilder.Application.Features.Invoices.Models.Update;
using InvoiceBuilder.Database;
using InvoiceBuilder.Domain.Entities;
using InvoiceBuilder.Domain.Results;
using Xunit;

namespace InvoiceBuilder.Application.Tests;

public sealed class InvoiceHandlersTests
{
	private static Customer AddCustomer(InvoiceBuilderContext dbContext)
	{
		var customer = Customer.Create(
			$"Co {Guid.NewGuid():N}", "Test Person", "1 Test Way", "10001",
			$"test-{Guid.NewGuid():N}@example.com", "US123456789");
		dbContext.Customers.Add(customer);
		dbContext.SaveChanges();
		return customer;
	}

	private static Sender AddSender(InvoiceBuilderContext dbContext)
	{
		var sender = Sender.Create($"Sender {Guid.NewGuid():N}", "Full Name", "Address", "VAT", "Bank");
		dbContext.Senders.Add(sender);
		dbContext.SaveChanges();
		return sender;
	}

	private static CreateInvoiceDto NewInvoiceDto(Guid customerId, Guid senderId, string? invoiceNumber = null) => new(
		InvoiceNumber: invoiceNumber ?? $"INV-{Guid.NewGuid():N}",
		InvoiceDate: DateTime.UtcNow.Date,
		DueDate: DateTime.UtcNow.Date.AddDays(30),
		Currency: "USD",
		Notes: "Notes",
		CustomerId: customerId,
		SenderId: senderId,
		Subtotal: 100m,
		TaxRate: 0.1m,
		TotalAmount: 110m,
		LineItems: [new InvoiceLineItemDto("Consulting", 1m, 100m, 100m)]);

	private static Invoice AddInvoice(InvoiceBuilderContext dbContext, Guid customerId, Guid senderId, string? invoiceNumber = null)
	{
		var invoice = Invoice.Create(
			invoiceNumber ?? $"INV-{Guid.NewGuid():N}", DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(30),
			"USD", "Notes", customerId, senderId, 100m, 0.1m, 110m,
			[new InvoiceLineItem { Id = Guid.NewGuid(), ItemName = "Consulting", Quantity = 1, UnitPrice = 100m, Total = 100m }]);
		dbContext.Invoices.Add(invoice);
		dbContext.SaveChanges();
		return invoice;
	}

	[Fact]
	public async Task Create_adds_invoice_and_returns_id_and_created_at()
	{
		await using var dbContext = TestDbContextFactory.Create();
		var customer = AddCustomer(dbContext);
		var sender = AddSender(dbContext);
		var handler = new CreateInvoiceHandler(dbContext);

		var result = await handler.Handle(
			new CreateInvoiceCommand(NewInvoiceDto(customer.Id, sender.Id)), CancellationToken.None);

		Assert.True(result.IsSuccess);
		Assert.NotEqual(Guid.Empty, result.Value!.Id);
		Assert.Single(dbContext.Invoices);
	}

	[Fact]
	public async Task Create_duplicate_invoice_number_returns_conflict()
	{
		await using var dbContext = TestDbContextFactory.Create();
		var customer = AddCustomer(dbContext);
		var sender = AddSender(dbContext);
		var invoiceNumber = $"INV-{Guid.NewGuid():N}";
		AddInvoice(dbContext, customer.Id, sender.Id, invoiceNumber);
		var handler = new CreateInvoiceHandler(dbContext);

		var result = await handler.Handle(
			new CreateInvoiceCommand(NewInvoiceDto(customer.Id, sender.Id, invoiceNumber)), CancellationToken.None);

		Assert.True(result.IsFailure);
		Assert.Equal("InvoiceAlreadyExists", result.Error!.Code);
		Assert.Equal(ResultErrorType.Conflict, result.Error.Type);
	}

	[Fact]
	public async Task Create_unknown_customer_returns_not_found()
	{
		await using var dbContext = TestDbContextFactory.Create();
		var sender = AddSender(dbContext);
		var handler = new CreateInvoiceHandler(dbContext);

		var result = await handler.Handle(
			new CreateInvoiceCommand(NewInvoiceDto(Guid.NewGuid(), sender.Id)), CancellationToken.None);

		Assert.True(result.IsFailure);
		Assert.Equal("CustomerNotFound", result.Error!.Code);
		Assert.Equal(ResultErrorType.NotFound, result.Error.Type);
	}

	[Fact]
	public async Task Create_unknown_sender_returns_not_found()
	{
		await using var dbContext = TestDbContextFactory.Create();
		var customer = AddCustomer(dbContext);
		var handler = new CreateInvoiceHandler(dbContext);

		var result = await handler.Handle(
			new CreateInvoiceCommand(NewInvoiceDto(customer.Id, Guid.NewGuid())), CancellationToken.None);

		Assert.True(result.IsFailure);
		Assert.Equal("SenderNotFound", result.Error!.Code);
	}

	[Fact]
	public async Task GetById_returns_invoice_with_customer_sender_and_line_items()
	{
		await using var dbContext = TestDbContextFactory.Create();
		var customer = AddCustomer(dbContext);
		var sender = AddSender(dbContext);
		var invoice = AddInvoice(dbContext, customer.Id, sender.Id);
		var handler = new GetInvoiceByIdHandler(dbContext);

		var result = await handler.Handle(new GetInvoiceByIdCommand(invoice.Id), CancellationToken.None);

		Assert.True(result.IsSuccess);
		Assert.Equal(invoice.InvoiceNumber, result.Value!.InvoiceNumber);
		Assert.Equal(customer.Id, result.Value.Customer.Id);
		Assert.Equal(sender.Id, result.Value.Sender.Id);
		Assert.Single(result.Value.LineItems);
	}

	[Fact]
	public async Task GetById_unknown_returns_not_found()
	{
		await using var dbContext = TestDbContextFactory.Create();
		var handler = new GetInvoiceByIdHandler(dbContext);

		var result = await handler.Handle(new GetInvoiceByIdCommand(Guid.NewGuid()), CancellationToken.None);

		Assert.True(result.IsFailure);
		Assert.Equal("InvoiceNotFound", result.Error!.Code);
	}

	[Fact]
	public async Task Update_modifies_invoice_number_customer_and_sender()
	{
		await using var dbContext = TestDbContextFactory.Create();
		var customer = AddCustomer(dbContext);
		var sender = AddSender(dbContext);
		var invoice = AddInvoice(dbContext, customer.Id, sender.Id);
		var newCustomer = AddCustomer(dbContext);
		var newSender = AddSender(dbContext);
		var handler = new UpdateInvoiceHandler(dbContext);
		var newNumber = $"INV-UPDATED-{Guid.NewGuid():N}";
		var update = new UpdateInvoiceDto(
			InvoiceNumber: newNumber,
			InvoiceDate: DateTime.UtcNow.Date,
			DueDate: DateTime.UtcNow.Date.AddDays(14),
			Currency: "EUR",
			Notes: "Updated notes",
			CustomerId: newCustomer.Id,
			SenderId: newSender.Id,
			Subtotal: 200m,
			TaxRate: 0.2m,
			TotalAmount: 240m);

		var result = await handler.Handle(new UpdateInvoiceCommand(invoice.Id, update), CancellationToken.None);

		Assert.True(result.IsSuccess);
		Assert.Equal(newNumber, result.Value!.InvoiceNumber);
		Assert.Equal(newCustomer.Id, result.Value.Customer.Id);
		Assert.Equal(newSender.Id, result.Value.Sender.Id);
		Assert.Equal("EUR", result.Value.Currency);
		Assert.Equal(240m, result.Value.TotalAmount);
	}

	[Fact]
	public async Task Update_unknown_returns_not_found()
	{
		await using var dbContext = TestDbContextFactory.Create();
		var customer = AddCustomer(dbContext);
		var sender = AddSender(dbContext);
		var handler = new UpdateInvoiceHandler(dbContext);
		var update = new UpdateInvoiceDto(
			$"INV-{Guid.NewGuid():N}", DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(30),
			"USD", "Notes", customer.Id, sender.Id, 100m, 0.1m, 110m);

		var result = await handler.Handle(new UpdateInvoiceCommand(Guid.NewGuid(), update), CancellationToken.None);

		Assert.True(result.IsFailure);
		Assert.Equal("InvoiceNotFound", result.Error!.Code);
	}

	[Fact]
	public async Task Update_duplicate_invoice_number_returns_conflict()
	{
		await using var dbContext = TestDbContextFactory.Create();
		var customer = AddCustomer(dbContext);
		var sender = AddSender(dbContext);
		var existingNumber = $"INV-{Guid.NewGuid():N}";
		AddInvoice(dbContext, customer.Id, sender.Id, existingNumber);
		var invoice = AddInvoice(dbContext, customer.Id, sender.Id);
		var handler = new UpdateInvoiceHandler(dbContext);
		var update = new UpdateInvoiceDto(
			existingNumber, invoice.InvoiceDate, invoice.DueDate,
			invoice.Currency, invoice.Notes, customer.Id, sender.Id, invoice.Subtotal, invoice.TaxRate, invoice.TotalAmount);

		var result = await handler.Handle(new UpdateInvoiceCommand(invoice.Id, update), CancellationToken.None);

		Assert.True(result.IsFailure);
		Assert.Equal("InvoiceAlreadyExists", result.Error!.Code);
		Assert.Equal(ResultErrorType.Conflict, result.Error.Type);
	}

	[Fact]
	public async Task Update_same_invoice_number_is_allowed()
	{
		await using var dbContext = TestDbContextFactory.Create();
		var customer = AddCustomer(dbContext);
		var sender = AddSender(dbContext);
		var invoice = AddInvoice(dbContext, customer.Id, sender.Id);
		var handler = new UpdateInvoiceHandler(dbContext);
		var update = new UpdateInvoiceDto(
			invoice.InvoiceNumber, invoice.InvoiceDate, invoice.DueDate,
			"EUR", invoice.Notes, customer.Id, sender.Id, invoice.Subtotal, invoice.TaxRate, invoice.TotalAmount);

		var result = await handler.Handle(new UpdateInvoiceCommand(invoice.Id, update), CancellationToken.None);

		Assert.True(result.IsSuccess);
		Assert.Equal("EUR", result.Value!.Currency);
	}

	[Fact]
	public async Task Update_unknown_customer_returns_not_found()
	{
		await using var dbContext = TestDbContextFactory.Create();
		var customer = AddCustomer(dbContext);
		var sender = AddSender(dbContext);
		var invoice = AddInvoice(dbContext, customer.Id, sender.Id);
		var handler = new UpdateInvoiceHandler(dbContext);
		var update = new UpdateInvoiceDto(
			invoice.InvoiceNumber, invoice.InvoiceDate, invoice.DueDate,
			invoice.Currency, invoice.Notes, Guid.NewGuid(), sender.Id, invoice.Subtotal, invoice.TaxRate, invoice.TotalAmount);

		var result = await handler.Handle(new UpdateInvoiceCommand(invoice.Id, update), CancellationToken.None);

		Assert.True(result.IsFailure);
		Assert.Equal("CustomerNotFound", result.Error!.Code);
	}

	[Fact]
	public async Task Delete_removes_invoice()
	{
		await using var dbContext = TestDbContextFactory.Create();
		var customer = AddCustomer(dbContext);
		var sender = AddSender(dbContext);
		var invoice = AddInvoice(dbContext, customer.Id, sender.Id);
		var handler = new DeleteInvoiceHandler(dbContext);

		var result = await handler.Handle(new DeleteInvoiceCommand(invoice.Id), CancellationToken.None);

		Assert.True(result.IsSuccess);
		Assert.Empty(dbContext.Invoices);
	}

	[Fact]
	public async Task Delete_unknown_returns_not_found()
	{
		await using var dbContext = TestDbContextFactory.Create();
		var handler = new DeleteInvoiceHandler(dbContext);

		var result = await handler.Handle(new DeleteInvoiceCommand(Guid.NewGuid()), CancellationToken.None);

		Assert.True(result.IsFailure);
		Assert.Equal("InvoiceNotFound", result.Error!.Code);
	}

	[Fact]
	public async Task GetInvoices_clamps_page_size_and_pages_forward()
	{
		await using var dbContext = TestDbContextFactory.Create();
		var customer = AddCustomer(dbContext);
		var sender = AddSender(dbContext);
		for (var i = 0; i < 7; i++)
		{
			AddInvoice(dbContext, customer.Id, sender.Id);
		}
		var handler = new GetInvoicesHandler(dbContext);

		var firstPage = await handler.Handle(
			new GetInvoicesCommand(new GetInvoicesDto(Cursor: null, IsNextPage: null, PageSize: 100)),
			CancellationToken.None);

		Assert.True(firstPage.IsSuccess);
		Assert.Equal(5, firstPage.Value!.PageSize);
		Assert.Equal(5, firstPage.Value.Items.Count);
		Assert.NotNull(firstPage.Value.NextCursor);
		Assert.Null(firstPage.Value.PreviousCursor);

		var secondPage = await handler.Handle(
			new GetInvoicesCommand(new GetInvoicesDto(firstPage.Value.NextCursor, IsNextPage: true, PageSize: 5)),
			CancellationToken.None);

		Assert.True(secondPage.IsSuccess);
		Assert.Equal(2, secondPage.Value!.Items.Count);
		Assert.Null(secondPage.Value.NextCursor);
		Assert.NotNull(secondPage.Value.PreviousCursor);
	}

	[Fact]
	public async Task GetInvoicePdf_returns_pdf_bytes_named_after_invoice_number()
	{
		await using var dbContext = TestDbContextFactory.Create();
		var customer = AddCustomer(dbContext);
		var sender = AddSender(dbContext);
		var invoice = AddInvoice(dbContext, customer.Id, sender.Id);
		var handler = new GetInvoicePdfHandler(dbContext);

		var result = await handler.Handle(new GetInvoicePdfCommand(invoice.Id), CancellationToken.None);

		Assert.True(result.IsSuccess);
		Assert.Equal($"{invoice.InvoiceNumber}.pdf", result.Value!.FileName);
		Assert.True(result.Value.Content.Length > 0);
		Assert.Equal("%PDF"u8.ToArray(), result.Value.Content[..4]);
	}

	[Fact]
	public async Task GetInvoicePdf_unknown_returns_not_found()
	{
		await using var dbContext = TestDbContextFactory.Create();
		var handler = new GetInvoicePdfHandler(dbContext);

		var result = await handler.Handle(new GetInvoicePdfCommand(Guid.NewGuid()), CancellationToken.None);

		Assert.True(result.IsFailure);
		Assert.Equal("InvoiceNotFound", result.Error!.Code);
	}
}
