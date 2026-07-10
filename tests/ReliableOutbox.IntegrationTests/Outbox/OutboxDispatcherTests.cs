using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ReliableOutbox.Infrastructure.Auditing;
using ReliableOutbox.Infrastructure.Outbox;
using ReliableOutbox.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace ReliableOutbox.IntegrationTests.Outbox;

public sealed class OutboxDispatcherTests : IAsyncLifetime
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
        PendingMessage_IsScheduledAndMarkedAsProcessed()
    {
        var response = await _client.PostAsJsonAsync(
            "/orders/outbox",
            new
            {
                customerName = "Dispatched order"
            });

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        await using var scope =
            _factory.Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider.GetRequiredService<
                ReliableOutboxDbContext>();

        var auditScheduler =
            scope.ServiceProvider.GetRequiredService<
                InMemoryAuditScheduler>();

        var dispatcher = new OutboxDispatcher(
            dbContext,
            auditScheduler);

        var dispatchedCount =
            await dispatcher.DispatchPendingAsync(
                batchSize: 10,
                CancellationToken.None);

        var order = await dbContext.Orders
            .AsNoTracking()
            .SingleAsync();

        var outboxMessage =
            await dbContext.OutboxMessages
                .AsNoTracking()
                .SingleAsync();

        var scheduledWorkItem =
            Assert.Single(auditScheduler.ScheduledItems);

        Assert.Equal(1, dispatchedCount);

        Assert.Equal(
            order.Id,
            scheduledWorkItem.OrderId);

        Assert.Equal(
            order.TransactionId,
            scheduledWorkItem.TransactionId);

        Assert.NotNull(outboxMessage.ProcessedOnUtc);
        Assert.Equal(1, outboxMessage.AttemptCount);
        Assert.Null(outboxMessage.LastError);
    }
}
