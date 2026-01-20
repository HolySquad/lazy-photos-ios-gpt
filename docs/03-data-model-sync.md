# Step 3 - Data Model and Sync

Core entities
- User: Id (uuid), Email, PasswordHash, CreatedAt, DeletedAt.
- Device: Id (uuid), UserId, Platform (ios|android), Model, AppVersion, CreatedAt, LastSeenAt.
- Photo: Id (uuid), UserId, DeviceId, StorageKey, Hash (sha256), FileName, SizeBytes, CapturedAt, UploadedAt, Width, Height, MimeType, LocationLat, LocationLon, CameraMake, CameraModel, IsDeleted, DeletedAt, UpdatedAt.
- Album: Id (uuid), UserId, Name, CoverPhotoId, CreatedAt, UpdatedAt, IsDeleted, DeletedAt.
- AlbumItem: AlbumId, PhotoId, Position, AddedAt (PK AlbumId+PhotoId).
- Tag/PhotoTag: Tag(Id, UserId, Label, CreatedAt), PhotoTag(PhotoId, TagId).
- ShareLink: Id, UserId, AlbumId?, PhotoId?, Token, ExpiresAt, CreatedAt, RevokedAt.
- UploadSession: Id (uuid), UserId, DeviceId, Hash, SizeBytes, ChunkSize, Status(pending|uploading|completed|failed), CreatedAt, CompletedAt, Error.
- DeltaCursor: opaque string derived from UpdatedAt/tombstone checkpoint.

Local storage (SQLite + disk cache)
- Tables: Photos (fields mirroring Photo + CursorUpdatedAt, ThumbPath, MediumPath), Albums, AlbumItems, Tags, PhotoTags, ShareLinks, PendingUploads (UploadSessionId, Hash, State, RetryAt), SyncState (singleton row with Cursor).
- Disk cache: thumbnails + medium images keyed by PhotoId and size; originals on-demand.

Sync strategy
- Bootstrap: GET /photos?limit=… returns first page for gallery plus cursor.
- Deltas: GET /feed?cursor=… returns items [{type: photo|album|tombstone, data, updatedAt}], apply with last-write-wins on UpdatedAt and propagate tombstones.
- Uploads: hash-first dedupe via upload session; if alreadyExists=true, skip chunk upload and just attach metadata/device.
- Background workers: resume queued uploads/edits when network is available; exponential backoff on failures.

Conflict handling
- Last-write-wins on metadata using UpdatedAt; album membership additive unless explicitly removed.
- Deletes are soft (IsDeleted + tombstone in feed) and synced across devices.

Offline behavior
- Read from SQLite/cache; queue edits/uploads; replay when online.

Import (Google Photos)
- Baseline: Google Takeout zip; optional direct Photos API if quotas allow.
- Map EXIF/metadata to Photo fields, preserve CapturedAt/Location; recreate albums when metadata present.
- Dedupe by content hash; track import job progress per user/device.

Security
- Encrypt sensitive local storage; use signed URLs for uploads/downloads.

Deliverables
- Data model document.
- Sync flow diagram.
- Local database schema.
