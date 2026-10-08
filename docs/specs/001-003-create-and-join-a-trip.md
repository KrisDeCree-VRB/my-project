# Feature Specification: Create a Trip and Get Everyone In

<!--
  TEMPLATE INSTRUCTIONS
  =====================
  Fill in each section focusing on WHAT the feature does and WHY — not HOW it should
  be implemented.

  Usage:
  - One spec per feature or functional slice
  - Store in docs/specs/ and version-control alongside your code
  - Use [NEEDS CLARIFICATION: question] markers for unresolved decisions (max 3)
  - Remove optional sections that don't apply — don't leave them as N/A
  - Reference your arc42 architecture docs where relevant rather than duplicating them
-->

## 1. Overview

| Field           | Value                                                        |
| --------------- | ------------------------------------------------------------ |
| Feature ID      | 001-003                                                       |
| Status          | Draft                                                         |
| Author          | Kris De Cree                                                  |
| Created         | 2026-10-08                                                    |
| Last updated    | 2026-10-08 — identity model aligned with spec 004-005         |
| Epic / Parent   | Story map — "Set up the trip and get everyone in" (stories 001, 002, 003) |
| Arc42 reference | Sections 3, 5, 8, 10 — currently unwritten templates; see §9.3 |

### 1.1 Problem Statement

A group of friends planning a weekend away has no shared home for the plan. It lives
in a group chat where decisions scroll away, nobody is sure who booked the house, and
the person who kicked the trip off ends up chasing people. The first barrier to fixing
that is getting everyone into one place: any tool that demands five friends create
accounts before they can look at a date will lose to the group chat.

### 1.2 Goal

One of the friends can create a trip — name, location, start and end dates — in under a
minute, share a single link into the group chat, and have everyone who opens it become a
named participant without creating an account. From that point on there is one canonical
trip with a known set of people attached to it, which every later feature (schedule,
bring list, tasks) builds on.

### 1.3 Non-Goals

- **No user accounts, passwords, or email verification.** Identity is a browser-level
  visitor bound to a participant, recoverable by re-opening the invite link.
- **No roles or permissions beyond the trip starter's control of the invite link.** Every
  participant can see and contribute to the whole plan; no admin/member hierarchy.
- **No trip content** — activities, bring list, tasks, comments and the overview are
  stories 006+ and out of scope here.
- **No attendance status** (in / out / unsure) — story 004, spec 004-005.
- **No cross-trip view.** A Participant is scoped to one Trip, and this feature shows one
  trip at a time. The Visitor that spans trips is defined here, but "the trips I'm part
  of" is story 005, spec 004-005.
- **No invite delivery.** The system produces a link; sending it is the user's job. No
  email or SMS is sent by the system at all.
- **No editing or deletion of a trip after creation** — deliberately deferred, see §10.

## 2. User Stories

### US-001: Create a trip

**As a** trip starter,
**I want** to create a trip with a name, location and start/end dates,
**so that** the group has one place to gather around.

_(Story map 001)_

### US-002: Share an invite link

**As a** trip starter,
**I want** a shareable link I can paste into our group chat,
**so that** friends can join without me setting up accounts for them.

_(Story map 002)_

### US-003: Control the invite link

**As a** trip starter,
**I want** to revoke and regenerate the invite link,
**so that** a link forwarded outside the group stops working.

_(Derived from story map 002)_

### US-004: Join a trip

**As an** invited friend,
**I want** to open the link, see what the trip is, and join under a display name,
**so that** everyone knows who I am in the plan.

_(Story map 003)_

### US-005: Get back into a trip

**As a** participant,
**I want** to return to the trip later — on the same device, or by re-opening the link
elsewhere —
**so that** I don't lose my place or end up as a duplicate person.

_(Story map 003, identity model)_

## 3. Functional Requirements

