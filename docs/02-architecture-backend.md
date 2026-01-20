# Step 2 - Architecture and Backend

Overview
- Self-hosted-first backend that runs on low-power ARM64 hardware (Raspberry Pi) and can scale to cloud later.

Core components
- API service (ASP.NET Core REST) for mobile clients.
- Authentication and identity (local email/password first, optional OAuth later).
- Metadata database (Postgres preferred; SQLite for dev/single-node).
- Object storage for images (self-hosted S3-compatible like MinIO, or local filesystem for single-node).
- Background workers for image processing (ARM64-friendly).
- Optional edge cache or CDN when exposed publicly.
- Server registration service to bind a self-hosted node to a LazyPhotos account.

API contract (MVP)
- Auth: POST /auth/login, POST /auth/refresh, POST /devices/register, POST /servers/claim.
- Upload: POST /upload-sessions (hash-first dedupe, returns uploadUrl, chunkSize, alreadyExists), PUT /upload-sessions/{id}/chunks?offset=…, POST /upload-sessions/{id}/complete -> { photoId }.
- Photos/albums: GET /photos?cursor=&limit=, GET /photos/{id}, GET /photos/{id}/download?size=thumb|medium|original (signed URL), POST /photos/{id}/metadata, DELETE /photos/{id}; POST /albums, PUT /albums/{id}, POST /albums/{id}/items, DELETE /albums/{id}/items/{photoId}, GET /albums.
- Sync feed: GET /feed?cursor=&limit= -> { cursor, hasMore, items[{ type: photo|album|tombstone, data, updatedAt }]}.
- Sharing: POST /share-links { photoId? albumId?, expiresAt? } -> { url, token }, DELETE /share-links/{id}.
- Import tracking: POST /imports { source: takeout, hash } -> { importJobId }, GET /imports/{id} -> status/progress/errors.
- OpenAPI stub: docs/openapi-mvp.yaml.

Image pipeline
- Upload initiation and chunked upload support.
- File validation (type/size) and optional malware scan.
- EXIF extraction.
- Thumbnail generation (multiple sizes tuned for low-power hardware).
- Optional transcoding for bandwidth-friendly delivery.

Sync endpoints
- Upload session creation.
- Delta feed for changes since a cursor.
- Batch metadata updates.
- Delete and restore flows (tombstones).

Observability
- Structured logs per request.
- Health endpoints and basic metrics for upload latency and failures.
- Tracing for image pipeline jobs (optional for single-node).

Deployment
- Docker Compose on Raspberry Pi (ARM64 images).
- Data volumes on attached SSD/NAS.
- Path to scale: split API/workers, move storage to MinIO cluster or cloud.
- First-run flow: server owner signs in and claims the node, generating a server ID bound to the account.

Decisions needed
- Target Pi model and storage footprint.
- MinIO vs local filesystem for objects.
- Remote access strategy (reverse proxy, Tailscale, or VPN).
- Optional CDN integration for public sharing links.
- Server claim and ownership model (single-owner vs shared admin).
- Data retention policy for deleted items.

Deliverables
- Architecture diagram with self-hosted baseline.
- API contract (OpenAPI).
- Raspberry Pi deployment guide and resource sizing.
