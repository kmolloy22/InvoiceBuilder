namespace InvoiceBuilder.Application.Shared.Responses.Invoices;

public sealed record InvoicePdfResult(string FileName, byte[] Content);
