using InvoiceBuilder.Application.Shared.Responses.Invoices;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace InvoiceBuilder.Application.Features.Invoices.Pdf;

public sealed class InvoicePdfDocument : IDocument
{
	private readonly GetInvoiceResult _invoice;

	public InvoicePdfDocument(GetInvoiceResult invoice)
	{
		_invoice = invoice;
	}

	public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

	public DocumentSettings GetSettings() => DocumentSettings.Default;

	public void Compose(IDocumentContainer container)
	{
		container.Page(page =>
		{
			page.Margin(40);
			page.Size(PageSizes.A4);
			page.DefaultTextStyle(x => x.FontSize(10));

			page.Header().Element(ComposeHeader);
			page.Content().Element(ComposeContent);
			page.Footer().AlignCenter().Text(text =>
			{
				text.Span("Page ");
				text.CurrentPageNumber();
				text.Span(" of ");
				text.TotalPages();
			});
		});
	}

	private void ComposeHeader(IContainer container)
	{
		container.Row(row =>
		{
			row.RelativeItem().Column(column =>
			{
				column.Item().Text("INVOICE").FontSize(20).Bold();
				column.Item().Text(_invoice.InvoiceNumber).FontSize(12).SemiBold();
			});

			row.RelativeItem().Column(column =>
			{
				column.Item().AlignRight().Text(_invoice.Sender.SenderCompanyName).SemiBold();
				column.Item().AlignRight().Text(_invoice.Sender.SenderFullName);
				column.Item().AlignRight().Text(_invoice.Sender.SenderAddress);
			});
		});
	}

	private void ComposeContent(IContainer container)
	{
		container.PaddingTop(20).Column(column =>
		{
			column.Spacing(20);

			column.Item().Row(row =>
			{
				row.RelativeItem().Column(c =>
				{
					c.Item().Text("Bill to").SemiBold();
					c.Item().Text(_invoice.Customer.CompanyName);
					c.Item().Text(_invoice.Customer.CustomerName);
					c.Item().Text(_invoice.Customer.CustomerAddress);
					c.Item().Text(_invoice.Customer.CustomerEmail);
				});

				row.RelativeItem().Column(c =>
				{
					c.Item().Row(r =>
					{
						r.RelativeItem().Text("Invoice date:");
						r.RelativeItem().AlignRight().Text(_invoice.InvoiceDate.ToString("yyyy-MM-dd"));
					});
					c.Item().Row(r =>
					{
						r.RelativeItem().Text("Due date:");
						r.RelativeItem().AlignRight().Text(_invoice.DueDate.ToString("yyyy-MM-dd"));
					});
					c.Item().Row(r =>
					{
						r.RelativeItem().Text("Currency:");
						r.RelativeItem().AlignRight().Text(_invoice.Currency);
					});
				});
			});

			column.Item().Element(ComposeLineItemsTable);

			column.Item().AlignRight().Width(250).Column(totals =>
			{
				totals.Item().Row(r =>
				{
					r.RelativeItem().Text("Subtotal");
					r.RelativeItem().AlignRight().Text($"{_invoice.Subtotal:N2} {_invoice.Currency}");
				});
				totals.Item().Row(r =>
				{
					r.RelativeItem().Text($"Tax ({_invoice.TaxRate * 100m:N2}%)");
					r.RelativeItem().AlignRight().Text($"{_invoice.TotalAmount - _invoice.Subtotal:N2} {_invoice.Currency}");
				});
				totals.Item().PaddingTop(5).BorderTop(1).PaddingTop(5).Row(r =>
				{
					r.RelativeItem().Text("Total").Bold();
					r.RelativeItem().AlignRight().Text($"{_invoice.TotalAmount:N2} {_invoice.Currency}").Bold();
				});
			});

			if (!string.IsNullOrWhiteSpace(_invoice.Notes))
			{
				column.Item().Column(c =>
				{
					c.Item().Text("Notes").SemiBold();
					c.Item().Text(_invoice.Notes);
				});
			}
		});
	}

	private void ComposeLineItemsTable(IContainer container)
	{
		container.Table(table =>
		{
			table.ColumnsDefinition(columns =>
			{
				columns.RelativeColumn(4);
				columns.RelativeColumn();
				columns.RelativeColumn();
				columns.RelativeColumn();
			});

			table.Header(header =>
			{
				header.Cell().Text("Item").SemiBold();
				header.Cell().AlignRight().Text("Qty").SemiBold();
				header.Cell().AlignRight().Text("Unit price").SemiBold();
				header.Cell().AlignRight().Text("Total").SemiBold();
				header.Cell().ColumnSpan(4).PaddingTop(5).BorderBottom(1);
			});

			foreach (var item in _invoice.LineItems)
			{
				table.Cell().Text(item.ItemName);
				table.Cell().AlignRight().Text(item.Quantity.ToString("N2"));
				table.Cell().AlignRight().Text(item.UnitPrice.ToString("N2"));
				table.Cell().AlignRight().Text(item.Total.ToString("N2"));
			}
		});
	}
}
