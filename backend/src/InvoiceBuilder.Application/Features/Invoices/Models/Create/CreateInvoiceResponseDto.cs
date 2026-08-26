namespace InvoiceBuilder.Application.Features.Invoices.Models.Create;

public sealed record CreateInvoiceResponseDto(
	/// <summary>
	/// The unique identifier of the created sender
	/// </summary>
	string Id,

	/// <summary>
	/// The URI location of the created sender resource
	/// </summary>
	string Location,

	/// <summary>
	/// Timestamp when the sender was created (UTC)
	/// </summary>
	DateTimeOffset CreatedAt
);
