namespace InvoiceBuilder.Application.Shared.Responses.Senders;

public sealed record GetSendersResult(
	List<SenderListItem> Items,
	int Offset,
	int Limit,
	int Total);

public sealed record SenderListItem(
	Guid Id,
	string SenderCompanyName,
	string SenderFullName,
	string SenderTaxVatId);
