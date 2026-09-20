using System.Globalization;
using System.Text.Json;
using System.Data.SQLite;

static class Program
{
    private static readonly string[] Lines =
    [
        "Sluagh Host", "Abhartach's Rot", "Cairn Oath", "Dullahan's Bulwark",
        "Abhartach's Bane", "Sluagh Covenant", "Sluaghbinder's Legacy", "Epic Spells"
    ];

    public static int Main(string[] args)
    {
        try
        {
            var values = ParseArgs(args);
            if (!values.TryGetValue("database", out var database) ||
                !values.TryGetValue("overlay", out var overlayPath))
                throw new ArgumentException("Usage: --database <db> --overlay <overlay.json>");
            if (!File.Exists(database) || !File.Exists(overlayPath))
                throw new FileNotFoundException("Database or overlay file was not found.");

            using var connection = new SQLiteConnection($"Data Source={database};Version=3;foreign keys=false;");
            connection.Open();
            using var document = JsonDocument.Parse(File.ReadAllText(overlayPath));
            var root = document.RootElement;
            if (root.GetProperty("format").GetInt32() != 1 ||
                !string.Equals(root.GetProperty("feature").GetString(), "Sluaghbinder", StringComparison.Ordinal))
                throw new InvalidDataException("Unsupported Sluaghbinder overlay format.");

            ValidateSchema(connection, root.GetProperty("tables"));
            using var transaction = connection.BeginTransaction();
            try
            {
                Execute(connection, transaction, "PRAGMA foreign_keys=OFF");
                DeleteIn(connection, transaction, "Specialization", "KeyName", Lines.Append("SluaghbinderCareer").ToArray());
                DeleteIn(connection, transaction, "SpellLine", "KeyName", Lines);
                DeleteIn(connection, transaction, "LineXSpell", "LineName", Lines);
                DeleteIn(connection, transaction, "SpecXAbility", "Spec", Lines.Append("SluaghbinderCareer").ToArray());
                Execute(connection, transaction, "DELETE FROM \"ClassXSpecialization\" WHERE \"ClassID\"=63");
                Execute(connection, transaction, "DELETE FROM \"Style\" WHERE \"ClassId\"=63");
                Execute(connection, transaction, "DELETE FROM \"Spell\" WHERE \"Spell_ID\" LIKE 'Sluaghbinder_%' OR \"SpellID\" BETWEEN 59000 AND 59084");
                DeleteIn(connection, transaction, "NpcTemplate", "TemplateId", Enumerable.Range(60170001, 7).Select(i => i.ToString(CultureInfo.InvariantCulture)).ToArray());
                DeleteIn(connection, transaction, "Mob", "Mob_ID", ["sluaghbinder_trainer_tir_na_nog", "sluaghbinder_bound_wisp_tir_na_nog"]);

                foreach (var table in root.GetProperty("tables").EnumerateObject())
                {
                    var columns = table.Value.GetProperty("columns").EnumerateArray().Select(x => x.GetString()!).ToArray();
                    foreach (var row in table.Value.GetProperty("rows").EnumerateArray())
                    {
                        var parameters = new List<SQLiteParameter>();
                        var names = new List<string>();
                        for (var i = 0; i < columns.Length; i++)
                        {
                            var name = "@p" + i;
                            names.Add(name);
                            var value = row.TryGetProperty(columns[i], out var jsonValue) ? JsonValue(jsonValue) : DBNull.Value;
                            parameters.Add(new SQLiteParameter(name, value));
                        }
                        using var command = connection.CreateCommand();
                        command.Transaction = transaction;
                        command.CommandText = $"INSERT OR REPLACE INTO {Identifier(table.Name)} ({string.Join(',', columns.Select(Identifier))}) VALUES ({string.Join(',', names)})";
                        command.Parameters.AddRange(parameters.ToArray());
                        command.ExecuteNonQuery();
                    }
                }
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }

            var check = Convert.ToString(Scalar(connection, null, "PRAGMA quick_check"), CultureInfo.InvariantCulture);
            if (!string.Equals(check, "ok", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"SQLite quick_check returned '{check}'.");
            Console.WriteLine("Sluaghbinder static data applied and SQLite quick_check passed.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Sluaghbinder patch failed: " + ex.Message);
            return 1;
        }
    }

    private static Dictionary<string, string> ParseArgs(string[] args)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i + 1 < args.Length; i += 2)
        {
            if (!args[i].StartsWith("--", StringComparison.Ordinal)) throw new ArgumentException("Invalid argument.");
            result[args[i][2..]] = args[i + 1];
        }
        return result;
    }

    private static object JsonValue(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Null => DBNull.Value,
        JsonValueKind.String => value.GetString() ?? string.Empty,
        JsonValueKind.Number when value.TryGetInt64(out var integer) => integer,
        JsonValueKind.Number when value.TryGetDouble(out var real) => real,
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        _ => value.GetRawText()
    };

    private static string Identifier(string value) => "\"" + value.Replace("\"", "\"\"") + "\"";

    private static void ValidateSchema(SQLiteConnection connection, JsonElement tables)
    {
        foreach (var table in tables.EnumerateObject())
        {
            var actual = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA table_info(" + Identifier(table.Name) + ")";
            using var reader = command.ExecuteReader();
            while (reader.Read()) actual.Add(reader.GetString(1));
            if (actual.Count == 0) throw new InvalidDataException($"Target database is missing table '{table.Name}'.");
            foreach (var column in table.Value.GetProperty("columns").EnumerateArray())
                if (!actual.Contains(column.GetString()!))
                    throw new InvalidDataException($"Target table '{table.Name}' is missing column '{column.GetString()}'.");
        }
    }

    private static void DeleteIn(SQLiteConnection connection, SQLiteTransaction transaction, string table, string column, IEnumerable<string> values)
    {
        var items = values.ToArray();
        if (items.Length == 0) return;
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        var names = new List<string>();
        for (var i = 0; i < items.Length; i++)
        {
            var name = "@v" + i;
            names.Add(name);
            command.Parameters.AddWithValue(name, items[i]);
        }
        command.CommandText = $"DELETE FROM {Identifier(table)} WHERE {Identifier(column)} IN ({string.Join(',', names)})";
        command.ExecuteNonQuery();
    }

    private static void Execute(SQLiteConnection connection, SQLiteTransaction? transaction, string sql)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private static object? Scalar(SQLiteConnection connection, SQLiteTransaction? transaction, string sql)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        return command.ExecuteScalar();
    }
}
