# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

ASP.NET Core **Razor Pages** web app on **.NET 10**, scaffolded from the default
`webapp` template and not yet customized. Nullable reference types and implicit
usings are enabled. Root namespace is `my_project` (the csproj name `my-project`
is not a valid C# identifier, hence the underscore).

There is no solution file and no README. Two .NET projects: the web app
(`my-project.csproj` at the root) and `tests/my-project.Tests`, plus an npm-based
end-to-end suite at `tests/e2e`. The test projects live *inside* the web project's folder,
so the web csproj removes `tests/**` from its globs; there is deliberately no `.sln`, so
`dotnet build`/`dotnet run` at the root stay unambiguous.

Persistence is EF Core + SQLite, schema created at startup with `EnsureCreated()` — no
migrations yet. See [ADR 001](docs/architecture/adr/001-persistence-with-ef-core-and-sqlite.md).

## Commands

```bash
dotnet build
dotnet run                      # https profile: https://localhost:7073 + http://localhost:5202
dotnet run --launch-profile http   # http only, port 5202
dotnet watch                    # hot reload during development
dotnet format                   # formatting / style fixes
```

```bash
dotnet test tests/my-project.Tests
dotnet test tests/my-project.Tests --filter "FullyQualifiedName~<TestName>"
```

```bash
cd tests/e2e && npm ci && npx playwright install chromium   # first time only
cd tests/e2e && npm test                 # headless
cd tests/e2e && npm run test:ui          # pick and watch tests interactively
cd tests/e2e && npm run report           # open the last HTML report
```

Three layers, and nothing else:

- **Unit** — the domain and services (`tests/.../Domain`, `tests/.../Services`).
- **Integration** — the real app over HTTP via `WebApplicationFactory` against in-memory
  SQLite (`tests/.../Integration`).
- **End-to-end** — [tests/e2e/](tests/e2e/): Playwright driving a real Chromium against a
  real `dotnet run`. Reserved for whole journeys across more than one browser; anything
  provable at a lower layer belongs there instead.

Playwright starts the app itself on **port 5203** with a throwaway SQLite file in the temp
directory, so the suite never touches a `dotnet run` already on 5202. It builds into
`tests/e2e/.playwright/artifacts` for the same reason — a running dev server holds a lock
on the project's own `bin/`.

Because identity is an HttpOnly cookie, "another person" means another BrowserContext: the
`device()` fixture in [tests/e2e/support/app.js](tests/e2e/support/app.js) hands out fresh
browsers, and the built-in `page` plays the first person (only it records traces and
failure screenshots).

## Structure

- [Program.cs](Program.cs) — minimal hosting startup; the only place services and
  middleware are registered. Any DI registration, auth, or persistence wiring goes here.
- [Pages/](Pages/) — Razor Pages. Each page is a `.cshtml` view plus a `.cshtml.cs`
  PageModel in namespace `my_project.Pages`. Shared layout and partials live in
  [Pages/Shared/](Pages/Shared/).
- [wwwroot/](wwwroot/) — static assets; Bootstrap and jQuery are vendored under
  `wwwroot/lib/`. Served via `MapStaticAssets()` / `WithStaticAssets()` (.NET 9+
  static asset pipeline with fingerprinting), not `UseStaticFiles()`.

## Documentation workflow

This repo is set up for a spec-driven workflow; documentation is intentionally
part of the development loop:

- `docs/architecture/` — **arc42** architecture documentation, numbered `01`–`12`
  with [00-table-of-contents.md](docs/architecture/00-table-of-contents.md) as the
  index. Currently empty templates — fill the relevant sections as the system takes
  shape rather than creating ad-hoc docs elsewhere.
- `docs/product/story-map.md` — user story map (not yet created). Produced by the
  `story-mapping` agent/skill; stories are numbered and those numbers drive spec
  file names.
- `docs/specs/NNN-<slug>.md` — feature specs (not yet created). Produced by the
  `/spec` skill ([.claude/skills/spec/SKILL.md](.claude/skills/spec/SKILL.md)); `NNN`
  must match the story number from the story map. Template:
  [.claude/skills/spec/spec-template.md](.claude/skills/spec/spec-template.md).
- `docs/domainmodel.md` — global, cross-feature domain model with a Mermaid class
  diagram. The `/spec` skill reads it before modelling a feature and updates it
  after every spec, so it stays the single source of ubiquitous language.

The intended order is: story map → spec → implementation. When implementing a
feature, read its spec and the referenced arc42 sections first, and reuse the
domain vocabulary established there.

## GitHub issues

Whenever the user mentions issues (creating, reading, listing, commenting,
closing), use the **GitHub MCP server** (`mcp__github__*` tools) — not `gh` or
the web UI. Always target this repository's remote:

- owner: `KrisDeCree-VRB`
- repo: `my-project`

## Conventions

- Build artifacts (`bin/`, `obj/`) and local tooling dirs are ignored via
  [.gitignore](.gitignore); they were tracked in the initial commit and have since
  been untracked.
- Feature-specific non-functional requirements belong in the feature spec;
  project-wide ones belong in arc42 section 10 and should be referenced, not duplicated.
