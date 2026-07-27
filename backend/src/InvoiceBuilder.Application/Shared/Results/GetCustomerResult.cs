namespace InvoiceBuilder.Application.Shared.Results;

public sealed record GetCustomerResult(
	Guid Id,
	string CompanyName,
	string CustomerName,
	string CustomerAddress,
	string PostalCode,
	string CustomerEmail,
	string CustomerTaxVatId);
