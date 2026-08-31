using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;
using Xunit;

namespace InvoiceBuilder.Api.IntegrationTests;

/// <summary>
/// Boots the real API (<see cref="Program"/>) against a throwaway PostgreSQL
/// container. The API's startup path runs the EF migrations and the data seed
/// against this container, so tests exercise the real schema and pipeline.
/// </summary>
public sealed class CustomerApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public async Task InitializeAsync()
    {
        await _database.StartAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _database.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Non-production path of AddInvoiceBuilderNpgSql + the migrate/seed on startup.
        builder.UseEnvironment("Development");

        // Point the Aspire Npgsql component at the container instead of the
        // connection string the AppHost would normally inject.
        builder.UseSetting("ConnectionStrings:InvoiceBuilderDB", _database.GetConnectionString());
    }
}
