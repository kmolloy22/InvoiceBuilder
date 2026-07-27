namespace InvoiceBuilder.Application.Shared.Results;

public sealed record UpdateCustomerResult(
	Guid Id,
	string CompanyName,
	string CustomerName,
	string CustomerAddress,
	string PostalCode,
	string CustomerEmail,
	string CustomerTaxVatId);
