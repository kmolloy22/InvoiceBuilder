namespace InvoiceBuilder.Application.Features.Senders.Models.Create;

public sealed record CreateSenderDto(
	string SenderCompanyName,
	string SenderFullName,
	string SenderAddress,
	string SenderTaxVatId,
	string BankDetails);
