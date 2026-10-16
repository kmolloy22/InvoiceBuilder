using InvoiceBuilder.Application.Features.Senders.Models.Create;
using InvoiceBuilder.Application.Features.Senders.Models.Get;
using InvoiceBuilder.Application.Features.Senders.Models.Update;
using InvoiceBuilder.Application.Shared.Responses.Senders;
using Refit;

namespace InvoiceBuilder.Web.Services;

public interface ISendersApiClient
{
	[Get("/api/senders")]
	Task<GetSendersResult> GetSendersAsync([Query] GetSendersDto request);

	[Post("/api/senders")]
	Task<CreateSenderResponseDto> CreateSenderAsync([Body] CreateSenderDto request);

	[Get("/api/senders/{id}")]
	Task<GetSenderResult> GetSenderByIdAsync(Guid id);

	[Put("/api/senders/{id}")]
	Task<UpdateSenderResult> UpdateSenderAsync(Guid id, [Body] UpdateSenderDto request);

	[Delete("/api/senders/{id}")]
	Task DeleteSenderAsync(Guid id);
}
