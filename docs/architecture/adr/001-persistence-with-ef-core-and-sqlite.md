# ADR 001 — Persistence with EF Core and SQLite

| Field    | Value                                    |
| -------- | ---------------------------------------- |
| Status   | Accepted                                 |
| Date     | 2026-10-08                               |
| Context  | Spec 001-003 §10 open question 1         |

## Context

Spec 001-003 is the first functional slice and needs durable storage for Trip,
Participant, InviteLink, Visitor and ParticipantBinding. The spec deliberately left the
mechanism open as an architecture decision.

The constraints that actually bear on the choice:

- INV-3 (unique display name per trip) has to hold under two simultaneous joins (EC-1),
  so the store must be able to enforce a unique constraint, not just the application.
- Scale is a handful of participants per trip (NFR-009). Nothing here needs a server.
- The project is a single Razor Pages app with no deployment infrastructure yet.

## Decision

Use **Entity Framework Core with SQLite**. The schema is created at startup with
`EnsureCreated()`; there are no migrations yet.

## Consequences

- The per-trip unique index on `(TripId, ComparisonKey)` enforces INV-3 at the store, and
  the resulting `DbUpdateException` is reported as "that name is taken".
- Integration tests run the real app against an in-memory SQLite connection, so they
  exercise that index rather than mocking around it.
- Single-writer: SQLite serialises writes. Fine at this scale, and the thing to revisit
  first if it stops being fine.
- `EnsureCreated()` cannot evolve an existing database. Switch to EF migrations before
  any deployment whose data has to survive a schema change.
- Moving to PostgreSQL later is a provider swap plus migrations; the domain model and the
  service layer do not change.
