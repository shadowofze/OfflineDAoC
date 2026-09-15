using System.Collections;
using System.Reflection;
using System.Text.Json;
using NUnit.Framework;

[TestFixture, NonParallelizable, Apartment(ApartmentState.STA)]
public class RealmEventsPresentationTests
{
    private static readonly Type Main = Assembly.Load("OfflineDAoC").GetType("OfflineDaoc.Launcher.MainForm")!;
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    private Form _form;
    private T Field<T>(string name) => (T)Main.GetField(name, Hidden)!.GetValue(_form)!;
    private void Render() => Main.GetMethod("RenderRealmEvents", Hidden)!.Invoke(_form, null);
    private static object[] Rows(DataGridView grid) => ((IEnumerable)grid.DataSource!).Cast<object>().ToArray();
    private static object Value(object row, string name) => row.GetType().GetProperty(name)!.GetValue(row)!;

    [SetUp]
    public void Setup()
    {
        _form = (Form)Activator.CreateInstance(Main)!;
        var objectives = new[]
        {
            new { Kind="Dragon", Name="Golestandt", Owner="Albion", State="Raid rally", Location="Dartmoor", Carrier="", Forces="Assigned: 240/240 · Present: 192", Id="dragon-albion", CooldownMilliseconds=0L, Phase="Staging", PhaseRemainingMilliseconds=1800000L },
            new { Kind="Epic dungeon", Name="Tuscaran Glacier", Owner="Midgard", State="RAID — clearing", Location="Tuscaran Glacier", Carrier="", Forces="Midgard 208", Id="epic-midgard", CooldownMilliseconds=0L, Phase="Battle", PhaseRemainingMilliseconds=14400000L },
            new { Kind="Keep", Name="Dun Ailinne", Owner="Hibernia", State="SIEGE — under attack", Location="Breifine", Carrier="", Forces="Albion 48 / Hibernia 32", Id="keep-1", CooldownMilliseconds=0L, Phase="", PhaseRemainingMilliseconds=0L },
            new { Kind="Dragon", Name="Cuuldurach", Owner="Hibernia", State="Respawning", Location="Sheeroe Hills", Carrier="", Forces="", Id="dragon-hibernia", CooldownMilliseconds=600000L, Phase="", PhaseRemainingMilliseconds=0L },
            new { Kind="Portal keep", Name="Protected Portal Keep", Owner="Albion", State="Secure", Location="Emain", Carrier="", Forces="", Id="protected", CooldownMilliseconds=0L, Phase="", PhaseRemainingMilliseconds=0L },
        };
        var participants = Enumerable.Range(1, 80).Select(i => new { EventId="dragon-albion", GroupId=$"party-{(i-1)/8+1}",
            Name=$"Raider{i:00}", Realm="Albion", Location="Dartmoor", Activity="Staging with party", X=10000+i, Y=20000, Z=1000 }).ToArray();
        var snapshot = Main.GetNestedType("RvrWorldSnapshot", BindingFlags.NonPublic)!;
        Main.GetField("_rvrWorld", Hidden)!.SetValue(_form, JsonSerializer.Deserialize(JsonSerializer.Serialize(new
            { UpdatedUtc=DateTime.UtcNow, Running=true, Objectives=objectives, Participants=participants }), snapshot));
        Main.GetField("_rvrServerRunning", Hidden)!.SetValue(_form, true);
        _ = _form.Handle;
        _ = Field<DataGridView>("_rvrObjectivesGrid").Handle;
        Render();
    }

    [TearDown] public void Cleanup() => _form.Dispose();

