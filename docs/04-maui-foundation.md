# Step 4 - .NET MAUI App Foundation

## Current Project Structure

```
src/
├── Lazy.Photos.App/               # MAUI app shell, DI, resources
│   ├── Features/                  # Feature-based organization
│   │   ├── Photos/                # Main gallery feature
│   │   │   ├── Models/            # PhotoItem, PhotoSection
│   │   │   ├── Services/          # PhotoLibraryService, ThumbnailService
│   │   │   ├── UseCases/          # ILoadPhotosUseCase, ICachePersistenceUseCase
│   │   │   ├── Permissions/       # PhotoLibraryPermissions
│   │   │   ├── MainPage.xaml      # Gallery grid view
│   │   │   └── MainPageViewModel.cs
│   │   ├── SignIn/                # Authentication
│   │   ├── Search/                # Search functionality
│   │   └── DevStats/              # Debug statistics
│   ├── Platforms/                 # Platform-specific code
│   │   ├── iOS/
│   │   └── Android/
│   ├── Converters/                # Value converters
│   ├── Resources/                 # Assets, fonts, images
│   ├── MauiProgram.cs             # DI container setup
│   └── AppShell.xaml              # Navigation structure
├── Lazy.Photos.Core/              # Domain layer
└── Lazy.Photos.Data/              # Infrastructure layer
```

## App Navigation

Shell navigation with tabs:

```xml
<Shell>
    <TabBar>
        <ShellContent Title="Photos" ContentTemplate="{DataTemplate local:MainPage}" />
        <ShellContent Title="Albums" ContentTemplate="{DataTemplate local:AlbumsPage}" />
        <ShellContent Title="Search" ContentTemplate="{DataTemplate local:SearchPage}" />
        <ShellContent Title="Settings" ContentTemplate="{DataTemplate local:SettingsPage}" />
    </TabBar>
</Shell>
```

## Dependency Injection

All services registered in `MauiProgram.cs`:

```csharp
public static MauiApp CreateMauiApp()
{
    var builder = MauiApp.CreateBuilder();
    builder.UseMauiApp<App>()
           .UseMauiCommunityToolkit();

    // Services
    builder.Services.AddSingleton<IPhotoLibraryService, PhotoLibraryService>();
    builder.Services.AddSingleton<IPhotoStateRepository, PhotoStateRepository>();
    builder.Services.AddSingleton<IThumbnailService, ThumbnailService>();
    builder.Services.AddSingleton<IPaginationManager, PaginationManager>();
    builder.Services.AddSingleton<IPhotoCacheService, PhotoCacheService>();

    // Use Cases
    builder.Services.AddTransient<ILoadPhotosUseCase, LoadPhotosUseCase>();
    builder.Services.AddTransient<ICachePersistenceUseCase, CachePersistenceUseCase>();

    // ViewModels & Pages
    builder.Services.AddTransient<MainPageViewModel>();
    builder.Services.AddTransient<MainPage>();

    return builder.Build();
}
```

## Responsive UI (iPhone mini to iPad Pro)

### Breakpoints

| Breakpoint | Width | Columns | Device Example |
|------------|-------|---------|----------------|
| Compact    | ≤400  | 2       | iPhone mini    |
| Medium     | ≤700  | 3       | iPhone Pro     |
| Large      | ≤1100 | 4       | iPad           |
| Expanded   | >1100 | 5       | iPad Pro       |

### Implementation

```csharp
protected override void OnSizeAllocated(double width, double height)
{
    base.OnSizeAllocated(width, height);

    var columns = width switch
    {
        <= 400 => 2,
        <= 700 => 3,
        <= 1100 => 4,
        _ => 5
    };

    // Calculate cell size based on available width
    const double horizontalPadding = 16;
    const double spacing = 4;
    var totalSpacing = (columns - 1) * spacing;
    var available = Math.Max(0, width - horizontalPadding - totalSpacing);
    var cellSize = Math.Floor(available / columns);

    if (BindingContext is MainPageViewModel vm)
        vm.UpdateGrid(columns, cellSize);
}
```

- Use `DeviceInfo.Idiom` and window size to switch layouts
- Support split-view layouts on tablets (list + detail)
- Scale typography and spacing via named sizes in theme
- Respect safe areas and notch/pill on all devices

## State and MVVM

Use CommunityToolkit.Mvvm for view models:

```csharp
public partial class MainPageViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<PhotoItem> photos = new();

    [ObservableProperty]
    private bool isBusy;

    [RelayCommand]
    private async Task RefreshAsync()
    {
        // Load photos
    }
}
```

Keep view models thin and testable.

## .NET Best Practices

- **Nullable reference types**: Enable and fix warnings early
- **Async/await**: End-to-end with `CancellationToken` support
- **DI for services**: Avoid service locator patterns
- **Centralized logging**: Use structured logs for errors
- **Input validation**: Handle exceptions at boundaries
- **UI bindings**: Keep compiled where possible, avoid heavy work on UI thread
- **Code formatting**: Follow Microsoft .NET conventions and `.editorconfig` rules

## Networking

### HttpClientFactory with Typed Clients

```csharp
builder.Services.AddHttpClient<IPhotosApiClient, PhotosApiClient>(client =>
{
    client.BaseAddress = new Uri("https://api.lazyphotos.local");
});
```

### Resiliency Policies

- Retries with exponential backoff
- Timeouts
- Circuit breaker for failing endpoints

## Local Storage

### SQLite for Metadata

```csharp
var dbPath = Path.Combine(FileSystem.AppDataDirectory, "photos-cache.db3");
```

### File Cache for Thumbnails

```csharp
var cacheDir = Path.Combine(FileSystem.CacheDirectory, "thumbnails");
```

## Background Work

### iOS

- BGTaskScheduler for background uploads
- Respect OS constraints and battery

### Android

- WorkManager for background uploads
- Handle Doze mode and battery optimization

## Cross-Platform Services

- **MediaPicker**: For photo selection
- **Permissions**: Handling for Photos and Camera
- **SecureStorage**: For tokens and sensitive data
- **Connectivity**: Network state monitoring

## Platform-Specific Code

Use conditional compilation:

```csharp
#if IOS
    // iOS-specific code using PhotoKit (PHAsset)
#elif ANDROID
    // Android-specific code using MediaStore
#endif
```

Platform-specific files in `Platforms/{iOS|Android}/` directories.

## Decisions Needed

- Confirm feature folder naming and scope for MAUI app
- Choose image caching strategy (FFImageLoading vs custom)

## Deliverables

- Solution skeleton and project references
- Baseline navigation and theming
- Core services for API, storage, and caching
- Feature-based app folder organization under `Features/`
