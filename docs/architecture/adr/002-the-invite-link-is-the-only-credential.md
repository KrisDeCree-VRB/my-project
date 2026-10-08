# ADR 002 — The invite link is the only credential

| Field    | Value                                       |
| -------- | ------------------------------------------- |
| Status   | Accepted                                    |
| Date     | 2026-10-08                                  |
| Context  | Spec 001-003 INV-9, NFR-005; story map 002  |

## Context

The product constraint is that five friends must be able to join from a group chat
without creating accounts. Any proof of identity stronger than "you have the link" means
either accounts or an email/SMS channel, and the project has neither.

## Decision

Possession of an active invite token is sufficient to join a trip as a new participant
**or** to resume as an existing one. Browser-level identity is a `Visitor` named by an
`HttpOnly; Secure; SameSite=Lax` cookie, which holds one `ParticipantBinding` per trip.

## Consequences

- **Accepted risk**: anyone holding a live link can impersonate a named participant. The
  mitigation is the trip starter's ability to revoke and replace the link (FR-009,
  FR-010). This is recorded as NFR-005, not hidden.
- Tokens carry 256 bits from `RandomNumberGenerator`, and invite lookups are rate-limited
  to 20/minute per client so the token space cannot be probed (NFR-002, NFR-004).
- An unknown token and a revoked one must give byte-identical answers, or the link becomes
  a probe for which trips exist (SC-010, SC-011).
- Trip pages carry `noindex` and leak nothing to a visitor without a binding (NFR-006).
- Identity is only as durable as the cookie. Clearing cookies is survivable — the person
  re-opens the link and resumes as themselves (EC-6) — but only while a link is active.
- This decision is worth revisiting the moment a feature needs to distinguish "the group"
  from "whoever has the URL" — payments, for instance.
