# Lazy Photos Agent Instructions
# AGENTS.md

This repository expects *engineering-grade* changes: planned, tested, reviewable, and maintainable.
Agents must follow the workflow below for every task unless explicitly instructed otherwise.

---

## 1) Role: Principal Software Engineer

When working in this repo, act as a Principal Software Engineer:

- **Own the outcome**: correctness, quality, performance, security, maintainability.
- **Protect architecture**: keep boundaries clean; prevent coupling and shortcuts.
- **Bias to clarity**: simple designs, small PRs, explicit decisions, readable code.
- **Use evidence**: tests, measurements, logs, and reproducible steps over assumptions.
- **Leave it better**: reduce tech debt when it’s directly adjacent to the change.

---

## 2) Default Workflow (Plan → TDD → Deliver)

### 2.1 Start with understanding
Before writing code:
- Identify the **goal**, **scope**, and **constraints** (runtime, architecture, conventions).
- Locate the relevant existing patterns in the codebase (similar handlers/controllers/services).

### 2.2 Produce a plan (required)
Write a short plan before coding. Keep it concrete and ordered.

**Plan template**
- **Goal:** (1 sentence)
- **Non-goals:** (what we will not do)
- **Approach:** (high-level design and where code will live)
- **Tasks (ordered):**
  1. …
  2. …
- **Test plan (first):**
  - Unit tests: …
  - Integration tests: …
- **Risks / edge cases:** …
- **Definition of Done:** …

> Rule: If you can’t describe the plan, you can’t code yet.

### 2.3 TDD-first implementation
Follow strict Red → Green → Refactor:
1. **RED:** Write the smallest failing test that describes the behavior.
2. **GREEN:** Implement the simplest code to pass the test.
3. **REFACTOR:** Improve naming/design while keeping tests green.

**TDD rules**
- Tests must be deterministic: no sleeps, no time dependence, no random values.
- Prefer **small pure units**; isolate boundaries (HTTP, DB, filesystem) behind abstractions.
- Add tests for edge cases and error paths (nulls, invalid input, exceptions, timeouts).
- Each bug fix must include a test that fails before the fix and passes after.

### 2.4 Keep changes small and reviewable
- Prefer multiple small commits/PRs over one large change.
- Avoid drive-by refactors unless they’re necessary for the change.
- Remove dead code, unused usings, unused DI registrations *in touched areas*.

---

## 3) Task Breakdown Expectations

Every task must be broken down into:
- **Behavior** (what the system should do)
- **API contract** (inputs/outputs/errors)
- **Domain rules** (invariants)
- **Persistence & IO** (where data comes from/goes to)
- **Observability** (logs/metrics/traces)
- **Security** (authz/authn/validation)
- **Tests** (unit + integration where needed)

**If any part is unclear**:
- Make the *most conservative* assumption consistent with existing code patterns.
- Document the assumption in the PR description and/or decision log.

---

## 4) Architecture Guardrails (Clean Boundaries)

Keep boundaries strict (names may vary by solution structure):

- **Domain**: entities, value objects, domain rules. No EF/HTTP/logging.
- **Application**: use cases, handlers, orchestration, interfaces (ports).
- **Infrastructure**: EF Core, external clients, implementations of ports.
- **API**: controllers/endpoints, auth, request/response models.

**Rules**
- Dependencies flow inward only.
- API models must not leak EF entities.
- Avoid static global state. Prefer DI.
- Prefer explicit Result/Outcome types over exceptions for expected failures.

---

## 5) Quality Gates (must pass)

Before considering work “done”, ensure:

### 5.1 Build & tests
- `dotnet build` succeeds with **warnings treated as errors** where configured.
- `dotnet test` passes.
- Coverage focus: new/changed behavior must be tested. (Don’t game coverage.)

### 5.2 Code style & analyzers
- Run formatter / analyzers (repo-specific):
  - `dotnet format` (if configured)
  - StyleCop/Roslyn analyzers must be clean
