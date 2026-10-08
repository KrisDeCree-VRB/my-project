# Feature Specification: Who's Coming, and My Trips

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
| Feature ID      | 004-005                                                       |
| Status          | Draft                                                         |
| Author          | Kris De Cree                                                  |
| Created         | 2026-10-08                                                    |
| Last updated    | 2026-10-08 — identity model folded back into spec 001-003     |
| Epic / Parent   | Story map — "Set up the trip and get everyone in" (stories 004, 005) |
| Arc42 reference | Sections 3, 5, 8, 9, 10 — see §9.3                            |

### 1.1 Problem Statement

A trip now has a list of people attached to it, but being listed is not the same as
coming. The group still can't answer the two questions that decide whether anything else
gets booked: how many of us are actually in, and is Sam ever going to say? Separately,
once a person is in more than one trip, each trip is a separate link they have to find
again in a chat — the planning tool reintroduces exactly the scrolling-back problem it
was built to remove.

### 1.2 Goal

Every participant can say whether they are in, out or unsure, and anyone can read the
headcount at a glance, including who hasn't answered. And any device that has joined
trips can see all of them in one list, ordered so the next weekend away is at the top,
without needing the original invite links.

### 1.3 Non-Goals

- **No nudging.** The system does not chase, remind or notify participants who haven't
  answered. It makes silence visible; acting on it stays a human job.
- **No deadline or lock on attendance.** A participant can change their answer at any
  time, including during the trip.
- **No capacity, quota or waiting list.** "In" is a statement of intent, not a booking
  against a number of beds.
- **No reason, note or plus-one attached to an answer.** Discussion belongs in comments
  (story 022).
- **No cross-device account.** The trips list is per-browser, built from that visitor's
  bindings — it is not a login (§1.3 of spec 001-003 still holds on accounts).
- **No leaving or removing a participant** — still unspecified; see §10.
- **No trip archiving or hiding from the list** — see §10.

## 2. User Stories

### US-001: Say whether I'm coming

**As a** participant,
**I want** to mark myself as in, out or unsure,
**so that** the group knows whether to count me.

_(Story map 004)_

### US-002: See who's coming

**As a** participant,
**I want** to see every participant's answer and the resulting headcount,
**so that** the group knows where it stands before booking anything.

_(Story map 004)_

### US-003: See the trips I'm part of

**As a** participant,
**I want** to see all the trips this device has joined,
**so that** I can jump back into the right one without hunting for a link.

_(Story map 005)_

### US-004: Find the right trip quickly

**As a** participant with several trips,
**I want** the next trip first and finished trips out of the way,
**so that** the list stays useful as trips accumulate.

_(Story map 005)_

## 3. Functional Requirements

| ID     | Requirement                                                                                                                                                   | Priority | User Story |
| ------ | --------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------- | ---------- |
| FR-001 | The system shall give every participant an attendance status of exactly one of: in, out, unsure.                                                               | Must     | US-001     |
| FR-002 | The system shall set a participant's attendance status to `unsure` when they join, including the trip starter.                                                  | Must     | US-001     |
| FR-003 | The system shall let a participant change their own attendance status at any time, with no limit on how often.                                                  | Must     | US-001     |
| FR-004 | The system shall prevent any participant from changing another participant's attendance status, including the trip starter.                                     | Must     | US-001     |
| FR-005 | The system shall record when a participant last changed their attendance status.                                                                                | Should   | US-001     |
| FR-006 | The system shall show, for every participant of a trip, their display name and current attendance status.                                                       | Must     | US-002     |
| FR-007 | The system shall show a headcount summarising how many participants are in, out and unsure.                                                                     | Must     | US-002     |
| FR-008 | The system shall make unanswered participants (`unsure`) visually distinguishable from those who are out, rather than merging them into a single "not coming".   | Must     | US-002     |
| FR-009 | The system shall reflect a participant's changed status to other participants on their next view of the trip, without requiring them to re-open the invite link. | Must     | US-002     |
| FR-010 | The system shall list every trip the current device is bound to.                                                                                                | Must     | US-003     |
| FR-011 | The system shall show, for each trip in the list, its name, location, date range, the device's own attendance status for it, and the number of participants who are in. | Must | US-003 |
| FR-012 | The system shall show a countdown in whole days to the start of each upcoming trip.                                                                             | Should   | US-003     |
| FR-013 | The system shall let a participant open any trip in the list directly, without an invite link.                                                                  | Must     | US-003     |
| FR-014 | The system shall show a device with no trips an empty state offering trip creation.                                                                             | Must     | US-003     |
| FR-015 | The system shall order upcoming trips by soonest start date first, and present trips whose end date has passed separately, most recent first.                   | Must     | US-004     |
| FR-016 | The system shall treat a trip as current, and keep it above past trips, from its start date up to and including its end date.                                   | Must     | US-004     |
| FR-017 | The system shall add a trip to a device's list when that device joins or resumes a participant, and the list shall survive across sessions on that device.      | Must     | US-003     |
| FR-018 | The system shall keep each device's trip list private to that device, disclosing it to no one else.                                                             | Must     | US-003     |