    [TestCase("Dragon", "Muster", "Raid rally — automatic", true)]
    [TestCase("Dragon", "Staging", "Raid rally — forced", true)]
    [TestCase("Dragon", "Waiting", "Raid rally — waiting for dragon landing", true)]
    [TestCase("Dragon", "Battle", "RAID — underway", true)]
    [TestCase("Epic dungeon", "Muster", "Raid rally — automatic", true)]
    [TestCase("Epic dungeon", "Staging", "Raid rally — travel/final staging", true)]
    [TestCase("Epic dungeon", "Waiting", "Raid rally — waiting for arrivals", true)]
    [TestCase("Epic dungeon", "Battle", "RAID — clearing", true)]
    [TestCase("Keep", "", "Keep siege rally", true)]
    [TestCase("Relic keep", "", "Relic siege rally", true)]
    [TestCase("Keep", "", "SIEGE — ongoing", true)]
    [TestCase("Relic keep", "", "SIEGE — ongoing", true)]
    [TestCase("Keep", "", "Under attack — no organized siege", true)]
    [TestCase("Relic", "", "SIEGE — relic escort / interception", true)]
    [TestCase("Relic", "", "ESCORT / INTERCEPTION", true)]
    [TestCase("Relic", "", "DROPPED — recoverable", true)]
    [TestCase("Dragon", "", "Raid rally — automatic", true)]
    [TestCase("Epic dungeon", "", "RAID — clearing", true)]
    [TestCase("Dragon", "", "Available", false)]
    [TestCase("Dragon", "", "Respawning", false)]
    [TestCase("Epic dungeon", "", "Event cooldown", false)]
    [TestCase("Epic dungeon", "", "Encounter unavailable", false)]
    [TestCase("Dragon", "", "Awaiting server snapshot", false)]
    [TestCase("Keep", "", "Secure", false)]
    [TestCase("Relic keep", "", "Secure", false)]
    [TestCase("Relic", "", "At shrine", false)]
    [TestCase("Portal keep", "Battle", "SIEGE — ongoing", false)]
    public void ActiveClassificationCoversEveryPublishedStageWithoutRelyingOnCountdown(string kind, string phase, string state, bool expected)
    {
        var type = Main.GetNestedType("RvrObjective", BindingFlags.NonPublic)!;
        var row = JsonSerializer.Deserialize(JsonSerializer.Serialize(new { Kind=kind, Name="Test", Owner="Albion",
            State=state, Location="Test", Carrier="", Forces="No organized rally", Phase=phase,
            PhaseRemainingMilliseconds=0L }), type)!;
        Assert.That(Value(row,"IsActiveEvent"), Is.EqualTo(expected));
    }

    [Test]
    public void ActiveCheckboxCombinesWithFiltersAndClearsHiddenSelectionWithoutRequests()
    {
        var check = Field<CheckBox>("_eventActiveOnly");
        var grid = Field<DataGridView>("_rvrObjectivesGrid");
        Assert.That(check.Checked, Is.False, "Default view remains unchanged");
        check.Checked = true;
        Assert.That(Rows(grid).Select(r=>Value(r,"Id")), Is.EquivalentTo(new[]{"dragon-albion","epic-midgard","keep-1"}));
        Field<ComboBox>("_eventKind").SelectedItem = "Dragon";
        Field<TextBox>("_eventSearch").Text = "Raider80";
        Assert.That(Rows(grid).Select(r=>Value(r,"Id")), Is.EqualTo(new[]{"dragon-albion"}));
        Assert.That(Rows(Field<DataGridView>("_eventMembers")).Length, Is.EqualTo(80));
        Field<ComboBox>("_eventRealm").SelectedItem = "Hibernia";
        Assert.That(Rows(grid), Is.Empty);
        Assert.That(Rows(Field<DataGridView>("_eventMembers")), Is.Empty);
        Assert.That(Field<Button>("_eventStart").Enabled, Is.False);
        Assert.That(Field<Button>("_eventReset").Enabled, Is.False);
        Field<ComboBox>("_eventRealm").SelectedIndex = 0;
        Field<ComboBox>("_eventKind").SelectedIndex = 0;
        Field<TextBox>("_eventSearch").Clear();
        Render(); // Snapshot refresh/sorting preserves the checked filter.
        Assert.That(check.Checked, Is.True);
        Assert.That(Rows(grid).Length, Is.EqualTo(3));
        check.Checked = false;
        Assert.That(Rows(grid).Length, Is.EqualTo(7));
        Assert.That(Field<bool>("_eventRequestPending"), Is.False);
    }

