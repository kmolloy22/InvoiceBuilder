using InvoiceBuilder.Application.Features.Customers;
using InvoiceBuilder.Application.Features.Customers.Models.Create;
using InvoiceBuilder.Application.Features.Customers.Models.Get;
using InvoiceBuilder.Application.Features.Customers.Models.Update;
using InvoiceBuilder.Database;
using InvoiceBuilder.Domain.Entities;
using InvoiceBuilder.Domain.Results;
using Xunit;

namespace InvoiceBuilder.Application.Tests;

public sealed class CustomerHandlersTests
{
	private static CreateCustomerDto NewCustomerDto() => new(
		CompanyName: $"Acme {Guid.NewGuid():N}",
		CustomerName: "Test Person",
		CustomerAddress: "1 Test Way",
		PostalCode: "10001",
		CustomerEmail: $"test-{Guid.NewGuid():N}@example.com",
		CustomerTaxVatId: "US123456789");

	private static Customer AddCustomer(InvoiceBuilderContext dbContext)
	{
		var customer = Customer.Create(
			$"Acme {Guid.NewGuid():N}", "Test Person", "1 Test Way", "10001",
			$"test-{Guid.NewGuid():N}@example.com", "US123456789");
		dbContext.Customers.Add(customer);
		dbContext.SaveChanges();
		return customer;
	}

	private static Invoice AddInvoiceFor(InvoiceBuilderContext dbContext, Guid customerId, Guid senderId)
	{
		var invoice = Invoice.Create(
			$"INV-{Guid.NewGuid():N}", DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(30),
			"USD", "Notes", customerId, senderId, 100m, 0m, 100m,
			[new InvoiceLineItem { Id = Guid.NewGuid(), ItemName = "Item", Quantity = 1, UnitPrice = 100m, Total = 100m }]);
		dbContext.Invoices.Add(invoice);
		dbContext.SaveChanges();
		return invoice;
	}

	[Fact]
	public async Task Create_adds_customer_and_returns_id_and_created_at()
	{
		await using var dbContext = TestDbContextFactory.Create();
		var handler = new CreateCustomerHandler(dbContext);
		var dto = NewCustomerDto();

		var result = await handler.Handle(new CreateCustomerCommand(dto), CancellationToken.None);

		Assert.True(result.IsSuccess);
		Assert.NotEqual(Guid.Empty, result.Value!.Id);
		Assert.Single(dbContext.Customers);
		Assert.Equal(dto.CompanyName, dbContext.Customers.Single().CompanyName);
	}

	[Fact]
	public async Task GetById_returns_customer()
	{
		await using var dbContext = TestDbContextFactory.Create();
		var customer = AddCustomer(dbContext);
		var handler = new GetCustomerByIdHandler(dbContext);

		var result = await handler.Handle(new GetCustomerByIdCommand(customer.Id), CancellationToken.None);

		Assert.True(result.IsSuccess);
		Assert.Equal(customer.CompanyName, result.Value!.CompanyName);
	}

	[Fact]
	public async Task GetById_unknown_returns_not_found()
	{
		await using var dbContext = TestDbContextFactory.Create();
		var handler = new GetCustomerByIdHandler(dbContext);

		var result = await handler.Handle(new GetCustomerByIdCommand(Guid.NewGuid()), CancellationToken.None);

		Assert.True(result.IsFailure);
		Assert.Equal("CustomerNotFound", result.Error!.Code);
		Assert.Equal(ResultErrorType.NotFound, result.Error.Type);
	}

	[Fact]
	public async Task Update_modifies_customer()
	{
		await using var dbContext = TestDbContextFactory.Create();
		var customer = AddCustomer(dbContext);
		var handler = new UpdateCustomerHandler(dbContext);
		var update = new UpdateCustomerDto(
			CompanyName: "Renamed Co", CustomerName: "Renamed Person", CustomerAddress: "2 New Road",
			PostalCode: "20002", CustomerEmail: "renamed@example.com", CustomerTaxVatId: "US999999999");

		var result = await handler.Handle(new UpdateCustomerCommand(customer.Id, update), CancellationToken.None);

		Assert.True(result.IsSuccess);
		Assert.Equal("Renamed Co", result.Value!.CompanyName);
		Assert.Equal("Renamed Co", dbContext.Customers.Single().CompanyName);
	}