| ID     | Requirement                                                                                                                                              | Priority | User Story |
| ------ | -------------------------------------------------------------------------------------------------------------------------------------------------------- | -------- | ---------- |
| FR-001 | The system shall let a visitor create a trip by supplying a name, a location and a start and end date.                                                     | Must     | US-001     |
| FR-002 | The system shall require trip name (1–100 chars) and location (1–200 chars), both free text.                                                               | Must     | US-001     |
| FR-003 | The system shall reject a trip whose end date is before its start date, or whose start date is before the current date. No maximum trip length is enforced. | Must     | US-001     |
| FR-004 | The system shall record the creating participant as the trip's starter, permanently and unchangeably.                                                      | Must     | US-001     |
| FR-005 | The system shall require the trip starter to supply their own display name as part of creating the trip, making them the trip's first participant.          | Must     | US-001     |
| FR-006 | The system shall generate an unguessable invite token for the trip at creation time.                                                                       | Must     | US-002     |
| FR-007 | The system shall present the trip starter with the full invite link in a form that can be copied in one action.                                            | Must     | US-002     |
| FR-008 | The system shall make the invite link available to every participant at any time while it is active, not only immediately after creation.                  | Should   | US-002     |
| FR-009 | The system shall allow only the trip starter to revoke the active invite link.                                                                             | Must     | US-003     |
| FR-010 | The system shall allow the trip starter to generate a replacement invite link, which immediately supersedes any previous one.                              | Must     | US-003     |
| FR-011 | The system shall continue to admit already-joined participants after a link is revoked; revocation blocks new joins only.                                  | Must     | US-003     |
| FR-012 | The system shall show a visitor opening an active invite link the trip's name, location, dates and current participants before they commit to joining.     | Must     | US-004     |
| FR-013 | The system shall refuse entry, with an explanatory message and no trip details, when an invite link is unknown or revoked.                                 | Must     | US-004     |
| FR-014 | The system shall create a participant and bind the requesting visitor to it when that visitor joins with a display name.                                   | Must     | US-004     |
| FR-015 | The system shall require display names to be 1–50 chars and unique within a trip, compared case-insensitively and ignoring surrounding whitespace.         | Must     | US-004     |
| FR-016 | The system shall take a visitor that already holds a binding to a participant of that trip straight into the trip, without re-asking for a name.           | Must     | US-005     |
| FR-017 | The system shall offer a visitor opening an active invite link with no binding for that trip the choice between joining as someone new or resuming as an existing participant. | Should | US-005 |
| FR-018 | The system shall let a participant change their own display name, subject to FR-015.                                                                       | Could    | US-005     |
| FR-019 | The system shall establish a visitor identity for any browser that creates or joins a trip, and persist it across sessions on that browser.                | Must     | US-005     |
| FR-020 | The system shall bind a visitor resuming an existing participant to that same participant, creating no second participant.                                 | Must     | US-005     |
| FR-021 | The system shall permit a participant to be bound from more than one visitor, so the same person can use a phone and a laptop.                             | Must     | US-005     |
| FR-022 | The system shall hold at most one binding per visitor per trip.                                                                                            | Must     | US-005     |

## 4. Acceptance Scenarios

### SC-001: Create a trip (FR-001, FR-004, FR-005, FR-006, FR-019)

```gherkin
Given I am a visitor with no trip
When I create a trip named "Ardennes weekend" in "Durbuy" from 2026-11-13 to 2026-11-15
  And I give my display name as "Kris"
Then the trip exists with those details
  And I am its only participant, named "Kris"
  And I am recorded as the trip starter
  And this browser is bound to the participant "Kris"
  And an invite link for the trip is shown to me
```

### SC-002: End date before start date is rejected (FR-003)

```gherkin
Given I am creating a trip
When I give a start date of 2026-11-15 and an end date of 2026-11-13
Then the trip is not created
  And I am told the end date cannot be before the start date
  And the details I already entered are still in the form
```

### SC-003: Start date in the past is rejected (FR-003)

```gherkin
Given today is 2026-10-08
When I try to create a trip starting 2026-10-07
Then the trip is not created
  And I am told a trip cannot start in the past
```

### SC-004: Copy the invite link (FR-007, FR-008)

```gherkin
Given I am a participant of a trip with an active invite link
When I open the trip
Then I can see the full invite link
  And I can copy it in a single action
```

### SC-005: Join via an invite link (FR-012, FR-014, FR-015)

