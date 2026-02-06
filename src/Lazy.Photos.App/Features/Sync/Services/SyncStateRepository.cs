using Microsoft.Data.Sqlite;
using Lazy.Photos.App.Features.Sync.Models;

namespace Lazy.Photos.App.Features.Sync.Services;

public sealed class SyncStateRepository : ISyncStateRepository
{
	private readonly string _dbPath;
	private readonly SemaphoreSlim _semaphore = new(1);

	public SyncStateRepository(string dbPath)
	{
		_dbPath = dbPath;
	}

	public async Task SaveStateAsync(SyncState state, CancellationToken ct)
	{
		await EnsureDatabaseAsync(ct);

		await _semaphore.WaitAsync(ct);
		try
		{
			await using var connection = new SqliteConnection($"Data Source={_dbPath}");
			await connection.OpenAsync(ct);

			await using var command = connection.CreateCommand();
			command.CommandText = """
				INSERT OR REPLACE INTO SyncState (
					Id, Status, TotalItems, CompletedItems, FailedItems,
					CurrentItemName, ProgressPercentage, ErrorMessage,
					StartedAtTicks, CompletedAtTicks,
					CurrentFileUploadedBytes, CurrentFileTotalBytes, CurrentFileProgressPercentage
				) VALUES (
					1, $status, $total, $completed, $failed,
					$current, $progress, $error,
					$started, $completedAt,
					$fileUploaded, $fileTotal, $fileProgress
				)
				""";

			command.Parameters.AddWithValue("$status", state.Status.ToString());
			command.Parameters.AddWithValue("$total", state.TotalItems);
			command.Parameters.AddWithValue("$completed", state.CompletedItems);
			command.Parameters.AddWithValue("$failed", state.FailedItems);
			command.Parameters.AddWithValue("$current", state.CurrentItemName ?? (object)DBNull.Value);
			command.Parameters.AddWithValue("$progress", state.ProgressPercentage);
			command.Parameters.AddWithValue("$error", state.ErrorMessage ?? (object)DBNull.Value);
			command.Parameters.AddWithValue("$started", state.StartedAt?.UtcTicks ?? (object)DBNull.Value);
			command.Parameters.AddWithValue("$completedAt", state.CompletedAt?.UtcTicks ?? (object)DBNull.Value);
			command.Parameters.AddWithValue("$fileUploaded", state.CurrentFileUploadedBytes);
			command.Parameters.AddWithValue("$fileTotal", state.CurrentFileTotalBytes);
			command.Parameters.AddWithValue("$fileProgress", state.CurrentFileProgressPercentage);

			await command.ExecuteNonQueryAsync(ct);
		}
		finally
		{
			_semaphore.Release();
		}
	}

	public async Task<SyncState?> LoadStateAsync(CancellationToken ct)
	{
		await EnsureDatabaseAsync(ct);

		await using var connection = new SqliteConnection($"Data Source={_dbPath}");
		await connection.OpenAsync(ct);

		await using var command = connection.CreateCommand();
		command.CommandText = """
			SELECT Status, TotalItems, CompletedItems, FailedItems,
				   CurrentItemName, ProgressPercentage, ErrorMessage,
				   StartedAtTicks, CompletedAtTicks,
				   CurrentFileUploadedBytes, CurrentFileTotalBytes, CurrentFileProgressPercentage
			FROM SyncState
			WHERE Id = 1
			""";

		await using var reader = await command.ExecuteReaderAsync(ct);
		if (await reader.ReadAsync(ct))
		{
			return new SyncState
			{
				Status = Enum.Parse<SyncStatus>(reader.GetString(0)),
				TotalItems = reader.GetInt32(1),
				CompletedItems = reader.GetInt32(2),
				FailedItems = reader.GetInt32(3),
				CurrentItemName = reader.IsDBNull(4) ? null : reader.GetString(4),
				ProgressPercentage = reader.GetDouble(5),
				ErrorMessage = reader.IsDBNull(6) ? null : reader.GetString(6),
				StartedAt = reader.IsDBNull(7) ? null : new DateTimeOffset(reader.GetInt64(7), TimeSpan.Zero),
				CompletedAt = reader.IsDBNull(8) ? null : new DateTimeOffset(reader.GetInt64(8), TimeSpan.Zero),
				CurrentFileUploadedBytes = reader.GetInt64(9),
				CurrentFileTotalBytes = reader.GetInt64(10),
				CurrentFileProgressPercentage = reader.GetDouble(11)
			};
		}

		return null;
	}

	public async Task ClearStateAsync(CancellationToken ct)
	{
		await EnsureDatabaseAsync(ct);

		await _semaphore.WaitAsync(ct);
		try
		{
			await using var connection = new SqliteConnection($"Data Source={_dbPath}");
			await connection.OpenAsync(ct);

			await using var command = connection.CreateCommand();
			command.CommandText = "DELETE FROM SyncState WHERE Id = 1";
			await command.ExecuteNonQueryAsync(ct);
		}
		finally
		{
			_semaphore.Release();
		}
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
				CREATE TABLE IF NOT EXISTS SyncState (
					Id INTEGER PRIMARY KEY CHECK (Id = 1),
					Status TEXT NOT NULL,
					TotalItems INTEGER NOT NULL,
					CompletedItems INTEGER NOT NULL,
					FailedItems INTEGER NOT NULL,
					CurrentItemName TEXT,
					ProgressPercentage REAL,
					ErrorMessage TEXT,
					StartedAtTicks INTEGER,
					CompletedAtTicks INTEGER,
					CurrentFileUploadedBytes INTEGER DEFAULT 0,
					CurrentFileTotalBytes INTEGER DEFAULT 0,
					CurrentFileProgressPercentage REAL DEFAULT 0
				);
				""";

			await command.ExecuteNonQueryAsync(ct);
		}
		finally
		{
			_semaphore.Release();
		}
	}
}
