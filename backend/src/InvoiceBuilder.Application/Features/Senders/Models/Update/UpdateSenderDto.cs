namespace InvoiceBuilder.Application.Features.Senders.Models.Update;

public sealed record UpdateSenderDto(
	string SenderCompanyName,
	string SenderFullName,
	string SenderAddress,
	string SenderTaxVatId,
	string BankDetails);
