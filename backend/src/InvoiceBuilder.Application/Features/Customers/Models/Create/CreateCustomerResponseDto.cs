namespace InvoiceBuilder.Application.Features.Customers.Models.Create;

public sealed record CreateCustomerResponseDto(
	/// <summary>
	/// The unique identifier of the created account
	/// </summary>
	string Id,

	/// <summary>
	/// The URI location of the created account resource
	/// </summary>
	string Location,

	/// <summary>
	/// Timestamp when the account was created (UTC)
	/// </summary>
	DateTimeOffset CreatedAt
);
