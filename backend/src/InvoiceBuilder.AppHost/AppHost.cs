var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
	.WithPgAdmin(pgAdmin =>
	{
		pgAdmin.WithHostPort(5050);
		pgAdmin.WithLifetime(ContainerLifetime.Persistent);
	});

var database = postgres.AddDatabase("InvoiceBuilderDB", "invoicebuilder");

builder.AddProject<Projects.InvoiceBuilder_Api>("invoicebuilder-api")
	.WithReference(database)
	.WaitFor(database);


await builder.Build().RunAsync();
