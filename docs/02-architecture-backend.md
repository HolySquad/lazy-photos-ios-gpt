# Step 2 - Architecture and Backend

## Overview

Self-hosted-first backend that runs on low-power ARM64 hardware (Raspberry Pi) and can scale to cloud later. The architecture follows Clean Architecture principles with clear separation of concerns.

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

## Solution Structure

```
LazyPhotos/
│
├── src/
│   ├── LazyPhotos.API/                    # ASP.NET Core Web API
│   │   ├── Controllers/
│   │   │   ├── AuthController.cs
│   │   │   ├── PhotosController.cs
│   │   │   ├── AlbumsController.cs
│   │   │   ├── SearchController.cs
│   │   │   └── ImportController.cs
│   │   ├── Middleware/
│   │   │   ├── ExceptionHandlingMiddleware.cs
│   │   │   └── RateLimitingMiddleware.cs
│   │   ├── Filters/
│   │   ├── DTOs/
│   │   ├── Mappers/
│   │   ├── appsettings.json
│   │   └── Program.cs
│   │
│   ├── LazyPhotos.Core/                   # Business Logic
│   │   ├── Models/
│   │   │   ├── Photo.cs
│   │   │   ├── Album.cs
│   │   │   ├── User.cs
│   │   │   ├── Tag.cs
│   │   │   └── SharedLink.cs
│   │   ├── Interfaces/
│   │   │   ├── IPhotoService.cs
│   │   │   ├── IAlbumService.cs
│   │   │   ├── IStorageService.cs
│   │   │   ├── IThumbnailService.cs
│   │   │   ├── IMetadataService.cs
│   │   │   └── IGooglePhotosImporter.cs
│   │   ├── Services/
│   │   │   ├── PhotoService.cs
│   │   │   ├── AlbumService.cs
│   │   │   ├── ThumbnailService.cs
│   │   │   ├── MetadataService.cs
│   │   │   └── GooglePhotosImporter.cs
│   │   ├── Exceptions/
│   │   └── Constants/
│   │
│   ├── LazyPhotos.Infrastructure/         # Data Access
│   │   ├── Data/
│   │   │   ├── LazyPhotosDbContext.cs
│   │   │   ├── Configurations/
│   │   │   │   ├── PhotoConfiguration.cs
│   │   │   │   ├── AlbumConfiguration.cs
│   │   │   │   └── UserConfiguration.cs
│   │   │   └── Migrations/
│   │   ├── Repositories/
│   │   │   ├── PhotoRepository.cs
│   │   │   ├── AlbumRepository.cs
│   │   │   └── UserRepository.cs
│   │   ├── Storage/
│   │   │   ├── LocalFileStorageService.cs
│   │   │   └── S3StorageService.cs (optional)
│   │   └── Identity/
│   │       └── ApplicationUser.cs
│   │
│   ├── LazyPhotos.Shared/                 # Shared Code
│   │   ├── DTOs/
│   │   ├── Models/
│   │   └── Helpers/
│   │
│   ├── LazyPhotos.Web/                    # Blazor Web App
│   │   ├── Components/
│   │   │   ├── Pages/
│   │   │   ├── Layout/
│   │   │   └── Shared/
│   │   ├── Services/
│   │   └── Program.cs
│   │
│   └── Lazy.Photos.App/                   # .NET MAUI App (Current)
│       ├── Platforms/
│       │   ├── Android/
│       │   └── iOS/
│       ├── Features/
│       └── MauiProgram.cs
│
├── tests/
│   ├── LazyPhotos.API.Tests/
│   ├── LazyPhotos.Core.Tests/
│   └── LazyPhotos.Infrastructure.Tests/
│
├── docker/
│   ├── Dockerfile
│   ├── docker-compose.yml
│   └── .dockerignore
│
└── docs/
```

## Technology Stack

### Backend
- **Framework**: ASP.NET Core 8+ (Web API)
- **ORM**: Entity Framework Core 8+
- **Database**: PostgreSQL 15+ (SQLite for dev/single-node)
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

## Core Components

### API Service (ASP.NET Core REST)
- RESTful endpoints for mobile clients
- JWT authentication with refresh tokens
- Rate limiting and request validation
- OpenAPI/Swagger documentation

### Authentication and Identity
- Local email/password first
- Optional OAuth later
- JWT tokens with refresh tokens
- Password hashing with BCrypt/Argon2
- Optional 2FA support

### Metadata Database
- Postgres preferred for production
- SQLite for dev/single-node
- EF Core migrations

