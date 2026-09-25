using System.Data.SQLite;
using System.Globalization;

internal static class Program
{
    private const string SeedId = "11caef62-3c17-4e0a-8399-fccb46fa3fa6";
    private const string SqlFileName = "shannon-beach-rat-camp.sql";

    private static int Main(string[] args)
    {
        try
        {
            if (args.Length != 4 || args[0] != "--database" || args[2] != "--sql")
                throw new ArgumentException("Usage: --database <copied-world.db> --sql <shannon-beach-rat-camp.sql>");

            string databasePath = Path.GetFullPath(args[1]);
            string sqlPath = Path.GetFullPath(args[3]);
            if (!File.Exists(databasePath) || !File.Exists(sqlPath) ||
                !Path.GetFileName(sqlPath).Equals(SqlFileName, StringComparison.OrdinalIgnoreCase))
                throw new FileNotFoundException("The copied world database or expected world-patch SQL is missing.");

            using var connection = new SQLiteConnection(
                $"Data Source={databasePath};Version=3;Pooling=False;Foreign Keys=False;Default Timeout=60");
            connection.Open();
            using var transaction = connection.BeginTransaction();
            try
            {
                const string seedQuery = "SELECT COUNT(*) FROM Mob WHERE Mob_ID='" + SeedId + "' " +
                    "AND Name='beach rat' AND Region=200 AND Model=567 AND X=306949 AND Y=627062 AND Z=6520";
                if (Convert.ToInt32(Scalar(connection, transaction, seedQuery), CultureInfo.InvariantCulture) != 1)
                    throw new InvalidDataException("The world lacks the expected Shannon Estuary beach-rat seed.");

                Execute(connection, transaction, File.ReadAllText(sqlPath));
                const string campQuery = "SELECT COUNT(*) FROM Mob WHERE Name='beach rat' AND Region=200 " +
                    "AND Level IN (1,2) AND X BETWEEN 306640 AND 307890 AND Y BETWEEN 626640 AND 627760";
                if (Convert.ToInt32(Scalar(connection, transaction, campQuery), CultureInfo.InvariantCulture) < 11)
                    throw new InvalidDataException("The Shannon Estuary low-level beach-rat camp was not fully installed.");

                if (!string.Equals(Convert.ToString(Scalar(connection, transaction, "PRAGMA quick_check"),
                        CultureInfo.InvariantCulture), "ok", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("SQLite quick_check failed; the world-patch transaction was rolled back.");

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }

            Console.WriteLine("Shannon Estuary beach-rat camp verified: 11 nearby level-1/2 rats.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("World patch failed: " + exception.Message);
            return 1;
        }
    }

    private static void Execute(SQLiteConnection connection, SQLiteTransaction transaction, string sql)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private static object? Scalar(SQLiteConnection connection, SQLiteTransaction transaction, string sql)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        return command.ExecuteScalar();
    }
}
