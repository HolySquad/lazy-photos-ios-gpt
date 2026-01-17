# Step 4 - .NET MAUI App Foundation

Project structure (example)
- src/LazyPhotos.App (UI and app shell)
- src/LazyPhotos.Core (models, interfaces, logic)
- src/LazyPhotos.Data (API and storage)

App basics
- Shell navigation with tabs for Photos, Albums, Search, Settings.
- Dependency injection via MauiAppBuilder.
- Centralized theme resources and styles.

Responsive UI (iPhone mini to iPad Pro)
- Use DeviceInfo.Idiom and window size to switch layouts.
- Define breakpoints (compact/medium/expanded) and adjust grid columns.
- Support split-view layouts on tablets (list + detail).
- Scale typography and spacing via named sizes in the theme.
- Respect safe areas and notch/pill on all devices.

State and MVVM
- Use CommunityToolkit.Mvvm for view models.
- Keep view models thin and testable.

Dotnet best practices
- Enable nullable reference types and fix warnings early.
- Prefer async/await end-to-end with CancellationToken support.
- Use DI for services; avoid service locator patterns.
- Centralize logging and use structured logs for errors.
- Validate inputs and handle exceptions at boundaries.
- Keep UI bindings compiled where possible and avoid heavy work on UI thread.
- Format code using Microsoft .NET conventions and standard .editorconfig rules.

Networking
- HttpClientFactory with typed clients.
- Resiliency policies for retries and timeouts.

Local storage
- SQLite for metadata.
- File cache service for thumbnails and originals.

Background work
- iOS BGTaskScheduler and Android WorkManager wrappers.
- Respect OS constraints and battery.

Cross-platform services
- MediaPicker for photo selection.
- Permissions handling for Photos and Camera.

Deliverables
- Solution skeleton and project references.
- Baseline navigation and theming.
- Core services for API, storage, and caching.
