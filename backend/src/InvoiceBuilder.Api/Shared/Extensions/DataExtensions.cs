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

		if(!await dbContext.Customers.AnyAsync())
		{
			var customers = new[]
		{
			Customer.Create(
				"Acme Corporation",
				"John Smith",
				"123 Business Ave, Suite 100",
				"10001",
				"john.smith@acme.com",
				"US123456789"),
			Customer.Create(
				"TechStart Inc",
				"Sarah Johnson",
				"456 Innovation Drive",
				"94105",
				"sarah.johnson@techstart.com",
				"US987654321"),
			Customer.Create(
				"Global Enterprises",
				"Michael Chen",
				"789 Corporate Blvd",
				"60601",
				"michael.chen@globalent.com",
				"US555444333"),
			Customer.Create(
				"Creative Solutions Ltd",
				"Emma Williams",
				"321 Design Street",
				"90210",
				"emma.williams@creative.com",
				"US222333444"),
			Customer.Create(
				"DataDriven Analytics",
				"James Rodriguez",
				"654 Analytics Way",
				"98101",
				"james.rodriguez@datadriven.com",
				"US111222333"),
			Customer.Create(
				"Cloud Systems Group",
				"Lisa Anderson",
				"987 Cloud Plaza, Floor 5",
				"75001",
				"lisa.anderson@cloudsys.com",
				"US444555666"),
			Customer.Create(
				"BuildRight Contractors",
				"David Martinez",
				"147 Construction Lane",
				"85001",
				"david.martinez@buildright.com",
				"US777888999"),
			Customer.Create(
				"FutureForward Technologies",
				"Rachel Green",
				"258 Tech Tower",
				"02101",
				"rachel.green@futureforward.com",
				"US333444555"),
			Customer.Create(
				"Premium Services Co",
				"Christopher Lee",
				"369 Service Road",
				"33101",
				"christopher.lee@premium.com",
				"US666777888"),
			Customer.Create(
				"Unified Digital Partners",
				"Victoria Martinez",
				"741 Digital Hub",
				"77001",
				"victoria.martinez@unified.com",
				"US999000111")
		};

			await dbContext.Customers.AddRangeAsync(customers);
			await dbContext.SaveChangesAsync();
		}
	}
}
