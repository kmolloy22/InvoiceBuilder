namespace InvoiceBuilder.Application.Shared.Responses.Senders;

public sealed record CreateSenderResult(
	Guid Id, DateTimeOffset CreatedAt);
