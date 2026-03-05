using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;
using Clippy.Models;

namespace Clippy.Services
{
    public class DatabaseService : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly string _imagesDir;

        public DatabaseService()
        {
            var dataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Clippy");
            Directory.CreateDirectory(dataDir);

            _imagesDir = Path.Combine(dataDir, "images");
            Directory.CreateDirectory(_imagesDir);

            var dbPath = Path.Combine(dataDir, "clippy.db");
            _connection = new SqliteConnection($"Data Source={dbPath}");
            _connection.Open();

            InitializeDatabase();
        }

        public string ImagesDirectory => _imagesDir;

        private void InitializeDatabase()
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS ClipboardHistory (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Content TEXT NOT NULL DEFAULT '',
                    HtmlContent TEXT,
                    Preview TEXT NOT NULL DEFAULT '',
                    EntryType INTEGER NOT NULL DEFAULT 0,
                    ImagePath TEXT,
                    CreatedAt TEXT NOT NULL,
                    IsPinned INTEGER NOT NULL DEFAULT 0,
                    ContentHash TEXT NOT NULL DEFAULT '',
                    SourceApp TEXT NOT NULL DEFAULT ''
                );
                CREATE INDEX IF NOT EXISTS idx_hash ON ClipboardHistory(ContentHash);
                CREATE INDEX IF NOT EXISTS idx_created ON ClipboardHistory(CreatedAt DESC);
                CREATE INDEX IF NOT EXISTS idx_pinned ON ClipboardHistory(IsPinned);
            ";
            cmd.ExecuteNonQuery();
        }

        public long Insert(ClipboardEntry entry)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO ClipboardHistory (Content, HtmlContent, Preview, EntryType, ImagePath, CreatedAt, IsPinned, ContentHash, SourceApp)
                VALUES (@content, @html, @preview, @type, @imgPath, @created, @pinned, @hash, @source);
                SELECT last_insert_rowid();
            ";
            cmd.Parameters.AddWithValue("@content", entry.Content);
            cmd.Parameters.AddWithValue("@html", (object?)entry.HtmlContent ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@preview", entry.Preview);
            cmd.Parameters.AddWithValue("@type", (int)entry.EntryType);
            cmd.Parameters.AddWithValue("@imgPath", (object?)entry.ImagePath ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@created", entry.CreatedAt.ToString("o"));
            cmd.Parameters.AddWithValue("@pinned", entry.IsPinned ? 1 : 0);
            cmd.Parameters.AddWithValue("@hash", entry.ContentHash);
            cmd.Parameters.AddWithValue("@source", entry.SourceApp);

            return (long)cmd.ExecuteScalar()!;
        }

        public ClipboardEntry? FindByHash(string hash)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT * FROM ClipboardHistory WHERE ContentHash = @hash LIMIT 1";
            cmd.Parameters.AddWithValue("@hash", hash);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
                return ReadEntry(reader);
            return null;
        }

        public void UpdateTimestamp(long id)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "UPDATE ClipboardHistory SET CreatedAt = @now WHERE Id = @id";
            cmd.Parameters.AddWithValue("@now", DateTime.Now.ToString("o"));
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        public List<ClipboardEntry> Search(string query, int limit = 200)
        {
            var results = new List<ClipboardEntry>();
            using var cmd = _connection.CreateCommand();

            if (string.IsNullOrWhiteSpace(query))
            {
                cmd.CommandText = @"
                    SELECT * FROM ClipboardHistory
                    ORDER BY IsPinned DESC, CreatedAt DESC
                    LIMIT @limit";
                cmd.Parameters.AddWithValue("@limit", limit);
            }
            else
            {
                cmd.CommandText = @"
                    SELECT * FROM ClipboardHistory
                    WHERE Content LIKE @q OR Preview LIKE @q OR SourceApp LIKE @q
                    ORDER BY IsPinned DESC, CreatedAt DESC
                    LIMIT @limit";
                cmd.Parameters.AddWithValue("@q", $"%{query}%");
                cmd.Parameters.AddWithValue("@limit", limit);
            }

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                results.Add(ReadEntry(reader));

            return results;
        }

        public void TogglePin(long id)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "UPDATE ClipboardHistory SET IsPinned = CASE WHEN IsPinned = 1 THEN 0 ELSE 1 END WHERE Id = @id";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        public void Delete(long id)
        {
            // Get entry to clean up image file if needed
            var entry = GetById(id);
            if (entry?.ImagePath != null && File.Exists(entry.ImagePath))
            {
                try { File.Delete(entry.ImagePath); } catch { }
            }

            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "DELETE FROM ClipboardHistory WHERE Id = @id";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        public void Clear(bool keepPinned = true)
        {
            // Clean up image files
            var entries = Search("", 10000);
            foreach (var e in entries)
            {
                if (!keepPinned || !e.IsPinned)
                {
                    if (e.ImagePath != null && File.Exists(e.ImagePath))
                        try { File.Delete(e.ImagePath); } catch { }
                }
            }

            using var cmd = _connection.CreateCommand();
            cmd.CommandText = keepPinned
                ? "DELETE FROM ClipboardHistory WHERE IsPinned = 0"
                : "DELETE FROM ClipboardHistory";
            cmd.ExecuteNonQuery();
        }

        public ClipboardEntry? GetById(long id)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT * FROM ClipboardHistory WHERE Id = @id";
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
                return ReadEntry(reader);
            return null;
        }

        public void EnforceMaxEntries(int maxEntries)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = @"
                DELETE FROM ClipboardHistory
                WHERE Id IN (
                    SELECT Id FROM ClipboardHistory
                    WHERE IsPinned = 0
                    ORDER BY CreatedAt DESC
                    LIMIT -1 OFFSET @max
                )";
            cmd.Parameters.AddWithValue("@max", maxEntries);
            cmd.ExecuteNonQuery();
        }

        public int GetCount()
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM ClipboardHistory";
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        private ClipboardEntry ReadEntry(SqliteDataReader reader)
        {
            return new ClipboardEntry
            {
                Id = reader.GetInt64(reader.GetOrdinal("Id")),
                Content = reader.GetString(reader.GetOrdinal("Content")),
                HtmlContent = reader.IsDBNull(reader.GetOrdinal("HtmlContent")) ? null : reader.GetString(reader.GetOrdinal("HtmlContent")),
                Preview = reader.GetString(reader.GetOrdinal("Preview")),
                EntryType = (ClipboardEntryType)reader.GetInt32(reader.GetOrdinal("EntryType")),
                ImagePath = reader.IsDBNull(reader.GetOrdinal("ImagePath")) ? null : reader.GetString(reader.GetOrdinal("ImagePath")),
                CreatedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("CreatedAt"))),
                IsPinned = reader.GetInt32(reader.GetOrdinal("IsPinned")) == 1,
                ContentHash = reader.GetString(reader.GetOrdinal("ContentHash")),
                SourceApp = reader.GetString(reader.GetOrdinal("SourceApp"))
            };
        }

        public void Dispose()
        {
            _connection?.Close();
            _connection?.Dispose();
        }
    }
}
