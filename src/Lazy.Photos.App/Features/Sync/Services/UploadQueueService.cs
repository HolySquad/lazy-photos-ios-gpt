using Microsoft.Data.Sqlite;
using Lazy.Photos.App.Features.Sync.Models;

namespace Lazy.Photos.App.Features.Sync.Services;

public sealed class UploadQueueService : IUploadQueueService
{
	private readonly string _dbPath;
	private readonly SemaphoreSlim _semaphore = new(1);

	public UploadQueueService(string dbPath)
	{
		_dbPath = dbPath;
	}

	public async Task EnqueueItemsAsync(IReadOnlyList<SyncQueueItem> items, CancellationToken ct)
	{
		await EnsureDatabaseAsync(ct);

		await _semaphore.WaitAsync(ct);
		try
		{
			await using var connection = new SqliteConnection($"Data Source={_dbPath}");
			await connection.OpenAsync(ct);

			await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(ct);

			await using var command = connection.CreateCommand();
			command.Transaction = transaction;
			command.CommandText = """
				INSERT INTO SyncQueue (
					Id, LocalPhotoId, Hash, LocalPath, FileName, SizeBytes, MimeType,
					CapturedAtTicks, Width, Height, LocationLat, LocationLon,
					Status, RetryCount, ErrorMessage, CreatedAtTicks, LastAttemptAtTicks
				) VALUES (
					$id, $localId, $hash, $path, $name, $size, $mime,
					$captured, $width, $height, $lat, $lon,
					$status, $retry, $error, $created, $attempt
				)
				ON CONFLICT(Id) DO UPDATE SET
					Status = excluded.Status,
					RetryCount = excluded.RetryCount,
					ErrorMessage = excluded.ErrorMessage,
					LastAttemptAtTicks = excluded.LastAttemptAtTicks
				""";

			command.Parameters.Add("$id", SqliteType.Text);
			command.Parameters.Add("$localId", SqliteType.Text);
			command.Parameters.Add("$hash", SqliteType.Text);
			command.Parameters.Add("$path", SqliteType.Text);
			command.Parameters.Add("$name", SqliteType.Text);
			command.Parameters.Add("$size", SqliteType.Integer);
			command.Parameters.Add("$mime", SqliteType.Text);
			command.Parameters.Add("$captured", SqliteType.Integer);
			command.Parameters.Add("$width", SqliteType.Integer);
			command.Parameters.Add("$height", SqliteType.Integer);
			command.Parameters.Add("$lat", SqliteType.Real);
			command.Parameters.Add("$lon", SqliteType.Real);
			command.Parameters.Add("$status", SqliteType.Text);
			command.Parameters.Add("$retry", SqliteType.Integer);
			command.Parameters.Add("$error", SqliteType.Text);
			command.Parameters.Add("$created", SqliteType.Integer);
			command.Parameters.Add("$attempt", SqliteType.Integer);

			foreach (var item in items)
			{
				command.Parameters["$id"].Value = item.Id;
				command.Parameters["$localId"].Value = item.LocalPhotoId;
				command.Parameters["$hash"].Value = item.Hash ?? (object)DBNull.Value;
				command.Parameters["$path"].Value = item.LocalPath;
				command.Parameters["$name"].Value = item.FileName;
				command.Parameters["$size"].Value = item.SizeBytes;
				command.Parameters["$mime"].Value = item.MimeType;
				command.Parameters["$captured"].Value = item.CapturedAt?.UtcTicks ?? (object)DBNull.Value;
				command.Parameters["$width"].Value = item.Width ?? (object)DBNull.Value;
				command.Parameters["$height"].Value = item.Height ?? (object)DBNull.Value;
				command.Parameters["$lat"].Value = item.LocationLat ?? (object)DBNull.Value;
				command.Parameters["$lon"].Value = item.LocationLon ?? (object)DBNull.Value;
				command.Parameters["$status"].Value = item.Status.ToString();
				command.Parameters["$retry"].Value = item.RetryCount;
				command.Parameters["$error"].Value = item.ErrorMessage ?? (object)DBNull.Value;
				command.Parameters["$created"].Value = item.CreatedAt.UtcTicks;
				command.Parameters["$attempt"].Value = item.LastAttemptAt?.UtcTicks ?? (object)DBNull.Value;

				await command.ExecuteNonQueryAsync(ct);
			}

			await transaction.CommitAsync(ct);
		}
		finally
		{
			_semaphore.Release();
		}
	}

