# CLAUDE.md - Project Guidelines for AI Assistants

## Project Overview

**Lazy Photos** is a self-hosted photo management solution with Google Photos import capability. It includes a cross-platform .NET MAUI mobile app (iOS/Android), a Blazor web app, and an ASP.NET Core backend. The app syncs photos between devices and a self-hosted backend, with a focus on performance on constrained hardware (Raspberry Pi backend, iPhone 8-class devices).

## Tech Stack

### Mobile App (Current Focus)
- **.NET MAUI 10.0** - Cross-platform framework (iOS 15.0+, Android 21.0+)
- **C# 12** - Modern C# with nullable reference types, file-scoped namespaces
- **CommunityToolkit.Mvvm** - MVVM implementation with source generators
- **CommunityToolkit.Maui** - Extended MAUI components
- **Microsoft.Data.Sqlite** - Local caching layer
- **XAML** - Declarative UI layouts

### Backend (Planned)
- **ASP.NET Core 8+** - Web API
- **Entity Framework Core 8+** - ORM
- **PostgreSQL 15+** - Database (SQLite for dev)
- **Hangfire** - Background jobs
- **Docker** - Containerization

### Web (Planned)
- **Blazor** - Web UI
- **MudBlazor** - UI components

## Architecture

### Clean Architecture + SOLID Principles

```
src/
├── Lazy.Photos.App/           # Presentation layer (MAUI app)
│   ├── Features/              # Feature-based organization
│   │   ├── Photos/            # Main gallery (ViewModels, Services, UseCases)
│   │   ├── SignIn/            # Authentication
│   │   ├── Search/            # Search functionality
│   │   └── DevStats/          # Debug statistics
│   ├── Platforms/             # Platform-specific code (iOS/Android)
│   └── MauiProgram.cs         # DI container setup
├── Lazy.Photos.Core/          # Domain layer (models, interfaces)
└── Lazy.Photos.Data/          # Infrastructure layer (API clients, contracts)
```

### Layer Responsibilities

- **Presentation**: ViewModels, XAML views, navigation
- **Application**: Use cases (`ILoadPhotosUseCase`, `ICachePersistenceUseCase`)
- **Domain**: `PhotoItem`, `PhotoSection` models
- **Infrastructure**: Services, repositories, platform integrations

## Code Conventions

### Naming

- `PascalCase` for public members, types, and methods
- `_camelCase` for private fields (underscore prefix)
- `IPrefixName` for interfaces
- Feature folders use singular names (`Photos`, `SignIn`)

### Async Patterns

```csharp
// Always use async/await with cancellation tokens
public async Task LoadPhotosAsync(CancellationToken cancellationToken = default)
{
    await _service.DoWorkAsync(cancellationToken).ConfigureAwait(false);
}
```

### Dependency Injection

- All services registered in `MauiProgram.cs`
- Depend on interfaces, never concrete implementations
- Use constructor injection exclusively

```csharp
public class MyViewModel
{
    private readonly IMyService _service;

    public MyViewModel(IMyService service)
    {
        _service = service;
    }
}
```

### MVVM with CommunityToolkit

```csharp
public partial class MyViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = string.Empty;

    [RelayCommand]
    private async Task LoadDataAsync() { }
}
```

## Build & Run

```bash
# Build solution
dotnet build Lazy.Photos.sln

# Build iOS (requires Mac with Xcode)
dotnet build src/Lazy.Photos.App/Lazy.Photos.App.csproj -f net10.0-ios

# Build Android
dotnet build src/Lazy.Photos.App/Lazy.Photos.App.csproj -f net10.0-android
```

## Key Files

| File | Purpose |
|------|---------|
| `src/Lazy.Photos.App/MauiProgram.cs` | DI registration, app bootstrap |
| `src/Lazy.Photos.App/AppShell.xaml` | Navigation structure |
| `src/Lazy.Photos.App/Features/Photos/MainPageViewModel.cs` | Main gallery logic |
| `src/Lazy.Photos.App/Features/Photos/UseCases/` | Business logic orchestration |
| `src/Lazy.Photos.App/Features/Photos/Services/` | Infrastructure services |
| `docs/` | Architecture documentation |

## Performance Constraints

The app targets constrained hardware - optimize accordingly:

- **Memory**: 32MB throttle threshold for thumbnail generation
- **Pagination**: 30 items per page default
- **Thumbnails**: Prioritize visible items, low-quality placeholders for off-screen
- **Threading**: Use chunked processing (6 items per chunk) with semaphores
- **Cancellation**: Always support `CancellationToken` for interruptible operations
- **Target device**: iPhone 8-class hardware (A11/2GB RAM)

## Platform-Specific Code

Use conditional compilation for platform differences:

```csharp
#if IOS
    // iOS-specific code using PhotoKit (PHAsset)
#elif ANDROID
    // Android-specific code using MediaStore
#endif
```

Platform code lives in `Platforms/{iOS|Android}/` directories.

## Documentation

Consult `/docs/` before making architectural decisions:

- `01-requirements.md` - Project vision and constraints
- `02-architecture-backend.md` - Backend API and infrastructure
- `03-data-model-sync.md` - Database schema and sync strategy
- `04-maui-foundation.md` - MAUI patterns and structure
- `05-core-features.md` - Feature specifications
- `06-performance-ux.md` - Performance optimization
- `07-security-compliance.md` - Security measures
- `08-testing-release.md` - Testing and release strategy
- `AGENTS.md` - AI agent guidelines

## Common Patterns

### Creating a New Feature

1. Create folder under `Features/YourFeature/`
2. Add ViewModel inheriting `ObservableObject`
3. Add XAML page with `x:DataType` binding
4. Register in `MauiProgram.cs` and `AppShell.xaml`
5. Extract use cases for complex business logic

### Adding a Service

1. Define interface in `Core` or feature folder
2. Implement in feature's `Services/` directory
3. Register as singleton/transient in `MauiProgram.cs`
4. Inject via constructor

### State Management

Use `IPhotoStateRepository` pattern for thread-safe collections:

```csharp
public interface IPhotoStateRepository
{
    void AddOrUpdate(PhotoItem item);
    IReadOnlyList<PhotoItem> GetOrderedPhotos();
    PhotoItem? GetByKey(string uniqueKey);
}
```

## What to Avoid

- Service locator pattern - use DI exclusively
- `async void` except for event handlers
- Blocking calls (`.Result`, `.Wait()`) on async methods
- Platform code outside `Platforms/` or `#if` blocks
- Hardcoded magic numbers - use constants
- Ignoring `CancellationToken` in async methods

## Testing

Currently no test project exists. When adding tests:

- Use xUnit or NUnit
- Mock interfaces for unit tests
- Test use cases independently from ViewModels
- Focus on business logic, not XAML bindings

## Self-Hosted Backend Guidelines

When implementing the backend:

- Target Raspberry Pi 4/5 (ARM64, 4GB+ RAM)
- Use Docker Compose for deployment
- PostgreSQL for production, SQLite for dev
- Limit concurrent operations (2 for thumbnail generation)
- Use chunked uploads (1MB chunks)
- Implement hash-first deduplication
