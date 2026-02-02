# Lazy Photos - Self-Hosted Google Photos Clone

A self-hosted photo management solution with Google Photos import capability, built on .NET 8+, running efficiently on Raspberry Pi.

## High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                         Clients                              │
├──────────────────┬──────────────────┬──────────────────────┤
│   Web App        │   iOS App        │   Android App        │
│   (Blazor)       │   (.NET MAUI)    │   (.NET MAUI)        │
└────────┬─────────┴────────┬─────────┴─────────┬────────────┘
         │                  │                   │
         └──────────────────┼───────────────────┘
                            │
                     HTTPS/REST API
                            │
         ┌──────────────────▼───────────────────┐
         │     ASP.NET Core Web API             │
         │     (LazyPhotos.API)                 │
         ├──────────────────────────────────────┤
         │  - Authentication & Authorization    │
         │  - Photo Upload/Download             │
         │  - Metadata Management               │
         │  - Search & Filtering                │
         │  - Google Photos Import              │
         │  - Album Management                  │
         └──────────┬───────────────────────────┘
                    │
         ┌──────────▼───────────────────────────┐
         │     Business Logic Layer             │
         │     (LazyPhotos.Core)                │
         ├──────────────────────────────────────┤
         │  - Services                          │
         │  - Domain Models                     │
         │  - Interfaces                        │
         │  - Business Rules                    │
         └──────────┬───────────────────────────┘
                    │
         ┌──────────▼───────────────────────────┐
         │     Data Access Layer                │
         │     (LazyPhotos.Infrastructure)      │
         ├──────────────────────────────────────┤
         │  - EF Core DbContext                 │
         │  - Repositories                      │
         │  - Data Models                       │
         │  - Migrations                        │
         └──────────┬───────────────────────────┘
                    │
         ┌──────────▼───────────────────────────┐
         │     PostgreSQL Database              │
         └──────────────────────────────────────┘
                    │
         ┌──────────▼───────────────────────────┐
         │     File Storage                     │
         │  - Original Photos                   │
         │  - Thumbnails (multiple sizes)       │
         │  - Optimized versions                │
         └──────────────────────────────────────┘
```

## Key Features

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

### Google Photos Import
- OAuth2 authentication with Google
- Batch download via Google Photos API
- Preserve metadata
- Progress tracking
- Resume capability

## Technology Stack

### Backend
- **Framework**: ASP.NET Core 8+ (Web API)
- **ORM**: Entity Framework Core 8+
- **Database**: PostgreSQL 15+
- **Authentication**: ASP.NET Core Identity + JWT
- **Image Processing**: ImageSharp or SkiaSharp
- **Background Jobs**: Hangfire (for thumbnail generation, import jobs)

### Frontend Web
- **Framework**: Blazor Server or Blazor WebAssembly
- **UI**: MudBlazor or Radzen Blazor Components

### Mobile
- **Framework**: .NET MAUI
- **UI**: MAUI Community Toolkit
- **Local Storage**: SQLite (for offline mode)

### Infrastructure
- **Containerization**: Docker
- **Reverse Proxy**: Nginx (for Raspberry Pi deployment)
- **File Storage**: Local filesystem (with optional S3/MinIO support)

## Current Solution Structure

```
LazyPhotos/
├── src/
│   ├── Lazy.Photos.App/           # .NET MAUI Mobile App
│   │   ├── Features/
│   │   │   ├── Photos/            # Main gallery feature
│   │   │   ├── SignIn/            # Authentication
│   │   │   ├── Search/            # Search functionality
│   │   │   └── DevStats/          # Debug statistics
│   │   ├── Platforms/
│   │   │   ├── iOS/
│   │   │   └── Android/
│   │   └── MauiProgram.cs
│   ├── Lazy.Photos.Core/          # Domain models & interfaces
│   └── Lazy.Photos.Data/          # API clients & contracts
├── docs/                          # Architecture documentation
├── CLAUDE.md                      # AI assistant guidelines
├── AGENTS.md                      # Agent instructions
└── Lazy.Photos.sln
```

## Build & Run

### Prerequisites
- .NET 10 SDK
- Xcode (for iOS development on Mac)
- Android SDK (for Android development)

### Build Commands

```bash
# Build entire solution
dotnet build Lazy.Photos.sln

