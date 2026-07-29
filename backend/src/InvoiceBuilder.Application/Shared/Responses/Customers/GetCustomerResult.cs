namespace InvoiceBuilder.Application.Shared.Responses.Customers;

public sealed record GetCustomerResult(
	Guid Id,
	string CompanyName,
	string CustomerName,
	string CustomerAddress,
	string PostalCode,
	string CustomerEmail,
	string CustomerTaxVatId);
