using System;
using System.Collections.Generic;
using System.IO;
using Markdig;
using Microsoft.Data.Sqlite;
using iText.Html2pdf;
using iText.Html2pdf.Resolver.Font;

public class Note
{
	public int LocalId { get; }
	public int GlobalId { get; }
	public string Name { get; set; }
	public string? Naiyou { get; set; }

	public int Revision { get; set; }
	public bool IsShared { get; set; }
	public bool Dirty { get; set; }
	public bool IsDeleted { get; set; }

	public DateTime CreatedAt { get; }
	public DateTime UpdatedAt { get; set; }

	public Note(int localId,
				int globalId,
				string name,
				string? naiyou = null,
				int revision = 0,
				bool isShared = false,
				bool dirty = false,
				bool isDeleted = false,
				DateTime? createdAt = null,
				DateTime? updatedAt = null)
	{
		LocalId = localId;
		GlobalId = globalId;
		Name = name;
		Naiyou = naiyou;
		Revision = revision;
		IsShared = isShared;
		Dirty = dirty;
		IsDeleted = isDeleted;
		CreatedAt = createdAt ?? DateTime.UtcNow;
		UpdatedAt = updatedAt ?? DateTime.UtcNow;
	}

	public string ReturnHtml()
	{
		if (string.IsNullOrEmpty(Naiyou)) return string.Empty;
		var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
		return Markdown.ToHtml(Naiyou, pipeline);
	}

	public void update_time()
	{
		UpdatedAt = DateTime.UtcNow;
	}
}

