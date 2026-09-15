using System.Data.SQLite;
using System.Diagnostics;
using System.Text.Json;

namespace OfflineDaoc.ProgressImport;

public sealed record Policy(string[] ProgressTables, string[] ClearTables);
public sealed record ImportSummary(long Accounts, long Characters, long Bots, long InventoryItems);

public static class ImportEngine
{
    public static readonly Policy Rules = JsonSerializer.Deserialize<Policy>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"progress-policy.json")))!;
    public static string LocateRuntime(string folder)
    {
        folder = Path.GetFullPath(folder);
        foreach (string candidate in new[] {Path.Combine(folder,"runtime"),folder})
            if (File.Exists(Path.Combine(candidate,"data","opendaoc.sqlite3.db"))) return candidate;
        throw new InvalidOperationException("Choose the old Offline DAoC folder (or its runtime folder). No data/opendaoc.sqlite3.db was found.");
    }
    static string Q(string value) => "\"" + value.Replace("\"","\"\"") + "\"";
    static SQLiteConnection Open(string path, bool readOnly)
    {
        var builder = new SQLiteConnectionStringBuilder {DataSource=path,ReadOnly=readOnly,FailIfMissing=true,Pooling=false,DefaultTimeout=10};
        var c = new SQLiteConnection(builder.ConnectionString); c.Open(); return c;
    }
    static long Scalar(SQLiteConnection c,string sql)
    { using var q=c.CreateCommand();q.CommandText=sql;return Convert.ToInt64(q.ExecuteScalar()); }
    static void Exec(SQLiteConnection c,string sql)
    { using var q=c.CreateCommand();q.CommandText=sql;q.ExecuteNonQuery(); }
    static HashSet<string> Tables(SQLiteConnection c,string schema="main")
    {
        using var q=c.CreateCommand();q.CommandText=$"SELECT name FROM {schema}.sqlite_master WHERE type='table'";
        using var r=q.ExecuteReader();var result=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while(r.Read())result.Add(r.GetString(0));return result;
    }
    static List<string> Columns(SQLiteConnection c,string table,string schema="main")
    {
        using var q=c.CreateCommand();q.CommandText=$"PRAGMA {schema}.table_info({Q(table)})";
        using var r=q.ExecuteReader();var result=new List<string>();while(r.Read())result.Add(r.GetString(1));return result;
    }
    static void CheckClosed(params string[] roots)
    {
        if(System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners().Any(p=>p.Port==10300))
            throw new InvalidOperationException("A local DAoC server is listening. Stop it before importing progress.");
        foreach(var process in Process.GetProcesses())
        using(process)
        {
            if (process.Id==Environment.ProcessId)continue;
            string name=process.ProcessName;
            if(name.Equals("CoreServer",StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Stop the DAoC server before importing progress.");
            if(name is not ("OfflineDAoC" or "game" or "connect"))continue;
            try
            {
                string? file=process.MainModule?.FileName;
                if(file!=null && roots.Any(root=>file.StartsWith(root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidOperationException("Close the old and new launchers and game clients before importing.");
            }
            catch(System.ComponentModel.Win32Exception) { throw new InvalidOperationException("Cannot verify that the launcher/client is closed. Close it before importing."); }
        }
    }
    static void Backup(string source,string destination)
    {
        using var from=Open(source,true);
        using var to=new SQLiteConnection(new SQLiteConnectionStringBuilder {DataSource=destination,Pooling=false}.ConnectionString);
        to.Open();from.BackupDatabase(to,"main","main",-1,null,0);
    }
    static void Integrity(SQLiteConnection c)
    {
        using var q=c.CreateCommand();q.CommandText="PRAGMA quick_check";
        if(!string.Equals(Convert.ToString(q.ExecuteScalar()),"ok",StringComparison.Ordinal))throw new InvalidDataException("Database integrity check failed.");
    }
    public static ImportSummary Inspect(string folder)
    {
        using var c=Open(Path.Combine(LocateRuntime(folder),"data","opendaoc.sqlite3.db"),true);
        var tables=Tables(c);
        foreach(var t in new[]{"Account","DOLCharacters","offline_world_bots","Inventory","ItemUnique","ItemTemplate"})
            if(!tables.Contains(t))throw new InvalidDataException($"This old version is missing {t}. Import has not changed anything; it needs a compatible Offline DAoC database.");
        return new(Scalar(c,"SELECT count(*) FROM Account"),Scalar(c,"SELECT count(*) FROM DOLCharacters"),
            Scalar(c,"SELECT count(*) FROM offline_world_bots"),Scalar(c,"SELECT count(*) FROM Inventory"));
    }
    public static string Import(string oldFolder,string newFolder,Action<string> progress)
    {
        string old=LocateRuntime(oldFolder),current=LocateRuntime(newFolder);
        if(string.Equals(old,current,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Old and new folders must be different.");
        CheckClosed(old,current);
        string oldDb=Path.Combine(old,"data","opendaoc.sqlite3.db"),newDb=Path.Combine(current,"data","opendaoc.sqlite3.db");
        string credentials=Path.Combine(old,"account.txt");
        if(!File.Exists(credentials))throw new InvalidDataException("The old runtime/account.txt is missing. Restore it first so the imported account can log in automatically.");
        var values=File.ReadLines(credentials).Select(l=>l.Split(':',2)).Where(p=>p.Length==2)
            .ToDictionary(p=>p[0].Trim(),p=>p[1].Trim(),StringComparer.OrdinalIgnoreCase);
        if(!values.TryGetValue("Account",out var account)||!values.TryGetValue("Password",out var password)||string.IsNullOrWhiteSpace(account)||string.IsNullOrEmpty(password))
            throw new InvalidDataException("The old account.txt does not contain valid Account and Password entries.");
        Inspect(old);
        using(var source=Open(oldDb,true))
        { using var q=source.CreateCommand();q.CommandText="SELECT count(*) FROM Account WHERE Name=@name";q.Parameters.AddWithValue("@name",account);
          if(Convert.ToInt64(q.ExecuteScalar())!=1)throw new InvalidDataException("The old credentials do not name an account in the old database."); }
        using var exclusive=new FileStream(Path.Combine(current,"data","progress-import.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        string backup=Path.Combine(current,"progress-backups",DateTime.Now.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N")[..6]);
        Directory.CreateDirectory(backup);
        string staged=Path.Combine(backup,"prepared.db"),snapshot=Path.Combine(backup,"source-snapshot.db"),credentialStage=Path.Combine(backup,"prepared-account.txt");
        progress("Backing up the new folder and taking a consistent, read-only snapshot of the old progress…");
        Backup(newDb,Path.Combine(backup,"opendaoc.sqlite3.db"));
        if(File.Exists(Path.Combine(current,"account.txt")))File.Copy(Path.Combine(current,"account.txt"),Path.Combine(backup,"account.txt"));
        Backup(oldDb,snapshot);File.Copy(Path.Combine(backup,"opendaoc.sqlite3.db"),staged);
        bool swapped=false;
        long existingMissing=0;
        try
        {
            using(var c=Open(staged,false))
            {
                Exec(c,"PRAGMA foreign_keys=OFF; PRAGMA journal_mode=DELETE; PRAGMA synchronous=FULL;");
                using(var attach=c.CreateCommand()){attach.CommandText="ATTACH DATABASE @path AS old";attach.Parameters.AddWithValue("@path",snapshot);attach.ExecuteNonQuery();}
                var sourceTables=Tables(c,"old");var targetTables=Tables(c);
                foreach(string unknown in sourceTables.Except(targetTables,StringComparer.OrdinalIgnoreCase).Where(t=>!t.StartsWith("sqlite_",StringComparison.OrdinalIgnoreCase)))
                    if(Scalar(c,$"SELECT count(*) FROM old.{Q(unknown)}")>0)
                        throw new InvalidDataException($"The old save contains an unsupported extra table ({unknown}). Import stopped rather than silently omitting it.");
                Exec(c,"BEGIN IMMEDIATE");
                foreach(string table in Rules.ProgressTables)
                {
                    if(!targetTables.Contains(table))throw new InvalidDataException($"New version is missing progress table {table}.");
                    Exec(c,$"DELETE FROM main.{Q(table)}");
                    if(!sourceTables.Contains(table))continue;
                    var oldColumns=Columns(c,table,"old");var newColumns=Columns(c,table);
                    if(oldColumns.Except(newColumns,StringComparer.OrdinalIgnoreCase).Any())
                        throw new InvalidDataException($"The old {table} schema is newer or incompatible. No progress has been installed.");
                    string fields=string.Join(",",oldColumns.Select(Q));
                    progress($"Transferring {table}…");
                    Exec(c,$"INSERT INTO main.{Q(table)} ({fields}) SELECT {fields} FROM old.{Q(table)}");
                    if(Scalar(c,$"SELECT count(*) FROM main.{Q(table)}")!=Scalar(c,$"SELECT count(*) FROM old.{Q(table)}"))
                        throw new InvalidDataException($"Row verification failed for {table}.");
                    if(Scalar(c,$"SELECT count(*) FROM (SELECT {fields} FROM old.{Q(table)} EXCEPT SELECT {fields} FROM main.{Q(table)})")!=0)
                        throw new InvalidDataException($"Content verification failed for {table}.");
                }
                // Keep updated definitions on ID collisions; preserve old custom
                // templates that are absent from the new world for real owned items.
                var templateColumns=Columns(c,"ItemTemplate","old");
                if(templateColumns.Except(Columns(c,"ItemTemplate"),StringComparer.OrdinalIgnoreCase).Any())throw new InvalidDataException("Unsupported ItemTemplate schema.");
                string templateFields=string.Join(",",templateColumns.Select(Q));
                Exec(c,$"INSERT OR IGNORE INTO main.ItemTemplate ({templateFields}) SELECT {templateFields} FROM old.ItemTemplate");
                if(sourceTables.Contains("DBHouse"))
                {
                    Exec(c,"UPDATE main.DBHouse SET OwnerID='',GuildName='',GuildHouse=0,HasConsignment=0,KeptMoney=0,Model=0,Name='' WHERE COALESCE(OwnerID,'')<>''");
                    var fields=Columns(c,"DBHouse").Intersect(Columns(c,"DBHouse","old"),StringComparer.OrdinalIgnoreCase)
                        .Except(new[]{"HouseNumber","X","Y","Z","RegionID","Heading","DBHouse_ID"},StringComparer.OrdinalIgnoreCase).ToArray();
                    Exec(c,"UPDATE main.DBHouse SET "+string.Join(",",fields.Select(f=>$"{Q(f)}=(SELECT o.{Q(f)} FROM old.DBHouse o WHERE o.HouseNumber=main.DBHouse.HouseNumber)"))+
                        " WHERE HouseNumber IN (SELECT HouseNumber FROM old.DBHouse WHERE COALESCE(OwnerID,'')<>'')");
                }
                foreach(string table in Rules.ClearTables)if(targetTables.Contains(table))Exec(c,$"DELETE FROM {Q(table)}");
                Exec(c,"UPDATE Account SET PrivLevel=1; INSERT OR REPLACE INTO offline_local_options(Key,Value) VALUES('MakeMeGM','false'); UPDATE ServerProperty SET Value='1' WHERE lower(Key) IN ('xp_rate','bot_xp_rate'); UPDATE offline_world_bots SET IsOnline=0;");
                Exec(c,"UPDATE offline_population_settings SET Value=CAST((SELECT count(*) FROM offline_world_bots WHERE IsRetired=0) AS TEXT) WHERE Key='ActiveTarget'; UPDATE offline_population_settings SET Value='true' WHERE Key='PopulationEnabled';");
                string Missing(string schema) => $"SELECT i.Inventory_ID FROM {schema}.Inventory i WHERE (COALESCE(i.UTemplate_Id,'')<>'' AND NOT EXISTS(SELECT 1 FROM {schema}.ItemUnique u WHERE u.Id_nb=i.UTemplate_Id)) OR (COALESCE(i.UTemplate_Id,'')='' AND COALESCE(i.ITemplate_Id,'')<>'' AND NOT EXISTS(SELECT 1 FROM {schema}.ItemTemplate t WHERE t.Id_nb=i.ITemplate_Id))";
                existingMissing=Scalar(c,$"SELECT count(*) FROM ({Missing("old")})");
                if(Scalar(c,$"SELECT count(*) FROM ({Missing("main")} EXCEPT {Missing("old")})")!=0)
                    throw new InvalidDataException("Import introduced an unresolved inventory template. No progress has been installed.");
                if(existingMissing>0)progress($"Note: preserving {existingMissing} already-unresolved inventory references from the old save; no items are deleted or invented.");
                Exec(c,"COMMIT; DETACH DATABASE old;");Integrity(c);
            }
            File.WriteAllText(credentialStage,$"Account: {account}\r\nPassword: {password}\r\n");
            CheckClosed(old,current);
            progress("Installing verified progress. Please do not close this window…");
            using(var c=Open(newDb,false))Exec(c,"PRAGMA wal_checkpoint(TRUNCATE); PRAGMA journal_mode=DELETE;");
            File.Replace(staged,newDb,null);swapped=true;
            string targetCredentials=Path.Combine(current,"account.txt");
            if(File.Exists(targetCredentials))File.Replace(credentialStage,targetCredentials,null);else File.Move(credentialStage,targetCredentials);
            using(var verified=Open(newDb,true))Integrity(verified);
            File.Delete(snapshot);
            File.WriteAllText(Path.Combine(backup,"IMPORT RESULT.txt"),"Import completed. Previous destination database and account.txt are rollback copies. Original old folder was not changed. XP=1x; GM off.\r\n"+
                (existingMissing>0?$"Source-save warning: {existingMissing} inventory references already lacked item definitions in the old database. They were preserved unchanged; this import did not create those missing definitions.\r\n":""));
            return backup;
        }
        catch
        {
            if(swapped)
            {
                string restore=Path.Combine(backup,"restore.db");File.Copy(Path.Combine(backup,"opendaoc.sqlite3.db"),restore);File.Replace(restore,newDb,null);
                if(File.Exists(Path.Combine(backup,"account.txt")))File.Copy(Path.Combine(backup,"account.txt"),Path.Combine(current,"account.txt"),true);
            }
            throw;
        }
    }
}