    [Test]
    public void MissingSnapshotHasNoFabricatedActiveEvents()
    {
        Main.GetField("_rvrWorld", Hidden)!.SetValue(_form, null);
        Field<CheckBox>("_eventActiveOnly").Checked = true;
        Assert.That(Rows(Field<DataGridView>("_rvrObjectivesGrid")), Is.Empty);
        Assert.That(Field<Button>("_eventStart").Enabled, Is.False);
        Field<CheckBox>("_eventActiveOnly").Checked = false;
        Assert.That(Rows(Field<DataGridView>("_rvrObjectivesGrid")).Length, Is.EqualTo(6));
    }

    [TestCase("Staging", 1800000L, "Staging 30:00")]
    [TestCase("Staging", 0L, "Waiting")]
    [TestCase("Battle", 14400000L, "Battle 240:00")]
    [TestCase("Waiting", 0L, "Waiting")]
    [TestCase("", 0L, "—")]
    public void StageTimerNeverInventsAnAssaultWhenCountdownExpires(string phase, long remaining, string expected)
    {
        var formatter = Main.GetMethod("FormatEventTimer", BindingFlags.Static | BindingFlags.NonPublic)!;
        Assert.That(formatter.Invoke(null, new object[] { phase, remaining }), Is.EqualTo(expected));
    }

    [Test]
    public void RealmTypeAndParticipantSearchFilterTheCorrectEvents()
    {
        var grid = Field<DataGridView>("_rvrObjectivesGrid");
        Assert.That(Rows(grid).Length, Is.EqualTo(7));
        Field<ComboBox>("_eventRealm").SelectedItem = "Albion";
        Assert.That(Rows(grid).Select(r => Value(r,"Id")), Is.EquivalentTo(new[] { "dragon-albion", "epic-albion", "keep-1" }));
        Field<ComboBox>("_eventKind").SelectedItem = "Dragon";
        Assert.That(Rows(grid).Select(r => Value(r,"Id")), Is.EqualTo(new[] { "dragon-albion" }));
        Field<TextBox>("_eventSearch").Text = "Raider80";
        Assert.That(Rows(grid).Length, Is.EqualTo(1));
        Field<TextBox>("_eventSearch").Text = "missing-bot";
        Assert.That(Rows(grid), Is.Empty);
    }

    [Test]
    public void CooldownsSortNumericallyAndSelectionKeepsAllEightyParticipants()
    {
        var grid = Field<DataGridView>("_rvrObjectivesGrid");
        Main.GetField("_eventSort", Hidden)!.SetValue(_form, "CooldownMilliseconds");
        Main.GetField("_eventAscending", Hidden)!.SetValue(_form, false);
        Render();
        Assert.That(Value(Rows(grid)[0],"Id"), Is.EqualTo("dragon-hibernia"));
        Field<TextBox>("_eventSearch").Text = "Golestandt";
        _ = Field<DataGridView>("_eventMembers").Handle;
        grid.CurrentCell = grid.Rows[0].Cells[0];
        Render();
        Assert.That(Rows(Field<DataGridView>("_eventMembers")).Length, Is.EqualTo(80));
        Assert.That(Field<Button>("_eventStart").Enabled, Is.True);
        Main.GetField("_rvrServerRunning", Hidden)!.SetValue(_form, false);
        Render();
        Assert.That(Field<Button>("_eventStart").Enabled, Is.False);
        Assert.That(Field<Button>("_eventReset").Enabled, Is.False);
    }

