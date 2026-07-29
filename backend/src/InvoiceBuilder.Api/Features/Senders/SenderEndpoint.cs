using InvoiceBuilder.Api.Features.Senders.SenderCreate;
using InvoiceBuilder.Api.Features.Senders.SenderDelete;
using InvoiceBuilder.Api.Features.Senders.SenderGetById;
using InvoiceBuilder.Api.Features.Senders.SendersGet;
using InvoiceBuilder.Api.Features.Senders.SenderUpdate;

namespace InvoiceBuilder.Api.Features.Senders;

public static class SenderEndpoint
{
	public static void MapSenders(this IEndpointRouteBuilder app)
	{
		var group = app.MapGroup("api/senders").WithTags("Senders");

		group.MapCreateSenderEndpoint();
		group.MapGetSendersEndpoint();
		group.MapGetByIdSenderEndpoint();
		group.MapUpdateSenderEndpoint();
		group.MapDeleteSenderEndpoint();
	}
}
