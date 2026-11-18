using InvoiceBuilder.Application.Features.Senders;
using InvoiceBuilder.Application.Features.Senders.Models.Create;
using InvoiceBuilder.Application.Features.Senders.Models.Get;
using InvoiceBuilder.Application.Features.Senders.Models.Update;
using InvoiceBuilder.Database;
using InvoiceBuilder.Domain.Entities;
using InvoiceBuilder.Domain.Results;
using Xunit;

namespace InvoiceBuilder.Application.Tests;

public sealed class SenderHandlersTests
{
	private static Sender AddSender(InvoiceBuilderContext dbContext)
	{
		var sender = Sender.Create(
			$"Sender Co {Guid.NewGuid():N}", "Test Sender", "1 Test Way", "US123456789",
			"Bank: Test Bank, IBAN: US00TEST000111");
		dbContext.Senders.Add(sender);
		dbContext.SaveChanges();
		return sender;
	}

	private static void AddInvoiceFor(InvoiceBuilderContext dbContext, Guid customerId, Guid senderId)
	{
		var invoice = Invoice.Create(
			$"INV-{Guid.NewGuid():N}", DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(30),
			"USD", "Notes", customerId, senderId, 100m, 0m, 100m,
			[new InvoiceLineItem { Id = Guid.NewGuid(), ItemName = "Item", Quantity = 1, UnitPrice = 100m, Total = 100m }]);
		dbContext.Invoices.Add(invoice);
		dbContext.SaveChanges();
	}

	[Fact]
	public async Task Create_adds_sender_and_returns_id_and_created_at()
	{
		await using var dbContext = TestDbContextFactory.Create();
		var handler = new CreateSenderHandler(dbContext);
		var dto = new CreateSenderDto(
			SenderCompanyName: $"Sender Co {Guid.NewGuid():N}", SenderFullName: "Test Sender",
			SenderAddress: "1 Test Way", SenderTaxVatId: "US123456789", BankDetails: "Bank: Test Bank");

		var result = await handler.Handle(new CreateSenderCommand(dto), CancellationToken.None);

		Assert.True(result.IsSuccess);
		Assert.NotEqual(Guid.Empty, result.Value!.Id);
		Assert.Single(dbContext.Senders);
	}

	[Fact]
	public async Task GetById_returns_sender()
	{
		await using var dbContext = TestDbContextFactory.Create();
		var sender = AddSender(dbContext);
		var handler = new GetSenderByIdHandler(dbContext);

		var result = await handler.Handle(new GetSenderByIdCommand(sender.Id), CancellationToken.None);

		Assert.True(result.IsSuccess);
		Assert.Equal(sender.SenderCompanyName, result.Value!.SenderCompanyName);
	}

	[Fact]
	public async Task GetById_unknown_returns_not_found()
	{
		await using var dbContext = TestDbContextFactory.Create();
		var handler = new GetSenderByIdHandler(dbContext);

		var result = await handler.Handle(new GetSenderByIdCommand(Guid.NewGuid()), CancellationToken.None);

		Assert.True(result.IsFailure);
		Assert.Equal("SenderNotFound", result.Error!.Code);
		Assert.Equal(ResultErrorType.NotFound, result.Error.Type);
	}

	[Fact]
	public async Task Update_modifies_sender()
	{
		await using var dbContext = TestDbContextFactory.Create();
		var sender = AddSender(dbContext);
		var handler = new UpdateSenderHandler(dbContext);
		var update = new UpdateSenderDto(
			SenderCompanyName: "Renamed Sender Co", SenderFullName: "Renamed Person",
			SenderAddress: "2 New Road", SenderTaxVatId: "US999999999", BankDetails: "Bank: New Bank");

		var result = await handler.Handle(new UpdateSenderCommand(sender.Id, update), CancellationToken.None);

		Assert.True(result.IsSuccess);
		Assert.Equal("Renamed Sender Co", result.Value!.SenderCompanyName);
		Assert.Equal("Renamed Sender Co", dbContext.Senders.Single().SenderCompanyName);
	}

	[Fact]
	public async Task Update_unknown_returns_not_found()
	{
		await using var dbContext = TestDbContextFactory.Create();
		var handler = new UpdateSenderHandler(dbContext);
		var update = new UpdateSenderDto("Ghost Co", "Ghost", "Nowhere", "US000000000", "None");

		var result = await handler.Handle(new UpdateSenderCommand(Guid.NewGuid(), update), CancellationToken.None);

		Assert.True(result.IsFailure);
		Assert.Equal("SenderNotFound", result.Error!.Code);
	}

	[Fact]
	public async Task Delete_removes_sender()
	{
		await using var dbContext = TestDbContextFactory.Create();
		var sender = AddSender(dbContext);
		var handler = new DeleteSenderHandler(dbContext);

		var result = await handler.Handle(new DeleteSenderCommand(sender.Id), CancellationToken.None);

		Assert.True(result.IsSuccess);
		Assert.Empty(dbContext.Senders);
	}

	[Fact]
	public async Task Delete_unknown_returns_not_found()
	{
		await using var dbContext = TestDbContextFactory.Create();
		var handler = new DeleteSenderHandler(dbContext);

		var result = await handler.Handle(new DeleteSenderCommand(Guid.NewGuid()), CancellationToken.None);

		Assert.True(result.IsFailure);
		Assert.Equal("SenderNotFound", result.Error!.Code);
	}

	[Fact]
	public async Task Delete_sender_with_invoices_returns_conflict_and_keeps_sender()
	{
		await using var dbContext = TestDbContextFactory.Create();
		var sender = AddSender(dbContext);
		var customer = Customer.Create($"Co {Guid.NewGuid():N}", "Name", "Address", "00000", "e@example.com", "VAT");
		dbContext.Customers.Add(customer);
		dbContext.SaveChanges();
		AddInvoiceFor(dbContext, customer.Id, sender.Id);
		var handler = new DeleteSenderHandler(dbContext);

		var result = await handler.Handle(new DeleteSenderCommand(sender.Id), CancellationToken.None);

		Assert.True(result.IsFailure);
		Assert.Equal("SenderInUse", result.Error!.Code);
		Assert.Equal(ResultErrorType.Conflict, result.Error.Type);
		Assert.Single(dbContext.Senders);
	}

	[Fact]
	public async Task GetSenders_clamps_page_size_and_pages_forward()
	{
		await using var dbContext = TestDbContextFactory.Create();
		for (var i = 0; i < 7; i++)
		{
			AddSender(dbContext);
		}
		var handler = new GetSendersHandler(dbContext);

		var firstPage = await handler.Handle(
			new GetSendersCommand(new GetSendersDto(Cursor: null, IsNextPage: null, PageSize: 100)),
			CancellationToken.None);

		Assert.True(firstPage.IsSuccess);
		Assert.Equal(5, firstPage.Value!.PageSize);
		Assert.Equal(5, firstPage.Value.Items.Count);
		Assert.NotNull(firstPage.Value.NextCursor);
		Assert.Null(firstPage.Value.PreviousCursor);

		var secondPage = await handler.Handle(
			new GetSendersCommand(new GetSendersDto(firstPage.Value.NextCursor, IsNextPage: true, PageSize: 5)),
			CancellationToken.None);

		Assert.True(secondPage.IsSuccess);
		Assert.Equal(2, secondPage.Value!.Items.Count);
		Assert.Null(secondPage.Value.NextCursor);
		Assert.NotNull(secondPage.Value.PreviousCursor);
	}
}
