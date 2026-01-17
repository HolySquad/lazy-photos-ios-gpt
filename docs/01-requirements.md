# Step 1 - Requirements and Scope

Goal
- Build a mobile photo app that backs up, organizes, searches, and shares photos with cross-device sync.

User stories
- As a user, I want automatic backup so I never lose photos.
- As a user, I want a fast grid to browse my library.
- As a user, I want to create albums and share links.
- As a user, I want to search by date, location, and keywords.
- As a user, I want to claim my self-hosted server by logging in and attaching it to my LazyPhotos account.

MVP features
- Authentication and account setup.
- Photo grid with infinite scroll.
- Photo detail viewer with share sheet.
- Albums (create, rename, add/remove photos).
- Search by metadata (date, location, filename, tags).
- Backup and sync with status and retry.
- Settings for backup (Wi-Fi only, upload quality, cache size).

Out of scope for v1
- Face recognition or advanced ML search.
- Collaborative album editing with live presence.
- Video editing.
- Desktop apps.

Non-functional requirements
- Smooth scrolling on 10k+ photos.
- Upload retry with backoff and offline queue.
- Reasonable battery and data usage.
- Self-hosted-first backend that runs on Raspberry Pi (ARM64) with external SSD storage.
- Clear privacy and data retention policy.

Decisions needed
- Storage provider and costs.
- Upload quality options (original vs compressed).
- Offline access scope (recent only vs full cache).
- Target self-hosted hardware baseline (Pi 4/5, RAM, storage).
- Metrics to define success (retention, upload completion, search usage).

Deliverables
- Product requirements document.
- Basic wireframes for key screens.
- Success metrics and rollout plan.
