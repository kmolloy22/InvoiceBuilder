using InvoiceBuilder.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvoiceBuilder.Infrastructure.Database.Configurations;

public class InvoiceLineItemEntityConfiguration : IEntityTypeConfiguration<InvoiceLineItem>
{
	public void Configure(EntityTypeBuilder<InvoiceLineItem> builder)
	{
		builder.HasKey(x => x.Id);

		builder.Property(x => x.Id).ValueGeneratedOnAdd();

		builder.Property(x => x.ItemName).IsRequired();
		builder.Property(x => x.Quantity).IsRequired();
		builder.Property(x => x.UnitPrice).IsRequired();
		builder.Property(x => x.Total).IsRequired();
	}
}
