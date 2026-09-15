using System.Collections;
using System.Data.SQLite;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;

[TestFixture, NonParallelizable, Apartment(ApartmentState.STA)]
public sealed class ExchangeDisplayTests
{
    private static readonly Assembly Launcher = Assembly.Load("OfflineDAoC");
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;

    [TestCase(100, "1s")]
    [TestCase(1000101, "1p 1s 1c")]
    public void LedgerSharesExistingCopperFormatter(long copper, string expected)
    {
        var method = Launcher.GetType("OfflineDaoc.Launcher.MainForm").GetMethod("FormatCopper", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(method.Invoke(null, [copper]), Is.EqualTo(expected));
    }

    [Test]
    public void ListingQueryResolvesTemplateKeysRatherThanInternalIds()
    {
        string sourcePath = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "OfflineDaoc.Launcher", "MainForm.cs"));
        string source = File.ReadAllText(sourcePath);
        string query = Regex.Match(source, @"SELECT COALESCE\(NULLIF\(u.Name.*?ORDER BY RealmName, ItemLevel DESC, ItemName", RegexOptions.Singleline).Value
            .Replace("{listedUtcColumn}", "COALESCE(i.RealmExchangeListedUtc, '')");
        Assert.That(query, Is.Not.Empty);
        using var db = new SQLiteConnection("Data Source=:memory:;Version=3;");
        db.Open();
        using var command = db.CreateCommand();
        command.CommandText = """
            CREATE TABLE Inventory (OwnerLot INTEGER, SellPrice INTEGER, ITemplate_Id TEXT, UTemplate_Id TEXT, Count INTEGER, OwnerID TEXT, RealmExchangeListedUtc TEXT);
            CREATE TABLE ItemUnique (Id_nb TEXT PRIMARY KEY, ItemUnique_ID TEXT, Name TEXT, Level INTEGER);
            CREATE TABLE ItemTemplate (Id_nb TEXT PRIMARY KEY, ItemTemplate_ID TEXT, Name TEXT, Level INTEGER);
            CREATE TABLE DOLCharacters (DOLCharacters_ID TEXT, Name TEXT);
            CREATE TABLE offline_world_bots (BotId INTEGER, Name TEXT);
            INSERT INTO DOLCharacters VALUES ('player-one','Seller');
            INSERT INTO ItemUnique VALUES ('Unique_123','different-row-id','Fortifying Cloth Gloves',5);
            INSERT INTO ItemTemplate VALUES ('sleeves_123','another-row-id','woolen padded sleeves',2);
            INSERT INTO Inventory VALUES (65002,100,NULL,'Unique_123',1,'player-one','');
            INSERT INTO Inventory VALUES (65002,100,'sleeves_123',NULL,1,'player-one','');
            INSERT INTO Inventory VALUES (65002,100,'missing-template',NULL,1,'player-one','');
            """;
        command.ExecuteNonQuery();
        command.CommandText = query;
        using var reader = command.ExecuteReader();
        var names = new List<string>();
        while (reader.Read()) names.Add(reader.GetString(0));
        Assert.That(names, Is.EqualTo(new[] { "Fortifying Cloth Gloves", "woolen padded sleeves", "Unknown item" }));
    }

    [Test]
    public void BotRefreshDoesNotReadRealmExchangeInventory()
    {
        string sourcePath = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "OfflineDaoc.Launcher", "MainForm.cs"));
        string source = File.ReadAllText(sourcePath);
        int statusStart = source.IndexOf("private DashboardSnapshot ReadSnapshot()", StringComparison.Ordinal);
        int exchangeStart = source.IndexOf("private List<AuctionRow> ReadRealmExchangeSnapshot()", StringComparison.Ordinal);
        int statusEnd = source.IndexOf("private static double ReadServerRate", statusStart, StringComparison.Ordinal);

        Assert.That(statusStart, Is.GreaterThan(0));
        Assert.That(exchangeStart, Is.GreaterThan(0));
        Assert.That(statusEnd, Is.GreaterThan(statusStart));
        Assert.That(source.Substring(statusStart, statusEnd - statusStart), Does.Not.Contain("FROM Inventory"));
        Assert.That(source.Substring(exchangeStart), Does.Contain("FROM Inventory"));
        Assert.That(source, Does.Contain("REFRESH REALM EXCHANGE"));
    }

    [Test]
    public void LedgerReadsLastFiftyFiltersRealmAndRendersWithoutServer()
    {
        string path = Path.Combine(Path.GetTempPath(), "daoc-launcher-ledger-" + Guid.NewGuid().ToString("N") + ".db");
        try
        {
            using (var db = new SQLiteConnection($"Data Source={path};Version=3;Pooling=False;"))
            {
                db.Open();
                using var command = db.CreateCommand();
                command.CommandText = "CREATE TABLE realm_exchange_sales (SaleSequence INTEGER, SoldUtc TEXT, Realm INTEGER, ItemName TEXT, Quantity INTEGER, SellerId TEXT, SellerName TEXT, BuyerId TEXT, BuyerName TEXT, PriceCopper INTEGER)";
                command.ExecuteNonQuery();
                for (int i = 1; i <= 53; i++)
                {
                    command.CommandText = $"INSERT INTO realm_exchange_sales VALUES ({i},'2026-08-31T13:00:00Z',{i % 3 + 1},'Test cloth gloves {i}',1,'player-one','Player seller','offlinebot:7','Bot buyer',100)";
                    command.ExecuteNonQuery();
                }
            }
            Type type = Launcher.GetType("OfflineDaoc.Launcher.ExchangeSalesForm");
            using var form = (Form)Activator.CreateInstance(type, [path]);
            var rows = (IList)type.GetMethod("ReadSales", Hidden).Invoke(form, null);
            Assert.That(rows.Count, Is.EqualTo(50));
            Assert.That(rows[0].GetType().GetProperty("ItemName").GetValue(rows[0]), Is.EqualTo("Test cloth gloves 53"));
            type.GetField("_rows", Hidden).SetValue(form, rows);
            type.GetMethod("BindRows", Hidden).Invoke(form, null);
            var filter = (ComboBox)type.GetField("_realm", Hidden).GetValue(form);
            var grid = (DataGridView)type.GetField("_grid", Hidden).GetValue(form);
            filter.SelectedItem = "Midgard";
            var filtered = (IList)grid.DataSource;
            Assert.That(filtered.Count, Is.EqualTo(17));
            Assert.That(filtered.Cast<object>().All(row => (string)row.GetType().GetProperty("Realm").GetValue(row) == "Midgard"), Is.True);
            filter.SelectedIndex = 0;
            // Create child window handles off-screen for a real WinForms layout
            // render; this isolated fixture never starts the launcher/server.
            form.ShowInTaskbar = false;
            form.StartPosition = FormStartPosition.Manual;
            form.Location = new Point(-20000, -20000);
            form.Show();
            Assert.That(SpinWait.SpinUntil(() =>
            {
                Application.DoEvents();
                return !(bool)type.GetField("_loading", Hidden).GetValue(form);
            }, 5000), Is.True, "The on-open read must finish without a running server.");
            Application.DoEvents();
            Assert.That(grid.Rows.Count, Is.EqualTo(50));
            form.PerformLayout();
            using var bitmap = new Bitmap(form.Width, form.Height);
            form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
            bitmap.Save(Path.Combine(TestContext.CurrentContext.WorkDirectory, "exchange-ledger-preview.png"));
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Test]
    public void MissingLedgerOrDatabaseIsAnEmptyViewNotAnError()
    {
        string path = Path.Combine(Path.GetTempPath(), "absent-daoc-ledger-" + Guid.NewGuid().ToString("N") + ".db");
        Type type = Launcher.GetType("OfflineDaoc.Launcher.ExchangeSalesForm");
        using var form = (Form)Activator.CreateInstance(type, [path]);
        Assert.That((IList)type.GetMethod("ReadSales", Hidden).Invoke(form, null), Is.Empty);
        Assert.That(File.Exists(path), Is.False);
    }
}