# Build iOS app (requires Mac with Xcode)
dotnet build src/Lazy.Photos.App/Lazy.Photos.App.csproj -f net10.0-ios

# Build Android app
dotnet build src/Lazy.Photos.App/Lazy.Photos.App.csproj -f net10.0-android
```

## Raspberry Pi Deployment

### Recommended Hardware
- Raspberry Pi 4/5 (4GB+ RAM)
- Fast SD card or SSD via USB
- Optional: External HDD for photo storage

### Docker Compose

```yaml
version: '3.8'
services:
  api:
    build: .
    ports:
      - "5000:8080"
    environment:
      - ConnectionStrings__DefaultConnection=...
      - ASPNETCORE_ENVIRONMENT=Production
    volumes:
      - ./photos:/app/photos
    depends_on:
      - db

  db:
    image: postgres:15-alpine
    environment:
      - POSTGRES_PASSWORD=...
    volumes:
      - pgdata:/var/lib/postgresql/data

volumes:
  pgdata:
```

### Configuration

```json
{
  "LazyPhotos": {
    "ThumbnailGeneration": {
      "MaxConcurrent": 2,
      "Quality": 80
    },
    "Storage": {
      "EnableCompression": true,
      "OriginalFormat": "jpeg",
      "MaxOriginalSize": 4096
    },
    "Database": {
      "ConnectionPoolSize": 10,
      "CommandTimeout": 60
    }
  }
}
```

## Development Phases

### Phase 1: MVP (Core Features)
- [x] MAUI project setup
- [x] Photo grid with infinite scroll
- [x] Thumbnail generation
- [ ] API project setup
- [ ] Database schema & EF Core setup
- [ ] User authentication
- [ ] Basic Blazor UI

### Phase 2: Enhanced Features
- [ ] Album management
- [ ] Search functionality
- [ ] Metadata extraction
- [ ] Timeline view

### Phase 3: Advanced Features
- [ ] Google Photos import
- [ ] Shared links
- [ ] Face detection (optional)
- [ ] Advanced search
- [ ] Duplicate detection

### Phase 4: Polish & Optimization
- [ ] Performance tuning
- [ ] Raspberry Pi optimization
- [ ] Mobile offline mode
- [ ] Backup/restore tools
- [ ] Admin dashboard

## Documentation

See the `/docs` folder for detailed documentation:

1. [Requirements](docs/01-requirements.md) - Project vision and constraints
2. [Backend Architecture](docs/02-architecture-backend.md) - API and infrastructure
3. [Data Model & Sync](docs/03-data-model-sync.md) - Database schema and sync strategy
4. [MAUI Foundation](docs/04-maui-foundation.md) - Mobile app structure
5. [Core Features](docs/05-core-features.md) - Feature specifications
6. [Performance & UX](docs/06-performance-ux.md) - Optimization strategies
7. [Security & Compliance](docs/07-security-compliance.md) - Security measures
8. [Testing & Release](docs/08-testing-release.md) - QA and deployment

## API Endpoints (Preview)

### Authentication
- `POST /api/auth/register`
- `POST /api/auth/login`
- `POST /api/auth/refresh`

### Photos
- `GET /api/photos` - List photos (paginated)
- `POST /api/photos/upload` - Upload photo(s)
- `GET /api/photos/{id}` - Get photo details
- `GET /api/photos/{id}/download` - Download original
- `GET /api/photos/{id}/thumbnail` - Get thumbnail
- `DELETE /api/photos/{id}` - Delete photo

### Albums
- `GET /api/albums` - List albums
- `POST /api/albums` - Create album
- `GET /api/albums/{id}` - Get album details
- `POST /api/albums/{id}/photos` - Add photos to album

### Search
- `GET /api/search?q={query}&from={date}&to={date}`

### Import
- `POST /api/import/google-photos/start` - Start import job
- `GET /api/import/jobs/{id}` - Get job status

## License

This project is self-hosted and privacy-focused. See LICENSE for details.