```gherkin
Given a trip "Ardennes weekend" in "Durbuy" exists with participant "Kris"
  And the trip has an active invite link
When I open that link in a browser holding no binding for this trip
Then I see the trip's name, location, dates and that Kris is coming
  And I am not yet a participant
When I join with the display name "Sam"
Then I am a participant of the trip named "Sam"
  And this browser is bound to the participant "Sam"
  And Kris sees Sam in the participant list
```

### SC-006: Duplicate display name is refused (FR-015)

```gherkin
Given a trip has a participant named "Sam"
When I open the invite link and try to join as "  sam  "
Then I do not become a participant
  And I am told that name is already taken in this trip
  And I am invited to choose a different name
```

### SC-007: Return to a trip on the same device (FR-016)

```gherkin
Given I joined a trip as "Sam" in this browser
When I open the trip again, by link or directly
Then I am taken straight into the trip as "Sam"
  And I am not asked for a display name
```

### SC-008: Resume as an existing participant from a new device (FR-017, FR-020, FR-021)

```gherkin
Given a trip has participants "Kris" and "Sam"
  And the trip has an active invite link
When I open that link on a device holding no binding for this trip
Then I am offered both joining as someone new and resuming as an existing participant
When I resume as "Sam"
Then this device is bound to the existing participant "Sam"
  And the participant "Sam" is bound from both devices
  And no second "Sam" is created
```

### SC-009: Revoke the invite link (FR-009, FR-010, FR-011)

```gherkin
Given I am the trip starter of a trip with an active invite link
  And "Sam" has already joined
When I revoke the invite link
Then the old link no longer admits anyone
  And Sam still has full access to the trip
When I generate a replacement link
Then the replacement admits new participants
  And the revoked link remains dead
```

### SC-010: A revoked link reveals nothing (FR-013)

```gherkin
Given an invite link for a trip has been revoked
When I open that link
Then I am told the invite is no longer valid
  And I am shown no trip name, location, dates or participants
```

### SC-011: Unknown link (FR-013)

```gherkin
Given no trip has the invite token "abc123"
When I open the invite link containing "abc123"
Then I am told the invite is not valid
  And the response is indistinguishable from that of a revoked link
```

_The final assertion is deliberate: if a dead link said "revoked" and an unknown one said
"no such trip", the link becomes a probe for which trips exist._

### SC-012: Only the trip starter controls the link (FR-009)

```gherkin
Given I am a participant of a trip but not its starter
When I open the trip
Then I can see and copy the invite link
  And no option to revoke or regenerate it is available to me
```

### SC-013: One identity per trip per device (FR-022)

```gherkin
Given this browser is bound to the participant "Sam" of a trip
When I open that trip's invite link again in this browser
Then I am taken into the trip as "Sam"
  And I am not offered the option to join as someone new
  And this browser still holds exactly one binding for this trip
```

## 5. Domain Model

_See [docs/domainmodel.md](../domainmodel.md) for the authoritative cross-feature view.
This feature establishes Trip, Participant, InviteLink, Visitor and ParticipantBinding.
Attendance status on Participant is added later by spec 004-005 and is not part of this
feature._

### 5.1 Entities

#### Trip

One weekend away. The aggregate that every later feature hangs off.

| Attribute            | Type     | Constraints                        | Description                                 |
| -------------------- | -------- | ---------------------------------- | ------------------------------------------- |
| id                   | UUID     | PK, generated                      |                                             |
| name                 | string   | required, 1–100 chars              | Free text, e.g. "Ardennes weekend"          |
| location             | string   | required, 1–200 chars              | Free text; not a structured address         |
| startDate            | date     | required, ≥ creation date          | First day of the trip                       |
| endDate              | date     | required, ≥ startDate              | Last day of the trip                        |
| starterParticipantId | UUID     | required, FK → Participant, immutable | The participant who created the trip     |
| createdAt            | datetime | generated, immutable               |                                             |

#### Participant

A person in one trip. No account; scoped to its trip.

