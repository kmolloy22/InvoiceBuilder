using InvoiceBuilder.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvoiceBuilder.Database.Configurations;

public class CustomerEntityConfiguration : IEntityTypeConfiguration<Customer>
{
	public void Configure(EntityTypeBuilder<Customer> builder)
	{
		builder.HasKey(x => x.Id);

		builder.Property(x => x.CompanyName).IsRequired();
		builder.Property(x => x.CustomerName).IsRequired();
		builder.Property(x => x.CustomerAddress).IsRequired();
		builder.Property(x => x.PostalCode).IsRequired();
		builder.Property(x => x.CustomerEmail).IsRequired();
		builder.Property(x => x.CustomerTaxVatId).IsRequired();
		builder.Property(x => x.CreatedAt).IsRequired();
	}
}
