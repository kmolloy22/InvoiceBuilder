namespace InvoiceBuilder.Application.Shared.Responses.Senders;

public sealed record UpdateSenderResult(
	Guid Id,
	string SenderCompanyName,
	string SenderFullName,
	string SenderAddress,
	string SenderTaxVatId,
	string BankDetails);