static class Exec
{
	private static readonly string? dbPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),".mdnotes");

	static Exec()
	{
		InitializeDatabase();
	}

	private static void InitializeDatabase()
	{
		using var connection = new SqliteConnection($"Data Source={dbPath}");
		connection.Open();
		
		var createTableSql = @"
			CREATE TABLE IF NOT EXISTS notes (
				local_id INTEGER PRIMARY KEY AUTOINCREMENT,
				global_id INTEGER NOT NULL DEFAULT 0,
				name TEXT NOT NULL,
				naiyou TEXT,
				revision INTEGER NOT NULL DEFAULT 0,
				is_shared INTEGER NOT NULL DEFAULT 0,
				dirty INTEGER NOT NULL DEFAULT 0,
				is_deleted INTEGER NOT NULL DEFAULT 0,
				created_at REAL NOT NULL,
				updated_at REAL NOT NULL
			);";
		
		using var command = connection.CreateCommand();
		command.CommandText = createTableSql;
		command.ExecuteNonQuery();
	}

	public static List<Note> load_list()
	{
		sync();
		return load_local();
	}

	public static List<Note> load_local(string keyword = "")
	{
		var notes = new List<Note>();
		var sql = "SELECT local_id, global_id, name, naiyou, revision, is_shared, dirty, is_deleted, created_at, updated_at FROM notes WHERE is_deleted = 0";
		if (!string.IsNullOrEmpty(keyword))
		{
			sql += " AND (name LIKE @keyword OR naiyou LIKE @keyword)";
		}
		sql += " ORDER BY updated_at DESC;";
		using var connection = new SqliteConnection($"Data Source={dbPath}");
		connection.Open();
		using var command = connection.CreateCommand();
		command.CommandText = sql;
		if (!string.IsNullOrEmpty(keyword))
		{
			command.Parameters.AddWithValue("@keyword", $"%{keyword}%");
		}
		using var reader = command.ExecuteReader();
		while (reader.Read())
		{
			var localId = reader.GetInt32(0);
			var globalId = reader.GetInt32(1);
			var name = reader.GetString(2);
			var naiyou = reader.IsDBNull(3) ? null : reader.GetString(3);
			var revision = reader.GetInt32(4);
			var isShared = reader.GetInt32(5) != 0;
			var dirty = reader.GetInt32(6) != 0;
			var isDeleted = reader.GetInt32(7) != 0;
			var createdAt = DateTimeOffset.FromUnixTimeSeconds((long)reader.GetDouble(8)).DateTime;
			var updatedAt = DateTimeOffset.FromUnixTimeSeconds((long)reader.GetDouble(9)).DateTime;
			var note = new Note(
				localId: localId,
				globalId: globalId,
				name: name,
				naiyou: naiyou,
				revision: revision,
				isShared: isShared,
				dirty: dirty,
				isDeleted: isDeleted,
				createdAt: createdAt,
				updatedAt: updatedAt
			);
			notes.Add(note);
		}
		return notes;
	}

	public static void to_pdf(string html, string path)
	{
		using var fileStream = new FileStream(path, FileMode.Create);
		ConverterProperties properties = new ConverterProperties();
		var fontProvider = new DefaultFontProvider(false, false, false);
		try
		{
			if (OperatingSystem.IsMacOS())
			{
				TryAddFont(fontProvider, "/Library/Fonts/Arial Unicode.ttf");
			}
			else if (OperatingSystem.IsWindows())
			{
				TryAddFont(fontProvider, "C:\\Windows\\Fonts\\arial.ttf");
			}
			else if (OperatingSystem.IsLinux())
			{
				TryAddFont(fontProvider, "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf");
			}
			properties.SetFontProvider(fontProvider);
			HtmlConverter.ConvertToPdf(html, fileStream, properties);
		} catch {
			HtmlConverter.ConvertToPdf(html, fileStream);
		}
	}
	
	private static void TryAddFont(DefaultFontProvider provider, string fontPath)
	{
		try
		{
			if (File.Exists(fontPath))
			{
				provider.AddFont(fontPath);
			}
		} catch {}
	}

	public static void save_html(string html, string path)
	{
		File.WriteAllText(path, html);
	}
	
	public static void add_note(Note new_note){
		using var connection = new SqliteConnection($"Data Source={dbPath}");
		connection.Open();
		var insertSql = @"
			INSERT INTO notes (local_id, global_id, name, naiyou, revision, is_shared, dirty, is_deleted, created_at, updated_at)
			VALUES (@local_id, @global_id, @name, @naiyou, @revision, @is_shared, @dirty, @is_deleted, @created_at, @updated_at);";
		using var command = connection.CreateCommand();
		command.CommandText = insertSql;
		command.Parameters.AddWithValue("@local_id", new_note.LocalId);
		command.Parameters.AddWithValue("@global_id", null);
		command.Parameters.AddWithValue("@name", new_note.Name);
		command.Parameters.AddWithValue("@naiyou", new_note.Naiyou ?? (object)DBNull.Value);
		command.Parameters.AddWithValue("@revison", 0);
		command.Parameters.AddWithValue("@is_shared", new_note.IsShared ? 1 : 0);
		command.Parameters.AddWithValue("@dirty", new_note.Dirty ? 1 : 0);
		command.Parameters.AddWithValue("@is_deleted", new_note.IsDeleted ? 1 : 0);
		command.Parameters.AddWithValue("@created_at", DateTime.UtcNow);
		command.Parameters.AddWithValue("@updated_at", DateTime.UtcNow);
		command.ExecuteNonQuery();
	}

	public static void update_note(Note updated_note)
	{
		Note tmp = updated_note;
		tmp.update_time();
		tmp.Dirty = true;
		tmp.Revision += 1;
		using var connection = new SqliteConnection($"Data Source={dbPath}");
		connection.Open();
		var updateSql = @"
			UPDATE notes
			SET name = @name,
				naiyou = @naiyou,
				revision = @revision,
				is_shared = @is_shared,
				dirty = @dirty,
				updated_at = @updated_at
			WHERE local_id = @local_id;";
		using var command = connection.CreateCommand();
		command.CommandText = updateSql;
		command.Parameters.AddWithValue("@name", tmp.Name);
		command.Parameters.AddWithValue("@naiyou", tmp.Naiyou ?? (object)DBNull.Value);
		command.Parameters.AddWithValue("@revison", tmp.Revision);
		command.Parameters.AddWithValue("@is_shared", tmp.IsShared ? 1 : 0);
		command.Parameters.AddWithValue("@dirty", tmp.Dirty ? 1 : 0);
		command.Parameters.AddWithValue("@updated_at", tmp.UpdatedAt);
		command.Parameters.AddWithValue("@local_id", tmp.LocalId);
		command.ExecuteNonQuery();
	}

	public static void delete_note(int local_id)
	{
		using var connection = new SqliteConnection($"Data Source={dbPath}");
		connection.Open();
		var deleteSql = @"
			UPDATE notes
			SET is_deleted = 1,
				dirty = 1,
				updated_at = @updated_at
			WHERE local_id = @local_id;";
		using var command = connection.CreateCommand();
		command.CommandText = deleteSql;
		command.Parameters.AddWithValue("@updated_at", DateTime.UtcNow);
		command.Parameters.AddWithValue("@local_id", local_id);
		command.ExecuteNonQuery();
	}

	private static void real_delete(int local_id)
	{
		using var connection = new SqliteConnection($"Data Source={dbPath}");
		connection.Open();
		var deleteSql = "DELETE FROM notes WHERE local_id = @local_id;";
		using var command = connection.CreateCommand();
		command.CommandText = deleteSql;
		command.Parameters.AddWithValue("@local_id", local_id);
		command.ExecuteNonQuery();
	}

	public static void sync()
	{
		// Synchronization logic would go here
	}
}