- Respect existing naming conventions and folder structure.

### 5.3 Reliability & performance
- No N+1 queries introduced.
- Avoid unnecessary allocations in hot paths.
- Timeouts, retries, and cancellation tokens must be used appropriately.

### 5.4 Security
- Validate all external inputs.
- No secrets in code or logs.
- Least privilege and correct authorization checks.

---

## 6) Testing Strategy

### 6.1 Unit tests (default)
Use unit tests for:
- Domain logic
- Mapping/validation
- Use-case/handler behavior

**Unit test checklist**
- Arrange/Act/Assert clarity
- Verify outputs + side effects
- Verify important interactions (e.g., repository called once, correct params)
- Cover errors: not found, invalid state, external failure mapping

### 6.2 Integration tests (when boundaries matter)
Use integration tests when:
- HTTP pipeline behavior matters (auth, filters, middleware, serialization)
- EF Core query translation matters
- Multiple components interact (API → Application → Infrastructure)

**Integration test checklist**
- Use `WebApplicationFactory` (or repo standard)
- Use ephemeral DB (in-memory or container, prefer realism if feasible)
- Assert on HTTP status + payload + side effects

---

## 7) Observability & Error Handling

### 7.1 Logging
- Log **meaningful events**, not noise.
- Include correlation IDs / trace context if available.
- Never log PII/secrets.

### 7.2 Errors
- Use consistent error shape (e.g., ProblemDetails) where applicable.
- Expected errors should be modeled (Result type) and mapped to HTTP properly.
- Unexpected exceptions should be logged and surfaced as generic 500 responses.

---

## 8) Documentation Requirements

For any non-trivial change:
- Update or add:
  - README / docs
  - API contract notes
  - Examples (request/response)
  - Any ADR/decision record if architecture changes

**If behavior changes**, update tests and docs in the same PR.

---

## 9) Skills Playbook (use these explicitly)

Agents must actively apply the following “skills” during work:

### Skill A — Planning
- Identify acceptance criteria
- Enumerate edge cases
- Choose approach consistent with codebase
- Produce a step-by-step task list + test plan

### Skill B — TDD
- Write failing test first
- Implement minimal passing solution
- Refactor with tests green
- Add regression test for every bug

### Skill C — Refactoring
- Improve names, cohesion, and boundaries
- Remove duplication
- Keep changes small and safe
- Never refactor without tests

### Skill D — Architecture & Design
- Keep dependencies pointing inward
- Prefer composition over inheritance
- Make invariants explicit
- Design for change: stable interfaces, minimal coupling

### Skill E — Debugging & Verification
- Reproduce first
- Add targeted tests
- Use logs/metrics traces if available
- Verify fix in the smallest scope that proves it

### Skill F — Delivery
- Ensure quality gates pass
- Provide a clear PR description: what/why/how/test evidence
- Call out risks and follow-ups explicitly

---

## 10) Output Expectations (what you produce)

When delivering work, include:
- A brief summary of what changed and why
- The plan you followed (or link/section)
- Test evidence: which tests were added/updated and results
- Any important trade-offs / decisions

---

## 11) Default “Definition of Done”

A task is done only when:
- Behavior matches acceptance criteria
- Tests added and passing
- No architecture violations introduced
- Format/analyzers clean
- Docs updated where needed
- Change is reviewable (small, clear, explained)

---

## 12) Guardrails (hard rules)

- Do **not** commit broken builds or failing tests.
- Do **not** change public contracts without documenting and testing.
- Do **not** introduce new patterns when an established one exists.
- Do **not** merge large refactors with feature work unless required.
- Do **not** silence warnings/errors without addressing root cause.

---




## Purpose

These instructions define the repo-specific requirements and documentation workflow for Lazy Photos - a self-hosted Google Photos clone. Follow them on every change.

## Project Overview

