# Step 6 - Performance and UX Polish

## Performance Targets

- **Scrolling**: Smooth 60fps on large libraries (10k+ photos)
- **Memory**: Minimal spikes when paging, stay under 100MB working set
- **Startup**: Cold start under 2 seconds
- **Baseline hardware**: iPhone 8 class (A11/2GB RAM)

## Mobile Performance Optimizations

### Image Loading Strategy

1. **Low-res placeholders first**
   - Show 150px thumbnails during scroll
   - Upgrade to 300px when scroll stops

2. **Prioritized loading**
   - Visible items loaded first
   - Off-screen items use low quality
   - Cancel pending loads on fast scroll

3. **Cache management**
   - Memory cache for visible thumbnails
   - Disk cache with size limits (default 100MB)
   - LRU eviction for old entries

### Implementation

```csharp
public async Task StartThumbnailFillAsync(
    IList<PhotoItem> photos,
    int visibleStart,
    int visibleEnd,
    Func<bool> isScrolling,
    CancellationToken ct)
{
    // Prioritize visible items
    var visibleItems = photos.Skip(visibleStart).Take(visibleEnd - visibleStart + 1);

    // Load visible first
    foreach (var photo in visibleItems)
    {
        if (ct.IsCancellationRequested) return;
        await LoadThumbnailAsync(photo, highQuality: !isScrolling());
    }

    // Then load nearby items
    // ...
}
```

## List Performance

### CollectionView Virtualization

- Use `CollectionView` with `ItemsLayout` for recycling
- Avoid heavy layout work in item templates
- Use simple bindings and compiled XAML

### Item Template Best Practices

```xml
<DataTemplate x:DataType="models:PhotoItem">
    <Grid HeightRequest="{Binding CellSize, Source={x:Reference Page}}"
          WidthRequest="{Binding CellSize, Source={x:Reference Page}}">
        <Image Source="{Binding ThumbnailSource}"
               Aspect="AspectFill" />
        <!-- Minimal sync indicator -->
        <Ellipse IsVisible="{Binding IsSynced}"
                 Fill="Green" WidthRequest="8" HeightRequest="8" />
    </Grid>
</DataTemplate>
```

### Avoid

- Complex layouts in templates
- Converters with heavy computation
- Large images not scaled to display size
- Nested ScrollViews

## Memory Management

### Thumbnail Generation

- **Throttle**: 32MB memory threshold
- **Chunks**: Process 6 items per chunk
- **Semaphores**: Limit concurrent operations

```csharp
private readonly SemaphoreSlim _thumbnailSemaphore = new(2);
private const int MemoryThrottleMB = 32;

public async Task GenerateThumbnailAsync(PhotoItem photo)
{
    await _thumbnailSemaphore.WaitAsync();
    try
    {
        if (GetAvailableMemoryMB() < MemoryThrottleMB)
            await Task.Delay(100); // Throttle

        // Generate thumbnail
    }
    finally
    {
        _thumbnailSemaphore.Release();
    }
}
```

### Image Sizing

- Decode images to display size only
- Never load full-resolution for thumbnails
- Use platform-specific APIs for efficient decoding

```csharp
#if IOS
var options = new PHImageRequestOptions
{
    DeliveryMode = PHImageRequestOptionsDeliveryMode.FastFormat,
    ResizeMode = PHImageRequestOptionsResizeMode.Fast
};
#endif
```

## Network Efficiency

### Uploads

- **Chunked uploads**: 1MB chunks for large files
- **Hash-first dedupe**: Skip upload if file exists
- **Resume capability**: Track uploaded chunks

### Downloads

- **Progressive loading**: Low quality first
- **Signed URLs**: Secure, time-limited access
- **Connection reuse**: HTTP/2 multiplexing

## UX Polish

### Loading States

- Skeleton screens during initial load
- Progress indicators for uploads
- Shimmer effect for loading thumbnails

### Empty States

- Friendly message when no photos
- Call-to-action for first upload
- Illustration/icon

### Error Handling

- Clear error messages
- Retry buttons with exponential backoff
- Offline mode indicators

### Transitions

- Subtle fade for photo viewer entry
- Shared element transitions (optional)
- Respect reduced motion settings

## Older Device Support (iPhone 8 Baseline)

### Constraints

- A11 Bionic chip
- 2GB RAM
- iOS 15.0 minimum

### Optimizations

- Enforce strict cache limits
- Reduce animation complexity
- Throttle background sync
- Lower quality thumbnails
- Disable optional visual effects

### Testing

- Test on actual iPhone 8 or simulator
- Monitor memory usage with Instruments
- Profile CPU during scroll

## Raspberry Pi Backend Optimization

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

### Recommendations

- Limit concurrent thumbnail generation
- Use quality/size tradeoffs
- Enable gzip compression
- Optimize database queries with indexes

## Deliverables

- Performance test plan
- Benchmark results on mid-range devices
- Memory profiling report
- Optimization checklist
