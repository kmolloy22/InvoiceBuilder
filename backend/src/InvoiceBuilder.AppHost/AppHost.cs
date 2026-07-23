var builder = DistributedApplication.CreateBuilder(args);



builder.AddProject<Projects.InvoiceBuilder_Api>("invoicebuilder-api");



await builder.Build().RunAsync();
