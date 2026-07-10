# ADR 001: Demonstrate the Dual-Write Failure

## Status

Accepted

## Context

The application must save business data in PostgreSQL and schedule audit work after the transaction commits.

These are two independent writes:

1. Commit business data to PostgreSQL.
2. Schedule work through an external job system.

If the process terminates after the database commit but before job scheduling, the business change remains while the audit work is permanently lost.

Scheduling before commit is also unsafe because the job may run even when the database transaction later rolls back.

## Decision

The first implementation will intentionally use the unsafe commit-then-schedule sequence.

An integration test will simulate process failure after the database commit and before audit scheduling.

This establishes the failure before introducing the transactional outbox pattern.

## Consequences

The initial implementation is deliberately unreliable.

It must not be presented as a production solution.

The failure test becomes the baseline proving why the transactional outbox is necessary.
