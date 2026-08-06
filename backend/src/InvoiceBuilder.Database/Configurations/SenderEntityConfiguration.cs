using InvoiceBuilder.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvoiceBuilder.Infrastructure.Database.Configurations;

public class SenderEntityConfiguration : IEntityTypeConfiguration<Sender>
{
	public void Configure(EntityTypeBuilder<Sender> builder)
	{
		builder.HasKey(x => x.Id);

		builder.Property(x => x.SenderCompanyName).IsRequired();
		builder.Property(x => x.SenderFullName).IsRequired();
		builder.Property(x => x.SenderAddress).IsRequired();
		builder.Property(x => x.SenderTaxVatId).IsRequired();
		builder.Property(x => x.BankDetails).IsRequired();
		builder.Property(x => x.CreatedAt).IsRequired();
	}
}
