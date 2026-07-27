namespace InvoiceBuilder.Application.Features.Customers.Models.Update;

public sealed record UpdateCustomerDto(
	string CompanyName,
	string CustomerName,
	string CustomerAddress,
	string PostalCode,
	string CustomerEmail,
	string CustomerTaxVatId);
