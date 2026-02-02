# Step 5 - Core Feature Build-Out

## Photo Management

### Gallery Grid
- Virtualized photo grid with incremental loading (CollectionView)
- Initial load shows first 30 photos
- Scrolling loads additional pages of 30
- Pull to refresh and error states
- Grouped by date with section headers
- Responsive columns (2-5 based on screen width)

### Photo Viewer
- Fullscreen viewer with swipe navigation
- Pinch to zoom with pan support
- Share and download actions
- Photo metadata display (date, location, camera)
- Delete confirmation

### Upload
- Single and batch upload support
- Chunked uploads for large files
- Progress indicator with cancel option
- Background upload continuation
- Auto-upload toggle in settings

## Album Management

### Features
- Create, edit, and delete albums
- Add or remove photos from albums
- Set album cover photo
- Album sharing with expiry links
- Sort albums by name/date

### UI
- Album grid with cover thumbnails
- Album detail view with photo grid
- Multi-select for batch operations

## Search & Filter

### Query Types
- Search by date range
- Search by location (map integration optional)
- Search by tags
- Search by camera model
- Full-text search (filenames, descriptions)

### UI
- Search bar with suggestions
- Recent searches history
- Filter chips for quick access
- Results grid with highlighting

## Backup and Sync

### Features
- Auto upload toggle
- Wi-Fi only option
- Upload quality settings (original/compressed)
- Upload progress and retry UI
- Last successful backup status
- Sync conflict resolution

### Status Indicators
- Green dot: Synced to server
- Orange dot: Pending upload
- Red dot: Upload failed (tap to retry)

## Sharing

### Share Links
- Create share link with optional expiry
- Password protection (optional)
- View count tracking
- Revoke share links

### Native Integration
- iOS share sheet
- Android share intent
- Copy link to clipboard

## Settings

### Account
- User profile display
- Sign out
- Account deletion

### Upload Settings
- Auto upload toggle
- Wi-Fi only
- Upload quality (Original/High/Medium)
- Include location data

### Storage
- Cache size display
- Clear cache option
- Storage quota usage

### Server
- Server connection status
- Server URL configuration
- Connection test

## API Endpoints

### Authentication
- `POST /api/auth/register` - Register new user
- `POST /api/auth/login` - Login and get JWT
- `POST /api/auth/refresh` - Refresh JWT token

### Photos
- `GET /api/photos` - List photos (paginated)
- `POST /api/photos/upload` - Upload photo(s)
- `GET /api/photos/{id}` - Get photo details
- `GET /api/photos/{id}/download` - Download original
- `GET /api/photos/{id}/thumbnail` - Get thumbnail
- `DELETE /api/photos/{id}` - Delete photo
- `PUT /api/photos/{id}` - Update metadata

### Albums
- `GET /api/albums` - List albums
- `POST /api/albums` - Create album
- `GET /api/albums/{id}` - Get album details
- `PUT /api/albums/{id}` - Update album
- `DELETE /api/albums/{id}` - Delete album
- `POST /api/albums/{id}/photos` - Add photos to album
- `DELETE /api/albums/{id}/photos/{photoId}` - Remove photo

### Search
- `GET /api/search?q={query}&from={date}&to={date}` - Search photos

### Sharing
- `POST /api/share-links` - Create share link
- `GET /api/share-links/{token}` - Access shared content
- `DELETE /api/share-links/{id}` - Delete share link

### Import
- `POST /api/import/google-photos/start` - Start import job
- `GET /api/import/jobs/{id}` - Get job status

### Sync
- `GET /api/feed?cursor=&limit=` - Delta feed for changes

## Google Photos Import

### Methods
1. **Google Takeout** (Recommended)
   - Upload ZIP file to server
   - Server processes in background
   - Progress tracking via import job

2. **Google Photos API** (Optional)
   - OAuth2 authentication flow
   - Rate-limited batch download
   - Resume capability

### Features
- Metadata preservation (dates, locations)
- Album structure recreation
- Duplicate detection by hash
- Progress tracking with error recovery

## Deliverables

- Feature complete MVP screens
- API wiring for all core actions
- Offline support for read operations
- Background upload implementation
