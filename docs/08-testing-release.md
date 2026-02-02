# Step 8 - Testing and Release

## Development Phases

### Phase 1: MVP (Core Features)
- [x] MAUI project setup
- [x] Photo grid with infinite scroll
- [x] Thumbnail generation (local)
- [ ] API project setup (ASP.NET Core)
- [ ] Database schema & EF Core setup
- [ ] User authentication (JWT)
- [ ] Basic photo upload/download endpoints
- [ ] Basic Blazor UI (upload, view grid)

### Phase 2: Enhanced Features
- [ ] Album management (CRUD)
- [ ] Search functionality
- [ ] EXIF metadata extraction
- [ ] Timeline view (grouped by date)
- [ ] Sync status indicators

### Phase 3: Advanced Features
- [ ] Google Photos import (Takeout)
- [ ] Shared links with expiry
- [ ] Face detection (optional)
- [ ] Advanced search (location, camera)
- [ ] Duplicate detection

### Phase 4: Polish & Optimization
- [ ] Performance tuning
- [ ] Raspberry Pi optimization
- [ ] Mobile offline mode
- [ ] Backup/restore tools
- [ ] Admin dashboard

## Testing Strategy

### Unit Tests

**What to Test**
- ViewModels and business logic
- Services (isolated with mocks)
- Use cases
- Data transformations

**Frameworks**
- xUnit or NUnit
- Moq for mocking
- FluentAssertions

```csharp
[Fact]
public async Task LoadPhotosUseCase_ReturnsPhotosFromCache()
{
    // Arrange
    var mockCache = new Mock<IPhotoCacheService>();
    mockCache.Setup(c => c.LoadCachedPhotosAsync())
             .ReturnsAsync(new List<PhotoItem> { /* test data */ });

    var useCase = new LoadPhotosUseCase(mockCache.Object, ...);

    // Act
    var result = await useCase.ExecuteAsync(CancellationToken.None);

    // Assert
    result.CachedPhotos.Should().NotBeEmpty();
}
```

### Integration Tests

**What to Test**
- API endpoints (full request/response)
- Database operations
- File storage operations
- Authentication flows

**Frameworks**
- ASP.NET Core TestServer
- Testcontainers for Postgres

### UI Tests

**What to Test**
- Core navigation flows
- Photo upload workflow
- Album creation
- Search functionality

**Frameworks**
- Appium (cross-platform)
- XCUITest (iOS)
- Espresso (Android)

### Coverage Targets

| Area | Target |
|------|--------|
| Core business logic | 80%+ |
| API controllers | 70%+ |
| ViewModels | 70%+ |
| UI tests | Critical paths |

## CI/CD Pipeline

### Build Pipeline

```yaml
# GitHub Actions example
name: Build and Test

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]

jobs:
  build:
    runs-on: macos-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Restore
        run: dotnet restore

      - name: Build
        run: dotnet build --no-restore

      - name: Test
        run: dotnet test --no-build --verbosity normal

  build-ios:
    runs-on: macos-latest
    steps:
      - uses: actions/checkout@v4
      - name: Build iOS
        run: dotnet build src/Lazy.Photos.App -f net10.0-ios -c Release

  build-android:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Build Android
        run: dotnet build src/Lazy.Photos.App -f net10.0-android -c Release
```

### Automated Tasks
- Code formatting check
- Static analysis (nullable warnings)
- Unit tests
- Integration tests
- Build for iOS and Android
- Versioning (GitVersion or similar)

## Release Process

### Mobile Apps

**iOS (TestFlight)**
1. Build Release configuration
2. Archive and sign with App Store certificate
3. Upload to App Store Connect
4. Distribute via TestFlight
5. Staged rollout (10% → 50% → 100%)

**Android (Play Store)**
1. Build Release APK/AAB
2. Sign with release keystore
3. Upload to Play Console
4. Internal testing → Closed beta → Production
5. Staged rollout

### Backend

**Docker Deployment**
1. Build Docker image
2. Push to container registry
3. Deploy to Raspberry Pi or cloud
4. Run database migrations
5. Health check verification

### Version Strategy

- **Semantic versioning**: MAJOR.MINOR.PATCH
- **Mobile**: Follow app store conventions
- **API**: Version in URL path (`/api/v1/...`)

## Beta Testing

### TestFlight (iOS)
- Internal testers (up to 100)
- External testers (up to 10,000)
- Crash reports and feedback

### Play Console (Android)
- Internal testing track
- Closed testing (invite only)
- Open testing
- Production with staged rollout

### Feedback Collection
- In-app feedback button
- Crash reporting (App Center or Sentry)
- Analytics (optional, privacy-respecting)

## Monitoring

### Mobile
- Crash reporting (Sentry, App Center)
- Performance metrics (startup time, scroll FPS)
- User analytics (opt-in)

### Backend
- Structured logging (Serilog)
- Health endpoints
- Upload success/failure rates
- API latency metrics
- Disk space monitoring (Pi)

### Alerts
- Server down
- High error rate
- Storage running low
- Database connection issues

## Release Checklist

### Pre-Release
- [ ] All tests passing
- [ ] Code review completed
- [ ] Security review (if applicable)
- [ ] Performance testing done
- [ ] Documentation updated
- [ ] Release notes written
- [ ] Version bumped

### Release
- [ ] Build artifacts created
- [ ] Signed with release certificates
- [ ] Uploaded to stores/registry
- [ ] Smoke tests in production
- [ ] Rollback plan ready

### Post-Release
- [ ] Monitor crash reports
- [ ] Monitor error rates
- [ ] Respond to user feedback
- [ ] Update known issues

## Deliverables

- Test plan and coverage targets
- CI/CD pipeline configuration
- Release checklist
- Post-launch monitoring dashboard
- Rollback procedures