    [TestCase(300, 1f)] [TestCase(600, 1f)] [TestCase(600, 1.5f)]
    public void RenderReadOnlyPreviewWithoutStartingLauncherServices(int height, float scale)
    {
        IEnumerable<Control> Descendants(Control control) => control.Controls.Cast<Control>().SelectMany(c => new[] { c }.Concat(Descendants(c)));
        TabPage page = Descendants(_form).OfType<TabPage>().Single(p => p.Text == "Realm Events");
        ((TabControl)page.Parent!).SelectedTab = page;
        _form.PerformLayout();
        // Show only the detached panel in a transparent off-screen test host.
        // The actual launcher's Shown handlers (refresh/services) never run.
        using var host = new Form { ShowInTaskbar = false, Opacity = 0, StartPosition = FormStartPosition.Manual,
            Location = new Point(-20000,-20000), ClientSize = new Size(1030,height), Font = _form.Font,
            BackColor = page.BackColor, ForeColor = _form.ForeColor };
        Control panel = page.Controls[0];
        host.Controls.Add(panel);
        host.Show();
        if (scale != 1) panel.Scale(new SizeF(scale, scale));
        host.PerformLayout();
        foreach (Control control in Descendants(host)) control.PerformLayout();
        Render();
        var grid = Field<DataGridView>("_rvrObjectivesGrid");
        foreach (DataGridViewRow row in grid.Rows)
            if ((string)Value(row.DataBoundItem!,"Id") == "dragon-albion") { grid.CurrentCell = row.Cells[0]; break; }
        Assert.That(Rows(Field<DataGridView>("_eventMembers")).Length, Is.EqualTo(80), "Selecting a row must immediately show that event, without an extra refresh.");
        using var bitmap = new Bitmap(panel.Width, panel.Height);
        panel.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
        string image = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"realm-events-preview-{height}-{scale}.png");
        bitmap.Save(image);
        TestContext.AddTestAttachment(image);
        Assert.That(Field<DataGridView>("_rvrObjectivesGrid").Height, Is.GreaterThan(70));
        Assert.That(Field<DataGridView>("_eventMembers").Height, Is.GreaterThan(70));
        int cooldownIndex = grid.Columns.Cast<DataGridViewColumn>().Single(c => c.DataPropertyName == "CooldownMilliseconds").Index;
        Rectangle cooldown = grid.GetCellDisplayRectangle(cooldownIndex, -1, false);
        if (scale == 1) Assert.That(cooldown.Right, Is.LessThanOrEqualTo(grid.ClientSize.Width));
        var active = Field<CheckBox>("_eventActiveOnly");
        Assert.That(active.Parent!.ClientRectangle.Contains(active.Bounds), Is.True, "Active Events checkbox must fit the filter bar");
        foreach (var button in new[] { Field<Button>("_eventStart"), Field<Button>("_eventReset") })
        {
            Control child = button;
            while (child != panel)
            {
                Assert.That(child.Bottom, Is.LessThanOrEqualTo(child.Parent!.ClientSize.Height),
                    $"{button.Text}: {child.GetType().Name} clipped vertically by {child.Parent.GetType().Name}");
                child = child.Parent;
            }
        }
    }

    [TestCase(false)] [TestCase(true)]
    public void MissingOrLegacySnapshotsShowAllSixPveEventsWithoutInventingReadiness(bool legacy)
    {
        var type = Main.GetNestedType("RvrWorldSnapshot", BindingFlags.NonPublic)!;
        Main.GetField("_rvrWorld", Hidden)!.SetValue(_form, legacy ? JsonSerializer.Deserialize(
            "{\"UpdatedUtc\":\"2026-09-13T07:50:59Z\",\"Running\":true,\"Objectives\":[{\"Kind\":\"Keep\",\"Name\":\"Caer Benowyc\",\"Owner\":\"Albion\",\"State\":\"Secure\",\"Location\":\"Albion\",\"Carrier\":\"\",\"Forces\":\"\"}]}", type) : null);
        Render();
        var grid = Field<DataGridView>("_rvrObjectivesGrid");
        var pve = Rows(grid).Where(row => (string)Value(row, "Kind") is "Dragon" or "Epic dungeon").ToArray();
        Assert.That(pve.Length, Is.EqualTo(6));
        Assert.That(pve.Select(row => Value(row, "Id")).Distinct().Count(), Is.EqualTo(6));
        Assert.That(pve.All(row => (bool)Value(row, "IsCatalogOnly")), Is.True);
        Field<ComboBox>("_eventKind").SelectedItem = "Epic dungeon";
        Assert.That(Rows(grid).Length, Is.EqualTo(3));
        grid.CurrentCell = grid.Rows[0].Cells[0];
        Assert.That(Field<Button>("_eventStart").Enabled, Is.False);
        Assert.That(Field<Button>("_eventReset").Enabled, Is.False);
    }
}
