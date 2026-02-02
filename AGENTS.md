# Lazy Photos Agent Instructions

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
