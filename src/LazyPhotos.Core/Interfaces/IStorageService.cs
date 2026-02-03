namespace LazyPhotos.Core.Interfaces;

public interface IStorageService
{
	Task<string> SaveChunkAsync(Guid sessionId, long offset, Stream content, CancellationToken cancellationToken = default);
	Task<string> FinalizeUploadAsync(Guid sessionId, string storageKey, CancellationToken cancellationToken = default);
	Task DeleteSessionFilesAsync(Guid sessionId, CancellationToken cancellationToken = default);
	Task<bool> ValidateChunkAsync(Guid sessionId, long expectedSize, CancellationToken cancellationToken = default);
}
