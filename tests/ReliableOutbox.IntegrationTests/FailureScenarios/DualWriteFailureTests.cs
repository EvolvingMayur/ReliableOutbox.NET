using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ReliableOutbox.Infrastructure.Auditing;
using ReliableOutbox.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace ReliableOutbox.IntegrationTests.FailureScenarios;

public sealed class DualWriteFailureTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres =
        new PostgreSqlBuilder()
            .WithImage("postgres:18-alpine")
            .WithDatabase("reliable_outbox_tests")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");

                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<ReliableOutboxDbContext>();
                    services.RemoveAll<
                        DbContextOptions<ReliableOutboxDbContext>>();

                    services.AddDbContext<ReliableOutboxDbContext>(
                        options => options.UseNpgsql(
                            _postgres.GetConnectionString()));
                });
            });

        _client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task
        CommitSucceeds_WhenProcessFailsBeforeAuditScheduling_AuditWorkIsLost()
    {
        var response = await _client.PostAsJsonAsync(
            "/orders?simulateCrash=true",
            new
            {
                customerName = "Dual-write demonstration"
            });

        Assert.Equal(
            HttpStatusCode.InternalServerError,
            response.StatusCode);

        await using var scope =
            _factory.Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider.GetRequiredService<
                ReliableOutboxDbContext>();

        var persistedOrder = await dbContext.Orders
            .AsNoTracking()
            .SingleAsync();

        var auditScheduler =
            scope.ServiceProvider.GetRequiredService<
                InMemoryAuditScheduler>();

        Assert.Equal(
            "Dual-write demonstration",
            persistedOrder.CustomerName);

        Assert.Empty(auditScheduler.ScheduledItems);
    }
}