## 4. Acceptance Scenarios

### SC-001: Everyone starts as unsure (FR-002)

```gherkin
Given a trip "Ardennes weekend" exists
When I join the trip as "Sam"
Then my attendance status is "unsure"
  And the headcount shows one more participant as unsure
```

### SC-002: Mark myself as in (FR-003, FR-005, FR-009)

```gherkin
Given I am a participant of a trip with status "unsure"
When I mark myself as "in"
Then my status is "in"
  And the time of the change is recorded
  And other participants see me as "in" when they next open the trip
```

### SC-003: Change my mind (FR-003)

```gherkin
Given I am a participant of a trip with status "in"
When I mark myself as "out"
Then my status is "out"
  And the headcount counts me as out, not in
When I mark myself as "in" again
Then my status is "in"
  And no limit prevents the change
```

### SC-004: The headcount (FR-006, FR-007, FR-008)

```gherkin
Given a trip has participants Kris (in), Sam (in), Alex (out) and Robin (unsure)
When I open the trip
Then I see all four participants with their statuses
  And the headcount reads 2 in, 1 out, 1 unsure
  And Robin is shown as not having answered, distinctly from Alex being out
```

### SC-005: I cannot answer for someone else (FR-004)

```gherkin
Given I am the trip starter
  And "Sam" is a participant with status "unsure"
When I view the participant list
Then no control is offered to change Sam's status
  And any attempt to set Sam's status is refused
```

### SC-006: See my trips (FR-010, FR-011, FR-013)

```gherkin
Given this device has joined "Ardennes weekend" as "Kris" and "Ski trip" as "Kris"
When I open my trips
Then I see both trips with their names, locations, date ranges, my own status and the number of people in
When I select "Ski trip"
Then I open that trip as "Kris" without using an invite link
```

### SC-007: Ordering (FR-012, FR-015, FR-016)

```gherkin
Given today is 2026-10-08
  And this device is part of "Ski trip" (2027-01-15 to 2027-01-17),
      "Ardennes weekend" (2026-11-13 to 2026-11-15)
      and "Summer house" (2026-07-10 to 2026-07-12)
When I open my trips
Then "Ardennes weekend" is listed first with a countdown of 36 days
  And "Ski trip" is listed second
  And "Summer house" is presented separately as a past trip
```

### SC-008: A trip under way stays at the top (FR-016)

```gherkin
Given today is 2026-11-14
  And a trip runs from 2026-11-13 to 2026-11-15
When I open my trips
Then that trip is listed among the current and upcoming trips, not the past ones
  And it shows that it is under way rather than a countdown
```

### SC-009: Empty state (FR-014)

```gherkin
Given this device is not part of any trip
When I open my trips
Then I am told I have no trips yet
  And I am offered the option to create one
  And no other person's trip is revealed
```

### SC-010: Resuming on a second device (FR-017)

```gherkin
Given I am "Kris" in "Ardennes weekend" on my laptop
  And my phone is not yet bound to that trip
When I open the invite link on my phone and resume as "Kris"
Then "Ardennes weekend" appears in my phone's trips list
  And my attendance status is the same on both devices
  And no second participant named "Kris" exists
```

### SC-011: The trips list is private to the device (FR-018)

```gherkin
Given this device is part of "Ardennes weekend" and "Ski trip"
When another participant of "Ardennes weekend" views that trip
Then they see my display name and attendance status for this trip
  And they learn nothing about any other trip this device is part of
```

