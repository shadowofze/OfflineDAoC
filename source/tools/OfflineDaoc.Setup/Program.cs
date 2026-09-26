using System.Data;
using System.Data.SQLite;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using DOL.Database;
using DOL.Database.Attributes;
using DOL.Database.Connection;
using DOL.GS;

internal static class Program
{
    private const int ExpectedConservativeRestoredSpawnCount = 983;
    private const int ExpectedPeriodMapRestoredSpawnCount = 1290;
    private const int ExpectedTotalRestoredSpawnCount =
        ExpectedConservativeRestoredSpawnCount + ExpectedPeriodMapRestoredSpawnCount;

    private static int Main(string[] args)
    {
        try
        {
            if (TryGetArgument(args, "--apply-classic165-spawns", out string migrationDatabase))
            {
                migrationDatabase = Path.GetFullPath(migrationDatabase);
                string backupPath = CreateConsistentBackup(migrationDatabase);
                using var migrationConnection = OpenForMigration(migrationDatabase);
                EnsureColumn(migrationConnection, "offline_world_bots", "ObjectivePveMode", "TEXT NOT NULL DEFAULT ''");
                EnsureColumn(migrationConnection, "offline_world_bots", "ObjectivePveKillTarget", "INTEGER NOT NULL DEFAULT 0");
                EnsureColumn(migrationConnection, "offline_world_bots", "ObjectivePveKills", "INTEGER NOT NULL DEFAULT 0");
                ApplyClassic165SpawnProfile(migrationConnection);
                RestoreClassic165ShroudedIslesAndDarknessFallsSpawns(migrationConnection);
                RestoreConservativeClassic165FrontierAndAlbionSpawns(migrationConnection);
                RestorePeriodMapClassic165FrontierAndAlbionSpawns(migrationConnection);
                EnsureRealmExchangeBrokers(migrationConnection);
                VerifyClassic165SpawnProfile(migrationConnection);
                Console.WriteLine($"Classic 1.65 spawn profile applied. Recoverable backup: {backupPath}");
                return 0;
            }

            var options = Options.Parse(args);
            Directory.CreateDirectory(Path.GetDirectoryName(options.DatabasePath)!);

            Console.WriteLine("Creating OpenDAoC SQLite schema (no game server will be started)...");
            CreateSchema(options.DatabasePath);

            using var connection = Open(options.DatabasePath);
            CreateOfflineTables(connection);
            if (Directory.Exists(options.SqlSourcePath))
            {
                Console.WriteLine("Importing the OpenDAoC 1.65 world data...");
                ImportWorldData(connection, options.SqlSourcePath);
            }

            ApplyClassic165SpawnProfile(connection);
            RestoreClassic165ShroudedIslesAndDarknessFallsSpawns(connection);
            RestoreConservativeClassic165FrontierAndAlbionSpawns(connection);
            RestorePeriodMapClassic165FrontierAndAlbionSpawns(connection);
            EnsureRealmExchangeBrokers(connection);
            ApplyClassicSiRules(connection);
            CreateAccount(connection, options.AccountName, options.Password);
            WriteCredentials(options);
            WriteRuleset(options);
            Verify(connection, options.AccountName);

            Console.WriteLine("Offline world setup completed successfully.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static void CreateSchema(string databasePath)
    {
        var connectionString = $"Data Source={databasePath};Version=3;Pooling=False;Journal Mode=Off;Synchronous=Off;Foreign Keys=False;Default Timeout=60";
        var database = ObjectDatabase.GetObjectDatabase(EConnectionType.DATABASE_SQLITE, connectionString)
            ?? throw new InvalidOperationException("The SQLite database provider is unavailable.");

        var assemblies = new[] { typeof(DbAccount).Assembly, typeof(DOL.GS.GameServer).Assembly };
        var registered = 0;

        foreach (var type in assemblies.SelectMany(GetLoadableTypes).Distinct())
        {
            if (!type.IsClass || type.IsAbstract || !typeof(DataObject).IsAssignableFrom(type))
            {
                continue;
            }

            if (!type.GetCustomAttributes<DOL.Database.Attributes.DataTable>(false).Any())
            {
                continue;
            }

            database.RegisterDataObject(type);
            registered++;
        }

        Console.WriteLine($"Registered {registered} server data models.");
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return exception.Types.Where(type => type is not null)!;
        }
    }

    private static SQLiteConnection Open(string databasePath)
    {
        var connection = new SQLiteConnection($"Data Source={databasePath};Version=3;Pooling=False;Foreign Keys=False;Default Timeout=60");
        connection.Open();
        Execute(connection, "PRAGMA busy_timeout=60000; PRAGMA foreign_keys=OFF; PRAGMA synchronous=OFF; PRAGMA journal_mode=MEMORY;");
        return connection;
    }

    private static SQLiteConnection OpenForMigration(string databasePath)
    {
        var connection = new SQLiteConnection($"Data Source={databasePath};Version=3;Pooling=False;Foreign Keys=False;Default Timeout=60");
        connection.Open();
        Execute(connection, "PRAGMA busy_timeout=60000; PRAGMA foreign_keys=OFF; PRAGMA journal_mode=WAL; PRAGMA synchronous=FULL;");
        return connection;
    }

    private static bool TryGetArgument(string[] args, string name, out string value)
    {
        for (int index = 0; index + 1 < args.Length; index++)
        {
            if (args[index].Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                value = args[index + 1];
                return !string.IsNullOrWhiteSpace(value);
            }
        }
        value = string.Empty;
        return false;
    }

    private static string CreateConsistentBackup(string databasePath)
    {
        if (!File.Exists(databasePath))
            throw new FileNotFoundException("The live world database does not exist.", databasePath);

        string backupDirectory = Path.Combine(Path.GetDirectoryName(databasePath)!, "backups");
        Directory.CreateDirectory(backupDirectory);
        string stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        string backupPath = Path.Combine(backupDirectory, $"before-classic165-spawns-{stamp}.sqlite3.db");
        using var source = new SQLiteConnection($"Data Source={databasePath};Version=3;Pooling=False;Read Only=True;Default Timeout=60");
        using var destination = new SQLiteConnection($"Data Source={backupPath};Version=3;Pooling=False;Default Timeout=60");
        source.Open();
        destination.Open();
        source.BackupDatabase(destination, "main", "main", -1, null, 0);
        return backupPath;
    }

    private static void CreateOfflineTables(SQLiteConnection connection)
    {
        Execute(connection, """
            CREATE TABLE IF NOT EXISTS offline_world_bots (
                BotId INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL UNIQUE,
                Realm INTEGER NOT NULL,
                ClassId INTEGER NOT NULL,
                ClassName TEXT NOT NULL,
                RaceId INTEGER NOT NULL DEFAULT 0,
                RaceName TEXT NOT NULL DEFAULT '',
                Gender INTEGER NOT NULL DEFAULT 1,
                Level INTEGER NOT NULL DEFAULT 1,
                Experience INTEGER NOT NULL DEFAULT 0,
                RealmPoints INTEGER NOT NULL DEFAULT 0,
                ZoneId INTEGER,
                ZoneName TEXT,
                Activity TEXT NOT NULL DEFAULT 'Not spawned',
                IsOnline INTEGER NOT NULL DEFAULT 0,
                IsAlive INTEGER NOT NULL DEFAULT 1,
                LastUpdateUtc TEXT NOT NULL,
                MoneyCopper INTEGER NOT NULL DEFAULT 0,
                InventoryRevision INTEGER NOT NULL DEFAULT 0,
                ObjectiveKind TEXT NOT NULL DEFAULT '',
                ObjectiveAssignmentId TEXT NOT NULL DEFAULT '',
                ObjectiveAssignedUtc TEXT NOT NULL DEFAULT '',
                ObjectivePhase TEXT NOT NULL DEFAULT '',
                ObjectiveExpiresUtc TEXT NOT NULL DEFAULT '',
                ObjectiveRvrEligibleUtc TEXT NOT NULL DEFAULT '',
                ObjectivePveMode TEXT NOT NULL DEFAULT '',
                ObjectivePveKillTarget INTEGER NOT NULL DEFAULT 0,
                ObjectivePveKills INTEGER NOT NULL DEFAULT 0,
                X INTEGER,
                Y INTEGER,
                Z INTEGER
            );
            CREATE INDEX IF NOT EXISTS IX_offline_world_bots_online ON offline_world_bots(IsOnline, Realm, Level);
            CREATE INDEX IF NOT EXISTS IX_offline_world_bots_realm ON offline_world_bots(Realm, ClassName);

            CREATE TABLE IF NOT EXISTS offline_bot_commands (
                CommandId INTEGER PRIMARY KEY AUTOINCREMENT,
                BotId INTEGER NOT NULL,
                CommandType TEXT NOT NULL,
                RequestedUtc TEXT NOT NULL,
                State TEXT NOT NULL DEFAULT 'Pending',
                CompletedUtc TEXT,
                Error TEXT
            );
            CREATE INDEX IF NOT EXISTS IX_offline_bot_commands_pending ON offline_bot_commands(State, CommandId);

            CREATE TABLE IF NOT EXISTS offline_population_settings (
                Key TEXT PRIMARY KEY,
                Value TEXT NOT NULL,
                Description TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS offline_runtime_status (
                Id INTEGER PRIMARY KEY CHECK (Id = 1),
                ServerState TEXT NOT NULL,
                ServerPid INTEGER,
                ActiveBots INTEGER NOT NULL DEFAULT 0,
                ServerMemoryMb REAL NOT NULL DEFAULT 0,
                TickP95Ms REAL NOT NULL DEFAULT 0,
                AiWorkQueue INTEGER NOT NULL DEFAULT 0,
                LastHeartbeatUtc TEXT
            );

            INSERT OR IGNORE INTO offline_runtime_status
                (Id, ServerState, ActiveBots)
                VALUES (1, 'Stopped', 0);

            CREATE TABLE IF NOT EXISTS offline_auction_listings (
                ListingId INTEGER PRIMARY KEY AUTOINCREMENT,
                SellerBotId INTEGER NOT NULL,
                ItemObjectId TEXT NOT NULL UNIQUE,
                ItemName TEXT NOT NULL,
                ItemLevel INTEGER NOT NULL,
                Quantity INTEGER NOT NULL DEFAULT 1,
                Quality INTEGER NOT NULL DEFAULT 100,
                ConditionPercent INTEGER NOT NULL DEFAULT 100,
                RarityScore REAL NOT NULL DEFAULT 0,
                StatUtilityScore REAL NOT NULL DEFAULT 0,
                MinimumBidCopper INTEGER NOT NULL,
                BuyoutCopper INTEGER NOT NULL,
                CurrentBidCopper INTEGER,
                HighBidderBotId INTEGER,
                State TEXT NOT NULL DEFAULT 'Active',
                CreatedUtc TEXT NOT NULL,
                ExpiresUtc TEXT NOT NULL,
                Version INTEGER NOT NULL DEFAULT 1
            );
            CREATE INDEX IF NOT EXISTS IX_offline_auction_active ON offline_auction_listings(State, ExpiresUtc, ItemLevel);
            CREATE INDEX IF NOT EXISTS IX_offline_auction_seller ON offline_auction_listings(SellerBotId, State);

            CREATE TABLE IF NOT EXISTS offline_auction_escrow (
                EscrowId INTEGER PRIMARY KEY AUTOINCREMENT,
                ListingId INTEGER NOT NULL,
                BidderBotId INTEGER NOT NULL,
                Copper INTEGER NOT NULL,
                State TEXT NOT NULL,
                CreatedUtc TEXT NOT NULL,
                ReleasedUtc TEXT
            );
            CREATE INDEX IF NOT EXISTS IX_offline_auction_escrow_listing ON offline_auction_escrow(ListingId, State);

            CREATE TABLE IF NOT EXISTS offline_auction_ledger (
                LedgerId INTEGER PRIMARY KEY AUTOINCREMENT,
                ListingId INTEGER NOT NULL,
                SellerBotId INTEGER NOT NULL,
                BuyerBotId INTEGER NOT NULL,
                ItemObjectId TEXT NOT NULL,
                Quantity INTEGER NOT NULL,
                CopperTransferred INTEGER NOT NULL,
                TransactionType TEXT NOT NULL,
                CompletedUtc TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS offline_auction_settings (
                Key TEXT PRIMARY KEY,
                Value TEXT NOT NULL,
                Description TEXT NOT NULL
            );
            """);

        // Additive migrations keep the setup utility safe to rerun over the prepared database.
        EnsureColumn(connection, "offline_world_bots", "RaceId", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(connection, "offline_world_bots", "RaceName", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "offline_world_bots", "Gender", "INTEGER NOT NULL DEFAULT 1");
        EnsureColumn(connection, "offline_world_bots", "InventoryRevision", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(connection, "offline_world_bots", "RegionId", "INTEGER");
        EnsureColumn(connection, "offline_world_bots", "Health", "INTEGER NOT NULL DEFAULT 1");
        EnsureColumn(connection, "offline_world_bots", "Mana", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(connection, "offline_world_bots", "Endurance", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(connection, "offline_world_bots", "BindRegionId", "INTEGER");
        EnsureColumn(connection, "offline_world_bots", "BindX", "INTEGER");
        EnsureColumn(connection, "offline_world_bots", "BindY", "INTEGER");
        EnsureColumn(connection, "offline_world_bots", "BindZ", "INTEGER");
        EnsureColumn(connection, "offline_world_bots", "SerializedSpecs", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "offline_world_bots", "SerializedAbilities", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "offline_world_bots", "SerializedCraftingSkills", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "offline_world_bots", "CurrentCampId", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "offline_world_bots", "ItineraryJson", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "offline_world_bots", "DeathCount", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(connection, "offline_world_bots", "IsRetired", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(connection, "offline_world_bots", "LastSavedUtc", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "offline_world_bots", "PersistedStateVersion", "INTEGER NOT NULL DEFAULT 1");
        EnsureColumn(connection, "offline_world_bots", "CurrentGoal", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "offline_world_bots", "TargetName", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "offline_world_bots", "TravelDestination", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "offline_world_bots", "ObjectiveProgress", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "offline_world_bots", "LastMeaningfulProgressUtc", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "offline_world_bots", "RecoveryCount", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(connection, "offline_world_bots", "ObjectiveKind", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "offline_world_bots", "ObjectiveAssignmentId", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "offline_world_bots", "ObjectiveAssignedUtc", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "offline_world_bots", "ObjectivePhase", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "offline_world_bots", "ObjectiveExpiresUtc", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "offline_world_bots", "ObjectiveRvrEligibleUtc", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "offline_world_bots", "ObjectivePveMode", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "offline_world_bots", "ObjectivePveKillTarget", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(connection, "offline_world_bots", "ObjectivePveKills", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(connection, "offline_runtime_status", "ServerMemoryMb", "REAL NOT NULL DEFAULT 0");
        EnsureColumn(connection, "offline_runtime_status", "TickP95Ms", "REAL NOT NULL DEFAULT 0");
        EnsureColumn(connection, "offline_runtime_status", "AiWorkQueue", "INTEGER NOT NULL DEFAULT 0");

        Execute(connection, "CREATE INDEX IF NOT EXISTS IX_offline_world_bots_objective ON offline_world_bots(ObjectiveKind, IsOnline, Realm, Level)");

        Execute(connection, """
            DELETE FROM offline_population_settings
            WHERE Key IN ('PopulationTarget', 'ActiveBotBudget', 'WarmBotBudget', 'BackgroundProgression', 'RealmDistribution');
            """);

        var settings = new (string Key, string Value, string Description)[]
        {
            ("PopulationEnabled", "false", "Master safety switch. No bots are created or spawned until explicitly enabled."),
            ("ActiveTarget", "0", "Requested number of real world bot objects. Zero until the owner attends the first spawn."),
            ("HardActiveCap", "0", "Legacy compatibility value only; the complete non-retired roster is always targeted."),
            ("InitialCohortSize", "3", "First measured cohort: one fully active bot per realm."),
            ("ActiveOnly", "true", "All progression requires a live bot object performing real world actions."),
            ("BackgroundProgression", "false", "Time-skipped and offline simulated progression is forbidden."),
            ("StartupRampMinutes", "60", "Stagger 30% of persisted bot logins through the first five minutes and the remaining 70% through the next 55 minutes."),
            ("AllowCharacterDeletion", "false", "Shutdown, restart, or lower population targets park characters and never delete progress, inventory, bank contents, equipment, or money."),
            ("AiBudgetMs", "4.0", "Stagger live bot thinking and defer excess AI work without simulating progress."),
            ("WorldActorOnly", "true", "Every progressing bot must remain a real loaded world actor."),
            ("ExperienceRate", "1.0", "Original one-times experience multiplier."),
            ("RulesetPatch", "1.65", "Classic pre-Trials of Atlantis balance target."),
            ("MaximumExpansion", "Shrouded Isles", "Classic and Shrouded Isles content only."),
        };

        using var command = connection.CreateCommand();
        command.CommandText = "INSERT OR REPLACE INTO offline_population_settings (Key, Value, Description) VALUES (@key, @value, @description)";
        command.Parameters.Add("@key", DbType.String);
        command.Parameters.Add("@value", DbType.String);
        command.Parameters.Add("@description", DbType.String);

        foreach (var setting in settings)
        {
            command.Parameters["@key"].Value = setting.Key;
            command.Parameters["@value"].Value = setting.Value;
            command.Parameters["@description"].Value = setting.Description;
            command.ExecuteNonQuery();
        }

        var auctionSettings = new (string Key, string Value, string Description)[]
        {
            ("Enabled", "true", "The real-item Realm Exchange is available through one broker in each capital city."),
            ("AlbionLocation", "10,36200,30300,8000", "Camelot City, on the verified vault corridor."),
            ("MidgardLocation", "101,32250,28294,8819", "Jordheim, on the verified vault corridor."),
            ("HiberniaLocation", "201,33197,31340,8000", "Tir na Nog, on the verified vault corridor."),
            ("RequireRealInventoryItem", "true", "Every listing must escrow an actual persisted inventory item."),
            ("RequireRealCoinEscrow", "true", "Every bid and buyout must reserve actual bot copper atomically."),
            ("AllowSyntheticListings", "false", "Never generate fake items, bids, sales, or market activity."),
            ("DefaultListingHours", "24", "Default real-time listing duration."),
        };

        using var auctionCommand = connection.CreateCommand();
        auctionCommand.CommandText = "INSERT OR REPLACE INTO offline_auction_settings (Key, Value, Description) VALUES (@key, @value, @description)";
        auctionCommand.Parameters.Add("@key", DbType.String);
        auctionCommand.Parameters.Add("@value", DbType.String);
        auctionCommand.Parameters.Add("@description", DbType.String);
        foreach (var setting in auctionSettings)
        {
            auctionCommand.Parameters["@key"].Value = setting.Key;
            auctionCommand.Parameters["@value"].Value = setting.Value;
            auctionCommand.Parameters["@description"].Value = setting.Description;
            auctionCommand.ExecuteNonQuery();
        }
    }

    private static void EnsureRealmExchangeBrokers(SQLiteConnection connection)
    {
        var brokers = new[]
        {
            new { Id = "offline-realm-exchange-albion", Realm = 1, Region = 10, X = 36200, Y = 30300, Z = 8000, Heading = 0, MaleModel = 79, FemaleModel = 45, Equipment = "AlbMerchantArmorStudded",
                GuardModel = 28, GuardEquipment = "be3e91b7-35a0-4a51-b8a0-6e9f7832f1e3", GuardName = "Royal Exchange Guard", Guard1X = 36110, Guard1Y = 30300, Guard2X = 36290, Guard2Y = 30300,
                Male = new[] { "Aldric", "Cedric", "Godfrey", "Leofric", "Oswin", "Renwald" }, Female = new[] { "Adalyn", "Elowen", "Isabel", "Roswen", "Ysanne" } },
            new { Id = "offline-realm-exchange-midgard", Realm = 2, Region = 101, X = 32250, Y = 28294, Z = 8819, Heading = 2048, MaleModel = 159, FemaleModel = 161, Equipment = "MidChainDarkCloak",
                GuardModel = 217, GuardEquipment = "MidTownGuard2", GuardName = "Valkyrie Exchange Guard", Guard1X = 32150, Guard1Y = 28294, Guard2X = 32350, Guard2Y = 28294,
                Male = new[] { "Arnvald", "Dagmund", "Eirik", "Haldgrim", "Sigsten", "Torulf" }, Female = new[] { "Astrid", "Brynhild", "Gudrun", "Ingrid", "Sigrid" } },
            new { Id = "offline-realm-exchange-hibernia", Realm = 3, Region = 201, X = 33197, Y = 31340, Z = 8000, Heading = 512, MaleModel = 384, FemaleModel = 312, Equipment = "HibClothAlt2",
                GuardModel = 387, GuardEquipment = "f845eb8e-1da2-4c34-86e5-a1b87108e9c1", GuardName = "Sentinel Exchange Guard", Guard1X = 33097, Guard1Y = 31340, Guard2X = 33297, Guard2Y = 31340,
                Male = new[] { "Aedan", "Branric", "Ciaran", "Eoghan", "Niallan", "Rian" }, Female = new[] { "Aine", "Caoilinn", "Eilwen", "Maeve", "Niamh", "Orla" } },
        };

        using var upsert = connection.CreateCommand();
        upsert.CommandText = """
            INSERT INTO Mob
                (ClassType, Name, Guild, X, Y, Z, Speed, Heading, Region, Model, Size, EquipmentTemplateID,
                 Strength, Constitution, Dexterity, Quickness, Intelligence, Piety, Empathy, Charisma,
                 Level, Realm, NPCTemplateID, Race, Flags, AggroLevel, AggroRange, MeleeDamageType,
                 RespawnInterval, FactionID, BodyType, HouseNumber, OwnerID, RoamingRange,
                 IsCloakHoodUp, Gender, PackageID, VisibleWeaponSlots, Mob_ID)
            VALUES
                (@class, @name, @guild, @x, @y, @z, 0, @heading, @region, @model, 50, @equipment,
                 60, 60, 60, 60, 60, 60, 60, 60,
                 50, @realm, 0, 0, 0, 0, 0, 2,
                 0, 0, 0, 0, '', 0,
                 0, @gender, 'offline_realm_exchange', 0, @id)
            ON CONFLICT(Mob_ID) DO UPDATE SET
                ClassType=excluded.ClassType, Name=excluded.Name, Guild=excluded.Guild,
                X=excluded.X, Y=excluded.Y, Z=excluded.Z, Heading=excluded.Heading,
                Region=excluded.Region, Model=excluded.Model, EquipmentTemplateID=excluded.EquipmentTemplateID,
                Level=excluded.Level, Realm=excluded.Realm, Gender=excluded.Gender,
                PackageID=excluded.PackageID
            """;
        foreach (string parameter in new[] { "@class", "@name", "@guild", "@equipment", "@id" }) upsert.Parameters.Add(parameter, DbType.String);
        foreach (string parameter in new[] { "@x", "@y", "@z", "@heading", "@region", "@model", "@realm", "@gender" }) upsert.Parameters.Add(parameter, DbType.Int32);

        foreach (var broker in brokers)
        {
            using var existing = connection.CreateCommand();
            existing.CommandText = "SELECT Name, Gender FROM Mob WHERE Mob_ID=@id";
            existing.Parameters.AddWithValue("@id", broker.Id);
            string currentName = null;
            int currentGender = 0;
            using (var reader = existing.ExecuteReader())
            {
                if (reader.Read())
                {
                    currentName = reader.IsDBNull(0) ? null : reader.GetString(0);
                    currentGender = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                }
            }

            bool female = currentGender == (int)eGender.Female || currentGender == 0 && RandomNumberGenerator.GetInt32(2) == 1;
            string[] names = female ? broker.Female : broker.Male;
            Upsert("DOL.GS.RealmExchangeBroker", currentName ?? names[RandomNumberGenerator.GetInt32(names.Length)], "Realm Exchange",
                broker.X, broker.Y, broker.Z, broker.Heading, broker.Region, female ? broker.FemaleModel : broker.MaleModel,
                broker.Equipment, broker.Realm, female ? (int)eGender.Female : (int)eGender.Male, broker.Id);

            Upsert("DOL.GS.GameGuard", broker.GuardName, "Exchange Guard", broker.Guard1X, broker.Guard1Y, broker.Z,
                broker.Heading, broker.Region, broker.GuardModel, broker.GuardEquipment, broker.Realm, 0, $"{broker.Id}-guard-left");
            Upsert("DOL.GS.GameGuard", broker.GuardName, "Exchange Guard", broker.Guard2X, broker.Guard2Y, broker.Z,
                broker.Heading, broker.Region, broker.GuardModel, broker.GuardEquipment, broker.Realm, 0, $"{broker.Id}-guard-right");
        }

        void Upsert(string classType, string name, string guild, int x, int y, int z, int heading, int region,
            int model, string equipment, int realm, int gender, string id)
        {
            upsert.Parameters["@class"].Value = classType;
            upsert.Parameters["@name"].Value = name;
            upsert.Parameters["@guild"].Value = guild;
            upsert.Parameters["@equipment"].Value = equipment;
            upsert.Parameters["@id"].Value = id;
            upsert.Parameters["@x"].Value = x;
            upsert.Parameters["@y"].Value = y;
            upsert.Parameters["@z"].Value = z;
            upsert.Parameters["@heading"].Value = heading;
            upsert.Parameters["@region"].Value = region;
            upsert.Parameters["@model"].Value = model;
            upsert.Parameters["@realm"].Value = realm;
            upsert.Parameters["@gender"].Value = gender;
            upsert.ExecuteNonQuery();
        }
    }

    private static void ApplyClassic165SpawnProfile(SQLiteConnection connection)
    {
        const string migrationId = "classic165-legacy-spawns-v1";
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        Execute(connection, """
            CREATE TABLE IF NOT EXISTS offline_world_migrations (
                MigrationId TEXT PRIMARY KEY,
                AppliedUtc TEXT NOT NULL,
                Description TEXT NOT NULL,
                ArchivedRows INTEGER NOT NULL
            );
            CREATE TABLE IF NOT EXISTS offline_classic165_removed_mobs AS
                SELECT Mob.*, '' AS RemovalReason, '' AS RemovedUtc
                FROM Mob WHERE 0;
            CREATE UNIQUE INDEX IF NOT EXISTS IX_offline_classic165_removed_mobs_id
                ON offline_classic165_removed_mobs(Mob_ID);
            """);

        using var alreadyApplied = connection.CreateCommand();
        alreadyApplied.CommandText = "SELECT COUNT(*) FROM offline_world_migrations WHERE MigrationId=@id";
        alreadyApplied.Parameters.AddWithValue("@id", migrationId);
        if (Convert.ToInt32(alreadyApplied.ExecuteScalar(), CultureInfo.InvariantCulture) > 0)
        {
            transaction.Commit();
            Console.WriteLine("Classic 1.65 legacy spawn profile is already active.");
            return;
        }

        string timestamp = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
        using var archive = connection.CreateCommand();
        archive.CommandText = """
            INSERT OR IGNORE INTO offline_classic165_removed_mobs
            SELECT Mob.*, 'Post-legacy generic neutral spawn excluded from the server 1.65 authority', @removed
            FROM Mob
            JOIN Regions ON Regions.RegionID = Mob.Region
            WHERE Regions.Expansion = 0
              AND Mob.ClassType = 'DOL.GS.GameNPC'
              AND Mob.Realm = 0
              AND Mob.Region <> 249
              AND COALESCE(Mob.PackageID, '') <> 'offline_realm_exchange'
              AND COALESCE(Mob.LastTimeRowUpdated, '') >= '2021-01-01';
            """;
        archive.Parameters.AddWithValue("@removed", timestamp);
        int archived = archive.ExecuteNonQuery();

        using var delete = connection.CreateCommand();
        delete.CommandText = """
            DELETE FROM Mob
            WHERE Mob_ID IN (
                SELECT Mob_ID FROM offline_classic165_removed_mobs
                WHERE RemovalReason = 'Post-legacy generic neutral spawn excluded from the server 1.65 authority'
            );
            """;
        delete.ExecuteNonQuery();

        using var record = connection.CreateCommand();
        record.CommandText = """
            INSERT INTO offline_world_migrations(MigrationId, AppliedUtc, Description, ArchivedRows)
            VALUES (@id, @utc,
                'Reproducible Classic/SI authority: retain legacy DOL neutral spawns and all service/scripted/custom NPCs; archive later generic neutral Atlas additions.',
                @count);
            """;
        record.Parameters.AddWithValue("@id", migrationId);
        record.Parameters.AddWithValue("@utc", timestamp);
        record.Parameters.AddWithValue("@count", archived);
        record.ExecuteNonQuery();
        transaction.Commit();
        Console.WriteLine($"Archived and removed {archived:N0} later generic neutral spawns; legacy Classic/SI spawns remain authoritative.");
    }

    private static void RestoreClassic165ShroudedIslesAndDarknessFallsSpawns(SQLiteConnection connection)
    {
        const string migrationId = "classic165-si-darkness-falls-spawns-v1";
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        using var alreadyApplied = connection.CreateCommand();
        alreadyApplied.CommandText = "SELECT COUNT(*) FROM offline_world_migrations WHERE MigrationId=@id";
        alreadyApplied.Parameters.AddWithValue("@id", migrationId);
        if (Convert.ToInt32(alreadyApplied.ExecuteScalar(), CultureInfo.InvariantCulture) > 0)
        {
            transaction.Commit();
            Console.WriteLine("Classic 1.65 Shrouded Isles and Darkness Falls spawn restoration is already active.");
            return;
        }

        var columns = new List<string>();
        using (var info = connection.CreateCommand())
        {
            info.CommandText = "PRAGMA table_info(Mob)";
            using var reader = info.ExecuteReader();
            while (reader.Read())
                columns.Add(reader.GetString(1));
        }
        string list = string.Join(",", columns.Select(QuoteIdentifier));
        using var restore = connection.CreateCommand();
        restore.CommandText = $"""
            INSERT OR IGNORE INTO Mob ({list})
            SELECT {list}
            FROM offline_classic165_removed_mobs
            WHERE Region=249 OR Region IN (SELECT RegionID FROM Regions WHERE Expansion=1)
            """;
        int restored = restore.ExecuteNonQuery();

        using var record = connection.CreateCommand();
        record.CommandText = """
            INSERT INTO offline_world_migrations(MigrationId, AppliedUtc, Description, ArchivedRows)
            VALUES (@id, @utc,
                'Restore archived Shrouded Isles and Classic Darkness Falls progression; SI and region 249 are outside the Classic overpopulation cleanup boundary.',
                @count);
            """;
        record.Parameters.AddWithValue("@id", migrationId);
        record.Parameters.AddWithValue("@utc", DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
        record.Parameters.AddWithValue("@count", restored);
        record.ExecuteNonQuery();
        transaction.Commit();
        Console.WriteLine($"Restored {restored:N0} Shrouded Isles and Classic Darkness Falls monster/NPC rows from the recoverable spawn archive.");
    }

    private static void RestoreConservativeClassic165FrontierAndAlbionSpawns(SQLiteConnection connection)
    {
        const string migrationId = "classic165-conservative-frontier-albion-spawns-v1";
        ApplyArchivedSpawnManifest(connection, migrationId,
            LoadRestoredSpawnIds("classic165_restored_spawn_ids.txt"),
            ExpectedConservativeRestoredSpawnCount,
            "Restore only archive rows corroborated by CapnBry or the 2002 Kirstena Cruachan map, cap source camps at two rows, and require installed-navmesh route validation.",
            "conservatively verified Old Frontier and Albion");
    }

    private static void RestorePeriodMapClassic165FrontierAndAlbionSpawns(SQLiteConnection connection)
    {
        const string migrationId = "classic165-period-map-frontier-albion-spawns-v2";
        ApplyArchivedSpawnManifest(connection, migrationId,
            LoadRestoredSpawnIds("classic165_period_restored_spawn_ids.txt"),
            ExpectedPeriodMapRestoredSpawnCount,
            "Restore only archived rows corroborated by 2002 Kirstena/Illia maps, CapnBry coordinates, or period Mount Collory/Cruachan reports; preserve original templates and require bidirectional installed-navmesh validation.",
            "period-map verified Old Frontier and Albion");
    }

    private static void ApplyArchivedSpawnManifest(SQLiteConnection connection, string migrationId,
        string[] mobIds, int expectedCount, string description, string label)
    {
        if (mobIds.Length != expectedCount)
            throw new InvalidOperationException($"{label} spawn manifest contains {mobIds.Length:N0} IDs; expected {expectedCount:N0}.");

        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        Execute(connection, """
            CREATE TABLE IF NOT EXISTS offline_classic165_restored_mobs (
                Mob_ID TEXT PRIMARY KEY,
                MigrationId TEXT NOT NULL,
                RestoredUtc TEXT NOT NULL
            );
            CREATE TEMP TABLE IF NOT EXISTS offline_restore_manifest_ids (
                Mob_ID TEXT PRIMARY KEY
            );
            DELETE FROM offline_restore_manifest_ids;
            """);

        using (var insertId = connection.CreateCommand())
        {
            insertId.CommandText = "INSERT INTO offline_restore_manifest_ids(Mob_ID) VALUES (@id)";
            SQLiteParameter parameter = insertId.Parameters.Add("@id", DbType.String);
            foreach (string mobId in mobIds)
            {
                parameter.Value = mobId;
                insertId.ExecuteNonQuery();
            }
        }

        using var alreadyApplied = connection.CreateCommand();
        alreadyApplied.CommandText = "SELECT COUNT(*) FROM offline_world_migrations WHERE MigrationId=@id";
        alreadyApplied.Parameters.AddWithValue("@id", migrationId);
        if (Convert.ToInt32(alreadyApplied.ExecuteScalar(), CultureInfo.InvariantCulture) > 0)
        {
            transaction.Commit();
            Console.WriteLine($"Classic 1.65 {label} spawn restoration is already active.");
            return;
        }

        using (var completeness = connection.CreateCommand())
        {
            completeness.CommandText = """
                SELECT COUNT(*)
                FROM offline_restore_manifest_ids ids
                WHERE NOT EXISTS (SELECT 1 FROM offline_classic165_removed_mobs archived WHERE archived.Mob_ID=ids.Mob_ID)
                  AND NOT EXISTS (SELECT 1 FROM Mob live WHERE live.Mob_ID=ids.Mob_ID)
                """;
            int missing = Convert.ToInt32(completeness.ExecuteScalar(), CultureInfo.InvariantCulture);
            if (missing != 0)
                throw new InvalidOperationException($"Conservative spawn migration is missing {missing:N0} archived source rows.");
        }

        var columns = new List<string>();
        using (var info = connection.CreateCommand())
        {
            info.CommandText = "PRAGMA table_info(Mob)";
            using var reader = info.ExecuteReader();
            while (reader.Read())
                columns.Add(reader.GetString(1));
        }
        string list = string.Join(",", columns.Select(QuoteIdentifier));
        using var restore = connection.CreateCommand();
        restore.CommandText = $"""
            INSERT OR IGNORE INTO Mob ({list})
            SELECT {string.Join(",", columns.Select(column => "archived." + QuoteIdentifier(column)))}
            FROM offline_classic165_removed_mobs archived
            JOIN offline_restore_manifest_ids ids ON ids.Mob_ID=archived.Mob_ID
            """;
        int restored = restore.ExecuteNonQuery();
        string timestamp = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);

        using (var track = connection.CreateCommand())
        {
            track.CommandText = """
                INSERT OR REPLACE INTO offline_classic165_restored_mobs(Mob_ID,MigrationId,RestoredUtc)
                SELECT Mob_ID,@migration,@utc FROM offline_restore_manifest_ids
                """;
            track.Parameters.AddWithValue("@migration", migrationId);
            track.Parameters.AddWithValue("@utc", timestamp);
            track.ExecuteNonQuery();
        }

        using (var record = connection.CreateCommand())
        {
            record.CommandText = """
                INSERT INTO offline_world_migrations(MigrationId, AppliedUtc, Description, ArchivedRows)
                VALUES (@id,@utc,
                    @description,
                    @count)
                """;
            record.Parameters.AddWithValue("@id", migrationId);
            record.Parameters.AddWithValue("@utc", timestamp);
            record.Parameters.AddWithValue("@description", description);
            record.Parameters.AddWithValue("@count", restored);
            record.ExecuteNonQuery();
        }
        transaction.Commit();
        Console.WriteLine($"Restored {restored:N0} {label} monster rows.");
    }

    private static string[] LoadRestoredSpawnIds(string suffix)
    {
        Assembly assembly = typeof(Program).Assembly;
        string resource = assembly.GetManifestResourceNames().SingleOrDefault(name =>
            name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"The Classic 1.65 spawn manifest '{suffix}' is missing.");
        using Stream stream = assembly.GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException($"The Classic 1.65 spawn manifest '{suffix}' could not be opened.");
        using var reader = new StreamReader(stream, Encoding.ASCII);
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (reader.ReadLine() is { } line)
        {
            line = line.Trim();
            if (line.Length == 0)
                continue;
            if (!Guid.TryParse(line, out _) &&
                (!long.TryParse(line, NumberStyles.None, CultureInfo.InvariantCulture, out long numericId) || numericId <= 0))
                throw new InvalidOperationException($"Invalid Mob_ID in Classic 1.65 spawn manifest '{suffix}': '{line}'.");
            result.Add(line);
        }
        return result.OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static void VerifyClassic165SpawnProfile(SQLiteConnection connection)
    {
        using var verify = connection.CreateCommand();
        verify.CommandText = """
            SELECT
                (SELECT COUNT(*) FROM offline_world_migrations WHERE MigrationId='classic165-legacy-spawns-v1'),
                (SELECT COUNT(*) FROM offline_world_migrations WHERE MigrationId='classic165-si-darkness-falls-spawns-v1'),
                (SELECT COUNT(*) FROM offline_world_migrations WHERE MigrationId='classic165-conservative-frontier-albion-spawns-v1'),
                (SELECT COUNT(*) FROM offline_world_migrations WHERE MigrationId='classic165-period-map-frontier-albion-spawns-v2'),
                (SELECT COUNT(*) FROM offline_classic165_restored_mobs),
                (SELECT COUNT(*) FROM Mob WHERE PackageID='offline_realm_exchange' AND ClassType='DOL.GS.RealmExchangeBroker'),
                (SELECT COUNT(*) FROM Mob WHERE PackageID='offline_realm_exchange' AND Guild='Exchange Guard'),
                (SELECT COUNT(*) FROM Mob JOIN Regions ON Regions.RegionID=Mob.Region
                    WHERE Regions.Expansion=0 AND Mob.ClassType='DOL.GS.GameNPC' AND Mob.Realm=0
                    AND Mob.Region <> 249
                    AND Mob.Mob_ID NOT IN (SELECT Mob_ID FROM offline_classic165_restored_mobs)
                    AND COALESCE(Mob.PackageID,'') <> 'offline-classic-frontier-dungeon-restored'
                    AND COALESCE(Mob.LastTimeRowUpdated,'') >= '2021-01-01');
            """;
        using var reader = verify.ExecuteReader();
        if (!reader.Read() || reader.GetInt32(0) != 1 || reader.GetInt32(1) != 1 || reader.GetInt32(2) != 1 ||
            reader.GetInt32(3) != 1 || reader.GetInt32(4) != ExpectedTotalRestoredSpawnCount || reader.GetInt32(5) != 3 ||
            reader.GetInt32(6) != 6 || reader.GetInt32(7) != 0)
            throw new InvalidOperationException("Classic 1.65 spawn or Realm Exchange verification failed.");
        Console.WriteLine($"Verified: 1.65 spawn profile, {ExpectedTotalRestoredSpawnCount:N0} source-checked restored monsters, 3 wealthy exchange brokers, and 6 capital-themed exchange guards.");
    }

    private static void EnsureColumn(SQLiteConnection connection, string table, string column, string definition)
    {
        using var info = connection.CreateCommand();
        info.CommandText = $"PRAGMA table_info({QuoteIdentifier(table)})";
        using var reader = info.ExecuteReader();
        while (reader.Read())
        {
            if (reader.GetString(1).Equals(column, StringComparison.OrdinalIgnoreCase))
                return;
        }

        Execute(connection, $"ALTER TABLE {QuoteIdentifier(table)} ADD COLUMN {QuoteIdentifier(column)} {definition}");
    }

    private static void ImportWorldData(SQLiteConnection connection, string sourcePath)
    {
        var tables = GetTables(connection);
        var files = Directory.EnumerateFiles(sourcePath, "*.sql").OrderBy(Path.GetFileName).ToArray();
        var importedRows = 0L;
        var skippedTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in files)
        {
            var fileRows = 0L;
            var statementCount = 0;
            var parsedStatementCount = 0;
            var matchedStatementCount = 0;
            var text = File.ReadAllText(file, Encoding.UTF8);
            foreach (var statement in ExtractReplaceStatements(text))
            {
                statementCount++;
                var parsed = ParseHeader(statement);
                if (parsed is not null) parsedStatementCount++;
                if (parsed is null || !tables.TryGetValue(parsed.Value.Table, out var existingColumns))
                {
                    if (parsed is not null)
                    {
                        skippedTables.Add(parsed.Value.Table);
                    }
                    continue;
                }
                matchedStatementCount++;

                var selected = parsed.Value.Columns
                    .Select((name, index) => (Name: name, Index: index))
                    .Where(column => existingColumns.Contains(column.Name))
                    .ToArray();

                if (selected.Length == 0)
                {
                    continue;
                }

                using var transaction = connection.BeginTransaction();
                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = $"INSERT OR REPLACE INTO {QuoteIdentifier(parsed.Value.Table)} ({string.Join(",", selected.Select(column => QuoteIdentifier(column.Name)))}) VALUES ({string.Join(",", selected.Select((_, index) => $"@p{index}"))})";
                for (var index = 0; index < selected.Length; index++)
                {
                    command.Parameters.Add($"@p{index}", DbType.Object);
                }

                foreach (var row in ParseRows(parsed.Value.Values))
                {
                    if (row.Count != parsed.Value.Columns.Length)
                    {
                        throw new FormatException($"Unexpected value count in {Path.GetFileName(file)} for table {parsed.Value.Table}.");
                    }

                    for (var index = 0; index < selected.Length; index++)
                    {
                        command.Parameters[index].Value = ToValue(row[selected[index].Index]);
                    }

                    command.ExecuteNonQuery();
                    importedRows++;
                    fileRows++;
                }

                transaction.Commit();
            }

            Console.WriteLine($"  {Path.GetFileName(file)}: {fileRows:N0} rows ({statementCount} statements, {parsedStatementCount} parsed, {matchedStatementCount} matched)");
        }

        Execute(connection, "PRAGMA journal_mode=WAL; PRAGMA synchronous=NORMAL; PRAGMA optimize;");
        Console.WriteLine($"Imported {importedRows:N0} world-data rows.");
        if (skippedTables.Count > 0)
        {
            Console.WriteLine($"Skipped non-runtime dump tables: {string.Join(", ", skippedTables.OrderBy(value => value))}");
        }
    }

    private static Dictionary<string, HashSet<string>> GetTables(SQLiteConnection connection)
    {
        var result = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        using var tables = connection.CreateCommand();
        tables.CommandText = "SELECT name FROM sqlite_master WHERE type='table'";
        using var reader = tables.ExecuteReader();
        var names = new List<string>();
        while (reader.Read())
        {
            names.Add(reader.GetString(0));
        }

        foreach (var name in names)
        {
            using var columns = connection.CreateCommand();
            columns.CommandText = $"PRAGMA table_info({QuoteIdentifier(name)})";
            using var columnReader = columns.ExecuteReader();
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            while (columnReader.Read())
            {
                set.Add(columnReader.GetString(1));
            }
            result[name] = set;
        }

        return result;
    }

    private static IEnumerable<string> ExtractReplaceStatements(string text)
    {
        const string marker = "REPLACE INTO";
        var search = 0;
        while (true)
        {
            var start = text.IndexOf(marker, search, StringComparison.OrdinalIgnoreCase);
            if (start < 0)
            {
                yield break;
            }

            var quoted = false;
            var escaped = false;
            for (var index = start; index < text.Length; index++)
            {
                var character = text[index];
                if (quoted)
                {
                    if (escaped)
                    {
                        escaped = false;
                    }
                    else if (character == '\\')
                    {
                        escaped = true;
                    }
                    else if (character == '\'')
                    {
                        quoted = false;
                    }
                }
                else if (character == '\'')
                {
                    quoted = true;
                }
                else if (character == ';')
                {
                    yield return text[start..index];
                    search = index + 1;
                    break;
                }

                if (index == text.Length - 1)
                {
                    yield break;
                }
            }
        }
    }

    private static (string Table, string[] Columns, string Values)? ParseHeader(string statement)
    {
        var firstTick = statement.IndexOf('`');
        var secondTick = firstTick < 0 ? -1 : statement.IndexOf('`', firstTick + 1);
        var columnsStart = secondTick < 0 ? -1 : statement.IndexOf('(', secondTick + 1);
        var columnsEnd = columnsStart < 0 ? -1 : statement.IndexOf(')', columnsStart + 1);
        var valuesMarker = columnsEnd < 0 ? -1 : statement.IndexOf("VALUES", columnsEnd + 1, StringComparison.OrdinalIgnoreCase);
        if (firstTick < 0 || secondTick < 0 || columnsStart < 0 || valuesMarker < 0 || columnsEnd < columnsStart)
        {
            return null;
        }

        var table = statement[(firstTick + 1)..secondTick];
        var columns = statement[(columnsStart + 1)..columnsEnd]
            .Split(',')
            .Select(value => value.Trim().Trim('`'))
            .ToArray();
        var values = statement[(valuesMarker + "VALUES".Length)..];
        return (table, columns, values);
    }

    private static IEnumerable<List<string>> ParseRows(string values)
    {
        var index = 0;
        while (index < values.Length)
        {
            while (index < values.Length && (char.IsWhiteSpace(values[index]) || values[index] == ',')) index++;
            if (index >= values.Length) yield break;
            if (values[index] != '(') throw new FormatException("Expected the beginning of a SQL value row.");
            index++;

            var row = new List<string>();
            var token = new StringBuilder();
            var quoted = false;
            var escaped = false;

            while (index < values.Length)
            {
                var character = values[index++];
                if (quoted)
                {
                    token.Append(character);
                    if (escaped)
                    {
                        escaped = false;
                    }
                    else if (character == '\\')
                    {
                        escaped = true;
                    }
                    else if (character == '\'')
                    {
                        quoted = false;
                    }
                    continue;
                }

                if (character == '\'')
                {
                    quoted = true;
                    token.Append(character);
                }
                else if (character == ',')
                {
                    row.Add(token.ToString().Trim());
                    token.Clear();
                }
                else if (character == ')')
                {
                    row.Add(token.ToString().Trim());
                    yield return row;
                    break;
                }
                else
                {
                    token.Append(character);
                }
            }
        }
    }

    private static object ToValue(string token)
    {
        if (token.Equals("NULL", StringComparison.OrdinalIgnoreCase)) return DBNull.Value;
        if (token.StartsWith("b'", StringComparison.OrdinalIgnoreCase) && token.EndsWith('\'')) token = token[1..];
        if (token.Length >= 2 && token[0] == '\'' && token[^1] == '\'') return DecodeMySqlString(token[1..^1]);
        if (long.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integer)) return integer;
        if (double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var real)) return real;
        return token;
    }

    private static string DecodeMySqlString(string value)
    {
        var output = new StringBuilder(value.Length);
        for (var index = 0; index < value.Length; index++)
        {
            if (value[index] != '\\' || index + 1 >= value.Length)
            {
                output.Append(value[index]);
                continue;
            }

            var escaped = value[++index];
            output.Append(escaped switch
            {
                '0' => '\0',
                'n' => '\n',
                'r' => '\r',
                't' => '\t',
                'Z' => (char)26,
                _ => escaped,
            });
        }
        return output.ToString();
    }

    private static void ApplyClassicSiRules(SQLiteConnection connection)
    {
        var rules = new (string Category, string Key, string Value, string Description)[]
        {
            ("rates", "xp_rate", "1", "Original one-times experience rate."),
            ("rates", "cl_xp_rate", "0", "Champion levels disabled for the Classic/SI ruleset."),
            ("rates", "rvr_zones_xp_rate", "1", "Original one-times frontier experience rate."),
            ("rates", "rp_rate", "1", "Original one-times realm-point rate."),
            ("rates", "bp_rate", "1", "Original one-times bounty-point rate."),
            ("rates", "money_drop", "1", "Original one-times money-drop rate."),
            ("system", "disabled_expansions", "3;4;5;6", "Disable Trials of Atlantis and every later expansion."),
            ("classes", "disabled_classes", "20;33;34;39;58-62", "Allow only Classic and Shrouded Isles classes; block Catacombs and later classes at creation and trainers."),
            ("classes", "disabled_races", "16-21", "Allow only Classic and Shrouded Isles races; block Trials of Atlantis and later races at creation."),
            ("autonomous_population", "population_enabled", "false", "No autonomous bot is created or spawned until the first supervised enablement; once enabled, bots remain active without requiring a human player online."),
            ("autonomous_population", "active_target", "0", "Start with zero active bots."),
            ("autonomous_population", "hard_active_cap", "0", "Legacy compatibility value only; it does not limit the complete roster."),
            ("autonomous_population", "initial_cohort_size", "3", "First observed cohort is one real bot per realm."),
            ("autonomous_population", "active_only", "true", "Forbid background or time-skipped progression."),
            ("autonomous_population", "startup_ramp_minutes", "15", "Stagger 33% of bot logins through the first five minutes and the remaining roster through the next ten minutes."),
            ("autonomous_population", "allow_character_deletion", "false", "Never delete bot characters or their progress when unloading them; deletion requires a separate explicit owner action."),
            ("autonomous_population", "ai_budget_ms", "4.0", "Per-loop autonomous decision budget. Excess real work waits for a later tick and is never simulated."),
            ("autonomous_population", "world_actor_only", "true", "Require every progressing bot to be a real loaded world actor."),
            ("system", "allow_all_realms", "true", "Offline account may create characters in all three realms."),
            ("system", "load_housing_items", "false", "Housing content is outside the initial Classic/SI world."),
            ("system", "load_housing_npc", "false", "Housing content is outside the initial Classic/SI world."),
            ("server", "disable_instances", "true", "Disable post-SI instanced content."),
            ("server", "use_new_passives_ras_scaling", "false", "Use classic realm-ability scaling."),
            ("server", "use_new_actives_ras_scaling", "false", "Use classic realm-ability scaling."),
            ("server", "free_respec", "false", "No custom free-respec convenience feature."),
            ("server", "serverlistupdate_enabled", "false", "Never advertise the private offline server."),
            ("server", "Discord_Webhook_Active", "false", "No external service integrations."),
            ("server", "enable_pve_speed", "false", "Disable later custom out-of-combat speed boost."),
            ("pve", "currency_exchange_allow", "false", "Disable post-SI currencies and exchanges."),
            ("pve", "bp_exchange_allow", "false", "Disable post-SI currencies and exchanges."),
            ("pve", "atlantis_teleport_plvl", "4", "No player account can enter Atlantis."),
            ("atlas_rog", "rog_toa_item_chance", "0", "Never generate Trials of Atlantis item bonuses."),
        };

        using var command = connection.CreateCommand();
        command.CommandText = "INSERT OR REPLACE INTO ServerProperty (Category, Key, Description, DefaultValue, Value) VALUES (@category, @key, @description, @value, @value)";
        command.Parameters.Add("@category", DbType.String);
        command.Parameters.Add("@key", DbType.String);
        command.Parameters.Add("@description", DbType.String);
        command.Parameters.Add("@value", DbType.String);

        foreach (var rule in rules)
        {
            command.Parameters["@category"].Value = rule.Category;
            command.Parameters["@key"].Value = rule.Key;
            command.Parameters["@description"].Value = rule.Description;
            command.Parameters["@value"].Value = rule.Value;
            command.ExecuteNonQuery();
        }
    }

    private static void CreateAccount(SQLiteConnection connection, string accountName, string password)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT OR REPLACE INTO Account
                (Name, Password, CreationDate, LastLogin, Realm, PrivLevel, Status, Mail,
                 LastLoginIP, LastClientVersion, Language, IsMuted, IsWarned, Notes,
                 IsTester, CharactersTraded, SoloCharactersTraded, DiscordID,
                 Realm_Timer_Realm, Realm_Timer_Last_Combat, LastDisconnected)
            VALUES
                (@name, @password, @created, NULL, 0, 1, 0, NULL,
                 '127.0.0.1', NULL, 'EN', 0, 0, 'Offline Classic/SI owner account',
                 0, 0, 0, NULL, 0, NULL, NULL)
            """;
        command.Parameters.AddWithValue("@name", accountName);
        command.Parameters.AddWithValue("@password", HashPassword(password));
        command.Parameters.AddWithValue("@created", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
        command.ExecuteNonQuery();
    }

    private static string HashPassword(string password)
    {
        var characters = password.ToCharArray();
        var bytes = new byte[characters.Length * 2];
        for (var index = 0; index < characters.Length; index++)
        {
            bytes[index * 2] = (byte)(characters[index] >> 8);
            bytes[index * 2 + 1] = (byte)characters[index];
        }

        var hash = MD5.HashData(bytes);
        return "##" + string.Concat(hash.Select(value => value.ToString("X", CultureInfo.InvariantCulture)));
    }

    private static void WriteCredentials(Options options)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(options.CredentialsPath)!);
        File.WriteAllText(options.CredentialsPath,
            $"Offline DAoC: Classic + Shrouded Isles{Environment.NewLine}" +
            $"Account: {options.AccountName}{Environment.NewLine}" +
            $"Password: {options.Password}{Environment.NewLine}" +
            "Server: 127.0.0.1:10300\r\n", Encoding.UTF8);
    }

    private static void WriteRuleset(Options options)
    {
        var path = Path.Combine(Path.GetDirectoryName(options.CredentialsPath)!, "ruleset.txt");
        File.WriteAllText(path, """
            Offline DAoC ruleset
            ====================
            Target patch: 1.65 (pre-Trials of Atlantis)
            Enabled content: Classic + Shrouded Isles
            XP / RP / BP / money rates: 1x
            Disabled: Trials of Atlantis, Catacombs, Darkness Rising, Labyrinth, champion levels,
                      post-SI currencies, instances, custom free respecs, custom PvE speed boosts,
                      server-list advertising, UPnP, and external webhooks.

            Autonomous population policy: active world objects only. No offline, time-skipped, warm-tier,
            cold-tier, or simulated progression is permitted. Population creation is disabled and the
            active target is zero until the owner attends the first measured spawn.

            Auction policy: real persisted items and real copper escrow only. Synthetic listings and
            transactions are forbidden. The auction is disabled and its location is intentionally unset.
            """, Encoding.UTF8);
    }

    private static void Verify(SQLiteConnection connection, string accountName)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                (SELECT COUNT(*) FROM sqlite_master WHERE type='table') AS Tables,
                (SELECT COUNT(*) FROM Account WHERE Name=@name) AS Accounts,
                (SELECT COUNT(*) FROM ServerProperty WHERE Key='xp_rate' AND Value='1') AS XpRules,
                (SELECT COUNT(*) FROM ServerProperty WHERE Key='disabled_expansions' AND Value='3;4;5;6') AS ExpansionRules,
                (SELECT COUNT(*) FROM ServerProperty WHERE Key='disabled_classes' AND Value='20;33;34;39;58-62') AS ClassRules,
                (SELECT COUNT(*) FROM ServerProperty WHERE Key='disabled_races' AND Value='16-21') AS RaceRules,
                (SELECT COUNT(*) FROM offline_population_settings WHERE Key='PopulationEnabled' AND Value='false') AS PopulationOff,
                (SELECT COUNT(*) FROM offline_population_settings WHERE Key='ActiveTarget' AND Value='0') AS TargetZero,
                (SELECT COUNT(*) FROM offline_population_settings WHERE Key='ActiveOnly' AND Value='true') AS ActiveOnly,
                (SELECT COUNT(*) FROM offline_world_bots) AS Bots,
                (SELECT COUNT(*) FROM offline_auction_settings WHERE Key='Enabled' AND Value='false') AS AuctionOff,
                (SELECT COUNT(*) FROM offline_auction_settings WHERE Key='AllowSyntheticListings' AND Value='false') AS SyntheticOff,
                (SELECT COUNT(*) FROM offline_auction_listings) AS Listings
            """;
        command.Parameters.AddWithValue("@name", accountName);
        using var reader = command.ExecuteReader();
        if (!reader.Read() || Enumerable.Range(1, 8).Any(index => reader.GetInt32(index) != 1) ||
            reader.GetInt32(9) != 0 || reader.GetInt32(10) != 1 || reader.GetInt32(11) != 1 || reader.GetInt32(12) != 0)
        {
            throw new InvalidOperationException("Database verification failed.");
        }
        Console.WriteLine($"Verified {reader.GetInt32(0)} SQLite tables, account, 1x XP, SI-only classes/races, active-only policy, zero bots, and zero auction listings.");
    }

    private static void Execute(SQLiteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private static string QuoteIdentifier(string value) => $"`{value.Replace("`", "``")}`";

    private sealed record Options(string DatabasePath, string SqlSourcePath, string AccountName, string Password, string CredentialsPath)
    {
        public static Options Parse(string[] args)
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index + 1 < args.Length; index += 2)
            {
                values[args[index]] = args[index + 1];
            }

            string Required(string name) => values.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value)
                ? Path.GetFullPath(value)
                : throw new ArgumentException($"Missing required argument {name}.");

            string Text(string name) => values.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value)
                ? value
                : throw new ArgumentException($"Missing required argument {name}.");

            return new Options(Required("--database"), Required("--sql-source"), Text("--account"), Text("--password"), Required("--credentials"));
        }
    }
}
