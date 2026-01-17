# LazyPhotos Agent Instructions

Purpose
These instructions define the repo-specific requirements and documentation workflow for LazyPhotos. Follow them on every change.

Project principles (always apply)
- Self-hosted-first backend that runs on Raspberry Pi (ARM64), with cloud as an optional scale-up path.
- Mobile performance and security are top priorities; the app must remain usable on iPhone 8-class hardware.
- Responsive UI from iPhone mini through iPad Pro, including safe areas and tablet split-view layouts.
- Support Google Photos import (Takeout baseline, optional API-based import if feasible).

Documentation workflow
- Primary outputs live in `docs/` and follow the step files listed in `docs/README.md`.
- Update the relevant step file(s) whenever a requirement or design decision changes.
- Keep sections structured with short bullet lists and add or update "Decisions needed" and "Deliverables" when applicable.
- If you add a new step or rename a step, update `docs/README.md` to match the sequence.

Backend constraints (self-hosted-first)
- Prefer ARM64-friendly components and containerized deployment via Docker Compose.
- Use Postgres by default; allow SQLite for single-node/dev use.
- Use self-hosted object storage (MinIO) or local filesystem for the single-node baseline.
- Keep resource usage suitable for Raspberry Pi with external SSD/NAS storage.

Mobile constraints (performance and security)
- Target iPhone 8 performance baseline with tight memory and cache limits.
- Decode images to display size; avoid large in-memory bitmaps and excessive animations.
- Use OS keychain/secure storage and TLS everywhere; avoid custom crypto.

Responsiveness
- Define compact/medium/expanded breakpoints and adjust grid columns accordingly.
- Support tablet split views and adaptive typography/spacing via theme tokens.

Import requirements
- Document Google Photos import flows and include metadata mapping and dedupe by hash.
- Track import jobs and progress for user visibility and recovery.