	[Fact]
	public async Task Update_unknown_returns_not_found()
	{
		await using var dbContext = TestDbContextFactory.Create();
		var handler = new UpdateCustomerHandler(dbContext);
		var update = new UpdateCustomerDto("Ghost Co", "Ghost", "Nowhere", "00000", "ghost@example.com", "US000000000");

		var result = await handler.Handle(new UpdateCustomerCommand(Guid.NewGuid(), update), CancellationToken.None);

		Assert.True(result.IsFailure);
		Assert.Equal("CustomerNotFound", result.Error!.Code);
	}

	[Fact]
	public async Task Delete_removes_customer()
	{
		await using var dbContext = TestDbContextFactory.Create();
		var customer = AddCustomer(dbContext);
		var handler = new DeleteCustomerHandler(dbContext);

		var result = await handler.Handle(new DeleteCustomerCommand(customer.Id), CancellationToken.None);

		Assert.True(result.IsSuccess);
		Assert.Empty(dbContext.Customers);
	}

	[Fact]
	public async Task Delete_unknown_returns_not_found()
	{
		await using var dbContext = TestDbContextFactory.Create();
		var handler = new DeleteCustomerHandler(dbContext);

		var result = await handler.Handle(new DeleteCustomerCommand(Guid.NewGuid()), CancellationToken.None);

		Assert.True(result.IsFailure);
		Assert.Equal("CustomerNotFound", result.Error!.Code);
	}

	[Fact]
	public async Task Delete_customer_with_invoices_returns_conflict_and_keeps_customer()
	{
		await using var dbContext = TestDbContextFactory.Create();
		var customer = AddCustomer(dbContext);
		var sender = Sender.Create($"Sender {Guid.NewGuid():N}", "Full Name", "Address", "VAT", "Bank");
		dbContext.Senders.Add(sender);
		dbContext.SaveChanges();
		AddInvoiceFor(dbContext, customer.Id, sender.Id);
		var handler = new DeleteCustomerHandler(dbContext);

		var result = await handler.Handle(new DeleteCustomerCommand(customer.Id), CancellationToken.None);

		Assert.True(result.IsFailure);
		Assert.Equal("CustomerInUse", result.Error!.Code);
		Assert.Equal(ResultErrorType.Conflict, result.Error.Type);
		Assert.Single(dbContext.Customers);
	}

	[Fact]
	public async Task GetCustomers_clamps_page_size_and_pages_forward()
	{
		await using var dbContext = TestDbContextFactory.Create();
		for (var i = 0; i < 7; i++)
		{
			AddCustomer(dbContext);
		}
		var handler = new GetCustomersHandler(dbContext);

		var firstPage = await handler.Handle(
			new GetCustomersCommand(new GetCustomersDto(Cursor: null, IsNextPage: null, PageSize: 100)),
			CancellationToken.None);

		Assert.True(firstPage.IsSuccess);
		Assert.Equal(5, firstPage.Value!.PageSize);
		Assert.Equal(5, firstPage.Value.Items.Count);
		Assert.NotNull(firstPage.Value.NextCursor);
		Assert.Null(firstPage.Value.PreviousCursor);

		var secondPage = await handler.Handle(
			new GetCustomersCommand(new GetCustomersDto(firstPage.Value.NextCursor, IsNextPage: true, PageSize: 5)),
			CancellationToken.None);

		Assert.True(secondPage.IsSuccess);
		Assert.Equal(2, secondPage.Value!.Items.Count);
		Assert.Null(secondPage.Value.NextCursor);
		Assert.NotNull(secondPage.Value.PreviousCursor);
	}
}
