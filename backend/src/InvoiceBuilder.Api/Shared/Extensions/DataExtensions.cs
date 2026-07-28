using Azure.Core;
using InvoiceBuilder.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InvoiceBuilder.Database.Extensions;

public static class DataExtensions
{
	public static async Task InitializeDbAsync(this WebApplication app)
	{
		await app.MigrateDbAsync();
		await app.SeedDbAsync();
		app.Logger.LogInformation(18, "The database is ready!");
	}

	public static WebApplicationBuilder AddInvoiceBuilderNpgSql<TContext>(
			this WebApplicationBuilder builder,
			string connectionStringName,
			TokenCredential? credential = null)
		where TContext : DbContext
	{
		if (builder.Environment.IsProduction())
		{
			builder.AddAzureNpgsqlDbContext<TContext>(
				connectionStringName,
				settings => settings.Credential = credential);
		}
		else
		{
			builder.AddAzureNpgsqlDbContext<TContext>(connectionStringName);
		}

		return builder;
	}

	private static async Task MigrateDbAsync(this WebApplication app)
	{
		using var scope = app.Services.CreateScope();
		InvoiceBuilderContext dbContext = scope.ServiceProvider
			.GetRequiredService<InvoiceBuilderContext>();

		await dbContext.Database.MigrateAsync();
	}

