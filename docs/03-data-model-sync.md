# Step 3 - Data Model and Sync

## Database Schema

### Users Table

```sql
CREATE TABLE Users (
    Id              UUID PRIMARY KEY,
    Username        VARCHAR(100) NOT NULL,
    Email           VARCHAR(255) NOT NULL UNIQUE,
    PasswordHash    VARCHAR(255) NOT NULL,
    CreatedAt       TIMESTAMP NOT NULL DEFAULT NOW(),
    LastLoginAt     TIMESTAMP,
    StorageQuotaBytes   BIGINT DEFAULT 10737418240,  -- 10GB default
    StorageUsedBytes    BIGINT DEFAULT 0,
    DeletedAt       TIMESTAMP
);
```

### Photos Table

```sql
CREATE TABLE Photos (
    Id              UUID PRIMARY KEY,
    UserId          UUID NOT NULL REFERENCES Users(Id),
    FileName        VARCHAR(255) NOT NULL,
    OriginalFileName VARCHAR(255) NOT NULL,
    FileSize        BIGINT NOT NULL,
    MimeType        VARCHAR(100) NOT NULL,
    Width           INT NOT NULL,
    Height          INT NOT NULL,
    TakenAt         TIMESTAMP,  -- From EXIF or file date
    UploadedAt      TIMESTAMP NOT NULL DEFAULT NOW(),
    StoragePath     VARCHAR(500) NOT NULL,
    ThumbnailPath   VARCHAR(500),
    Hash            VARCHAR(64) NOT NULL,  -- SHA256 for duplicate detection
    Latitude        DECIMAL(10, 8),
    Longitude       DECIMAL(11, 8),
    CameraModel     VARCHAR(100),
    FNumber         DECIMAL(4, 2),
    ExposureTime    VARCHAR(20),
    ISO             INT,
    FocalLength     DECIMAL(6, 2),
    IsDeleted       BOOLEAN DEFAULT FALSE,
    DeletedAt       TIMESTAMP,
    UpdatedAt       TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_photos_userid ON Photos(UserId);
CREATE INDEX idx_photos_hash ON Photos(Hash);
CREATE INDEX idx_photos_takenat ON Photos(TakenAt);
CREATE INDEX idx_photos_updatedat ON Photos(UpdatedAt);
```

### Albums Table

```sql
CREATE TABLE Albums (
    Id              UUID PRIMARY KEY,
    UserId          UUID NOT NULL REFERENCES Users(Id),
    Name            VARCHAR(255) NOT NULL,
    Description     TEXT,
    CoverPhotoId    UUID REFERENCES Photos(Id),
    CreatedAt       TIMESTAMP NOT NULL DEFAULT NOW(),
    UpdatedAt       TIMESTAMP NOT NULL DEFAULT NOW(),
    IsShared        BOOLEAN DEFAULT FALSE,
    IsDeleted       BOOLEAN DEFAULT FALSE,
    DeletedAt       TIMESTAMP
);

CREATE INDEX idx_albums_userid ON Albums(UserId);
```

### PhotoAlbums Table (Many-to-Many)

```sql
CREATE TABLE PhotoAlbums (
    PhotoId     UUID NOT NULL REFERENCES Photos(Id),
    AlbumId     UUID NOT NULL REFERENCES Albums(Id),
    AddedAt     TIMESTAMP NOT NULL DEFAULT NOW(),
    Position    INT DEFAULT 0,
    PRIMARY KEY (PhotoId, AlbumId)
);

CREATE INDEX idx_photoalbums_albumid ON PhotoAlbums(AlbumId);
```

### Tags Table

```sql
CREATE TABLE Tags (
    Id          UUID PRIMARY KEY,
    Name        VARCHAR(100) NOT NULL UNIQUE,
    CreatedAt   TIMESTAMP NOT NULL DEFAULT NOW()
);
```

### PhotoTags Table (Many-to-Many)

```sql
CREATE TABLE PhotoTags (
    PhotoId     UUID NOT NULL REFERENCES Photos(Id),
    TagId       UUID NOT NULL REFERENCES Tags(Id),
    PRIMARY KEY (PhotoId, TagId)
);

CREATE INDEX idx_phototags_tagid ON PhotoTags(TagId);
```

