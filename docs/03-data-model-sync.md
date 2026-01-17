# Step 3 - Data Model and Sync

Core entities
- User, Device, Photo, Album, AlbumItem, Tag, ShareLink.

Photo fields (example)
- Id, UserId, StorageKey, Hash, FileName, SizeBytes.
- CapturedAt, UploadedAt, Location, CameraMake, CameraModel.
- Width, Height, DurationSeconds, MimeType.

Album fields (example)
- Id, UserId, Name, CreatedAt, UpdatedAt.

Local storage
- SQLite for metadata and sync state.
- Disk cache for thumbnails and recently viewed originals.

Sync strategy
- Local queue for uploads and edits.
- Delta API for server changes since cursor.
- Background workers to sync when network is available.
- Exponential backoff for retries.

Conflict handling
- Last-write-wins for metadata edits.
- Album membership is additive unless explicitly removed.
- Deletions use tombstones to propagate.

Offline behavior
- Read cached photos and albums.
- Queue edits and uploads for later.

Import (Google Photos)
- Support import via Google Takeout (zip) as the baseline flow.
- Optional direct import using Google Photos API if access and quotas allow.
- Map metadata to Photo fields, preserve CapturedAt and Location where possible.
- Recreate albums from Takeout metadata when available.
- Dedupe by content hash to avoid reuploading existing items.
- Track import jobs and progress per user/device.

Security
- Encrypt sensitive local storage.
- Use signed URLs for uploads and downloads.

Deliverables
- Data model document.
- Sync flow diagram.
- Local database schema.
