# Step 6 - Performance and UX Polish

Performance targets
- Smooth 60 fps scrolling on large libraries.
- Minimal memory spikes when paging.
- Baseline performance on iPhone 8-class hardware.

Image loading
- Use low-res placeholders.
- Prefetch nearby thumbnails.
- Evict old cache entries.

List performance
- CollectionView with virtualization.
- Avoid heavy layout work in item templates.

UX polish
- Loading states and empty states.
- Subtle transitions for entering viewer.
- Clear error and retry flows.

Network efficiency
- Chunked uploads for large files.
- Avoid re-uploading identical files (hash check).

Older device support (iPhone 8 baseline)
- Test on A11/2GB RAM constraints and enforce cache limits.
- Decode images to display size to avoid large bitmap allocations.
- Keep animations lightweight and respect reduced-motion settings.
- Throttle background sync to avoid CPU and battery spikes.

Deliverables
- Performance test plan.
- Benchmark results on mid-range devices.