### SharedLinks Table

```sql
CREATE TABLE SharedLinks (
    Id          UUID PRIMARY KEY,
    AlbumId     UUID REFERENCES Albums(Id),
    PhotoId     UUID REFERENCES Photos(Id),
    Token       VARCHAR(64) NOT NULL UNIQUE,
    CreatedAt   TIMESTAMP NOT NULL DEFAULT NOW(),
    ExpiresAt   TIMESTAMP,
    ViewCount   INT DEFAULT 0,
    Password    VARCHAR(255),
    RevokedAt   TIMESTAMP,

    CHECK (AlbumId IS NOT NULL OR PhotoId IS NOT NULL)
);

CREATE INDEX idx_sharedlinks_token ON SharedLinks(Token);
```

### Devices Table

```sql
CREATE TABLE Devices (
    Id          UUID PRIMARY KEY,
    UserId      UUID NOT NULL REFERENCES Users(Id),
    Platform    VARCHAR(20) NOT NULL,  -- ios, android
    Model       VARCHAR(100),
    AppVersion  VARCHAR(20),
    CreatedAt   TIMESTAMP NOT NULL DEFAULT NOW(),
    LastSeenAt  TIMESTAMP
);

CREATE INDEX idx_devices_userid ON Devices(UserId);
```

### UploadSessions Table

```sql
CREATE TABLE UploadSessions (
    Id          UUID PRIMARY KEY,
    UserId      UUID NOT NULL REFERENCES Users(Id),
    DeviceId    UUID REFERENCES Devices(Id),
    Hash        VARCHAR(64) NOT NULL,
    SizeBytes   BIGINT NOT NULL,
    ChunkSize   INT DEFAULT 1048576,  -- 1MB default
    Status      VARCHAR(20) NOT NULL DEFAULT 'pending',
    CreatedAt   TIMESTAMP NOT NULL DEFAULT NOW(),
    CompletedAt TIMESTAMP,
    Error       TEXT
);

CREATE INDEX idx_uploadsessions_hash ON UploadSessions(Hash);
```

### GooglePhotosImportJobs Table

```sql
CREATE TABLE GooglePhotosImportJobs (
    Id              UUID PRIMARY KEY,
    UserId          UUID NOT NULL REFERENCES Users(Id),
    Status          VARCHAR(20) NOT NULL DEFAULT 'pending',
    TotalPhotos     INT DEFAULT 0,
    ImportedPhotos  INT DEFAULT 0,
    FailedPhotos    INT DEFAULT 0,
    SkippedPhotos   INT DEFAULT 0,
    StartedAt       TIMESTAMP,
    CompletedAt     TIMESTAMP,
    ErrorMessage    TEXT
);

CREATE INDEX idx_importjobs_userid ON GooglePhotosImportJobs(UserId);
```

## Core Entities (C# Models)

```csharp
public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public long StorageQuotaBytes { get; set; }
    public long StorageUsedBytes { get; set; }
    public DateTime? DeletedAt { get; set; }
}

public class Photo
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string FileName { get; set; }
    public string OriginalFileName { get; set; }
    public long FileSize { get; set; }
    public string MimeType { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public DateTime? TakenAt { get; set; }
    public DateTime UploadedAt { get; set; }
    public string StoragePath { get; set; }
    public string? ThumbnailPath { get; set; }
    public string Hash { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? CameraModel { get; set; }
    public decimal? FNumber { get; set; }
    public string? ExposureTime { get; set; }
    public int? ISO { get; set; }
    public decimal? FocalLength { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class Album
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public Guid? CoverPhotoId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsShared { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
```

## Local Storage (Mobile App - SQLite)

