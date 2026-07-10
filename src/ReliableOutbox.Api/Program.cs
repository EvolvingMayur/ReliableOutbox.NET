using Microsoft.EntityFrameworkCore;
using ReliableOutbox.Api.Orders;
using ReliableOutbox.Domain.Auditing;
using ReliableOutbox.Infrastructure.Auditing;
using ReliableOutbox.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException(
        "The PostgreSQL connection string is missing.");

builder.Services.AddDbContext<ReliableOutboxDbContext>(
    options => options.UseNpgsql(connectionString));

builder.Services.AddSingleton<InMemoryAuditScheduler>();

builder.Services.AddSingleton<IAuditScheduler>(
    serviceProvider =>
        serviceProvider.GetRequiredService<InMemoryAuditScheduler>());

builder.Services.AddScoped<NaiveOrderService>();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext =
        scope.ServiceProvider
            .GetRequiredService<ReliableOutboxDbContext>();

    await dbContext.Database.EnsureCreatedAsync();
}

app.MapPost(
    "/orders",
    async (
        CreateOrderRequest request,
        bool? simulateCrash,
        NaiveOrderService orderService,
        CancellationToken cancellationToken) =>
    {
        if (string.IsNullOrWhiteSpace(request.CustomerName))
        {
            return Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    [nameof(request.CustomerName)] =
                        ["Customer name is required."]
                });
        }

        try
        {
            var order = await orderService.CreateAsync(
                request.CustomerName,
                simulateCrashAfterCommit: simulateCrash ?? false,
                cancellationToken);

            return Results.Created(
                $"/orders/{order.Id}",
                new
                {
                    order.Id,
                    order.TransactionId,
                    order.CustomerName,
                    order.CreatedOnUtc
                });
        }
        catch (SimulatedProcessCrashException exception)
        {
            return Results.Problem(
                title: "Simulated process failure",
                detail: exception.Message,
                statusCode:
                    StatusCodes.Status500InternalServerError);
        }
    });

app.MapGet(
    "/orders",
    async (
        ReliableOutboxDbContext dbContext,
        CancellationToken cancellationToken) =>
    {
        var orders = await dbContext.Orders
            .AsNoTracking()
            .OrderBy(order => order.CreatedOnUtc)
            .ToListAsync(cancellationToken);

        return Results.Ok(orders);
    });

app.MapGet(
    "/audit-work-items",
    (InMemoryAuditScheduler scheduler) =>
        Results.Ok(scheduler.ScheduledItems));

app.Run();

public partial class Program;
