using LazyPhotos.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LazyPhotos.Infrastructure.Services;

public class LocalStorageService : IStorageService
{
	private readonly string _baseUploadPath;
	private readonly ILogger<LocalStorageService> _logger;

	public LocalStorageService(IConfiguration configuration, ILogger<LocalStorageService> logger)
	{
		_baseUploadPath = configuration["Storage:UploadPath"] ?? "uploads";
		_logger = logger;

		// Ensure base directories exist
		Directory.CreateDirectory(_baseUploadPath);
		Directory.CreateDirectory(Path.Combine(_baseUploadPath, "temp"));
	}

	public async Task<string> SaveChunkAsync(Guid sessionId, long offset, Stream content, CancellationToken cancellationToken = default)
	{
		var tempDir = Path.Combine(_baseUploadPath, "temp", sessionId.ToString());
		Directory.CreateDirectory(tempDir);

		var chunkPath = Path.Combine(tempDir, $"chunk_{offset}");

		await using var fileStream = new FileStream(chunkPath, FileMode.Create, FileAccess.Write);
		await content.CopyToAsync(fileStream, cancellationToken);

		_logger.LogDebug("Saved chunk at offset {Offset} for session {SessionId}", offset, sessionId);

		return chunkPath;
	}

	public async Task<string> FinalizeUploadAsync(Guid sessionId, string storageKey, CancellationToken cancellationToken = default)
	{
		var tempDir = Path.Combine(_baseUploadPath, "temp", sessionId.ToString());
		var finalPath = Path.Combine(_baseUploadPath, storageKey);

		// Ensure destination directory exists
		var destDir = Path.GetDirectoryName(finalPath);
		if (!string.IsNullOrEmpty(destDir))
		{
			Directory.CreateDirectory(destDir);
		}

		// Combine all chunks in order
		var chunkFiles = Directory.GetFiles(tempDir, "chunk_*")
			.OrderBy(f => long.Parse(Path.GetFileName(f).Replace("chunk_", "")))
			.ToList();

		await using var finalStream = new FileStream(finalPath, FileMode.Create, FileAccess.Write);

		foreach (var chunkFile in chunkFiles)
		{
			await using var chunkStream = new FileStream(chunkFile, FileMode.Open, FileAccess.Read);
			await chunkStream.CopyToAsync(finalStream, cancellationToken);
		}

		// Clean up temp directory
		Directory.Delete(tempDir, true);

		_logger.LogInformation("Finalized upload for session {SessionId} to {StorageKey}", sessionId, storageKey);

		return finalPath;
	}

	public Task DeleteSessionFilesAsync(Guid sessionId, CancellationToken cancellationToken = default)
	{
		var tempDir = Path.Combine(_baseUploadPath, "temp", sessionId.ToString());

		if (Directory.Exists(tempDir))
		{
			Directory.Delete(tempDir, true);
			_logger.LogDebug("Deleted temp files for session {SessionId}", sessionId);
		}

		return Task.CompletedTask;
	}

	public Task<bool> ValidateChunkAsync(Guid sessionId, long expectedSize, CancellationToken cancellationToken = default)
	{
		var tempDir = Path.Combine(_baseUploadPath, "temp", sessionId.ToString());

		if (!Directory.Exists(tempDir))
		{
			return Task.FromResult(false);
		}

		var chunkFiles = Directory.GetFiles(tempDir, "chunk_*");
		var totalSize = chunkFiles.Sum(f => new FileInfo(f).Length);

		return Task.FromResult(totalSize == expectedSize);
	}
}
