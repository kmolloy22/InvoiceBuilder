using InvoiceBuilder.Application.Features.Invoices.Models.Create;
using InvoiceBuilder.Application.Features.Invoices.Models.Get;
using InvoiceBuilder.Application.Features.Invoices.Models.Update;
using InvoiceBuilder.Application.Shared.Responses.Invoices;
using Refit;

namespace InvoiceBuilder.Web.Services;

public interface IInvoicesApiClient
{
	[Get("/api/invoices")]
	Task<GetInvoicesResult> GetInvoicesAsync([Query] GetInvoicesDto request);

	[Post("/api/invoices")]
	Task<CreateInvoiceResponseDto> CreateInvoiceAsync([Body] CreateInvoiceDto request);

	[Get("/api/invoices/{id}")]
	Task<GetInvoiceResult> GetInvoiceByIdAsync(Guid id);

	[Get("/api/invoices/{id}/pdf")]
	Task<HttpResponseMessage> GetInvoicePdfAsync(Guid id);

	[Put("/api/invoices/{id}")]
	Task<GetInvoiceResult> UpdateInvoiceAsync(Guid id, [Body] UpdateInvoiceDto request);

	[Delete("/api/invoices/{id}")]
	Task DeleteInvoiceAsync(Guid id);
}