	private static async Task SeedDbAsync(this WebApplication app)
	{
		using var scope = app.Services.CreateScope();
		InvoiceBuilderContext dbContext = scope.ServiceProvider
			.GetRequiredService<InvoiceBuilderContext>();

		if (!await dbContext.Customers.AnyAsync())
		{
			var customers = new[]
			{
				Customer.Create("Acme Corporation", "John Smith", "123 Business Ave, Suite 100", "10001", "john.smith@acme.com", "US123456789"),
				Customer.Create("TechStart Inc", "Sarah Johnson", "456 Innovation Drive", "94105", "sarah.johnson@techstart.com", "US987654321"),
				Customer.Create("Global Enterprises", "Michael Chen", "789 Corporate Blvd", "60601", "michael.chen@globalent.com", "US555444333"),
				Customer.Create("Creative Solutions Ltd", "Emma Williams", "321 Design Street", "90210", "emma.williams@creative.com", "US222333444"),
				Customer.Create("DataDriven Analytics", "James Rodriguez", "654 Analytics Way", "98101", "james.rodriguez@datadriven.com", "US111222333"),
				Customer.Create("Cloud Systems Group", "Lisa Anderson", "987 Cloud Plaza, Floor 5", "75001", "lisa.anderson@cloudsys.com", "US444555666"),
				Customer.Create("BuildRight Contractors", "David Martinez", "147 Construction Lane", "85001", "david.martinez@buildright.com", "US777888999"),
				Customer.Create("FutureForward Technologies", "Rachel Green", "258 Tech Tower", "02101", "rachel.green@futureforward.com", "US333444555"),
				Customer.Create("Premium Services Co", "Christopher Lee", "369 Service Road", "33101", "christopher.lee@premium.com", "US666777888"),
				Customer.Create("Unified Digital Partners", "Victoria Martinez", "741 Digital Hub", "77001", "victoria.martinez@unified.com", "US999000111")
			};

			await dbContext.Customers.AddRangeAsync(customers);
		}

		if (!await dbContext.Senders.AnyAsync())
		{
			var senders = new[]
			{
				Sender.Create(
					"Invoice Builder LLC",
					"Kevin Ruhl",
					"500 Market Street, New York, NY 10002",
					"USINV000111",
					"Bank: Contoso Bank, IBAN: US00CONT000111222333, SWIFT: CTSOUS33"),
				Sender.Create(
					"Invoice Builder Europe Ltd",
					"Ana Novak",
					"14 Innovation Park, Dublin D02",
					"EUINV000222",
					"Bank: Fabrikam Bank, IBAN: IE00FABR444555666, SWIFT: FBKMIE2D")
			};

			await dbContext.Senders.AddRangeAsync(senders);
		}

		await dbContext.SaveChangesAsync();

		if (!await dbContext.Invoices.AnyAsync())
		{
			var customers = await dbContext.Customers
				.OrderBy(x => x.CompanyName)
				.Take(3)
				.ToListAsync();

			var senders = await dbContext.Senders
				.OrderBy(x => x.SenderCompanyName)
				.Take(2)
				.ToListAsync();

			if (customers.Count == 0 || senders.Count == 0)
			{
				app.Logger.LogWarning("Skipping invoice seed because required customers or senders are missing.");
				return;
			}

			var invoice1Items = new List<InvoiceLineItem>
			{
				CreateLineItem("Web Application Development", 40m, 120m),
				CreateLineItem("Cloud Hosting Setup", 1m, 750m),
				CreateLineItem("Support Retainer", 1m, 500m)
			};
			var invoice1Subtotal = invoice1Items.Sum(x => x.Total);
			const decimal invoice1TaxRate = 0.20m;
			var invoice1TaxAmount = invoice1Subtotal * invoice1TaxRate;
			var invoice1Total = invoice1Subtotal + invoice1TaxAmount;

			var invoice2Items = new List<InvoiceLineItem>
			{
				CreateLineItem("Data Migration", 24m, 95m),
				CreateLineItem("API Integration", 16m, 110m)
			};
			var invoice2Subtotal = invoice2Items.Sum(x => x.Total);
			const decimal invoice2TaxRate = 0.10m;
			var invoice2TaxAmount = invoice2Subtotal * invoice2TaxRate;
			var invoice2Total = invoice2Subtotal + invoice2TaxAmount;

			var invoice3Items = new List<InvoiceLineItem>
			{
				CreateLineItem("UI/UX Design Sprint", 32m, 85m),
				CreateLineItem("Accessibility Review", 8m, 100m),
				CreateLineItem("QA Regression Pack", 12m, 70m)
			};
			var invoice3Subtotal = invoice3Items.Sum(x => x.Total);
			const decimal invoice3TaxRate = 0.15m;
			var invoice3TaxAmount = invoice3Subtotal * invoice3TaxRate;
			var invoice3Total = invoice3Subtotal + invoice3TaxAmount;

			var invoices = new[]
			{
				Invoice.Create(
					"INV-2026-0001",
					DateTime.UtcNow.Date.AddDays(-14),
					DateTime.UtcNow.Date.AddDays(16),
					"USD",
					"Thank you for your business.",
					customers[0].Id,
					senders[0].Id,
					invoice1Subtotal,
					invoice1TaxRate,
					invoice1Total,
					invoice1Items),
				Invoice.Create(
					"INV-2026-0002",
					DateTime.UtcNow.Date.AddDays(-7),
					DateTime.UtcNow.Date.AddDays(23),
					"USD",
					"Net 30 payment terms.",
					customers[1].Id,
					senders[0].Id,
					invoice2Subtotal,
					invoice2TaxRate,
					invoice2Total,
					invoice2Items),
				Invoice.Create(
					"INV-2026-0003",
					DateTime.UtcNow.Date,
					DateTime.UtcNow.Date.AddDays(30),
					"EUR",
					"Please reference invoice number on payment.",
					customers[2].Id,
					senders[1].Id,
					invoice3Subtotal,
					invoice3TaxRate,
					invoice3Total,
					invoice3Items)
			};

			await dbContext.Invoices.AddRangeAsync(invoices);
			await dbContext.SaveChangesAsync();
		}
	}

	private static InvoiceLineItem CreateLineItem(string itemName, decimal quantity, decimal unitPrice)
	{
		return new InvoiceLineItem
		{
			ItemName = itemName,
			Quantity = quantity,
			UnitPrice = unitPrice,
			Total = quantity * unitPrice
		};
	}
}
