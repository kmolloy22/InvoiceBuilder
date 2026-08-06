using InvoiceBuilder.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InvoiceBuilder.Database;

public class InvoiceBuilderContext(DbContextOptions<InvoiceBuilderContext> options)
	: DbContext(options)
{
	public DbSet<Customer> Customers => Set<Customer>();
	public DbSet<Sender> Senders => Set<Sender>();
	public DbSet<Invoice> Invoices => Set<Invoice>();
	public DbSet<InvoiceLineItem> InvoiceLineItems => Set<InvoiceLineItem>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.ApplyConfigurationsFromAssembly(typeof(InvoiceBuilderContext).Assembly);
	}
}