## 5. Domain Model

_See [docs/domainmodel.md](../domainmodel.md) for the authoritative cross-feature view.
This feature **adds** attendance to Participant. Visitor and ParticipantBinding already
exist — spec 001-003 defines them — and this feature is the first to read across them,
which is all story 005 needs. See 5.5._

### 5.1 Entities

#### Participant _(existing — changed)_

| Attribute              | Type     | Constraints                                            | Description                        |
| ---------------------- | -------- | ------------------------------------------------------ | ---------------------------------- |
| id                     | UUID     | PK, generated                                          | _(existing)_                       |
| tripId                 | UUID     | required, FK → Trip, immutable                         | _(existing)_                       |
| displayName            | string   | required, 1–50 chars, unique per trip                  | _(existing)_                       |
| joinedAt               | datetime | generated, immutable                                   | _(existing)_                       |
| **attendanceStatus**   | enum     | required, [in, out, unsure], defaults to `unsure`      | **new** — the answer to "coming?"  |
| **statusChangedAt**    | datetime | null until first change, then updated on every change  | **new**                            |

#### Visitor _(existing — unchanged)_

One browser that has used the system. Defined in spec 001-003; repeated here because the
trips list is derived from it.

| Attribute    | Type     | Constraints                           | Description                        |
| ------------ | -------- | ------------------------------------- | ---------------------------------- |
| id           | UUID     | PK, generated                         |                                    |
| sessionToken | string   | required, unique, ≥ 128 bits entropy  | Stored in an HttpOnly cookie       |
| createdAt    | datetime | generated, immutable                  |                                    |
| lastSeenAt   | datetime | updated on access                     | Drives inactivity expiry           |

#### ParticipantBinding _(existing — unchanged)_

Links one visitor to one participant. Defined in spec 001-003. A visitor holds at most one
binding per trip; a participant may be bound from several visitors (phone and laptop).
This feature traverses these bindings in the other direction — from a visitor to all of
its trips.

| Attribute     | Type     | Constraints                                   | Description                         |
| ------------- | -------- | --------------------------------------------- | ----------------------------------- |
| id            | UUID     | PK, generated                                 |                                     |
| visitorId     | UUID     | required, FK → Visitor                        |                                     |
| participantId | UUID     | required, FK → Participant                    |                                     |
| boundAt       | datetime | generated, immutable                          |                                     |

_Trip and InviteLink are unchanged by this feature._

### 5.2 Relationships

- A **Participant** has exactly one **attendance status** at any moment.
- A **Visitor** has many **ParticipantBindings**; each binding points at exactly one
  **Participant**.
- A **Participant** may be bound from many **Visitors**.
- A **Visitor** has at most one **ParticipantBinding** per **Trip** — this is what makes
  "my trips" a list of distinct trips rather than a list of identities.
- A **Visitor's** trips are derived: visitor → bindings → participants → trips. There is
  no direct Visitor-to-Trip relationship to keep in step.

### 5.3 Value Objects

#### Headcount

A derived, read-only summary of a trip's participants. Never stored; always computed.

| Attribute | Type    | Constraints              |
| --------- | ------- | ------------------------ |
| in        | integer | ≥ 0                      |
| out       | integer | ≥ 0                      |
| unsure    | integer | ≥ 0                      |

### 5.4 Domain Rules and Invariants

- **INV-10 — Everyone has an answer**: every Participant has exactly one attendance
  status; there is no null or missing state. Not having answered is represented by
  `unsure`, which is a real answer, not an absence.
- **INV-11 — Attendance is self-declared**: a participant's attendance status may be
  changed only through a binding to that same participant. No participant, including the
  trip starter, can set another's.
- **INV-12 — Attendance is never final**: no state, date or action makes a participant's
  status unchangeable.
- **INV-13 — The headcount is total**: `in + out + unsure` always equals the trip's
  participant count.
- **INV-14 — One identity per trip per device** _(existing, from spec 001-003)_: a Visitor
  holds at most one ParticipantBinding whose Participant belongs to a given Trip. Restated
  here because it is what makes "my trips" a list of distinct trips.
