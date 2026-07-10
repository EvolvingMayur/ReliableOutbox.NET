# ReliableOutbox.NET

A .NET case study demonstrating the dual-write problem, transactional outbox pattern, at-least-once delivery, and idempotent message processing.

## Current phase

The first phase intentionally reproduces this failure:

1. Business data commits successfully.
2. The application terminates before audit work is scheduled.
3. The database change survives.
4. The required background work is permanently lost.

The transactional outbox will be introduced only after this failure is demonstrated by an integration test.

## Technology

* .NET 10
* ASP.NET Core
* Entity Framework Core
* PostgreSQL 18
* xUnit
* Testcontainers

## Local PostgreSQL

Start the database:

```bash
docker compose up -d
docker compose ps
```

PostgreSQL is exposed locally on port `5435`.

Development connection string:

```text
Host=localhost;Port=5435;Database=reliable_outbox;Username=postgres;Password=postgres
```