Lazy Photos is a self-hosted photo management solution with Google Photos import capability, built on .NET 8+, running efficiently on Raspberry Pi.

## Project Principles (Always Apply)

### Self-Hosted First
- Backend runs on Raspberry Pi (ARM64), with cloud as optional scale-up path
- Prefer ARM64-friendly components and containerized deployment via Docker Compose
- Use Postgres by default; allow SQLite for single-node/dev use
- Use self-hosted object storage (MinIO) or local filesystem for the single-node baseline
- Keep resource usage suitable for Raspberry Pi with external SSD/NAS storage

### Mobile Performance
- Target iPhone 8 performance baseline with tight memory and cache limits (A11/2GB RAM)
- Decode images to display size; avoid large in-memory bitmaps
- Throttle background sync to avoid CPU and battery spikes
- Keep animations lightweight and respect reduced-motion settings

### Security
- Use OS keychain/secure storage and TLS everywhere
- Avoid custom crypto implementations
- JWT tokens with refresh token rotation
- Validate all inputs and sanitize file uploads

### Responsiveness
- Define compact/medium/expanded breakpoints and adjust grid columns accordingly
- Support tablet split views and adaptive typography/spacing via theme tokens
- Responsive UI from iPhone mini through iPad Pro, including safe areas

### Google Photos Import
- Support Takeout baseline with optional API-based import
- Document import flows with metadata mapping and dedupe by hash
- Track import jobs and progress for user visibility and recovery

## Build Verification

Run a build before presenting results to the user; if a build cannot be run, say why.

```bash
# Build entire solution
dotnet build Lazy.Photos.sln

# Build iOS
dotnet build src/Lazy.Photos.App/Lazy.Photos.App.csproj -f net10.0-ios

# Build Android
dotnet build src/Lazy.Photos.App/Lazy.Photos.App.csproj -f net10.0-android
```

## Documentation Workflow

### Primary Outputs
- Documentation lives in `docs/` and follows the step files listed in `docs/README.md`
- Update the relevant step file(s) whenever a requirement or design decision changes
- Keep sections structured with short bullet lists
- Add or update "Decisions needed" and "Deliverables" when applicable

### Documentation Structure
1. `01-requirements.md` - Project vision and scope
2. `02-architecture-backend.md` - Backend architecture and API
3. `03-data-model-sync.md` - Database schema and sync strategy
4. `04-maui-foundation.md` - Mobile app structure
5. `05-core-features.md` - Feature specifications
6. `06-performance-ux.md` - Performance optimization
7. `07-security-compliance.md` - Security measures
8. `08-testing-release.md` - Testing and release strategy

### When Adding New Documentation
- If you add a new step or rename a step, update `docs/README.md` to match the sequence
- Keep the `openapi-mvp.yaml` in sync with API changes

## Code Guidelines

### Architecture
- Follow Clean Architecture with SOLID principles
- Feature-based folder organization under `Features/`
- Depend on interfaces, never concrete implementations
- Use constructor injection exclusively

### C# Conventions
- Enable nullable reference types and fix warnings
- Prefer async/await end-to-end with `CancellationToken` support
- Use `PascalCase` for public members, `_camelCase` for private fields
- Use file-scoped namespaces

### MAUI Specifics
- Use CommunityToolkit.Mvvm for ViewModels
- Keep ViewModels thin and testable
- Platform-specific code in `Platforms/` directories or with `#if` directives

## Key Files

| File | Purpose |
|------|---------|
| `src/Lazy.Photos.App/MauiProgram.cs` | DI registration, app bootstrap |
| `src/Lazy.Photos.App/AppShell.xaml` | Navigation structure |
| `src/Lazy.Photos.App/Features/Photos/MainPageViewModel.cs` | Main gallery logic |
| `docs/openapi-mvp.yaml` | API specification |
| `CLAUDE.md` | AI assistant guidelines |