| Attribute   | Type     | Constraints                                            | Description                     |
| ----------- | -------- | ------------------------------------------------------ | ------------------------------- |
| id          | UUID     | PK, generated                                          |                                 |
| tripId      | UUID     | required, FK → Trip, immutable                         |                                 |
| displayName | string   | required, 1–50 chars, unique per trip (trimmed, case-insensitive) | How the group sees them |
| joinedAt    | datetime | generated, immutable                                   |                                 |

#### InviteLink

A shareable credential admitting new participants to one trip.

| Attribute | Type     | Constraints                                     | Description                        |
| --------- | -------- | ----------------------------------------------- | ---------------------------------- |
| id        | UUID     | PK, generated                                   |                                    |
| tripId    | UUID     | required, FK → Trip, immutable                  |                                    |
| token     | string   | required, globally unique, ≥ 128 bits entropy, URL-safe | The secret in the link     |
| status    | enum     | [active, revoked]                               |                                    |
| createdAt | datetime | generated, immutable                            |                                    |
| revokedAt | datetime | set if and only if status = revoked             |                                    |

#### Visitor

One browser that has used the system — the device-level identity. This is how identity
works without accounts: the visitor is what the cookie names, and it holds the bindings
that say who this browser is in each trip.

| Attribute    | Type     | Constraints                          | Description                      |
| ------------ | -------- | ------------------------------------ | -------------------------------- |
| id           | UUID     | PK, generated                        |                                  |
| sessionToken | string   | required, unique, ≥ 128 bits entropy | Stored in an HttpOnly cookie     |
| createdAt    | datetime | generated, immutable                 |                                  |
| lastSeenAt   | datetime | updated on access                    | Drives inactivity expiry         |

#### ParticipantBinding

Links one visitor to one participant. A visitor holds at most one binding per trip; a
participant may be bound from several visitors — phone and laptop.

| Attribute     | Type     | Constraints                | Description                             |
| ------------- | -------- | -------------------------- | --------------------------------------- |
| id            | UUID     | PK, generated              |                                         |
| visitorId     | UUID     | required, FK → Visitor     |                                         |
| participantId | UUID     | required, FK → Participant |                                         |
| boundAt       | datetime | generated, immutable       |                                         |

### 5.2 Relationships

- A **Trip** has many **Participants**; a **Participant** belongs to exactly one **Trip**.
- A **Trip** has exactly one **starter**, which is one of its own Participants.
- A **Trip** has many **InviteLinks** over time, but at most one with status `active`.
- A **Visitor** holds many **ParticipantBindings**; each binding identifies exactly one
  **Participant**.
- A **Participant** may be bound from many **Visitors** (one per device).
- A **Visitor** holds at most one **ParticipantBinding** per **Trip**.

### 5.3 Value Objects

_None in this feature. Dates are plain dates; the trip's date range is expressed as two
attributes rather than a range object until a second feature needs it._

### 5.4 Domain Rules and Invariants

- **INV-1 — Trip dates are ordered**: `endDate ≥ startDate`.
- **INV-2 — Trips start in the future**: at creation, `startDate ≥` today. Not re-checked
  afterwards; a trip already under way stays valid.
- **INV-3 — Unique names within a trip**: no two Participants of the same Trip share a
  displayName, compared trimmed and case-insensitively. Across trips, names repeat freely.
- **INV-4 — A trip always has a starter**: `starterParticipantId` is set at creation and
  never changes.
- **INV-5 — The starter is a participant of their own trip**: the referenced Participant's
  `tripId` equals the Trip's id.
- **INV-6 — At most one active link**: a Trip has zero or one InviteLink with status
  `active`; generating a replacement revokes the incumbent in the same step.
- **INV-7 — Revocation is terminal**: a revoked InviteLink never returns to active.
- **INV-8 — Membership outlives the link**: revoking or replacing an InviteLink never
  removes or blocks an existing Participant.
- **INV-9 — The link is the only credential**: possession of an active invite token is
  sufficient to join as a new participant *or* resume an existing one. This feature
  defines no stronger proof of identity.
- **INV-14 — One identity per trip per device**: a Visitor holds at most one
  ParticipantBinding whose Participant belongs to a given Trip. _(Numbered to match the
  global model; introduced alongside the Visitor, enforced from this feature onward.)_

