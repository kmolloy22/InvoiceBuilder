namespace InvoiceBuilder.Application.Shared.Responses.Senders;

public sealed record GetSendersResult(
	List<SenderListItem> Items,
	Guid? NextCursor,
	Guid? PreviousCursor,
	int PageSize);

public sealed record SenderListItem(
	Guid Id,
	string SenderCompanyName,
	string SenderFullName,
	string SenderTaxVatId);
