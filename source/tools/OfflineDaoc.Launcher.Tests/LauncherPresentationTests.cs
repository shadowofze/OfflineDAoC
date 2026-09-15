using System.Reflection;
using System.Data.SQLite;
using NUnit.Framework;

[TestFixture, NonParallelizable, Apartment(ApartmentState.STA)]
public sealed class LauncherPresentationTests
{
    private static readonly Assembly Launcher = Assembly.Load("OfflineDAoC");
    private static readonly BindingFlags HiddenStatic = BindingFlags.Static | BindingFlags.NonPublic;
    private static readonly BindingFlags HiddenInstance = BindingFlags.Instance | BindingFlags.NonPublic;

    [TestCase("SIEGE — under attack", "Danger")]
    [TestCase("SIEGE — ongoing", "Danger")]
    [TestCase("SIEGE — relic escort / interception", "Danger")]
    [TestCase(" siege — under attack ", "Danger")]
    [TestCase("DROPPED — recoverable", "Danger")]
    [TestCase("Relic siege rally", "GoldLight")]
    [TestCase("SIEGE RALLY", "GoldLight")]
    [TestCase("ESCORT / INTERCEPTION", "Purple")]
    [TestCase("Intercepting relic", "Purple")]
    [TestCase("Secure", "Text")]
    [TestCase("At shrine", "Text")]
    public void RvrStatusColorsRemainCorrectWhenSelected(string status, string expected)
    {
        var style = new DataGridViewCellStyle();
        var main = Launcher.GetType("OfflineDaoc.Launcher.MainForm")!;
        main.GetMethod("ApplyRvrStatusStyle", HiddenStatic)!.Invoke(null, new object[] { style, status });
        Color color = expected == "Purple" ? Color.FromArgb(196, 160, 240)
            : (Color)Launcher.GetType("OfflineDaoc.Launcher.DaocTheme")!.GetField(expected,
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!.GetValue(null)!;
        Assert.That(style.ForeColor, Is.EqualTo(color));
        Assert.That(style.SelectionForeColor, Is.EqualTo(color));
    }

    [Test]
    public void RvrOverviewHidesPortalKeepsFromAnOlderSnapshot()
    {
        var main = Launcher.GetType("OfflineDaoc.Launcher.MainForm")!;
        using var form = (Form)Activator.CreateInstance(main)!;
        var snapshotType = main.GetNestedType("RvrWorldSnapshot", BindingFlags.NonPublic)!;
        string json = """
            {"UpdatedUtc":"2026-09-04T15:00:00Z","Running":true,"Objectives":[
            {"Kind":"Keep","Name":"Albion Portal Keep","Owner":"Albion","State":"Secure","Location":"Midgard","Carrier":"","Forces":""},
            {"Kind":"Keep","Name":"Caer Benowyc","Owner":"Albion","State":"SIEGE — under attack","Location":"Albion","Carrier":"","Forces":""},
            {"Kind":"Relic keep","Name":"Castle Excalibur","Owner":"Albion","State":"Secure","Location":"Albion","Carrier":"","Forces":""}]}
            """;
        main.GetField("_rvrWorld", HiddenInstance)!.SetValue(form, System.Text.Json.JsonSerializer.Deserialize(json, snapshotType));
        main.GetMethod("RenderActiveRvr", HiddenInstance)!.Invoke(form, null);
        var grid = (DataGridView)main.GetField("_rvrObjectivesGrid", HiddenInstance)!.GetValue(form)!;
        var names = ((System.Collections.IEnumerable)grid.DataSource!).Cast<object>()
            .Select(row => row.GetType().GetProperty("Name")!.GetValue(row)).ToArray();
        Assert.That(names, Is.EquivalentTo(new[] { "Caer Benowyc", "Castle Excalibur", "Golestandt", "Gjalpinulva", "Cuuldurach", "Caer Sidi", "Tuscaran Glacier", "Galladoria" }));
    }

    [Test]
    public void GmCheckboxCanGrantAndRevokeOnlyTheSelectedAccountsPrivilege()
    {
        using var db = new SQLiteConnection("Data Source=:memory:;Version=3;New=True;");
        db.Open();
        using var command = db.CreateCommand();
        command.CommandText = "CREATE TABLE Account(Name TEXT PRIMARY KEY, PrivLevel INTEGER); INSERT INTO Account VALUES ('offline',1),('bot-account',1)";
        command.ExecuteNonQuery();
        var persist = Launcher.GetType("OfflineDaoc.Launcher.MainForm")!.GetMethod("PersistGmSetting", HiddenStatic)!;
        foreach (bool enabled in new[] { true, false, true, false })
        {
            persist.Invoke(null, new object[] { db, "offline", enabled });
            command.CommandText = "SELECT PrivLevel FROM Account WHERE Name='offline'";
            Assert.That(Convert.ToInt32(command.ExecuteScalar()), Is.EqualTo(enabled ? 2 : 1));
            command.CommandText = "SELECT Value FROM offline_local_options WHERE Key='MakeMeGM'";
            Assert.That(command.ExecuteScalar(), Is.EqualTo(enabled ? "true" : "false"));
            command.CommandText = "SELECT PrivLevel FROM Account WHERE Name='bot-account'";
            Assert.That(Convert.ToInt32(command.ExecuteScalar()), Is.EqualTo(1));
        }
        Assert.Throws<TargetInvocationException>(() => persist.Invoke(null, new object[] { db, "missing", true }));
        command.CommandText = "SELECT Value FROM offline_local_options WHERE Key='MakeMeGM'";
        Assert.That(command.ExecuteScalar(), Is.EqualTo("false"), "A missing account must roll back the option too.");
    }

    [Test]
    public void VersionIsManuallyPinnedAndRefreshRunsEveryFiveMinutes()
    {
        Type mainFormType = Launcher.GetType("OfflineDaoc.Launcher.MainForm")!;
        Assert.That(mainFormType.GetField("DisplayVersion", HiddenStatic)!.GetRawConstantValue(), Is.EqualTo("0.3"));
        Assert.That(mainFormType.GetField("AutoRefreshMilliseconds", HiddenStatic)!.GetRawConstantValue(), Is.EqualTo(300_000));
        Assert.That(mainFormType.GetField("RvrSnapshotRefreshMilliseconds", HiddenStatic)!.GetRawConstantValue(), Is.EqualTo(30_000));
        Assert.That(mainFormType.GetField("ServerReadinessPollMilliseconds", HiddenStatic)!.GetRawConstantValue(), Is.EqualTo(500));

        using var form = (Form)Activator.CreateInstance(mainFormType)!;
        var timer = (System.Windows.Forms.Timer)mainFormType.GetField("_autoRefresh", HiddenInstance)!.GetValue(form)!;
        var readinessTimer = (System.Windows.Forms.Timer)mainFormType.GetField("_serverReadinessPoll", HiddenInstance)!.GetValue(form)!;
        Assert.That(timer.Interval, Is.EqualTo(300_000));
        Assert.That(readinessTimer.Interval, Is.EqualTo(500));
        Assert.That(readinessTimer.Enabled, Is.False, "The fast probe must only run during server startup.");

        mainFormType.GetMethod("BeginServerReadinessPolling", HiddenInstance)!.Invoke(form, null);
        Assert.That(readinessTimer.Enabled, Is.True, "Starting the server must arm the readiness probe.");
        readinessTimer.Stop();
    }

    [TestCase(300.0, "5:00")]
    [TestCase(299.1, "5:00")]
    [TestCase(61.0, "1:01")]
    [TestCase(0.0, "0:00")]
    [TestCase(-5.0, "0:00")]
    public void AutoRefreshCountdownUsesTheExistingDisplayClockWithoutPolling(double seconds, string expected)
    {
        Type mainFormType = Launcher.GetType("OfflineDaoc.Launcher.MainForm")!;
        MethodInfo format = mainFormType.GetMethod("FormatAutoRefreshRemaining",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;
        Assert.That(format.Invoke(null, new object[] { TimeSpan.FromSeconds(seconds) }), Is.EqualTo(expected));

        using var form = (Form)Activator.CreateInstance(mainFormType)!;
        var displayClock = (System.Windows.Forms.Timer)mainFormType.GetField("_displayClock", HiddenInstance)!.GetValue(form)!;
        Assert.That(displayClock.Interval, Is.EqualTo(1000));
        Assert.That(mainFormType.GetFields(HiddenInstance)
            .Count(field => field.FieldType == typeof(System.Windows.Forms.Timer)), Is.EqualTo(3),
            "The countdown must reuse the existing display timer and add no polling timer.");
    }

    [Test]
    public void AutoRefreshCountdownRepaintsOnlyItsSmallValueLabel()
    {
        Type mainFormType = Launcher.GetType("OfflineDaoc.Launcher.MainForm")!;
        using var form = (Form)Activator.CreateInstance(mainFormType)!;
        var footer = (Label)mainFormType.GetField("_footer", HiddenInstance)!.GetValue(form)!;
        var caption = (Label)mainFormType.GetField("_autoRefreshCountdownCaption", HiddenInstance)!.GetValue(form)!;
        var value = (Label)mainFormType.GetField("_autoRefreshCountdownValue", HiddenInstance)!.GetValue(form)!;

        footer.Text = "Static dashboard summary";
        mainFormType.GetMethod("UpdateAutoRefreshCountdown", HiddenInstance)!.Invoke(form, null);

        Assert.That(footer.Text, Is.EqualTo("Static dashboard summary"));
        Assert.That(caption.Text, Is.EqualTo("Auto-refresh in"));
        Assert.That(value.Text, Is.EqualTo("5:00"));
        Assert.That(TextRenderer.MeasureText(caption.Text, caption.Font).Width, Is.LessThan(caption.Width),
            "The fixed caption must fit on one line at the launcher's active DPI.");
        Assert.That(value.Width, Is.LessThan(caption.Width));
    }

    [Test]
    public void CleanedLayoutKeepsInformationWithoutRedundantRealmTabOrRowSelectors()
    {
        Type mainFormType = Launcher.GetType("OfflineDaoc.Launcher.MainForm")!;
        using var form = (Form)Activator.CreateInstance(mainFormType)!;
        IReadOnlyList<Control> controls = Descendants(form).ToList();

        Assert.That(controls.OfType<Label>().Any(label => label.Text.Contains("VERSION 0.3", StringComparison.Ordinal)), Is.True);
        Label version = controls.OfType<Label>().Single(label => label.Text == "VERSION 0.3");
        Assert.That(version.Font.Bold, Is.True);
        Assert.That(version.Font.Size, Is.GreaterThanOrEqualTo(12));
        Assert.That(controls.OfType<Label>().Any(label => label.Text.Contains("1× PROGRESSION", StringComparison.Ordinal)), Is.False);
        Assert.That(controls.OfType<Label>().Any(label => label.Text.Contains("CLASSIC 1.65", StringComparison.Ordinal)), Is.False);
        Assert.That(controls.OfType<Label>().Any(label => label.Text == "BOT AI DELAY"), Is.True);
        Assert.That(controls.OfType<Label>().Any(label => label.Text.Contains("BOT TICK P95", StringComparison.Ordinal)), Is.False);
        var performanceValue = (Label)mainFormType.GetField("_performanceValue", HiddenInstance)!.GetValue(form)!;
        var helpTip = (ToolTip)mainFormType.GetField("_helpTip", HiddenInstance)!.GetValue(form)!;
        Assert.That(helpTip.GetToolTip(performanceValue), Does.Contain("95 out of 100"));
        Assert.That(helpTip.GetToolTip(performanceValue), Does.Contain("1,000 ms equals one second"));
        Assert.That(controls.OfType<TabControl>().SelectMany(tab => tab.TabPages.Cast<TabPage>()).Select(page => page.Text),
            Does.Not.Contain("Realm Status"));
        Assert.That(controls.OfType<TabControl>().SelectMany(tab => tab.TabPages.Cast<TabPage>()).Select(page => page.Text),
            Does.Contain("XP Settings"));
        Assert.That(controls.OfType<DataGridView>(), Is.Not.Empty);
        Assert.That(controls.OfType<DataGridView>().All(grid => !grid.RowHeadersVisible), Is.True);
        var populationGrid = (DataGridView)mainFormType.GetField("_grid", HiddenInstance)!.GetValue(form)!;
        Assert.That(populationGrid.ContextMenuStrip!.Items.OfType<ToolStripMenuItem>().Select(item => item.Text),
            Does.Contain("Teleport to"));
        Assert.That(controls.Any(control => control.GetType().FullName == "OfflineDaoc.Launcher.FantasyBanner"), Is.True);
        Assert.That(controls.Count(control => control.GetType().FullName == "OfflineDaoc.Launcher.RealmShieldPicture"), Is.EqualTo(1));

        var playerRate = (ComboBox)mainFormType.GetField("_playerXpRate", HiddenInstance)!.GetValue(form)!;
        var botRate = (ComboBox)mainFormType.GetField("_botXpRate", HiddenInstance)!.GetValue(form)!;
        Assert.That(playerRate, Is.Not.SameAs(botRate));
        Assert.That(playerRate.Items.Cast<object>().Select(item => item.ToString()),
            Is.EqualTo(new[] { "1×  Original", "2×", "3×", "5×", "10×" }));
        Assert.That(botRate.Items.Cast<object>().Select(item => item.ToString()),
            Is.EqualTo(new[] { "1×  Original", "2×", "3×", "5×", "10×" }));
        var realmButtons = (System.Collections.IEnumerable)mainFormType.GetField("_realmGenerateButtons", HiddenInstance)!.GetValue(form)!;
        Assert.That(realmButtons.Cast<Button>().Count(button => button.Text == "+ Lv.1"), Is.EqualTo(3));
        Assert.That(realmButtons.Cast<Button>().Count(button => button.Text == "+ Lv.50"), Is.EqualTo(3));
    }

    [Test]
    public void StopStateIsImmediateRedAndXpControlsExplainTheirLockedAndApplyingStates()
    {
        Type mainFormType = Launcher.GetType("OfflineDaoc.Launcher.MainForm")!;
        using var form = (Form)Activator.CreateInstance(mainFormType)!;
        var serverState = (Label)mainFormType.GetField("_serverState", HiddenInstance)!.GetValue(form)!;
        var start = (Button)mainFormType.GetField("_startButton", HiddenInstance)!.GetValue(form)!;
        var stop = (Button)mainFormType.GetField("_stopButton", HiddenInstance)!.GetValue(form)!;
        var play = (Button)mainFormType.GetField("_playButton", HiddenInstance)!.GetValue(form)!;

        mainFormType.GetMethod("ShowStoppingState", HiddenInstance)!.Invoke(form, null);
        Assert.That(serverState.Text, Is.EqualTo("STOPPING…"));
        Assert.That(serverState.ForeColor.R, Is.GreaterThan(serverState.ForeColor.G));
        Assert.That(serverState.ForeColor.R, Is.GreaterThan(serverState.ForeColor.B));
        Assert.That(new[] { start, stop, play }.All(button => !button.Enabled), Is.True);

        Type snapshotType = mainFormType.GetNestedType("DashboardSnapshot", BindingFlags.NonPublic)!;
        object runningSnapshot = snapshotType.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Single(constructor => constructor.GetParameters().Length == 10)
            .Invoke(new object[] { "Running", null!, null!, 0, 0d, 0d, 1d, 1d, false, null! });
        mainFormType.GetField("_stoppingServer", HiddenInstance)!.SetValue(form, false);
        mainFormType.GetMethod("UpdateXpRateControls", HiddenInstance)!.Invoke(form, new[] { runningSnapshot });
        var playerRate = (ComboBox)mainFormType.GetField("_playerXpRate", HiddenInstance)!.GetValue(form)!;
        var botRate = (ComboBox)mainFormType.GetField("_botXpRate", HiddenInstance)!.GetValue(form)!;
        var xpStatus = (Label)mainFormType.GetField("_xpSettingsStatus", HiddenInstance)!.GetValue(form)!;
        Assert.That(playerRate.Enabled || botRate.Enabled, Is.False);
        var gm = (CheckBox)mainFormType.GetField("_makeMeGm", HiddenInstance)!.GetValue(form)!;
        Assert.That(gm.Enabled, Is.False);
        Assert.That(xpStatus.Text, Does.Contain("XP RATE LOCKED"));

        mainFormType.GetMethod("ShowXpRateApplying", HiddenInstance)!.Invoke(form, new object[] { "3×", true });
        Assert.That(xpStatus.Text, Does.StartWith("APPLYING 3× TO YOUR PLAYER XP"));
        Assert.That(xpStatus.ForeColor.G, Is.GreaterThan(xpStatus.ForeColor.R));
        Assert.That(playerRate.Enabled || botRate.Enabled || start.Enabled, Is.False);
        Assert.That(form.UseWaitCursor, Is.False, "Applying a rate must not leave a busy cursor on child controls.");
    }

    [Test]
    public void StartingStateIsImmediateAndKeepsEnterRealmLockedUntilReadinessRefresh()
    {
        Type mainFormType = Launcher.GetType("OfflineDaoc.Launcher.MainForm")!;
        using var form = (Form)Activator.CreateInstance(mainFormType)!;
        var serverState = (Label)mainFormType.GetField("_serverState", HiddenInstance)!.GetValue(form)!;
        var start = (Button)mainFormType.GetField("_startButton", HiddenInstance)!.GetValue(form)!;
        var stop = (Button)mainFormType.GetField("_stopButton", HiddenInstance)!.GetValue(form)!;
        var play = (Button)mainFormType.GetField("_playButton", HiddenInstance)!.GetValue(form)!;
        var footer = (Label)mainFormType.GetField("_footer", HiddenInstance)!.GetValue(form)!;

        mainFormType.GetMethod("ShowStartingState", HiddenInstance)!.Invoke(form, null);

        Assert.That(serverState.Text, Is.EqualTo("STARTING…"));
        Assert.That(serverState.ForeColor, Is.EqualTo(Color.FromArgb(194, 157, 77)));
        Assert.That(new[] { start, stop, play }.All(button => !button.Enabled), Is.True);
        Assert.That(footer.Text, Does.Contain("Enter Realm will unlock automatically"));
    }

    [Test]
    public void SnapshotReadDisablesEveryButtonAndRestoresEachPriorState()
    {
        Type mainFormType = Launcher.GetType("OfflineDaoc.Launcher.MainForm")!;
        using var form = (Form)Activator.CreateInstance(mainFormType)!;
        List<Button> buttons = Descendants(form).OfType<Button>().ToList();
        Assert.That(buttons.Count, Is.GreaterThan(5));
        buttons[0].Enabled = false;
        Dictionary<Button, bool> originalStates = buttons.ToDictionary(button => button, button => button.Enabled);

        object savedStates = mainFormType.GetMethod("CaptureAndDisableButtonsForSnapshot", HiddenInstance)!
            .Invoke(form, null)!;
        Assert.That(buttons.All(button => !button.Enabled), Is.True);
        Assert.That(form.UseWaitCursor, Is.False, "Snapshot feedback is provided by disabled buttons and READING text.");

        mainFormType.GetMethod("RestoreButtonsAfterSnapshot", HiddenStatic)!
            .Invoke(null, new[] { savedStates });
        Assert.That(buttons.All(button => button.Enabled == originalStates[button]), Is.True);
    }

    [Test]
    public void EmbeddedFantasyHeaderLoadsAndLauncherRendersWithoutStartingServer()
    {
        const string resourceName = "OfflineDaoc.Launcher.Assets.offline-daoc-header.png";
        using Stream resource = Launcher.GetManifestResourceStream(resourceName)!;
        Assert.That(resource, Is.Not.Null);
        using var artwork = Image.FromStream(resource);
        Assert.That(artwork.Width, Is.GreaterThanOrEqualTo(1_000));
        Assert.That(artwork.Width / (double)artwork.Height, Is.GreaterThan(4.5));
        using Stream shieldResource = Launcher.GetManifestResourceStream("OfflineDaoc.Launcher.Assets.offline-daoc-realm-shield.png")!;
        using var shield = Image.FromStream(shieldResource);
        Assert.That((shield.Width, shield.Height), Is.EqualTo((96, 128)));

        Type mainFormType = Launcher.GetType("OfflineDaoc.Launcher.MainForm")!;
        using var form = (Form)Activator.CreateInstance(mainFormType)!;
        form.ShowInTaskbar = false;
        form.StartPosition = FormStartPosition.Manual;
        form.Location = new Point(-20_000, -20_000);
        form.Show();
        Application.DoEvents();
        form.PerformLayout();
        var playButton = (Button)mainFormType.GetField("_playButton", HiddenInstance)!.GetValue(form)!;
        Control banner = Descendants(form).Single(control => control.GetType().FullName == "OfflineDaoc.Launcher.FantasyBanner");
        Assert.That(playButton.RectangleToScreen(playButton.ClientRectangle).Bottom,
            Is.LessThanOrEqualTo(banner.RectangleToScreen(banner.ClientRectangle).Bottom - 8));
        using var preview = new Bitmap(form.Width, form.Height);
        form.DrawToBitmap(preview, new Rectangle(Point.Empty, preview.Size));
        preview.Save(Path.Combine(TestContext.CurrentContext.WorkDirectory, "launcher-v0.2-preview.png"));

        TabControl mainTabs = Descendants(form).OfType<TabControl>()
            .First(tab => tab.TabPages.Cast<TabPage>().Any(page => page.Text == "XP Settings"));
        mainTabs.SelectedTab = mainTabs.TabPages.Cast<TabPage>().First(page => page.Text == "XP Settings");
        Application.DoEvents();
        using var xpPreview = new Bitmap(form.Width, form.Height);
        form.DrawToBitmap(xpPreview, new Rectangle(Point.Empty, xpPreview.Size));
        xpPreview.Save(Path.Combine(TestContext.CurrentContext.WorkDirectory, "launcher-v0.2-xp-settings-preview.png"));
    }

    [Test]
    public void PlayerAndBotRatesPersistIndependently()
    {
        string path = Path.Combine(Path.GetTempPath(), "offline-daoc-xp-rates-" + Guid.NewGuid().ToString("N") + ".db");
        try
        {
            using (var database = new SQLiteConnection($"Data Source={path};Version=3;Pooling=False;"))
            {
                database.Open();
                using var create = database.CreateCommand();
                create.CommandText = """
                    CREATE TABLE ServerProperty
                    (Category TEXT NOT NULL, `Key` VARCHAR(255) NOT NULL PRIMARY KEY, Description TEXT NOT NULL,
                     DefaultValue TEXT NOT NULL, Value TEXT NOT NULL, LastTimeRowUpdated DATETIME NOT NULL,
                     ServerProperty_ID VARCHAR(255));
                    INSERT INTO ServerProperty VALUES
                    ('rates','xp_rate','player','1','1','2000-01-01','');
                    """;
                create.ExecuteNonQuery();
            }

            Type mainFormType = Launcher.GetType("OfflineDaoc.Launcher.MainForm")!;
            using var form = (Form)Activator.CreateInstance(mainFormType)!;
            mainFormType.GetField("_database", HiddenInstance)!.SetValue(form, path);
            MethodInfo persist = mainFormType.GetMethod("PersistXpRate", HiddenInstance)!;
            persist.Invoke(form, new object[] { "xp_rate", 3d });
            persist.Invoke(form, new object[] { "bot_xp_rate", 10d });

            using var verify = new SQLiteConnection($"Data Source={path};Version=3;Pooling=False;Read Only=True;");
            verify.Open();
            using var command = verify.CreateCommand();
            command.CommandText = "SELECT `Key`, Value FROM ServerProperty WHERE `Key` IN ('xp_rate','bot_xp_rate') ORDER BY `Key`";
            using var reader = command.ExecuteReader();
            var values = new Dictionary<string, string>();
            while (reader.Read()) values[reader.GetString(0)] = reader.GetString(1);
            Assert.That(values["xp_rate"], Is.EqualTo("3"));
            Assert.That(values["bot_xp_rate"], Is.EqualTo("10"));
        }
        finally
        {
            SQLiteConnection.ClearAllPools();
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Test]
    public void TeleportQueueTargetsTheClickedOnlineBotAndCoalescesPendingClicks()
    {
        using var database = new SQLiteConnection("Data Source=:memory:;Version=3;");
        database.Open();
        using (var create = database.CreateCommand())
        {
            create.CommandText = """
                CREATE TABLE offline_world_bots (BotId INTEGER PRIMARY KEY, IsOnline INTEGER NOT NULL, IsRetired INTEGER NOT NULL);
                CREATE TABLE offline_bot_commands
                    (CommandId INTEGER PRIMARY KEY AUTOINCREMENT, BotId INTEGER NOT NULL, CommandType TEXT NOT NULL,
                     RequestedUtc TEXT NOT NULL, State TEXT NOT NULL, RequestedByAccount TEXT, CompletedUtc TEXT, Error TEXT);
                INSERT INTO offline_world_bots VALUES (11,1,0),(12,1,0),(13,0,0);
                """;
            create.ExecuteNonQuery();
        }

        Type mainFormType = Launcher.GetType("OfflineDaoc.Launcher.MainForm")!;
        MethodInfo queue = mainFormType.GetMethod("InsertOrReplaceTeleportCommand",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;
        queue.Invoke(null, new object[] { database, 11L, "offline", DateTime.UtcNow });
        queue.Invoke(null, new object[] { database, 12L, "offline", DateTime.UtcNow.AddSeconds(1) });

        using var verify = database.CreateCommand();
        verify.CommandText = "SELECT COUNT(*), MAX(BotId), MAX(RequestedByAccount), MAX(CommandType) FROM offline_bot_commands WHERE State='Pending'";
        using var reader = verify.ExecuteReader();
        Assert.That(reader.Read(), Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(reader.GetInt32(0), Is.EqualTo(1));
            Assert.That(reader.GetInt64(1), Is.EqualTo(12));
            Assert.That(reader.GetString(2), Is.EqualTo("offline"));
            Assert.That(reader.GetString(3), Is.EqualTo("TeleportToBot"));
        });
    }

    private static IEnumerable<Control> Descendants(Control root)
    {
        foreach (Control child in root.Controls)
        {
            yield return child;
            foreach (Control descendant in Descendants(child))
                yield return descendant;
        }
    }
}