- **INV-15 — A trips list is private**: a Visitor's set of bindings is visible only to
  that Visitor. Within a trip, a participant's display name and attendance status are
  visible to co-participants; the existence of their other trips is not.

### 5.5 Changes to the Global Domain Model

| Change | Nature |
| ------ | ------ |
| **Participant gains `attendanceStatus` and `statusChangedAt`.** | Pure addition. INV-3 (unique display names) and every other rule of spec 001-003 are untouched. |
| **Headcount** is introduced as the model's first value object. | Pure addition; derived, never stored. |
| **INV-10 … INV-13 and INV-15** are added. | Pure addition. No existing invariant is weakened or reworded. |

Nothing existing is replaced. The Visitor + ParticipantBinding model this feature relies
on was originally drafted here and has since been folded back into spec 001-003, which is
where it belongs: a device identity is part of joining a trip, not part of listing them.
Spec 001-003 was refactored accordingly, so the two specs now describe one model rather
than a model and its correction.

The alternative considered was to give each participant a single session token and derive
"my trips" by scanning sessions, with no Visitor entity. Rejected: the device identity
stays implicit and unnamed, INV-14 has nowhere to live, and one person using a phone and a
laptop becomes two participants in the same trip.

## 6. Non-Functional Requirements

| ID      | Category    | Requirement                                                                                                                                             |
| ------- | ----------- | --------------------------------------------------------------------------------------------------------------------------------------------------------- |
| NFR-001 | Performance | Changing an attendance status is acknowledged in < 300ms at p95; the trips list renders in < 500ms at p95 on a mobile connection.                        |
| NFR-002 | Performance | The headcount is computed per request without a stored counter, and must remain within NFR-001 at the 50-participant ceiling (spec 001-003 NFR-009).     |
| NFR-003 | Security    | Attendance changes are authorised solely by the requesting visitor's binding to that participant (INV-11); the participant id in a request is never trusted on its own. |
| NFR-004 | Privacy     | A visitor's trips list is derived only from that visitor's own bindings and is never addressable by another visitor (INV-15).                            |
| NFR-005 | Reliability | Concurrent status changes for the same participant resolve to the last one received; no change is silently dropped without the later value winning.      |
| NFR-006 | Usability   | Attendance status is changeable in one action from the trip view, without a separate page or confirmation step.                                          |
| NFR-007 | Usability   | Attendance states are distinguishable without relying on colour alone, so "unsure" is not lost to a colour-blind reader (FR-008).                        |
| NFR-008 | Scale       | The trips list is designed for ≤ 20 trips per device without pagination.                                                                                 |

## 7. Edge Cases and Error Scenarios

| ID    | Scenario                                                                 | Expected Behavior                                                                                                        |
| ----- | ------------------------------------------------------------------------ | -------------------------------------------------------------------------------------------------------------------------- |
| EC-1  | Same participant changes status from phone and laptop simultaneously      | Last write wins (NFR-005); both devices show the winning value on next view.                                              |
| EC-2  | Status is set to its current value                                        | Accepted as a no-op, but `statusChangedAt` is not advanced — re-affirming is not answering anew.                           |
| EC-3  | A request supplies an attendance value outside [in, out, unsure]           | Rejected; the participant's status is unchanged.                                                                          |
| EC-4  | A visitor requests a status change for a participant it is not bound to    | Refused, and the response does not reveal whether that participant exists (NFR-003).                                      |
| EC-5  | Trip has exactly one participant (just created)                            | Headcount reads 0 in, 0 out, 1 unsure — the starter is counted like anyone else (FR-002).                                 |
| EC-6  | Every participant is out                                                  | Shown plainly as 0 in; the system does not cancel, warn about or editorialise on the trip.                                |
| EC-7  | Visitor's cookie is cleared                                               | The trips list is empty; trips are recovered only by re-opening an invite link and resuming (spec 001-003 FR-017).        |
| EC-8  | Invite link for a listed trip has since been revoked                      | The trip stays in the device's list and remains fully accessible — revocation blocks new joins only (INV-8).              |
| EC-9  | Device is bound to a trip whose start date has passed                     | Listed under past trips, still openable and still allows status changes (INV-12).                                         |
| EC-10 | Countdown viewed on the trip's start date                                 | Reads as starting today, not "0 days" or a negative number; from day one onward the trip reads as under way (FR-016).     |
| EC-11 | Two trips in the list share the same name                                 | Both shown; location and dates disambiguate. Names are not unique across trips, by design.                                |
| EC-12 | Visitor has a binding to a participant that no longer exists              | The binding is ignored and the trip omitted from the list, rather than rendering a broken row.                            |
| EC-13 | Device bound to a trip across a time-zone change or daylight-saving shift | Countdown and past/current/upcoming classification are computed in a single, consistent reference for the trip's dates — a trip never flickers between current and past on a refresh. |

