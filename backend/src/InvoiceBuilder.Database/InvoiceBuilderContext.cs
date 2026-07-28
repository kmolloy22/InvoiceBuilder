using InvoiceBuilder.Database.Configurations;
using InvoiceBuilder.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InvoiceBuilder.Database;

public class InvoiceBuilderContext(DbContextOptions<InvoiceBuilderContext> options)
	: DbContext(options)
{
	public DbSet<Customer> Customers => Set<Customer>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.ApplyConfigurationsFromAssembly(typeof(CustomerEntityConfiguration).Assembly);
	}
}
