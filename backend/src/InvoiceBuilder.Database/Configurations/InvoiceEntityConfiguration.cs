using InvoiceBuilder.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvoiceBuilder.Infrastructure.Database.Configurations;

public class InvoiceEntityConfiguration : IEntityTypeConfiguration<Invoice>
{
	public void Configure(EntityTypeBuilder<Invoice> builder)
	{
		builder.HasKey(x => x.Id);
		builder.HasIndex(x => x.InvoiceNumber);

		builder.Property(x => x.InvoiceNumber).IsRequired();
		builder.Property(x => x.InvoiceDate).IsRequired();
		builder.Property(x => x.DueDate).IsRequired();
		builder.Property(x => x.Currency).IsRequired();
		builder.Property(x => x.Notes).IsRequired();
		builder.Property(x => x.CustomerId).IsRequired();
		builder.Property(x => x.SenderId).IsRequired();
		builder.Property(x => x.Subtotal).IsRequired();
		builder.Property(x => x.TaxRate).IsRequired();
		builder.Property(x => x.TotalAmount).IsRequired();
		builder.Property(x => x.CreatedAt).IsRequired();

		builder.HasOne(x => x.Customer)
			.WithMany()
			.HasForeignKey(x => x.CustomerId)
			.OnDelete(DeleteBehavior.Restrict);

		builder.HasOne(x => x.Sender)
			.WithMany()
			.HasForeignKey(x => x.SenderId)
			.OnDelete(DeleteBehavior.Restrict);

		builder.HasMany(x => x.LineItems)
			.WithOne(x => x.Invoice)
			.HasForeignKey(x => x.InvoiceId);

		builder.Navigation(x => x.LineItems)
			.UsePropertyAccessMode(PropertyAccessMode.Field);
	}
}