## 6. Non-Functional Requirements

| ID      | Category    | Requirement                                                                                                                                                                                   |
| ------- | ----------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| NFR-001 | Performance | Opening an invite link renders the trip preview in < 500ms at p95 on a mobile connection; trip creation completes in < 1s at p95.                                                              |
| NFR-002 | Security    | Invite tokens carry ≥ 128 bits of entropy from a cryptographically secure source and are URL-safe.                                                                                             |
| NFR-003 | Security    | Visitor session tokens are ≥ 128 bits of entropy, stored in an `HttpOnly`, `Secure`, `SameSite=Lax` cookie, and never exposed in a URL.                                                        |
| NFR-004 | Security    | Invite-link lookups are rate-limited per client (20 attempts/minute) so the token space cannot be probed in bulk.                                                                              |
| NFR-005 | Security    | **Accepted risk**: possession of an active invite link is the sole credential (INV-9). Anyone holding it can join as a new participant or resume as an existing one, including impersonating a named friend. The mitigation is the starter's ability to revoke (FR-009). |
| NFR-006 | Privacy     | A trip's name, location, dates and participant names are disclosed only to holders of an active invite link or to visitors bound to one of its participants — never to unauthenticated visitors or search engines (`noindex` on invite pages). |
| NFR-007 | Reliability | Visitor identities survive at least 90 days of inactivity before expiring, so a trip planned two months out does not silently lose people.                                                     |
| NFR-008 | Usability   | The create-trip and join flows are each a single screen, usable one-handed on a 360px-wide viewport.                                                                                           |
| NFR-009 | Scale       | Designed for ≤ 50 participants per trip; no pagination of the participant list is required below that.                                                                                         |

## 7. Edge Cases and Error Scenarios

| ID    | Scenario                                                                          | Expected Behavior                                                                                                 |
| ----- | --------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------- |
| EC-1  | Two people submit the same display name simultaneously                            | One wins; the other is told the name is taken and re-prompted. Uniqueness enforced at the store, not only in the form (INV-3). |
| EC-2  | Starter clicks "regenerate" twice in quick succession                             | Exactly one active link survives (INV-6); the earlier one is revoked.                                              |
| EC-3  | Invite link opened by a visitor already bound within the **same** trip             | Straight in as the existing identity (FR-016); no prompt, no second participant, no second binding (INV-14).       |
| EC-4  | Invite link opened by a visitor bound within **another** trip only                 | Treated as a fresh arrival for this trip; the other binding is untouched. The visitor identity itself is reused, not replaced. |
| EC-5  | Browser has cookies disabled                                                       | Join is refused with a plain explanation that the trip needs cookies to remember who you are — no silent failure that looks like a successful join. |
| EC-6  | Participant clears cookies, re-opens the link, resumes as themselves               | Works (FR-017, FR-020); a new visitor is created and bound to the same participant. The old visitor is orphaned and expires. |
| EC-7  | Session cookie names a visitor that no longer exists                               | Treated as a fresh browser: a new visitor is established and the invite flow runs again, rather than showing an error page. |
| EC-8  | The trip's dates pass while the trip is live                                       | Trip remains fully accessible and joinable (INV-2 is creation-time only).                                          |
| EC-9  | Display name entered as whitespace only, or with leading/trailing spaces           | Trimmed first; whitespace-only is rejected as empty (FR-015).                                                      |
| EC-10 | Display name containing emoji or non-Latin script                                  | Accepted; the 50-char limit counts user-perceived characters, not bytes.                                            |
| EC-11 | Starter revokes the link and never generates a new one                             | Trip continues to work for everyone already in; no new joins possible. A valid end state, not an error.             |
| EC-12 | Invite link opened by a crawler or chat-app link preview                           | No trip details in the preview — generic title only, so a chat unfurl does not leak the location to a wider chat. A visitor identity is not established for a request that does not join. |
| EC-13 | A visitor's binding points at a participant that no longer exists                  | The binding is ignored and the visitor treated as unbound for that trip.                                           |