```sql
-- Photos table (mirrors server with sync metadata)
CREATE TABLE Photos (
    Id              TEXT PRIMARY KEY,
    UserId          TEXT NOT NULL,
    FileName        TEXT NOT NULL,
    FileSize        INTEGER NOT NULL,
    MimeType        TEXT NOT NULL,
    Width           INTEGER NOT NULL,
    Height          INTEGER NOT NULL,
    TakenAt         TEXT,
    UploadedAt      TEXT NOT NULL,
    Hash            TEXT NOT NULL,
    Latitude        REAL,
    Longitude       REAL,
    CameraModel     TEXT,
    IsDeleted       INTEGER DEFAULT 0,
    UpdatedAt       TEXT NOT NULL,
    CursorUpdatedAt TEXT,
    ThumbPath       TEXT,
    MediumPath      TEXT,
    SyncStatus      TEXT DEFAULT 'synced'
);

-- Albums table
CREATE TABLE Albums (
    Id              TEXT PRIMARY KEY,
    UserId          TEXT NOT NULL,
    Name            TEXT NOT NULL,
    Description     TEXT,
    CoverPhotoId    TEXT,
    CreatedAt       TEXT NOT NULL,
    UpdatedAt       TEXT NOT NULL,
    IsShared        INTEGER DEFAULT 0,
    IsDeleted       INTEGER DEFAULT 0
);

-- Album items
CREATE TABLE AlbumItems (
    AlbumId     TEXT NOT NULL,
    PhotoId     TEXT NOT NULL,
    Position    INTEGER DEFAULT 0,
    AddedAt     TEXT NOT NULL,
    PRIMARY KEY (AlbumId, PhotoId)
);

-- Pending uploads queue
CREATE TABLE PendingUploads (
    Id              TEXT PRIMARY KEY,
    LocalPath       TEXT NOT NULL,
    Hash            TEXT NOT NULL,
    Status          TEXT DEFAULT 'pending',
    RetryAt         TEXT,
    RetryCount      INTEGER DEFAULT 0,
    Error           TEXT,
    CreatedAt       TEXT NOT NULL
);

-- Sync state
CREATE TABLE SyncState (
    Key     TEXT PRIMARY KEY,
    Value   TEXT NOT NULL
);
```

### Disk Cache

- **Thumbnails**: `{CacheDir}/thumbs/{PhotoId}.jpg`
- **Medium images**: `{CacheDir}/medium/{PhotoId}.jpg`
- **Originals**: On-demand download, not cached by default

## Sync Strategy

### Bootstrap Sync

1. `GET /api/photos?limit=100` - First page with cursor
2. Store in local SQLite
3. Continue fetching until `hasMore: false`

### Delta Sync

1. `GET /api/feed?cursor={lastCursor}&limit=50`
2. Returns `{ cursor, hasMore, items }`
3. Apply with last-write-wins on `UpdatedAt`
4. Propagate tombstones for deletes

### Upload Flow

1. Compute SHA256 hash
2. `POST /api/upload-sessions` with hash
3. If `alreadyExists: true`, skip (dedupe)
4. Upload chunks via `PUT /upload-sessions/{id}/chunks`
5. Complete with `POST /upload-sessions/{id}/complete`

### Background Workers

- Resume queued uploads when network available
- Exponential backoff: 1s, 2s, 4s, 8s (max 5min)
- Respect battery and Wi-Fi-only settings

## Conflict Handling

- **Metadata**: Last-write-wins using `UpdatedAt`
- **Album membership**: Additive unless explicitly removed
- **Deletes**: Soft delete with tombstones in sync feed

## Offline Behavior

- Read from SQLite/cache
- Queue edits/uploads
- Replay when online

## Google Photos Import

### Supported Methods

1. **Google Takeout ZIP** (recommended)
2. **Google Photos API** (optional, rate-limited)

### Metadata Mapping

| Google Photos | Lazy Photos |
|---------------|-------------|
| photoTakenTime | TakenAt |
| geoData | Latitude/Longitude |
| title | FileName |
| description | (metadata) |

### Deduplication

- Compare by SHA256 hash
- Skip existing photos
- Track progress per import job

## Security

- Encrypt sensitive local storage
- Use signed URLs for uploads/downloads
- TLS for all network calls

## Deliverables

- Data model document (this file)
- Sync flow diagram
- Local database schema
- EF Core migrations
