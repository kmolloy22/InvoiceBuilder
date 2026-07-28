namespace InvoiceBuilder.Domain.Entities;

public class InvoiceLineItem
{
	public Guid Id { get; set; }

	public required string ItemName { get; set; }

	public decimal Quantity { get; set; }

	public decimal UnitPrice { get; set; }

	public decimal Total { get; set; }

	public Guid InvoiceId { get; set; }

	public Invoice Invoice { get; set; } = null!;
}