## 8. Success Criteria

| ID     | Criterion                                                                                                             |
| ------ | ------------------------------------------------------------------------------------------------------------------------- |
| SUC-01 | All acceptance scenarios SC-001 … SC-011 pass in CI.                                                                  |
| SUC-02 | From opening a trip, a participant can state whether they are coming in a single action.                              |
| SUC-03 | A reader of the trip can tell, without counting manually, how many are in and who has not yet answered.                |
| SUC-04 | A device that is part of three trips reaches any one of them in two actions from first load, with no invite link.      |
| SUC-05 | `in + out + unsure` equals the participant count in every test scenario, including after concurrent changes (INV-13).  |
| SUC-06 | Invariants INV-10 … INV-15 each have at least one automated test.                                                      |
| SUC-07 | The same person using a phone and a laptop has one attendance status and one row in the participant list (SC-010). |

## 9. Dependencies and Constraints

### 9.1 Dependencies

- **Spec 001-003** — Trip, Participant, InviteLink, Visitor and ParticipantBinding. This
  feature adds one attribute to Participant and reads across bindings; it cannot be
  implemented before it.
- The persistence choice is still an open question in spec 001-003 (§10 #1). Adding
  `attendanceStatus` to an existing Participant store is a small migration, but it is a
  migration — worth settling the persistence decision before 001-003 ships.

### 9.2 Constraints

- ASP.NET Core Razor Pages on .NET 10, per [CLAUDE.md](../../CLAUDE.md).
- Still no accounts: the trips list must work from a cookie alone (§1.3).
- Attendance must stay meaningful without notifications, since none exist (§1.3).

### 9.3 Architecture References

| Arc42 Section                    | Relevance to This Feature                                                                       |
| -------------------------------- | ------------------------------------------------------------------------------------------------- |
| 3. Context & Scope               | No new actors; the visitor is already named by spec 001-003.                                       |
| 5. Building Block View           | Adds attendance to Participant and the derived Headcount; no new building blocks.                   |
| 6. Runtime View                  | Adds two read paths: the participant list with its headcount, and visitor → bindings → trips.       |
| 8. Crosscutting Concepts         | Authorisation rule "a visitor may act only through its own bindings" (NFR-003) generalises beyond this feature — it is the pattern every later write operation follows. |
| 9. Architecture Decisions (ADRs) | Needs an ADR for the Visitor + ParticipantBinding identity model, recording §5.5's rejected alternative. Owned by spec 001-003, motivated by this one. |
| 10. Quality Requirements         | NFR-007 (not colour alone) is project-wide; promote it rather than repeating it per feature.       |
| 12. Glossary                     | Adds: Attendance status, Headcount, Visitor, Binding.                                              |

## 10. Open Questions

| #   | Question                                                                                           | Owner | Status | Resolution                                                                 |
| --- | ---------------------------------------------------------------------------------------------------- | ----- | ------ | -------------------------------------------------------------------------- |
| 1   | Can a participant leave a trip, or be removed — and does leaving remove the trip from their list?   | Kris  | Open   | Carried over from spec 001-003 §10 #3; marking yourself "out" is the current substitute. |
| 2   | Should a device be able to hide or archive a past trip from its list?                               | Kris  | Open   | Deferred until lists get long in practice; NFR-008 caps the pain at 20.    |
| 3   | Does the trip starter need a way to see who has not answered, beyond the `unsure` count?            | Kris  | Open   | FR-008 makes it visible; a dedicated view is deferred pending real use.    |

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
  - [~] Arc42 references point to the right sections — sections exist but are still unwritten
  - [x] No more than 3 [NEEDS CLARIFICATION] markers remain (there are none)
  - [x] Open questions are assigned and have a resolution path
-->
