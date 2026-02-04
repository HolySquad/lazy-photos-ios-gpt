using Microsoft.Data.Sqlite;
using Lazy.Photos.App.Features.Logs.Models;

namespace Lazy.Photos.App.Features.Logs.Services;

public sealed class LogRepository : ILogRepository
{
	private readonly string _dbPath;
	private readonly SemaphoreSlim _semaphore = new(1);

	public LogRepository(string dbPath)
	{
		_dbPath = dbPath;
	}

	public async Task AddLogAsync(LogEntry entry, CancellationToken ct)
	{
		await EnsureDatabaseAsync(ct);

		await _semaphore.WaitAsync(ct);
		try
		{
			await using var connection = new SqliteConnection($"Data Source={_dbPath}");
			await connection.OpenAsync(ct);

			await using var command = connection.CreateCommand();
			command.CommandText = """
				INSERT INTO Logs (TimestampTicks, Level, Category, Message, Exception, Context)
				VALUES ($ticks, $level, $category, $message, $exception, $context)
				""";

			command.Parameters.AddWithValue("$ticks", entry.Timestamp.UtcTicks);
			command.Parameters.AddWithValue("$level", entry.Level.ToString());
			command.Parameters.AddWithValue("$category", entry.Category);
			command.Parameters.AddWithValue("$message", entry.Message);
			command.Parameters.AddWithValue("$exception", entry.Exception ?? (object)DBNull.Value);
			command.Parameters.AddWithValue("$context", entry.Context ?? (object)DBNull.Value);

			await command.ExecuteNonQueryAsync(ct);
		}
		finally
		{
			_semaphore.Release();
		}
	}

	public async Task<IReadOnlyList<LogEntry>> GetLogsAsync(
		int limit,
		int offset,
		LogLevel? minLevel,
		string? categoryFilter,
		CancellationToken ct)
	{
		await EnsureDatabaseAsync(ct);

		await using var connection = new SqliteConnection($"Data Source={_dbPath}");
		await connection.OpenAsync(ct);

		await using var command = connection.CreateCommand();
		var whereConditions = new List<string>();

		if (minLevel.HasValue)
		{
			whereConditions.Add("Level IN (" + GetLevelFilter(minLevel.Value) + ")");
		}

		if (!string.IsNullOrWhiteSpace(categoryFilter))
		{
			whereConditions.Add("Category = $category");
			command.Parameters.AddWithValue("$category", categoryFilter);
		}

		var whereClause = whereConditions.Count > 0 ? "WHERE " + string.Join(" AND ", whereConditions) : "";

		command.CommandText = $"""
			SELECT Id, TimestampTicks, Level, Category, Message, Exception, Context
			FROM Logs
			{whereClause}
			ORDER BY TimestampTicks DESC
			LIMIT $limit OFFSET $offset
			""";

		command.Parameters.AddWithValue("$limit", limit);
		command.Parameters.AddWithValue("$offset", offset);

		var logs = new List<LogEntry>();
		await using var reader = await command.ExecuteReaderAsync(ct);
		while (await reader.ReadAsync(ct))
		{
			logs.Add(new LogEntry
			{
				Id = reader.GetInt64(0),
				Timestamp = new DateTimeOffset(reader.GetInt64(1), TimeSpan.Zero),
				Level = Enum.Parse<LogLevel>(reader.GetString(2)),
				Category = reader.GetString(3),
				Message = reader.GetString(4),
				Exception = reader.IsDBNull(5) ? null : reader.GetString(5),
				Context = reader.IsDBNull(6) ? null : reader.GetString(6)
			});
		}

		return logs;
	}

	public async Task<int> GetLogCountAsync(LogLevel? minLevel, string? categoryFilter, CancellationToken ct)
	{
		await EnsureDatabaseAsync(ct);

		await using var connection = new SqliteConnection($"Data Source={_dbPath}");
		await connection.OpenAsync(ct);

		await using var command = connection.CreateCommand();
		var whereConditions = new List<string>();

		if (minLevel.HasValue)
		{
			whereConditions.Add("Level IN (" + GetLevelFilter(minLevel.Value) + ")");
		}

		if (!string.IsNullOrWhiteSpace(categoryFilter))
		{
			whereConditions.Add("Category = $category");
			command.Parameters.AddWithValue("$category", categoryFilter);
		}

		var whereClause = whereConditions.Count > 0 ? "WHERE " + string.Join(" AND ", whereConditions) : "";

		command.CommandText = $"SELECT COUNT(*) FROM Logs {whereClause}";

		var result = await command.ExecuteScalarAsync(ct);
		return result != null ? Convert.ToInt32(result) : 0;
	}

	public async Task ClearOldLogsAsync(int maxDays, CancellationToken ct)
	{
		await EnsureDatabaseAsync(ct);

		await _semaphore.WaitAsync(ct);
		try
		{
			await using var connection = new SqliteConnection($"Data Source={_dbPath}");
			await connection.OpenAsync(ct);

			await using var command = connection.CreateCommand();
			var cutoffTicks = DateTimeOffset.UtcNow.AddDays(-maxDays).UtcTicks;
			command.CommandText = "DELETE FROM Logs WHERE TimestampTicks < $cutoff";
			command.Parameters.AddWithValue("$cutoff", cutoffTicks);

			await command.ExecuteNonQueryAsync(ct);
		}
		finally
		{
			_semaphore.Release();
		}
	}

	public async Task ClearAllLogsAsync(CancellationToken ct)
	{
		await EnsureDatabaseAsync(ct);

		await _semaphore.WaitAsync(ct);
		try
		{
			await using var connection = new SqliteConnection($"Data Source={_dbPath}");
			await connection.OpenAsync(ct);

			await using var command = connection.CreateCommand();
			command.CommandText = "DELETE FROM Logs";
			await command.ExecuteNonQueryAsync(ct);
		}
		finally
		{
			_semaphore.Release();
		}
	}

	public async Task<long> GetLogDatabaseSizeAsync(CancellationToken ct)
	{
		await EnsureDatabaseAsync(ct);

		if (File.Exists(_dbPath))
		{
			return await Task.Run(() => new FileInfo(_dbPath).Length, ct);
		}

		return 0;
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
				CREATE TABLE IF NOT EXISTS Logs (
					Id INTEGER PRIMARY KEY AUTOINCREMENT,
					TimestampTicks INTEGER NOT NULL,
					Level TEXT NOT NULL,
					Category TEXT NOT NULL,
					Message TEXT NOT NULL,
					Exception TEXT,
					Context TEXT
				);

				CREATE INDEX IF NOT EXISTS idx_logs_timestamp ON Logs(TimestampTicks DESC);
				CREATE INDEX IF NOT EXISTS idx_logs_level ON Logs(Level);
				CREATE INDEX IF NOT EXISTS idx_logs_category ON Logs(Category);
				""";

			await command.ExecuteNonQueryAsync(ct);
		}
		finally
		{
			_semaphore.Release();
		}
	}

	private static string GetLevelFilter(LogLevel minLevel)
	{
		return minLevel switch
		{
			LogLevel.Debug => "'Debug','Info','Warning','Error'",
			LogLevel.Info => "'Info','Warning','Error'",
			LogLevel.Warning => "'Warning','Error'",
			LogLevel.Error => "'Error'",
			_ => "'Debug','Info','Warning','Error'"
		};
	}
}
