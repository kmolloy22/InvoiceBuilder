namespace InvoiceBuilder.Application.Features.Customers.Models.Create;

public sealed record CreateCustomerDto(
	string CompanyName,
	string CustomerName,
	string CustomerAddress,
	string PostalCode,
	string CustomerEmail,
	string CustomerTaxVatId);
