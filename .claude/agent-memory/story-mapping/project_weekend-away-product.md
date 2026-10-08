---
name: project-weekend-away-product
description: The product this repo is being built into — a multi-user weekend-away-with-friends planner (trip, activities, bring list, tasks)
metadata:
  type: project
---

The `my-project` Razor Pages app is being built into **Weekend Away**: a planner for a small group of friends going away for a weekend together. A trip has a location and dates; around it the group plans activities, a bring/packing list, and task assignments.

**Why:** Today this coordination is scattered across group chats — nobody knows who booked the house or who's bringing the barbecue. The app's value is consolidating plan + list + responsibilities in one shared place.

**How to apply:** It is explicitly **multi-user** — every participant can see and contribute, not just the trip starter. Avoid single-user/personal-tool designs. Use the domain vocabulary from the story map: *trip, participant, activity, bring list item (claimed/packed), task (assigned/done)*.

Story map lives at `docs/product/story-map.md` (stories 001–023, five activities). Story numbers drive spec filenames `docs/specs/NNN-<slug>.md`.

Open questions never answered by the user (may reshape scope later): whether this is a real tool or a learning/demo project, and whether real accounts/auth are wanted versus the lightweight share-link + display-name joining assumed in stories 002–003.
