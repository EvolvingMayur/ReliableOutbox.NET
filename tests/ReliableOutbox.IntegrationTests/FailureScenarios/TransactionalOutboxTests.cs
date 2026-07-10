using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ReliableOutbox.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace ReliableOutbox.IntegrationTests.FailureScenarios;

public sealed class TransactionalOutboxTests : IAsyncLifetime
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
        CrashAfterCommit_PreservesOrderAndOutboxMessage()
    {
        var response = await _client.PostAsJsonAsync(
            "/orders/outbox?simulateCrashAfterCommit=true",
            new
            {
                customerName = "Reliable order"
            });

        Assert.Equal(
            HttpStatusCode.InternalServerError,
            response.StatusCode);

        await using var scope =
            _factory.Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider.GetRequiredService<
                ReliableOutboxDbContext>();

        var order = await dbContext.Orders
            .AsNoTracking()
            .SingleAsync();

        var outboxMessage =
            await dbContext.OutboxMessages
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            order.TransactionId,
            outboxMessage.TransactionId);

        Assert.Equal(
            "AuditWorkRequested",
            outboxMessage.MessageType);

        Assert.Contains(
            order.Id.ToString(),
            outboxMessage.Payload);
    }

    [Fact]
    public async Task
        FailureBeforeCommit_RollsBackOrderAndOutboxMessage()
    {
        var response = await _client.PostAsJsonAsync(
            "/orders/outbox?simulateFailureBeforeCommit=true",
            new
            {
                customerName = "Rolled-back order"
            });

        Assert.Equal(
            HttpStatusCode.InternalServerError,
            response.StatusCode);

        await using var scope =
            _factory.Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider.GetRequiredService<
                ReliableOutboxDbContext>();

        Assert.False(
            await dbContext.Orders.AnyAsync());

        Assert.False(
            await dbContext.OutboxMessages.AnyAsync());
    }
}
