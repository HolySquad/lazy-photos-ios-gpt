# Step 1 - Requirements and Scope

## Project Overview

Lazy Photos is a self-hosted photo management solution with Google Photos import capability, built on .NET 8+, running efficiently on Raspberry Pi.

## Goal

Build a mobile photo app that backs up, organizes, searches, and shares photos with cross-device sync, while maintaining complete user control over their data through self-hosting.

## User Stories

### Core Functionality
- As a user, I want automatic backup so I never lose photos.
- As a user, I want a fast grid to browse my library with smooth 60fps scrolling.
- As a user, I want to create albums and share links.
- As a user, I want to search by date, location, and keywords.
- As a user, I want to claim my self-hosted server by logging in and attaching it to my Lazy Photos account.

### Import & Migration
- As a user, I want to import my Google Photos library via Takeout or API.
- As a user, I want to preserve all metadata during import (dates, locations, descriptions).
- As a user, I want to track import progress and resume if interrupted.

### Privacy & Control
- As a user, I want my photos stored only on my own hardware.
- As a user, I want complete control over sharing and access.
- As a user, I want to delete my data permanently when requested.

## MVP Features

### Photo Management
- Upload (single/batch)
- Auto thumbnail generation (multiple sizes: 150px, 300px, 1080px)
- EXIF metadata extraction
- Duplicate detection (via hash)
- Geolocation support
- Timeline view (by date taken)

### Album Management
- Create/edit/delete albums
- Add/remove photos
- Album covers
- Shared albums (read-only links)

### Search & Filter
- Search by date range
- Search by location
- Search by tags
- Search by camera model
- Full-text search (photo names, descriptions)

### Backup & Sync
- Auto upload toggle
- Upload progress and retry UI
- Status for last successful backup
- Wi-Fi only option
- Upload quality settings (original vs compressed)
- Cache size management

### Google Photos Import
- OAuth2 authentication with Google
- Batch download via Google Photos API
- Google Takeout ZIP import
- Preserve metadata
- Progress tracking
- Resume capability

### Settings
- Upload quality, Wi-Fi only, cache size
- Account and sign-out
- Server connection management

## Out of Scope for v1

- Face recognition or advanced ML search
- Collaborative album editing with live presence
- Video editing
- Desktop apps (web serves this purpose)
- Real-time collaboration features

## Non-Functional Requirements

### Performance
- Smooth 60fps scrolling on 10k+ photos
- Baseline performance on iPhone 8-class hardware (A11/2GB RAM)
- Minimal memory spikes when paging
- Reasonable battery and data usage

### Reliability
- Upload retry with backoff and offline queue
- Graceful degradation when offline
- Data integrity with hash verification

### Scalability
- Self-hosted backend that runs on Raspberry Pi (ARM64) with external SSD storage
- Optional cloud scale-up path for advanced users

### Privacy & Security
- Clear privacy and data retention policy
- HTTPS/TLS for all communications
- Encrypted storage for sensitive data
- GDPR and CCPA compliance readiness

## Technology Stack

### Backend
- **Framework**: ASP.NET Core 8+ (Web API)
- **ORM**: Entity Framework Core 8+
- **Database**: PostgreSQL 15+ (SQLite for dev/single-node)
- **Authentication**: ASP.NET Core Identity + JWT
- **Image Processing**: ImageSharp or SkiaSharp
- **Background Jobs**: Hangfire

### Frontend Web
- **Framework**: Blazor Server or Blazor WebAssembly
- **UI**: MudBlazor or Radzen Blazor Components

### Mobile
- **Framework**: .NET MAUI 10+
- **UI**: MAUI Community Toolkit
- **Local Storage**: SQLite
- **MVVM**: CommunityToolkit.Mvvm

### Infrastructure
- **Containerization**: Docker
- **Reverse Proxy**: Nginx
- **File Storage**: Local filesystem (optional S3/MinIO)

## Target Hardware

### Server (Self-Hosted)
- Raspberry Pi 4/5 (4GB+ RAM)
- Fast SD card or SSD via USB
- External HDD/NAS for photo storage

### Mobile Devices
- iOS 15.0+ (iPhone 8 baseline)
- Android 21.0+ (equivalent performance tier)

## Decisions Needed

- Storage provider and costs for optional cloud tier
- Upload quality options (original vs compressed presets)
- Offline access scope (recent only vs full cache)
- Target self-hosted hardware baseline (Pi 4/5, RAM, storage)
- Metrics to define success (retention, upload completion, search usage)
- Server claim and ownership model (single-owner vs shared admin)

## Deliverables

- Product requirements document (this file)
- Architecture documentation (see 02-architecture-backend.md)
- Database schema (see 03-data-model-sync.md)
- API specification (see openapi-mvp.yaml)
- Basic wireframes for key screens
- Success metrics and rollout plan