	public async Task<IReadOnlyList<SyncQueueItem>> GetPendingItemsAsync(CancellationToken ct)
	{
		await EnsureDatabaseAsync(ct);

		await using var connection = new SqliteConnection($"Data Source={_dbPath}");
		await connection.OpenAsync(ct);

		await using var command = connection.CreateCommand();
		command.CommandText = """
			SELECT Id, LocalPhotoId, Hash, LocalPath, FileName, SizeBytes, MimeType,
				   CapturedAtTicks, Width, Height, LocationLat, LocationLon,
				   Status, RetryCount, ErrorMessage, CreatedAtTicks, LastAttemptAtTicks
			FROM SyncQueue
			WHERE Status IN ('Pending', 'Hashing', 'Uploading')
			ORDER BY CreatedAtTicks ASC
			""";

		var items = new List<SyncQueueItem>();
		await using var reader = await command.ExecuteReaderAsync(ct);
		while (await reader.ReadAsync(ct))
		{
			items.Add(ReadQueueItem(reader));
		}

		return items;
	}

	public async Task UpdateItemStatusAsync(string itemId, QueueItemStatus status, string? errorMessage, CancellationToken ct)
	{
		await EnsureDatabaseAsync(ct);

		await _semaphore.WaitAsync(ct);
		try
		{
			await using var connection = new SqliteConnection($"Data Source={_dbPath}");
			await connection.OpenAsync(ct);

			await using var command = connection.CreateCommand();
			command.CommandText = """
				UPDATE SyncQueue
				SET Status = $status,
					ErrorMessage = $error,
					LastAttemptAtTicks = $attempt
				WHERE Id = $id
				""";

			command.Parameters.AddWithValue("$status", status.ToString());
			command.Parameters.AddWithValue("$error", errorMessage ?? (object)DBNull.Value);
			command.Parameters.AddWithValue("$attempt", DateTimeOffset.UtcNow.UtcTicks);
			command.Parameters.AddWithValue("$id", itemId);

			await command.ExecuteNonQueryAsync(ct);
		}
		finally
		{
			_semaphore.Release();
		}
	}

	public async Task MarkAsUploadedAsync(string itemId, CancellationToken ct)
	{
		await UpdateItemStatusAsync(itemId, QueueItemStatus.Uploaded, null, ct);
	}

	public async Task ClearCompletedItemsAsync(CancellationToken ct)
	{
		await EnsureDatabaseAsync(ct);

		await _semaphore.WaitAsync(ct);
		try
		{
			await using var connection = new SqliteConnection($"Data Source={_dbPath}");
			await connection.OpenAsync(ct);

			await using var command = connection.CreateCommand();
			command.CommandText = """
				DELETE FROM SyncQueue
				WHERE Status IN ('Uploaded', 'Failed', 'Skipped', 'Cancelled')
				""";

			await command.ExecuteNonQueryAsync(ct);
		}
		finally
		{
			_semaphore.Release();
		}
	}

