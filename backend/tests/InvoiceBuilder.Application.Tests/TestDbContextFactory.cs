using System.Runtime.CompilerServices;
using InvoiceBuilder.Database;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;

namespace InvoiceBuilder.Application.Tests;

internal static class TestDbContextFactory
{
	[ModuleInitializer]
	public static void SetQuestPdfLicense()
	{
		QuestPDF.Settings.License = LicenseType.Community;
	}

	public static InvoiceBuilderContext Create()
	{
		var options = new DbContextOptionsBuilder<InvoiceBuilderContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;

		return new InvoiceBuilderContext(options);
	}
}
