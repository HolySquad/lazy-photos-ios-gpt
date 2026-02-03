using Microsoft.Data.Sqlite;
using System.IO;
using Lazy.Photos.App.Features.Photos.Models;

namespace Lazy.Photos.App.Features.Photos.Services;

public sealed class PhotoCacheService : IPhotoCacheService, IPhotoCacheMaintenance
{
	private readonly string _dbPath;

	public PhotoCacheService(string dbPath)
	{
		_dbPath = dbPath;
	}

	public async Task<IReadOnlyList<PhotoItem>> GetCachedPhotosAsync(CancellationToken ct)
	{
		await EnsureDatabaseAsync(ct);

		await using var connection = new SqliteConnection($"Data Source={_dbPath}");
		await connection.OpenAsync(ct);

		var command = connection.CreateCommand();
		command.CommandText = """
			SELECT Id, DisplayName, TakenAtTicks, Hash, FolderName, ThumbUri, FullUri, IsSynced
			FROM Photos
			ORDER BY TakenAtTicks DESC
			""";

		var items = new List<PhotoItem>();
		await using var reader = await command.ExecuteReaderAsync(ct);
		while (await reader.ReadAsync(ct))
		{
			var id = reader.GetString(0);
			var displayName = reader.IsDBNull(1) ? null : reader.GetString(1);
			var ticks = reader.GetInt64(2);
			var hash = reader.IsDBNull(3) ? null : reader.GetString(3);
			var folder = reader.IsDBNull(4) ? null : reader.GetString(4);
			var thumb = reader.IsDBNull(5) ? null : reader.GetString(5);
			var full = reader.IsDBNull(6) ? null : reader.GetString(6);
			var isSynced = reader.GetBoolean(7);

			items.Add(new PhotoItem
			{
				Id = id,
				DisplayName = displayName,
				TakenAt = ticks == 0 ? null : new DateTimeOffset(ticks, TimeSpan.Zero),
				Hash = hash,
				FolderName = folder,
				Thumbnail = ToImageSource(thumb),
				FullImage = ToImageSource(full),
				IsSynced = isSynced
			});
		}

		return items;
	}

	public async Task SavePhotosAsync(IReadOnlyList<PhotoItem> photos, CancellationToken ct)
	{
		await EnsureDatabaseAsync(ct);
		await using var connection = new SqliteConnection($"Data Source={_dbPath}");
		await connection.OpenAsync(ct);

		await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(ct);

		// Create single command with UPSERT, reuse for all photos
		await using var upsert = connection.CreateCommand();
		upsert.Transaction = transaction;
		upsert.CommandText = """
			INSERT INTO Photos (Id, DisplayName, TakenAtTicks, Hash, FolderName, ThumbUri, FullUri, IsSynced)
			VALUES ($id, $name, $ticks, $hash, $folder, $thumb, $full, $synced)
			ON CONFLICT(Id) DO UPDATE SET
				DisplayName = excluded.DisplayName,
				TakenAtTicks = excluded.TakenAtTicks,
				Hash = excluded.Hash,
				FolderName = excluded.FolderName,
				ThumbUri = excluded.ThumbUri,
				FullUri = excluded.FullUri,
				IsSynced = excluded.IsSynced
			""";

		// Add parameters once
		upsert.Parameters.Add("$id", SqliteType.Text);
		upsert.Parameters.Add("$name", SqliteType.Text);
		upsert.Parameters.Add("$ticks", SqliteType.Integer);
		upsert.Parameters.Add("$hash", SqliteType.Text);
		upsert.Parameters.Add("$folder", SqliteType.Text);
		upsert.Parameters.Add("$thumb", SqliteType.Text);
		upsert.Parameters.Add("$full", SqliteType.Text);
		upsert.Parameters.Add("$synced", SqliteType.Integer);

		foreach (var photo in photos)
		{
			// Reuse command, update parameter values only
			upsert.Parameters["$id"].Value = photo.Id ?? Guid.NewGuid().ToString();
			upsert.Parameters["$name"].Value = photo.DisplayName ?? (object)DBNull.Value;
			upsert.Parameters["$ticks"].Value = photo.TakenAt?.UtcTicks ?? 0;
			upsert.Parameters["$hash"].Value = photo.Hash ?? (object)DBNull.Value;
			upsert.Parameters["$folder"].Value = photo.FolderName ?? (object)DBNull.Value;
			upsert.Parameters["$thumb"].Value = ToUriString(photo.Thumbnail) ?? (object)DBNull.Value;
			upsert.Parameters["$full"].Value = ToUriString(photo.FullImage) ?? (object)DBNull.Value;
			upsert.Parameters["$synced"].Value = photo.IsSynced;

			await upsert.ExecuteNonQueryAsync(ct);
		}

		await transaction.CommitAsync(ct);
	}

	private async Task EnsureDatabaseAsync(CancellationToken ct)
	{
		if (File.Exists(_dbPath))
			return;

		var directory = Path.GetDirectoryName(_dbPath);
		if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
			Directory.CreateDirectory(directory);

		await using var connection = new SqliteConnection($"Data Source={_dbPath}");
		await connection.OpenAsync(ct);

		var command = connection.CreateCommand();
		command.CommandText = """
			CREATE TABLE IF NOT EXISTS Photos (
				Id TEXT PRIMARY KEY,
				DisplayName TEXT,
				TakenAtTicks INTEGER,
				Hash TEXT,
				FolderName TEXT,
				ThumbUri TEXT,
				FullUri TEXT,
				IsSynced INTEGER
			);

			CREATE INDEX IF NOT EXISTS idx_TakenAt ON Photos(TakenAtTicks DESC);
			CREATE INDEX IF NOT EXISTS idx_Hash ON Photos(Hash) WHERE Hash IS NOT NULL;
			""";
		await command.ExecuteNonQueryAsync(ct);
	}

	private static ImageSource? ToImageSource(string? uri)
	{
		if (string.IsNullOrWhiteSpace(uri))
			return null;

		if (Uri.TryCreate(uri, UriKind.Absolute, out var parsed))
		{
			if (parsed.IsFile && File.Exists(parsed.LocalPath))
				return ImageSource.FromFile(parsed.LocalPath);
			return ImageSource.FromUri(parsed);
		}

		if (File.Exists(uri))
			return ImageSource.FromFile(uri);

		return null;
	}

	private static string? ToUriString(ImageSource? source)
	{
		switch (source)
		{
			case UriImageSource uriSource:
				return uriSource.Uri?.ToString();
			case FileImageSource fileSource when !string.IsNullOrWhiteSpace(fileSource.File):
				return Path.GetFullPath(fileSource.File);
		}
		return null;
	}

	public Task<string> GetDatabaseSizeAsync(CancellationToken ct)
	{
		if (!File.Exists(_dbPath))
			return Task.FromResult("0 KB");

		var length = new FileInfo(_dbPath).Length;
		return Task.FromResult(FormatSize(length));
	}

	public Task ClearCacheAsync(CancellationToken ct)
	{
		if (File.Exists(_dbPath))
			File.Delete(_dbPath);
		return Task.CompletedTask;
	}

	private static string FormatSize(long bytes)
	{
		if (bytes < 1024)
			return $"{bytes} B";
		if (bytes < 1024 * 1024)
			return $"{bytes / 1024.0:F1} KB";
		return $"{bytes / 1024.0 / 1024.0:F1} MB";
	}
}