## 8. Success Criteria

| ID     | Criterion                                                                                                        |
| ------ | ------------------------------------------------------------------------------------------------------------------- |
| SUC-01 | All acceptance scenarios SC-001 … SC-013 pass in CI.                                                             |
| SUC-02 | A trip starter can go from a blank page to a copyable invite link in a single screen and under 60 seconds.        |
| SUC-03 | An invited friend can go from opening the link to being a named participant in under 15 seconds, with no account. |
| SUC-04 | A participant returning after 30 days on the same device lands in the trip without re-identifying themselves.     |
| SUC-05 | A revoked link admits nobody and discloses nothing, verified by SC-010 and SC-011.                                |
| SUC-06 | Invariants INV-1 … INV-9 and INV-14 each have at least one automated test.                                        |
| SUC-07 | The same person using two devices appears exactly once in a trip's participant list (SC-008).                     |

## 9. Dependencies and Constraints

### 9.1 Dependencies

- None on other features — this is the first functional slice and every later story
  (004 onwards) depends on it.
- Requires persistent storage, which the project does not yet have. Choosing it is an
  architecture decision, not a spec decision — see §10.
- Requires HTTPS in all environments where a real invite link is shared (NFR-003).

### 9.2 Constraints

- ASP.NET Core Razor Pages on .NET 10, per [CLAUDE.md](../../CLAUDE.md); no SPA framework.
- No email or SMS infrastructure exists, and this feature introduces none (§1.3).
- Identity must work without accounts — a product constraint inherited from story map
  story 002, not a technical shortcut.

### 9.3 Architecture References

The arc42 documents are currently unfilled templates. This feature is the first to
constrain them, so these rows record what it **should** write into them rather than what
it reads from them.

| Arc42 Section                     | Relevance to This Feature                                                                     |
| --------------------------------- | --------------------------------------------------------------------------------------------- |
| 3. Context & Scope                | Establishes the only external actors: trip starter and invited friend, both unauthenticated, each reaching the system through a browser-level visitor. |
| 5. Building Block View            | Introduces the first domain components: Trip, Participant, InviteLink, Visitor, ParticipantBinding. |
| 8. Crosscutting Concepts          | Visitor-cookie identity, token generation, the authorisation rule "a visitor may act only through its own bindings", and the validation/error-display pattern all start here. |
| 9. Architecture Decisions (ADRs)  | Needs ADRs for the persistence choice and for link-as-credential identity (NFR-005).           |
| 10. Quality Requirements          | NFR-006 (privacy of trip details) and NFR-008 (mobile-first) are project-wide, not feature-local — promote them. |
| 12. Glossary                      | Trip, Participant, Trip starter, Invite link, Display name, Visitor, Binding enter the ubiquitous language here. |

## 10. Open Questions

| #   | Question                                                                                     | Owner | Status | Resolution                                               |
| --- | ---------------------------------------------------------------------------------------------- | ----- | ------ | -------------------------------------------------------- |
| 1   | Which persistence mechanism backs the domain model?                                          | Kris  | Open   | Architecture decision; record as an ADR in arc42 §9.      |
| 2   | Can a trip's name, location or dates be edited after creation, and by whom?                  | Kris  | Open   | Out of scope here (§1.3); needs its own story.            |
| 3   | Can a participant leave a trip, or be removed by the starter?                                | Kris  | Open   | Not covered by stories 001–003; also open in spec 004-005 §10 #1. |

---

<!--
  CHECKLIST
  =========
  - [x] Problem statement is clear and concise
  - [x] All user stories have acceptance scenarios
  - [x] Each functional requirement traces to a user story
  - [x] Domain model covers all entities mentioned in the requirements
  - [x] Domain rules and invariants are listed
  - [x] Edge cases cover failure modes, not just happy paths
  - [x] Non-functional requirements are specific and measurable
  - [~] Arc42 references point to the right sections — sections exist but are unwritten
  - [x] No more than 3 [NEEDS CLARIFICATION] markers remain (there are none)
  - [x] Open questions are assigned and have a resolution path
  - [x] Domain model agrees with docs/domainmodel.md
-->
