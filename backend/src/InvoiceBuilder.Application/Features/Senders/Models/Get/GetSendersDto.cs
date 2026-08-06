namespace InvoiceBuilder.Application.Features.Senders.Models.Get;

public sealed record GetSendersDto(
	Guid? Cursor,
	bool? IsNextPage,
	int PageSize = 5);