	public async Task<QueueStatistics> GetStatisticsAsync(CancellationToken ct)
	{
		await EnsureDatabaseAsync(ct);

		await using var connection = new SqliteConnection($"Data Source={_dbPath}");
		await connection.OpenAsync(ct);

		await using var command = connection.CreateCommand();
		command.CommandText = """
			SELECT
				COUNT(*) as Total,
				SUM(CASE WHEN Status = 'Pending' THEN 1 ELSE 0 END) as Pending,
				SUM(CASE WHEN Status = 'Uploading' THEN 1 ELSE 0 END) as Uploading,
				SUM(CASE WHEN Status IN ('Uploaded', 'Skipped') THEN 1 ELSE 0 END) as Completed,
				SUM(CASE WHEN Status = 'Failed' THEN 1 ELSE 0 END) as Failed
			FROM SyncQueue
			""";

		await using var reader = await command.ExecuteReaderAsync(ct);
		if (await reader.ReadAsync(ct))
		{
			return new QueueStatistics(
				Total: reader.GetInt32(0),
				Pending: reader.GetInt32(1),
				Uploading: reader.GetInt32(2),
				Completed: reader.GetInt32(3),
				Failed: reader.GetInt32(4)
			);
		}

		return new QueueStatistics(0, 0, 0, 0, 0);
	}

	private async Task EnsureDatabaseAsync(CancellationToken ct)
	{
		if (File.Exists(_dbPath))
			return;

		await _semaphore.WaitAsync(ct);
		try
		{
			if (File.Exists(_dbPath))
				return;

			Directory.CreateDirectory(Path.GetDirectoryName(_dbPath)!);

			await using var connection = new SqliteConnection($"Data Source={_dbPath}");
			await connection.OpenAsync(ct);

			await using var command = connection.CreateCommand();
			command.CommandText = """
				CREATE TABLE IF NOT EXISTS SyncQueue (
					Id TEXT PRIMARY KEY,
					LocalPhotoId TEXT NOT NULL,
					Hash TEXT,
					LocalPath TEXT NOT NULL,
					FileName TEXT NOT NULL,
					SizeBytes INTEGER NOT NULL,
					MimeType TEXT NOT NULL,
					CapturedAtTicks INTEGER,
					Width INTEGER,
					Height INTEGER,
					LocationLat REAL,
					LocationLon REAL,
					Status TEXT NOT NULL DEFAULT 'Pending',
					RetryCount INTEGER DEFAULT 0,
					ErrorMessage TEXT,
					CreatedAtTicks INTEGER NOT NULL,
					LastAttemptAtTicks INTEGER
				);

				CREATE INDEX IF NOT EXISTS idx_syncqueue_status ON SyncQueue(Status);
				CREATE INDEX IF NOT EXISTS idx_syncqueue_hash ON SyncQueue(Hash) WHERE Hash IS NOT NULL;
				""";

			await command.ExecuteNonQueryAsync(ct);
		}
		finally
		{
			_semaphore.Release();
		}
	}

	private static SyncQueueItem ReadQueueItem(SqliteDataReader reader)
	{
		return new SyncQueueItem
		{
			Id = reader.GetString(0),
			LocalPhotoId = reader.GetString(1),
			Hash = reader.IsDBNull(2) ? null : reader.GetString(2),
			LocalPath = reader.GetString(3),
			FileName = reader.GetString(4),
			SizeBytes = reader.GetInt64(5),
			MimeType = reader.GetString(6),
			CapturedAt = reader.IsDBNull(7) ? null : new DateTimeOffset(reader.GetInt64(7), TimeSpan.Zero),
			Width = reader.IsDBNull(8) ? null : reader.GetInt32(8),
			Height = reader.IsDBNull(9) ? null : reader.GetInt32(9),
			LocationLat = reader.IsDBNull(10) ? null : reader.GetDouble(10),
			LocationLon = reader.IsDBNull(11) ? null : reader.GetDouble(11),
			Status = Enum.Parse<QueueItemStatus>(reader.GetString(12)),
			RetryCount = reader.GetInt32(13),
			ErrorMessage = reader.IsDBNull(14) ? null : reader.GetString(14),
			CreatedAt = new DateTimeOffset(reader.GetInt64(15), TimeSpan.Zero),
			LastAttemptAt = reader.IsDBNull(16) ? null : new DateTimeOffset(reader.GetInt64(16), TimeSpan.Zero)
		};
	}
}