### Object Storage
- Self-hosted S3-compatible (MinIO) or local filesystem
- Organized by user and date
- Multiple thumbnail sizes

### Background Workers
- ARM64-friendly image processing
- Thumbnail generation queue
- Import job processing

### Server Registration
- Bind self-hosted node to user account
- Server ID generation and claims

## API Contract (MVP)

### Authentication
- `POST /api/auth/register` - Register new user
- `POST /api/auth/login` - Login and get JWT
- `POST /api/auth/refresh` - Refresh JWT token
- `POST /api/devices/register` - Register device
- `POST /api/servers/claim` - Claim server for account

### Photos
- `GET /api/photos?cursor=&limit=` - List photos (paginated)
- `POST /api/photos/upload` - Upload photo(s)
- `GET /api/photos/{id}` - Get photo details
- `GET /api/photos/{id}/download?size=thumb|medium|original` - Download (signed URL)
- `PUT /api/photos/{id}` - Update metadata
- `DELETE /api/photos/{id}` - Delete photo

### Albums
- `GET /api/albums` - List albums
- `POST /api/albums` - Create album
- `GET /api/albums/{id}` - Get album details
- `PUT /api/albums/{id}` - Update album
- `DELETE /api/albums/{id}` - Delete album
- `POST /api/albums/{id}/photos` - Add photos
- `DELETE /api/albums/{id}/photos/{photoId}` - Remove photo

### Search
- `GET /api/search?q={query}&from={date}&to={date}` - Search photos

### Sync Feed
- `GET /api/feed?cursor=&limit=` - Delta feed for changes
- Returns `{ cursor, hasMore, items[{ type: photo|album|tombstone, data, updatedAt }]}`

### Sharing
- `POST /api/share-links` - Create share link
- `DELETE /api/share-links/{id}` - Delete share link

### Import
- `POST /api/import/google-photos/start` - Start import job
- `GET /api/import/jobs/{id}` - Get job status

OpenAPI specification: `docs/openapi-mvp.yaml`

## Image Pipeline

1. **Upload Initiation**
   - Chunked upload support for large files
   - Hash-first dedupe check

2. **File Validation**
   - Type validation (JPEG, PNG, HEIC, RAW)
   - Size limits
   - Optional malware scan

3. **EXIF Extraction**
   - Date taken, GPS coordinates
   - Camera model, exposure settings

4. **Thumbnail Generation**
   - Multiple sizes: 150px, 300px, 1080px
   - Quality optimized for low-power hardware

5. **Storage**
   - Original preserved
   - Thumbnails cached
   - Organized by date/user

## Observability

- Structured logs per request
- Health endpoints (`/health`, `/health/ready`)
- Basic metrics for upload latency and failures
- Tracing for image pipeline jobs (optional)

## Raspberry Pi Deployment

### Docker Compose

```yaml
version: '3.8'
services:
  api:
    build: .
    ports:
      - "5000:8080"
    environment:
      - ConnectionStrings__DefaultConnection=Host=db;Database=lazyphotos;Username=postgres;Password=...
      - ASPNETCORE_ENVIRONMENT=Production
    volumes:
      - ./photos:/app/photos
    depends_on:
      - db

  db:
    image: postgres:15-alpine
    environment:
      - POSTGRES_PASSWORD=...
      - POSTGRES_DB=lazyphotos
    volumes:
      - pgdata:/var/lib/postgresql/data

  web:
    build: ./LazyPhotos.Web
    ports:
      - "80:8080"
    depends_on:
      - api

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

### Recommended Hardware

- Raspberry Pi 4/5 (4GB+ RAM)
- Fast SD card or SSD via USB
- Optional: External HDD for photo storage

### Scaling Path

1. **Single Node**: API + workers + Postgres on same Pi
2. **Split Services**: Separate API from workers
3. **External Storage**: Move to MinIO cluster
4. **Cloud Scale**: Deploy to cloud with managed Postgres

## Decisions Needed

- Target Pi model and storage footprint
- MinIO vs local filesystem for objects
- Remote access strategy (reverse proxy, Tailscale, or VPN)
- Optional CDN integration for public sharing links
- Server claim and ownership model (single-owner vs shared admin)
- Data retention policy for deleted items

## Deliverables

- Architecture diagram with self-hosted baseline
- API contract (OpenAPI) - see `openapi-mvp.yaml`
- Raspberry Pi deployment guide and resource sizing
- Docker Compose configuration
