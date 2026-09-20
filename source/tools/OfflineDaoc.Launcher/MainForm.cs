using System.Data.SQLite;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace OfflineDaoc.Launcher;

internal sealed partial class MainForm : Form
{
    // Public optional expansion label. The author's private installation uses
    // its separate 0.4 label and is never changed by this repository.
    internal const string DisplayVersion = "0.31b";
    internal const int AutoRefreshMilliseconds = 5 * 60 * 1000;
    internal const int RvrSnapshotRefreshMilliseconds = 30 * 1000;
    internal const int LiveBotSnapshotMaxAgeMilliseconds = 20_000;
    internal const int LiveBotSnapshotWaitMilliseconds = 3_000;
    internal const int ServerReadinessPollMilliseconds = 500;
    private readonly string _root = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
    private readonly string _database;
    private readonly string _serverDirectory;
    private readonly string _serverExecutable;
    private readonly string _clientDirectory;
    private readonly string _clientConnector;
    private readonly string _logsDirectory;
    private readonly BindingSource _botSource = new();
    private readonly List<BotRow> _bots = [];
    private readonly BindingSource _auctionSource = new();
    private readonly List<AuctionRow> _auctions = [];
    private readonly List<GroupRow> _groups = [];
    private readonly Label _serverState = CardValue();
    private readonly Label _onlineValue = CardValue();
    private readonly Label _albionValue = CardValue();
    private readonly Label _midgardValue = CardValue();
    private readonly Label _hiberniaValue = CardValue();
    private readonly Label _performanceValue = CardValue();
    private readonly Label _footer = new()
    {
        Dock = DockStyle.Fill,
        ForeColor = DaocTheme.Muted,
        BackColor = DaocTheme.StoneDark,
        Font = new Font("Georgia", 8f),
        Padding = new Padding(8, 0, 0, 0),
        TextAlign = ContentAlignment.MiddleLeft,
    };
    private readonly Label _autoRefreshCountdownCaption = new()
    {
        Dock = DockStyle.Fill,
        ForeColor = DaocTheme.Muted,
        BackColor = DaocTheme.StoneDark,
        Font = new Font("Georgia", 8f),
        Text = "Auto-refresh in",
        TextAlign = ContentAlignment.MiddleRight,
    };
    private readonly Label _autoRefreshCountdownValue = new()
    {
        Dock = DockStyle.Fill,
        ForeColor = DaocTheme.GoldLight,
        BackColor = DaocTheme.StoneDark,
        Font = new Font("Georgia", 8f, FontStyle.Bold),
        Text = "5:00",
        TextAlign = ContentAlignment.MiddleCenter,
    };
    private readonly ComboBox _realmFilter = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 130 };
    private readonly TextBox _search = new() { PlaceholderText = "Search name, class, zone or activity", Width = 280 };
    private readonly CheckBox _onlineOnly = new()
    {
        Text = "ONLINE ONLY",
        AutoSize = false,
        Size = new Size(132, 31),
        Margin = new Padding(8, 0, 0, 0),
        Padding = new Padding(4, 0, 0, 0),
        FlatStyle = FlatStyle.Flat,
        ForeColor = DaocTheme.GoldLight,
        BackColor = DaocTheme.Panel,
        Font = new Font("Georgia", 8.25f, FontStyle.Bold),
        TextAlign = ContentAlignment.MiddleLeft,
        Cursor = Cursors.Hand,
        AccessibleName = "Show online bots only",
    };
    private readonly TextBox _auctionSearch = new() { PlaceholderText = "Search item, seller or state", Width = 300 };
    private readonly TabControl _auctionRealmTabs = new() { Width = 280, Height = 29, SizeMode = TabSizeMode.Fixed, ItemSize = new Size(87, 22) };
    private readonly TextBox _groupSearch = new() { PlaceholderText = "Search bot, zone or faction", Width = 340 };
    private readonly ComboBox _playerXpRate = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 108 };
    private readonly ComboBox _botXpRate = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 108 };
    private readonly CheckBox _makeMeGm = new() { Text = "Make Me a GM", AutoSize = true, ForeColor = DaocTheme.Parchment, Font = new Font("Georgia", 9f, FontStyle.Bold) };
    private readonly Label _xpSettingsStatus = new()
    {
        AutoSize = true,
        ForeColor = DaocTheme.Muted,
        Font = new Font("Georgia", 8.5f, FontStyle.Italic),
        Text = "Rates are loaded from the saved server settings.",
    };
    private readonly Label _groupSearchStatus = new() { AutoSize = true, ForeColor = DaocTheme.Muted, Margin = new Padding(10, 7, 0, 0) };
    private readonly List<Label> _groupMemberLabels = [];
    private readonly List<(Label Label, BotRow Bot)> _memberCountdowns = [];
    private readonly List<(Label Label, BotRow Bot)> _taskCountdowns = [];
    private readonly System.Windows.Forms.Timer _displayClock = new() { Interval = 1000 };
    private readonly System.Windows.Forms.Timer _autoRefresh = new() { Interval = AutoRefreshMilliseconds };
    private readonly System.Windows.Forms.Timer _serverReadinessPoll = new() { Interval = ServerReadinessPollMilliseconds };
    private readonly ToolTip _helpTip = new()
    {
        AutomaticDelay = 250,
        AutoPopDelay = 12_000,
        ReshowDelay = 100,
        ShowAlways = true,
    };
    private readonly DataGridView _grid = new();
    private readonly DataGridView _auctionGrid = new();
    private readonly BindingSource _rvrSource = new();
    private readonly DataGridView _rvrGrid = new();
    private readonly DataGridView _rvrObjectivesGrid = new();
    private readonly Label _rvrUpdated = new();
    private readonly ComboBox _rvrRealm = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120 };
    private readonly TextBox _rvrSearch = new() { Width = 240, PlaceholderText = "Search RvR name, zone or task" };
    private RvrWorldSnapshot? _rvrWorld;
    private bool _rvrServerRunning;
    private string _rvrSortProperty = "Name";
    private bool _rvrSortAscending = true;
    private readonly FlowLayoutPanel _groupsPanel = new();
    private Button? _refreshButton;
    private Button? _refreshExchangeButton;
    private Button? _deleteBotButton;
    private Button? _deleteAllBotsButton;
    private readonly List<Button> _realmGenerateButtons = [];
    private readonly Button _startButton;
    private readonly Button _stopButton;
    private readonly Button _playButton;
    private Process? _serverProcess;
    private RollingServerLog? _serverLog;
    private bool _stoppingServer;
    private bool _refreshing;
    private bool _refreshingExchange;
    private bool _loadingXpRates;
    private bool _savingXpRates;
    private bool _generatingBot;
    private DateTime _nextAutoRefreshUtc;
    private DateTime _nextRvrSnapshotRefreshUtc;
    private string _dashboardFooterSummary = string.Empty;
    private string _botSortProperty = "Name";
    private bool _botSortAscending = true;
    private string _auctionSortProperty = "ItemName";
    private bool _auctionSortAscending = true;
    private ExchangeSalesForm? _salesLedger;

    public MainForm()
    {
        _database = Path.Combine(_root, "data", "opendaoc.sqlite3.db");
        _serverDirectory = Path.Combine(_root, "server");
        _serverExecutable = Path.Combine(_serverDirectory, "CoreServer.exe");
        string officialClientDirectory = Path.Combine(_root, "client-opendaoc", "app");
        _clientDirectory = File.Exists(Path.Combine(officialClientDirectory, "game.dll"))
            ? officialClientDirectory
            : Path.Combine(_root, "client");
        _clientConnector = Path.Combine(_clientDirectory, "connect.exe");
        _logsDirectory = Path.Combine(_root, "logs");

        Text = "Offline DAoC — Classic + Shrouded Isles";
        MinimumSize = new Size(960, 620);
        Size = new Size(1100, 700);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = DaocTheme.Void;
        ForeColor = DaocTheme.Text;
        Font = new Font("Georgia", 8.5f);
        AutoScaleMode = AutoScaleMode.Dpi;

        var stoneFrame = new StoneSurface { Dock = DockStyle.Fill };
        Controls.Add(stoneFrame);

        var rootLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 4,
            ColumnCount = 1,
            Padding = new Padding(5),
            BackColor = Color.Transparent,
        };
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 132));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 116));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 31));
        stoneFrame.Controls.Add(rootLayout);

        _startButton = ActionButton("START SERVER", Color.FromArgb(110, 128, 70));
        _stopButton = ActionButton("STOP SERVER", Color.FromArgb(151, 67, 57));
        _playButton = ActionButton("ENTER REALM", Color.FromArgb(65, 94, 145));
        _startButton.Click += (_, _) => StartServer();
        _stopButton.Click += async (_, _) => await StopServerAsync();
        _playButton.Click += (_, _) => LaunchClient();

        rootLayout.Controls.Add(BuildHeader(), 0, 0);
        rootLayout.Controls.Add(BuildCards(), 0, 1);
        rootLayout.Controls.Add(BuildTabs(), 0, 2);
        rootLayout.Controls.Add(BuildFooter(), 0, 3);

        _realmFilter.Items.AddRange(["All realms", "Albion", "Midgard", "Hibernia"]);
        _realmFilter.SelectedIndex = 0;
        _realmFilter.SelectedIndexChanged += (_, _) => ApplyFilter();
        _search.TextChanged += (_, _) => ApplyFilter();
        _onlineOnly.CheckedChanged += (_, _) => ApplyFilter();
        _auctionSearch.TextChanged += (_, _) => ApplyAuctionFilter();
        _auctionRealmTabs.SelectedIndexChanged += (_, _) => ApplyAuctionFilter();
        ConfigureXpRateSelector(_playerXpRate);
        ConfigureXpRateSelector(_botXpRate);
        _playerXpRate.SelectedIndexChanged += async (_, _) => await SaveXpRateAsync("xp_rate", _playerXpRate);
        _botXpRate.SelectedIndexChanged += async (_, _) => await SaveXpRateAsync("bot_xp_rate", _botXpRate);
        _groupSearch.TextChanged += (_, _) => ApplyGroupSearch();
        _groupSearch.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape) { _groupSearch.Clear(); e.SuppressKeyPress = true; }
            if (e.KeyCode == Keys.Enter) { ApplyGroupSearch(); e.SuppressKeyPress = true; }
        };

        _displayClock.Tick += (_, _) => RefreshVisibleCountdowns();
        _autoRefresh.Tick += async (_, _) =>
        {
            ScheduleNextAutoRefresh();
            await RefreshDashboardAsync();
        };
        _serverReadinessPoll.Tick += async (_, _) => await RefreshWhenServerReadyAsync();
        Shown += async (_, _) =>
        {
            // v0.31/v0.31b migration: move only the two Hibernian exchange
            // guards beside Eilwen.  The operation is idempotent and is
            // deliberately isolated from all other world rows.
            EnsureRealmExchangeGuardPositions();
            await RefreshDashboardAsync();
            await RefreshRealmExchangeAsync();
            _displayClock.Start();
            ScheduleNextAutoRefresh();
            UpdateAutoRefreshCountdown();
            _autoRefresh.Start();
        };
        FormClosing += (_, _) =>
        {
            _serverReadinessPoll.Stop();
            _serverLog?.Dispose();
        };
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _displayClock.Dispose();
            _autoRefresh.Dispose();
            _serverReadinessPoll.Dispose();
            _helpTip.Dispose();
        }
        base.Dispose(disposing);
    }

    private void RefreshVisibleCountdowns()
    {
        // Repaint only visible clock cells/labels from the last manual snapshot.
        // No ResetBindings, resort, card reconstruction, DB reads or server writes.
        UpdateAutoRefreshCountdown();
        RefreshRvrSnapshotIfDue();
        if (_rvrObjectivesGrid.Visible)
            foreach (DataGridViewColumn column in _rvrObjectivesGrid.Columns)
                if (column.DataPropertyName == "PhaseRemainingMilliseconds")
                    _rvrObjectivesGrid.InvalidateColumn(column.Index);
        foreach (DataGridView grid in new[] { _grid, _rvrGrid })
        {
            if (!grid.Visible) continue;
            foreach (DataGridViewColumn column in grid.Columns)
                if (column.DataPropertyName is "TaskRemaining" or "NameWithMeetUpTimer")
                    grid.InvalidateColumn(column.Index);
        }
        if (!_groupsPanel.Visible) return;
        Rectangle viewport = _groupsPanel.RectangleToScreen(_groupsPanel.ClientRectangle);
        foreach (var (label, bot) in _memberCountdowns)
            if (label.Visible && viewport.IntersectsWith(label.RectangleToScreen(label.ClientRectangle)))
                label.Text = GroupMemberText(bot);
        foreach (var (label, bot) in _taskCountdowns)
            if (label.Visible && viewport.IntersectsWith(label.RectangleToScreen(label.ClientRectangle)))
                label.Text = bot.GroupTimerText;
    }

    private void RefreshRvrSnapshotIfDue()
    {
        DateTime nowUtc = DateTime.UtcNow;
        if (_refreshing || nowUtc < _nextRvrSnapshotRefreshUtc)
            return;
        _nextRvrSnapshotRefreshUtc = nowUtc.AddMilliseconds(RvrSnapshotRefreshMilliseconds);
        RvrWorldSnapshot? latest = ReadRvrWorld();
        if (latest == null || _rvrWorld != null && latest.UpdatedUtc <= _rvrWorld.UpdatedUtc)
            return;
        _rvrWorld = latest;
        RenderActiveRvr();
    }

    private void ScheduleNextAutoRefresh() =>
        _nextAutoRefreshUtc = DateTime.UtcNow.AddMilliseconds(AutoRefreshMilliseconds);

    private void UpdateAutoRefreshCountdown()
    {
        TimeSpan remaining = _nextAutoRefreshUtc == default
            ? TimeSpan.FromMilliseconds(AutoRefreshMilliseconds)
            : _nextAutoRefreshUtc - DateTime.UtcNow;
        string text = FormatAutoRefreshRemaining(remaining);
        if (!string.Equals(_autoRefreshCountdownValue.Text, text, StringComparison.Ordinal))
            _autoRefreshCountdownValue.Text = text;
    }

    internal static string FormatAutoRefreshRemaining(TimeSpan remaining)
    {
        int seconds = Math.Max(0, (int)Math.Ceiling(remaining.TotalSeconds));
        return $"{seconds / 60}:{seconds % 60:00}";
    }

    private static string GroupMemberText(BotRow member)
    {
        var badges = new List<string>();
        if (!string.IsNullOrWhiteSpace(member.GroupRole))
            badges.Add(member.GroupRole.ToUpperInvariant());
        if (member.Name.Equals(member.GroupLeaderName, StringComparison.OrdinalIgnoreCase))
            badges.Add("LEADER");
        if (member.Name.Equals(member.GroupPullerName, StringComparison.OrdinalIgnoreCase))
            badges.Add("PULLER");
        string role = badges.Count == 0 ? string.Empty : "  " + string.Join(" ", badges.Select(badge => $"[{badge}]"));
        return $"{member.NameWithMeetUpTimer}{role}   •   Level {member.Level} {member.ClassName}   •   {member.ZoneName}";
    }

    private Control BuildHeader()
    {
        var banner = new FantasyBanner { Dock = DockStyle.Fill, Margin = new Padding(2, 2, 2, 3) };
        var header = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, BackColor = Color.Transparent, Padding = new Padding(12, 7, 10, 6) };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 145));

        var titlePanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        titlePanel.Controls.Add(new RealmShieldPicture
        {
            Size = new Size(43, 58),
            Location = new Point(3, 7),
        });
        titlePanel.Controls.Add(new Label
        {
            AutoSize = true,
            Text = "OFFLINE DAoC",
            Font = new Font("Georgia", 28f, FontStyle.Bold),
            ForeColor = DaocTheme.GoldLight,
            Location = new Point(52, 5),
        });
        titlePanel.Controls.Add(new Label
        {
            AutoSize = true,
            Text = "CLASSIC + SHROUDED ISLES  •  PLAY ALONE OR ADVENTURE WITH LIVING PLAYER BOTS",
            Font = new Font("Georgia", 9f),
            ForeColor = Color.FromArgb(196, 145, 70),
            Location = new Point(55, 57),
        });
        titlePanel.Controls.Add(new Label
        {
            AutoSize = true,
            Text = $"VERSION {DisplayVersion}",
            Font = new Font("Georgia", 12f, FontStyle.Bold),
            ForeColor = DaocTheme.GoldLight,
            Location = new Point(55, 79),
        });
        header.Controls.Add(titlePanel, 0, 0);

        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(3, 5, 0, 0), BackColor = Color.Transparent };
        foreach (var button in new[] { _startButton, _stopButton, _playButton })
        {
            button.Width = 132;
            button.Height = 26;
            button.Margin = new Padding(2, 2, 2, 2);
        }
        actions.Controls.Add(_startButton);
        actions.Controls.Add(_stopButton);
        actions.Controls.Add(_playButton);
        header.Controls.Add(actions, 1, 0);
        banner.Controls.Add(header);
        return banner;
    }

    private Control BuildCards()
    {
        var cards = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 6, RowCount = 1, Padding = new Padding(0, 4, 0, 5), BackColor = Color.Transparent };
        // Realm cards need room for two generation actions; status cards do not.
        foreach (float width in new[] { 16f, 13f, 19f, 19f, 19f, 14f })
            cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, width));
        cards.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        cards.Controls.Add(Card("SERVER", _serverState, DaocTheme.Iron), 0, 0);
        _serverState.Font = new Font("Georgia", 12f, FontStyle.Bold);
        cards.Controls.Add(Card("ONLINE BOTS", _onlineValue, DaocTheme.Gold), 1, 0);
        cards.Controls.Add(Card("ALBION", _albionValue, DaocTheme.Albion, RealmGenerateButton(1, "ALBION", DaocTheme.Albion)), 2, 0);
        cards.Controls.Add(Card("MIDGARD", _midgardValue, DaocTheme.Midgard, RealmGenerateButton(2, "MIDGARD", DaocTheme.Midgard)), 3, 0);
        cards.Controls.Add(Card("HIBERNIA", _hiberniaValue, DaocTheme.Hibernia, RealmGenerateButton(3, "HIBERNIA", DaocTheme.Hibernia)), 4, 0);
        Panel performanceCard = Card("BOT AI DELAY", _performanceValue, DaocTheme.Gold);
        const string performanceHelp = "How long bot AI updates take. 95 out of 100 updates finish within this time. Lower is better. ms means milliseconds; 1,000 ms equals one second.";
        SetToolTip(performanceCard, performanceHelp);
        cards.Controls.Add(performanceCard, 5, 0);
        return cards;
    }

    private void SetToolTip(Control root, string text)
    {
        _helpTip.SetToolTip(root, text);
        foreach (Control child in root.Controls)
            SetToolTip(child, text);
    }

    private Control RealmGenerateButton(int realm, string realmName, Color accent)
    {
        var split = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty, Padding = Padding.Empty };
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        split.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        split.Controls.Add(RealmGenerateLevelButton(realm, realmName, accent, 1), 0, 0);
        split.Controls.Add(RealmGenerateLevelButton(realm, realmName, accent, 50), 1, 0);
        return split;
    }

    private Button RealmGenerateLevelButton(int realm, string realmName, Color accent, int level)
    {
        Button button = ActionButton($"+ Lv.{level}", accent);
        if (button is RuneButton rune) rune.ShowOrnaments = false;
        button.Margin = new Padding(1, 0, 1, 0);
        button.Font = new Font("Georgia", 8f, FontStyle.Bold);
        button.Height = 22;
        button.Dock = DockStyle.Fill;
        _realmGenerateButtons.Add(button);
        button.Click += async (_, _) =>
        {
            if (_generatingBot)
                return;

            int batchSize = GetGenerationBatchSize();
            _generatingBot = true;
            SetRealmGenerationEnabled(false);
            _footer.Text = batchSize == 1
                ? $"Rolling a new level {level} {realmName[0] + realmName[1..].ToLowerInvariant()} character…"
                : $"Creating {batchSize} level {level} {realmName[0] + realmName[1..].ToLowerInvariant()} characters as one protected batch…";
            try
            {
                IReadOnlyList<BotCharacterGenerator.Identity> identities =
                    await Task.Run(() => GenerateBotCharacters(realm, batchSize, level));
                _realmFilter.SelectedItem = realmName[0] + realmName[1..].ToLowerInvariant();
                await RefreshDashboardAsync();
                BotCharacterGenerator.Identity last = identities[^1];
                SelectBot(last.Name);
                _footer.Text = batchSize == 1
                    ? $"Created {last.Name}, a level {level} {last.RaceName} {last.ClassName}. Queued for staggered login."
                    : $"Created all {batchSize} level {level} {realmName[0] + realmName[1..].ToLowerInvariant()} characters. All are queued for staggered login.";
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, exception.Message, "Unable to generate bot", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _generatingBot = false;
                SetRealmGenerationEnabled(true);
            }
        };
        return button;
    }

    private static int GetGenerationBatchSize()
    {
        if ((GetAsyncKeyState(VkLeftControl) & 0x8000) != 0)
            return 100;
        if ((GetAsyncKeyState(VkLeftShift) & 0x8000) != 0)
            return 10;
        return 1;
    }

    private void SetRealmGenerationEnabled(bool enabled)
    {
        foreach (Button button in _realmGenerateButtons)
            button.Enabled = enabled;
    }

    private Control BuildTabs()
    {
        var tabs = new DaocTabControl { Dock = DockStyle.Fill };
        var population = new TabPage("Active Population") { BackColor = DaocTheme.Panel, ForeColor = DaocTheme.Text };
        var populationLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
        populationLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 43));
        populationLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        populationLayout.Controls.Add(BuildFilters(), 0, 0);
        populationLayout.Controls.Add(BuildGrid(), 0, 1);
        population.Controls.Add(populationLayout);

        var auction = new TabPage("Realm Exchange") { BackColor = Color.FromArgb(48, 38, 26), ForeColor = DaocTheme.Parchment };
        auction.Controls.Add(BuildAuctionPanel());
        var groups = new TabPage("Active Groups") { BackColor = DaocTheme.Panel, ForeColor = DaocTheme.Text };
        groups.Controls.Add(BuildActiveGroupsPanel());
        var rvr = new TabPage("Active RvR") { BackColor = DaocTheme.Panel, ForeColor = DaocTheme.Text };
        rvr.Controls.Add(BuildActiveRvrPanel());
        var xpSettings = new TabPage("XP Settings") { BackColor = DaocTheme.Panel, ForeColor = DaocTheme.Text };
        xpSettings.Controls.Add(BuildXpSettingsPanel());
        tabs.TabPages.Add(population);
        tabs.TabPages.Add(groups);
        tabs.TabPages.Add(rvr);
        var events = new TabPage("Realm Events") { BackColor = DaocTheme.Panel, ForeColor = DaocTheme.Text };
        events.Controls.Add(BuildRealmEventsPanel());
        tabs.TabPages.Add(events);
        var records = new TabPage("Realm Records") { BackColor = DaocTheme.Panel, ForeColor = DaocTheme.Text };
        var eventRecords = new RealmEventRecordsControl(Path.Combine(_serverDirectory, "realm-event-records.sqlite3"));
        records.Controls.Add(eventRecords);
        records.Enter += async (_, _) => await eventRecords.RefreshAsync();
        tabs.TabPages.Add(records);
        tabs.TabPages.Add(xpSettings);
        var botGoals = new TabPage("Bot Goals Setting") { BackColor = DaocTheme.Panel, ForeColor = DaocTheme.Text };
        _botGoalsSettings = new BotGoalsSettingsControl(
            Path.Combine(_serverDirectory, OfflineDaoc.Configuration.BotGoalSettings.FileName), BotGoalsServerStopped);
        botGoals.Controls.Add(_botGoalsSettings);
        tabs.TabPages.Add(botGoals);
        tabs.TabPages.Add(auction);
        return tabs;
    }

    private Control BuildXpSettingsPanel()
    {
        var surface = new InsetPanel
        {
            Dock = DockStyle.Top,
            Height = 274,
            Margin = new Padding(18),
            Padding = new Padding(20),
            Accent = DaocTheme.Gold,
        };
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 5,
            BackColor = Color.Transparent,
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 210));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 135));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var titlePanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        titlePanel.Controls.Add(new Label
        {
            AutoSize = true,
            Text = "EXPERIENCE RATE CONTROL",
            Font = new Font("Georgia", 13f, FontStyle.Bold),
            ForeColor = DaocTheme.GoldLight,
            Location = new Point(0, 12),
        });
        layout.Controls.Add(titlePanel, 0, 0);
        layout.SetColumnSpan(titlePanel, 3);
        layout.Controls.Add(XpRateLabel("YOUR PLAYER XP"), 0, 1);
        layout.Controls.Add(_playerXpRate, 1, 1);
        layout.Controls.Add(XpRateDescription("Applies to real player characters only."), 2, 1);
        layout.Controls.Add(XpRateLabel("AUTONOMOUS BOT XP"), 0, 2);
        layout.Controls.Add(_botXpRate, 1, 2);
        layout.Controls.Add(XpRateDescription("Applies to persistent player bots only; companion helpers remain XP-neutral."), 2, 2);
        _xpSettingsStatus.Dock = DockStyle.Fill;
        _xpSettingsStatus.TextAlign = ContentAlignment.MiddleLeft;
        _xpSettingsStatus.Font = new Font("Georgia", 9f, FontStyle.Bold | FontStyle.Italic);
        layout.Controls.Add(_makeMeGm, 0, 3);
        layout.SetColumnSpan(_makeMeGm, 2);
        layout.Controls.Add(XpRateDescription("Applies to your account at the next server startup."), 2, 3);
        _makeMeGm.CheckedChanged += async (_, _) => await SaveGmSettingAsync();
        layout.Controls.Add(_xpSettingsStatus, 0, 4);
        layout.SetColumnSpan(_xpSettingsStatus, 3);
        surface.Controls.Add(layout);
        return surface;
    }

    private static Label XpRateLabel(string text) => new()
    {
        Dock = DockStyle.Fill,
        Text = text,
        ForeColor = DaocTheme.Parchment,
        Font = new Font("Georgia", 9f, FontStyle.Bold),
        TextAlign = ContentAlignment.MiddleLeft,
    };

    private static Label XpRateDescription(string text) => new()
    {
        Dock = DockStyle.Fill,
        Text = text,
        ForeColor = DaocTheme.Parchment,
        Font = new Font("Georgia", 9.25f, FontStyle.Bold),
        TextAlign = ContentAlignment.MiddleLeft,
    };

    private static void ConfigureXpRateSelector(ComboBox selector)
    {
        StyleInput(selector);
        selector.Anchor = AnchorStyles.Left;
        selector.Margin = new Padding(0, 10, 0, 9);
        selector.Items.AddRange(new object[]
        {
            new XpRateOption(1, "1×  Original"),
            new XpRateOption(2, "2×"),
            new XpRateOption(3, "3×"),
            new XpRateOption(5, "5×"),
            new XpRateOption(10, "10×"),
        });
    }

    private Control BuildActiveGroupsPanel()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            ColumnCount = 1,
            Padding = new Padding(7),
            BackColor = DaocTheme.Panel,
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = "⚔  ACTIVE GROUPS  •  Name (MM:SS) shows the meet-up deadline",
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Georgia", 9.5f, FontStyle.Bold),
            ForeColor = DaocTheme.GoldLight,
            BackColor = DaocTheme.StoneDark,
            Padding = new Padding(11, 0, 0, 0),
            BorderStyle = BorderStyle.Fixed3D,
        }, 0, 0);

        var filters = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Padding = new Padding(3) };
        StyleInput(_groupSearch);
        filters.Controls.Add(_groupSearch);
        var clearSearch = ActionButton("CLEAR", DaocTheme.Panel);
        clearSearch.Width = 80;
        clearSearch.Click += (_, _) => _groupSearch.Clear();
        filters.Controls.Add(clearSearch);
        filters.Controls.Add(_groupSearchStatus);
        layout.Controls.Add(filters, 0, 1);

        _groupsPanel.Dock = DockStyle.Fill;
        _groupsPanel.AutoScroll = true;
        _groupsPanel.FlowDirection = FlowDirection.TopDown;
        _groupsPanel.WrapContents = false;
        _groupsPanel.Padding = new Padding(3, 6, 3, 6);
        _groupsPanel.BackColor = DaocTheme.Panel;
        _groupsPanel.SizeChanged += (_, _) => ResizeGroupCards();
        layout.Controls.Add(_groupsPanel, 0, 2);
        return layout;
    }

    private Control BuildActiveRvrPanel()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 4,
            ColumnCount = 1,
            Padding = new Padding(7),
            BackColor = DaocTheme.Panel,
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 0));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _rvrUpdated.Dock = DockStyle.Fill;
        _rvrUpdated.TextAlign = ContentAlignment.MiddleLeft;
        _rvrUpdated.Font = new Font("Georgia", 9f, FontStyle.Bold);
        _rvrUpdated.ForeColor = DaocTheme.GoldLight;
        _rvrUpdated.Padding = new Padding(8, 0, 0, 0);
        layout.Controls.Add(BuildActiveRvrHeader(), 0, 0);

        _rvrGrid.Dock = DockStyle.Fill;
        _rvrGrid.ReadOnly = true;
        _rvrGrid.AllowUserToAddRows = false;
        _rvrGrid.AllowUserToDeleteRows = false;
        _rvrGrid.AllowUserToResizeRows = false;
        _rvrGrid.AutoGenerateColumns = false;
        _rvrGrid.RowHeadersVisible = false;
        _rvrGrid.MultiSelect = false;
        _rvrGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _rvrGrid.BackgroundColor = DaocTheme.StoneDark;
        _rvrGrid.BorderStyle = BorderStyle.Fixed3D;
        _rvrGrid.GridColor = Color.FromArgb(78, 70, 56);
        _rvrGrid.EnableHeadersVisualStyles = false;
        _rvrGrid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(61, 54, 43),
            ForeColor = DaocTheme.GoldLight,
            SelectionBackColor = Color.FromArgb(61, 54, 43),
            Font = new Font("Georgia", 8.25f, FontStyle.Bold),
            Padding = new Padding(2),
        };
        _rvrGrid.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(35, 31, 26),
            ForeColor = DaocTheme.Text,
            SelectionBackColor = Color.FromArgb(90, 72, 43),
            SelectionForeColor = DaocTheme.GoldLight,
            Padding = new Padding(2),
            Font = new Font("Georgia", 8.25f),
        };
        _rvrGrid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(42, 37, 30) };
        _rvrGrid.ColumnHeadersHeight = 28;
        _rvrGrid.RowTemplate.Height = 25;
        _rvrGrid.DataSource = _rvrSource;
        _rvrGrid.Columns.Add(TextColumn("Name / meet-up", "NameWithMeetUpTimer", 135));
        _rvrGrid.Columns.Add(TextColumn("Realm", "Realm", 70));
        _rvrGrid.Columns.Add(TextColumn("Class", "ClassName", 95));
        _rvrGrid.Columns.Add(TextColumn("Lvl", "Level", 45));
        _rvrGrid.Columns.Add(TextColumn("Zone", "ZoneName", 100));
        _rvrGrid.Columns.Add(TextColumn("Formation", "RvrFormation", 100));
        _rvrGrid.Columns.Add(TextColumn("Phase", "ObjectivePhase", 85));
        _rvrGrid.Columns.Add(TextColumn("Task left", "TaskRemaining", 90));
        _rvrGrid.Columns.Add(TextColumn("Current objective", "Activity", 260, DataGridViewAutoSizeColumnMode.Fill));
        _rvrGrid.CellFormatting += (_, eventArgs) =>
        {
            if (eventArgs.RowIndex < 0 || eventArgs.ColumnIndex < 0 || _rvrGrid.Rows[eventArgs.RowIndex].DataBoundItem is not BotRow bot)
                return;
            if (_rvrGrid.Columns[eventArgs.ColumnIndex].DataPropertyName == "Realm")
            {
                eventArgs.CellStyle.ForeColor = bot.Realm switch
                {
                    "Albion" => Color.FromArgb(225, 116, 105),
                    "Midgard" => Color.FromArgb(124, 161, 215),
                    "Hibernia" => Color.FromArgb(112, 178, 112),
                    _ => Color.White,
                };
            }
        };
        foreach (DataGridViewColumn column in _rvrGrid.Columns)
            column.SortMode = DataGridViewColumnSortMode.Programmatic;
        _rvrGrid.ColumnHeaderMouseClick += (_, e) =>
        {
            string property = _rvrGrid.Columns[e.ColumnIndex].DataPropertyName;
            _rvrSortAscending = property == _rvrSortProperty ? !_rvrSortAscending : true;
            _rvrSortProperty = property;
            RenderActiveRvr();
        };

        _rvrObjectivesGrid.Dock = DockStyle.Fill;
        _rvrObjectivesGrid.ReadOnly = true;
        _rvrObjectivesGrid.AllowUserToAddRows = false;
        _rvrObjectivesGrid.AllowUserToDeleteRows = false;
        _rvrObjectivesGrid.RowHeadersVisible = false;
        _rvrObjectivesGrid.AutoGenerateColumns = false;
        _rvrObjectivesGrid.BackgroundColor = DaocTheme.StoneDark;
        _rvrObjectivesGrid.EnableHeadersVisualStyles = false;
        _rvrObjectivesGrid.ColumnHeadersDefaultCellStyle = _rvrGrid.ColumnHeadersDefaultCellStyle.Clone();
        _rvrObjectivesGrid.DefaultCellStyle = _rvrGrid.DefaultCellStyle.Clone();
        _rvrObjectivesGrid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
        _rvrObjectivesGrid.Columns.Add(TextColumn("Type", "Marker", 110));
        _rvrObjectivesGrid.Columns.Add(TextColumn("Keep / relic", "Name", 165));
        _rvrObjectivesGrid.Columns.Add(TextColumn("Held by", "Owner", 85));
        _rvrObjectivesGrid.Columns.Add(TextColumn("Battle status", "State", 180));
        _rvrObjectivesGrid.Columns[^1].DefaultCellStyle.WrapMode = DataGridViewTriState.True;
        _rvrObjectivesGrid.Columns.Add(TextColumn("Carrier", "Carrier", 110));
        _rvrObjectivesGrid.Columns.Add(TextColumn("Location", "Location", 110));
        _rvrObjectivesGrid.Columns.Add(TextColumn("Rally attendance / battle", "Forces", 260, DataGridViewAutoSizeColumnMode.Fill));
        _rvrObjectivesGrid.Columns[^1].MinimumWidth = 260;
        _rvrObjectivesGrid.Columns[^1].DefaultCellStyle.WrapMode = DataGridViewTriState.True;
        _rvrObjectivesGrid.CellFormatting += (_, e) =>
        {
            if (e.RowIndex < 0 || _rvrObjectivesGrid.Rows[e.RowIndex].DataBoundItem is not RvrObjective row) return;
            ApplyRvrStatusStyle(e.CellStyle, row.State);
        };
        var filters = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        _rvrRealm.Items.AddRange(["All realms", "Albion", "Midgard", "Hibernia"]);
        _rvrRealm.SelectedIndex = 0;
        _rvrRealm.SelectedIndexChanged += (_, _) => RenderActiveRvr();
        _rvrSearch.TextChanged += (_, _) => RenderActiveRvr();
        filters.Controls.Add(new Label { AutoSize = true, Text = "Filter:", ForeColor = DaocTheme.GoldLight, Padding = new Padding(0, 5, 0, 0) });
        filters.Controls.Add(_rvrRealm);
        filters.Controls.Add(_rvrSearch);
        filters.Controls.Add(new Label { AutoSize = true, Text = "Click column headings to sort · uses the main Refresh", ForeColor = DaocTheme.GoldLight, Padding = new Padding(5, 5, 0, 0) });
        layout.Controls.Add(filters, 0, 2);
        layout.Controls.Add(_rvrGrid, 0, 3);
        return layout;
    }

    private void RenderActiveGroups()
    {
        _groupsPanel.SuspendLayout();
        _groupMemberLabels.Clear();
        _memberCountdowns.Clear();
        _taskCountdowns.Clear();
        // Refresh replaces cards; dispose them rather than leaking controls and fonts.
        foreach (Control oldCard in _groupsPanel.Controls.Cast<Control>().ToArray()) oldCard.Dispose();
        _groupsPanel.Controls.Clear();
        if (_groups.Count == 0)
        {
            _groupsPanel.Controls.Add(new Label
            {
                AutoSize = false,
                Width = Math.Max(400, _groupsPanel.ClientSize.Width - 28),
                Height = 72,
                Text = "No active playerbot groups in the current snapshot.\nGroups appear here after matchmaking and disappear after disbanding.",
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = DaocTheme.Muted,
                BackColor = DaocTheme.StoneDark,
                Font = new Font("Georgia", 9f, FontStyle.Italic),
                BorderStyle = BorderStyle.Fixed3D,
            });
        }
        else
        {
            int number = 1;
            foreach (GroupRow group in _groups.OrderBy(row => row.Realm).ThenBy(row => row.GroupId, StringComparer.OrdinalIgnoreCase))
                _groupsPanel.Controls.Add(BuildGroupCard(group, number++));
        }
        ResizeGroupCards();
        _groupsPanel.ResumeLayout();
        ApplyGroupSearch();
    }

    private void ApplyGroupSearch()
    {
        // Work entirely on the last dashboard snapshot. No DB query, HTTP call,
        // refresh, or new card construction is performed while typing.
        string query = _groupSearch.Text.Trim();
        Control? first = null;
        Control? exact = null;
        int matches = 0;
        _groupsPanel.SuspendLayout();
        foreach (Control card in _groupsPanel.Controls)
        {
            if (card.Tag is not GroupRow group) continue;
            bool visible = query.Length == 0 ||
                group.Realm.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                group.Members.Any(member => member.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    member.ZoneName.Contains(query, StringComparison.OrdinalIgnoreCase));
            card.Visible = visible;
            if (!visible) continue;
            matches++;
            first ??= card;
            if (query.Length > 0 && group.Members.Any(member => member.Name.Equals(query, StringComparison.OrdinalIgnoreCase)))
                exact ??= card;
        }
        foreach (Label label in _groupMemberLabels)
        {
            bool hit = query.Length > 0 && label.Tag is string name && name.Contains(query, StringComparison.OrdinalIgnoreCase);
            label.BackColor = hit ? Color.FromArgb(105, 79, 30) : Color.Transparent;
            label.ForeColor = hit ? DaocTheme.GoldLight : DaocTheme.Text;
        }
        _groupSearchStatus.Text = query.Length == 0 ? $"{matches} groups" : matches == 0
            ? "No group matches that bot, zone or faction in the snapshot."
            : $"{matches} matching group{(matches == 1 ? string.Empty : "s")}";
        _groupsPanel.ResumeLayout(true);
        if (query.Length > 0 && (exact ?? first) is Control target)
            _groupsPanel.ScrollControlIntoView(target);
    }

    private Control BuildGroupCard(GroupRow group, int number)
    {
        // Small parties still need room for phase, shared goal and route status.
        int memberRowsHeight = Math.Max(205, group.Members.Count * 27 + 9);
        var card = new TableLayoutPanel
        {
            Tag = group,
            Width = Math.Max(560, _groupsPanel.ClientSize.Width - 28),
            Height = 43 + memberRowsHeight,
            RowCount = 2,
            ColumnCount = 2,
            Margin = new Padding(0, 0, 0, 9),
            BackColor = Color.FromArgb(35, 31, 26),
            CellBorderStyle = TableLayoutPanelCellBorderStyle.Single,
        };
        card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));
        card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
        card.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        card.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        string levels = group.Members.Count == 0 ? "—" : group.Members.Min(member => member.Level) == group.Members.Max(member => member.Level)
            ? group.Members[0].Level.ToString()
            : $"{group.Members.Min(member => member.Level)}–{group.Members.Max(member => member.Level)}";
        var heading = new Label
        {
            Dock = DockStyle.Fill,
            Text = $"GROUP {number}  •  {group.Realm.ToUpperInvariant()}  •  {group.Members.Count} PLAYERBOT{(group.Members.Count == 1 ? string.Empty : "S")}  •  LEVELS {levels}",
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(10, 0, 0, 0),
            BackColor = RealmGroupColor(group.Realm),
            ForeColor = DaocTheme.GoldLight,
            Font = new Font("Georgia", 9f, FontStyle.Bold),
        };
        card.Controls.Add(heading, 0, 0);
        card.SetColumnSpan(heading, 2);

        var roster = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = group.Members.Count + 1, Padding = new Padding(5, 4, 5, 4), BackColor = Color.FromArgb(31, 29, 25) };
        foreach (BotRow member in group.Members.OrderBy(member => member.Level).ThenBy(member => member.Name, StringComparer.OrdinalIgnoreCase))
        {
            roster.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
            var memberLabel = new Label
            {
                Tag = member.Name,
                Dock = DockStyle.Fill,
                Text = GroupMemberText(member),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(6, 0, 0, 0),
                ForeColor = DaocTheme.Text,
                BackColor = Color.Transparent,
                Font = new Font("Georgia", 8.25f),
            };
            roster.Controls.Add(memberLabel);
            _groupMemberLabels.Add(memberLabel);
            _memberCountdowns.Add((memberLabel, member));
        }
        roster.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        card.Controls.Add(roster, 0, 1);
        var details = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(44, 38, 29) };
        details.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = $"LEADER: {group.LeaderName}\nTOWN: {group.RendezvousName}\nPULLER: {group.PullerName}\n\n" +
                   $"{group.Phase.ToUpperInvariant()}\n\n{group.SharedGoal}\n\n{group.Status}",
            TextAlign = ContentAlignment.TopLeft,
            Padding = new Padding(13, 10, 10, 8),
            ForeColor = Color.FromArgb(214, 196, 151),
            BackColor = Color.FromArgb(44, 38, 29),
            Font = new Font("Georgia", 8.5f),
        });
        // Arrived meetup members intentionally have no personal deadline. Do
        // not choose one of those rows as the card's clock owner or the group
        // falsely displays 0:00 while other members still have time remaining.
        BotRow? timerOwner = group.Phase == "Meeting up"
            ? group.Members.Where(member => member.AssemblyRemainingMilliseconds.HasValue)
                .OrderByDescending(member => member.AssemblyRemainingMilliseconds)
                .FirstOrDefault()
            : group.Members.FirstOrDefault();
        if (timerOwner != null)
        {
            var timerLabel = new Label
            {
                Dock = DockStyle.Top, Height = 28, Padding = new Padding(13, 4, 0, 0),
                Text = timerOwner.GroupTimerText, ForeColor = DaocTheme.GoldLight,
                Font = new Font("Georgia", 8.5f, FontStyle.Bold)
            };
            details.Controls.Add(timerLabel);
            _taskCountdowns.Add((timerLabel, timerOwner));
        }
        card.Controls.Add(details, 1, 1);
        return card;
    }

    private void ResizeGroupCards()
    {
        int width = Math.Max(560, _groupsPanel.ClientSize.Width - 28);
        foreach (Control control in _groupsPanel.Controls)
            control.Width = width;
    }

    private static Color RealmGroupColor(string realm) => realm switch
    {
        "Albion" => Color.FromArgb(76, 42, 35),
        "Midgard" => Color.FromArgb(37, 54, 75),
        "Hibernia" => Color.FromArgb(43, 66, 38),
        _ => Color.FromArgb(61, 54, 43),
    };

    private Control BuildFilters()
    {
        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Padding = new Padding(5, 7, 0, 4), BackColor = DaocTheme.Panel };
        StyleInput(_realmFilter);
        StyleInput(_search);
        panel.Controls.Add(_realmFilter);
        panel.Controls.Add(_search);
        _refreshButton = ActionButton("REFRESH", Color.FromArgb(126, 104, 67));
        _refreshButton.Width = 100;
        _refreshButton.Click += async (_, _) => await RefreshDashboardAsync();
        panel.Controls.Add(_refreshButton);
        _deleteBotButton = ActionButton("DELETE CHARACTER…", Color.FromArgb(151, 67, 57));
        _deleteBotButton.Width = 174;
        _deleteBotButton.Enabled = false;
        _deleteBotButton.Click += async (_, _) => await DeleteSelectedBotAsync();
        panel.Controls.Add(_deleteBotButton);
        panel.Controls.Add(_onlineOnly);
        _helpTip.SetToolTip(_onlineOnly, "Show only online bots in the current realm and search results. Uncheck to show all bots.");
        return panel;
    }

    private Control BuildFooter()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = DaocTheme.StoneDark,
            ColumnCount = 4,
            RowCount = 1,
            Margin = new Padding(0),
            Padding = new Padding(0),
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 48));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 168));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.Controls.Add(_footer, 0, 0);
        panel.Controls.Add(_autoRefreshCountdownCaption, 1, 0);
        panel.Controls.Add(_autoRefreshCountdownValue, 2, 0);
        _deleteAllBotsButton = ActionButton("DELETE ALL BOTS…", Color.FromArgb(151, 45, 40));
        _deleteAllBotsButton.Dock = DockStyle.Fill;
        _deleteAllBotsButton.Margin = new Padding(0);
        _deleteAllBotsButton.Enabled = false;
        _deleteAllBotsButton.Click += async (_, _) => await DeleteAllBotsAsync();
        panel.Controls.Add(_deleteAllBotsButton, 3, 0);
        return panel;
    }

    private Control BuildAuctionPanel()
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, Padding = new Padding(7), BackColor = Color.FromArgb(48, 38, 26) };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 39));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var exchangeHeader = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(69, 51, 31) };
        exchangeHeader.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = "✦  THE REALM EXCHANGE  ✦    Camelot • Jordheim • Tir na Nog",
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Georgia", 10f, FontStyle.Bold),
            ForeColor = DaocTheme.GoldLight,
            BackColor = Color.FromArgb(69, 51, 31),
            Padding = new Padding(11, 0, 0, 0),
            BorderStyle = BorderStyle.Fixed3D,
        });
        var salesButton = ActionButton("50 MOST RECENTLY SOLD", DaocTheme.Gold);
        salesButton.Dock = DockStyle.Right;
        salesButton.Width = 215;
        salesButton.Click += (_, _) =>
        {
            if (_salesLedger == null || _salesLedger.IsDisposed)
            {
                _salesLedger = new ExchangeSalesForm(_database);
                _salesLedger.Show(this);
            }
            else { _salesLedger.WindowState = FormWindowState.Normal; _salesLedger.Activate(); }
        };
        exchangeHeader.Controls.Add(salesButton);
        _refreshExchangeButton = ActionButton("REFRESH REALM EXCHANGE", Color.FromArgb(126, 104, 67));
        _refreshExchangeButton.Dock = DockStyle.Right;
        _refreshExchangeButton.Width = 215;
        _refreshExchangeButton.Click += async (_, _) => await RefreshRealmExchangeAsync();
        exchangeHeader.Controls.Add(_refreshExchangeButton);
        layout.Controls.Add(exchangeHeader, 0, 0);

        var filters = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(0, 6, 0, 3), BackColor = Color.FromArgb(48, 38, 26) };
        _auctionSearch.BackColor = Color.FromArgb(220, 204, 163);
        _auctionSearch.ForeColor = DaocTheme.Ink;
        _auctionSearch.BorderStyle = BorderStyle.FixedSingle;
        filters.Controls.Add(_auctionSearch);
        foreach (string realm in new[] { "Albion", "Midgard", "Hibernia" })
            _auctionRealmTabs.TabPages.Add(realm);
        _auctionRealmTabs.SelectedIndex = 0;
        filters.Controls.Add(_auctionRealmTabs);
        filters.Controls.Add(new Label
        {
            AutoSize = true,
            Margin = new Padding(12, 6, 0, 0),
            Text = "Listings expire after 24h • startup/manual refresh only",
            ForeColor = Color.FromArgb(196, 174, 126),
            Font = new Font("Georgia", 8f, FontStyle.Italic),
        });
        layout.Controls.Add(filters, 0, 1);

        ConfigureAuctionGrid();
        layout.Controls.Add(_auctionGrid, 0, 2);
        return layout;
    }

    private void ConfigureAuctionGrid()
    {
        _auctionGrid.Dock = DockStyle.Fill;
        _auctionGrid.ReadOnly = true;
        _auctionGrid.AllowUserToAddRows = false;
        _auctionGrid.AllowUserToDeleteRows = false;
        _auctionGrid.AllowUserToResizeRows = false;
        _auctionGrid.AutoGenerateColumns = false;
        _auctionGrid.RowHeadersVisible = false;
        _auctionGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _auctionGrid.BackgroundColor = Color.FromArgb(205, 187, 144);
        _auctionGrid.BorderStyle = BorderStyle.Fixed3D;
        _auctionGrid.GridColor = Color.FromArgb(127, 103, 64);
        _auctionGrid.EnableHeadersVisualStyles = false;
        _auctionGrid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(80, 59, 35), ForeColor = DaocTheme.GoldLight,
            SelectionBackColor = Color.FromArgb(80, 59, 35), Font = new Font("Georgia", 8.25f, FontStyle.Bold), Padding = new Padding(2),
        };
        _auctionGrid.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(215, 199, 157), ForeColor = DaocTheme.Ink,
            SelectionBackColor = Color.FromArgb(117, 85, 45), SelectionForeColor = Color.White, Padding = new Padding(2),
            Font = new Font("Georgia", 8.25f),
        };
        _auctionGrid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(201, 182, 138) };
        _auctionGrid.ColumnHeadersHeight = 28;
        _auctionGrid.RowTemplate.Height = 25;
        _auctionGrid.DataSource = _auctionSource;
        _auctionGrid.Columns.Add(TextColumn("Item", "ItemName", 230, DataGridViewAutoSizeColumnMode.Fill));
        _auctionGrid.Columns.Add(TextColumn("Seller", "Seller", 130));
        _auctionGrid.Columns.Add(TextColumn("Level", "ItemLevel", 60));
        _auctionGrid.Columns.Add(TextColumn("Qty", "Quantity", 55));
        _auctionGrid.Columns.Add(TextColumn("Sale type", "CurrentBid", 105));
        _auctionGrid.Columns.Add(TextColumn("Price", "Buyout", 120));
        _auctionGrid.Columns.Add(TextColumn("Expires / remaining", "Expires", 210));
        _auctionGrid.Columns.Add(TextColumn("Realm", "State", 85));
        foreach (DataGridViewColumn column in _auctionGrid.Columns)
            column.SortMode = DataGridViewColumnSortMode.Programmatic;
        _auctionGrid.ColumnHeaderMouseClick += (_, eventArgs) => SortAuctionsByColumn(eventArgs.ColumnIndex);
    }

    private Control BuildGrid()
    {
        _grid.Dock = DockStyle.Fill;
        _grid.ReadOnly = true;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.AllowUserToResizeRows = false;
        _grid.AutoGenerateColumns = false;
        _grid.RowHeadersVisible = false;
        _grid.MultiSelect = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.BackgroundColor = DaocTheme.StoneDark;
        _grid.BorderStyle = BorderStyle.Fixed3D;
        _grid.GridColor = Color.FromArgb(78, 70, 56);
        _grid.EnableHeadersVisualStyles = false;
        _grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(61, 54, 43),
            ForeColor = DaocTheme.GoldLight,
            SelectionBackColor = Color.FromArgb(61, 54, 43),
            Font = new Font("Georgia", 8.25f, FontStyle.Bold),
            Padding = new Padding(2),
        };
        _grid.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(35, 31, 26),
            ForeColor = DaocTheme.Text,
            SelectionBackColor = Color.FromArgb(90, 72, 43),
            SelectionForeColor = DaocTheme.GoldLight,
            Padding = new Padding(2),
            Font = new Font("Georgia", 8.25f),
        };
        _grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(42, 37, 30) };
        _grid.ColumnHeadersHeight = 28;
        _grid.RowTemplate.Height = 25;
        _grid.DataSource = _botSource;
        _grid.Columns.Add(TextColumn("Name", "Name", 120));
        _grid.Columns.Add(TextColumn("Realm", "Realm", 70));
        _grid.Columns.Add(TextColumn("Race", "RaceName", 85));
        _grid.Columns.Add(TextColumn("Gender", "Gender", 60));
        _grid.Columns.Add(TextColumn("Class", "ClassName", 105));
        _grid.Columns.Add(TextColumn("Lvl", "Level", 45));
        _grid.Columns.Add(TextColumn("Zone", "ZoneName", 110));
        _grid.Columns.Add(TextColumn("Task left", "TaskRemaining", 95));
        _grid.Columns.Add(TextColumn("Activity", "Activity", 210, DataGridViewAutoSizeColumnMode.Fill));
        _grid.Columns.Add(TextColumn("State", "State", 65));
        foreach (DataGridViewColumn column in _grid.Columns)
            column.SortMode = DataGridViewColumnSortMode.Programmatic;
        _grid.ColumnHeaderMouseClick += (_, eventArgs) => SortBotsByColumn(eventArgs.ColumnIndex);
        _grid.CellFormatting += (_, eventArgs) =>
        {
            if (eventArgs.RowIndex < 0 || eventArgs.ColumnIndex < 0 || _grid.Rows[eventArgs.RowIndex].DataBoundItem is not BotRow bot) return;
            if (_grid.Columns[eventArgs.ColumnIndex].DataPropertyName == "Realm")
            {
                eventArgs.CellStyle.ForeColor = bot.Realm switch
                {
                    "Albion" => Color.FromArgb(225, 116, 105),
                    "Midgard" => Color.FromArgb(124, 161, 215),
                    "Hibernia" => Color.FromArgb(112, 178, 112),
                    _ => Color.White,
                };
            }

            if (!bot.IsOnline)
            {
                eventArgs.CellStyle.BackColor = eventArgs.RowIndex % 2 == 0
                    ? Color.FromArgb(29, 28, 25)
                    : Color.FromArgb(33, 31, 27);
                eventArgs.CellStyle.ForeColor = _grid.Columns[eventArgs.ColumnIndex].DataPropertyName == "Realm"
                    ? Color.FromArgb(eventArgs.CellStyle.ForeColor.R / 2 + 45, eventArgs.CellStyle.ForeColor.G / 2 + 42, eventArgs.CellStyle.ForeColor.B / 2 + 36)
                    : Color.FromArgb(126, 120, 107);
                eventArgs.CellStyle.SelectionBackColor = Color.FromArgb(68, 61, 50);
                eventArgs.CellStyle.SelectionForeColor = Color.FromArgb(188, 178, 153);
            }
        };
        _grid.SelectionChanged += (_, _) => UpdateDeleteButton();
        _grid.CellMouseDown += (_, eventArgs) =>
        {
            if (eventArgs.Button != MouseButtons.Right || eventArgs.RowIndex < 0)
                return;

            _grid.ClearSelection();
            DataGridViewRow row = _grid.Rows[eventArgs.RowIndex];
            row.Selected = true;
            _grid.CurrentCell = row.Cells[eventArgs.ColumnIndex >= 0 ? eventArgs.ColumnIndex : 0];
        };
        var menu = new ContextMenuStrip();
        var teleportItem = new ToolStripMenuItem("Teleport to");
        teleportItem.Click += async (_, _) => await TeleportToSelectedBotAsync();
        menu.Items.Add(teleportItem);
        menu.Items.Add(new ToolStripSeparator());
        var deleteItem = new ToolStripMenuItem("Delete character forever…");
        deleteItem.Click += async (_, _) => await DeleteSelectedBotAsync();
        menu.Items.Add(deleteItem);
        menu.Opening += (_, args) =>
        {
            BotRow? bot = SelectedBot();
            bool canTeleport = bot is { BotId: not null, IsOnline: true, DeletionQueued: false } && IsServerRunning();
            bool canDelete = bot?.CanDelete == true;
            teleportItem.Enabled = canTeleport;
            teleportItem.ToolTipText = canTeleport ? $"Teleport your logged-in character to {bot!.Name}." : "The server and selected playerbot must both be online.";
            deleteItem.Enabled = canDelete;
            args.Cancel = bot == null || !canTeleport && !canDelete;
        };
        _grid.ContextMenuStrip = menu;
        return _grid;
    }

    private async Task RefreshDashboardAsync()
    {
        if (_refreshing) return;
        _refreshing = true;
        Dictionary<Button, bool> buttonStates = CaptureAndDisableButtonsForSnapshot();
        bool buttonsRestored = false;
        if (_refreshButton != null)
        {
            _refreshButton.Text = "READING…";
        }
        _footer.Text = "Reading current playerbot status…";
        try
        {
            var snapshot = await Task.Run(ReadSnapshot);
            // Restore the pre-refresh baseline while still on the UI thread,
            // then apply the authoritative states from the completed snapshot.
            // No click can be processed between these operations.
            RestoreButtonsAfterSnapshot(buttonStates);
            buttonsRestored = true;
            _bots.Clear();
            _bots.AddRange(snapshot.Bots);
            _groups.Clear();
            _groups.AddRange(snapshot.Groups);
            _rvrWorld = snapshot.RvrWorld;
            _rvrServerRunning = snapshot.ServerState.Equals("Running", StringComparison.OrdinalIgnoreCase);
            bool snapshotStarting = snapshot.ServerState.Equals("Starting", StringComparison.OrdinalIgnoreCase);
            string displayedServerState = _stoppingServer
                ? "Stopping…"
                : snapshotStarting
                    ? "Starting…"
                    : snapshot.ServerState;
            _serverState.Text = displayedServerState.ToUpperInvariant();
            _serverState.ForeColor = _stoppingServer
                ? DaocTheme.Danger
                : snapshotStarting
                    ? DaocTheme.Gold
                : snapshot.ServerState.Equals("Running", StringComparison.OrdinalIgnoreCase)
                    ? DaocTheme.Success
                    : DaocTheme.Muted;
            _onlineValue.Text = snapshot.Bots.Count(bot => bot.IsOnline).ToString("N0");
            _albionValue.Text = RealmRosterValue(snapshot.Bots, "Albion");
            _midgardValue.Text = RealmRosterValue(snapshot.Bots, "Midgard");
            _hiberniaValue.Text = RealmRosterValue(snapshot.Bots, "Hibernia");
            _performanceValue.Text = $"{snapshot.TickP95Ms:0.0} ms";
            _startButton.Enabled = !_resettingKeepsRelics && !_savingXpRates && !_stoppingServer && snapshot.ServerState == "Stopped" && File.Exists(_serverExecutable);
            _resetKeepsRelics.Enabled = !_resettingKeepsRelics && !_savingXpRates && snapshot.ServerState == "Stopped" && BotGoalsServerStopped();
            _stopButton.Enabled = !_stoppingServer && snapshot.ServerState is "Running" or "Starting";
            _playButton.Enabled = !_stoppingServer && snapshot.ServerState == "Running" && File.Exists(_clientConnector);
            UpdateXpRateControls(snapshot);
            ApplyFilter();
            UpdateDeleteButton();
            RenderActiveGroups();
            RenderActiveRvr();
            _dashboardFooterSummary = $"Realm Exchange {_auctions.Count:N0} listings • Memory {snapshot.ServerMemoryMb:0} MB • Bot status updated {DateTime.Now:T}";
            _footer.Text = _dashboardFooterSummary;
            UpdateAutoRefreshCountdown();
        }
        catch (Exception exception)
        {
            _footer.Text = $"Dashboard refresh failed: {exception.Message}";
        }
        finally
        {
            if (!buttonsRestored)
                RestoreButtonsAfterSnapshot(buttonStates);
            _refreshing = false;
            if (_refreshButton != null)
                _refreshButton.Text = "REFRESH";
        }
    }

    private async Task RefreshRealmExchangeAsync()
    {
        if (_refreshingExchange)
            return;

        _refreshingExchange = true;
        if (_refreshExchangeButton != null)
        {
            _refreshExchangeButton.Enabled = false;
            _refreshExchangeButton.Text = "READING EXCHANGE…";
        }
        _footer.Text = "Reading the Realm Exchange inventory…";
        try
        {
            List<AuctionRow> auctions = await Task.Run(ReadRealmExchangeSnapshot);
            _auctions.Clear();
            _auctions.AddRange(auctions);
            ApplyAuctionFilter();
            _dashboardFooterSummary = $"Realm Exchange {_auctions.Count:N0} listings • Exchange updated {DateTime.Now:T}";
            _footer.Text = _dashboardFooterSummary;
        }
        catch (Exception exception)
        {
            _footer.Text = $"Realm Exchange refresh failed: {exception.Message}";
        }
        finally
        {
            _refreshingExchange = false;
            if (_refreshExchangeButton != null)
            {
                _refreshExchangeButton.Text = "REFRESH REALM EXCHANGE";
                _refreshExchangeButton.Enabled = true;
            }
        }
    }

    private List<AuctionRow> ReadRealmExchangeSnapshot()
    {
        var auctions = new List<AuctionRow>();
        if (!File.Exists(_database))
            return auctions;

        using var connection = new SQLiteConnection($"Data Source={_database};Version=3;Read Only=True;Pooling=False;Default Timeout=5");
        connection.Open();
        if (!TableExists(connection, "Inventory") || !TableExists(connection, "ItemTemplate") ||
            !TableExists(connection, "ItemUnique"))
            return auctions;

        using var command = connection.CreateCommand();
        string listedUtcColumn = ColumnExists(connection, "Inventory", "RealmExchangeListedUtc")
            ? "COALESCE(i.RealmExchangeListedUtc, '')" : "''";
        command.CommandText = $"""
            SELECT COALESCE(NULLIF(u.Name, ''), NULLIF(t.Name, ''), 'Unknown item') AS ItemName,
                   COALESCE(c.Name, b.Name, 'Unknown seller') AS Seller,
                   COALESCE(u.Level, t.Level, 0) AS ItemLevel,
                   i.Count,
                   i.SellPrice,
                   CASE i.OwnerLot WHEN 65001 THEN 'Albion' WHEN 65002 THEN 'Midgard' WHEN 65003 THEN 'Hibernia' ELSE 'Unknown' END AS RealmName,
                   i.OwnerID, {listedUtcColumn} AS ListedUtc
            FROM Inventory i
            LEFT JOIN ItemTemplate t ON t.Id_nb = i.ITemplate_Id
            LEFT JOIN ItemUnique u ON u.Id_nb = i.UTemplate_Id
            LEFT JOIN DOLCharacters c ON c.DOLCharacters_ID = i.OwnerID
            LEFT JOIN offline_world_bots b ON i.OwnerID = ('offlinebot:' || b.BotId)
            WHERE i.OwnerLot IN (65001, 65002, 65003)
              AND i.SellPrice > 0
            ORDER BY RealmName, ItemLevel DESC, ItemName
            """;
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            long buyoutCopper = reader.GetInt64(4);
            string expiry = "Never (player listing)";
            if (reader.GetString(6).StartsWith("offlinebot:", StringComparison.OrdinalIgnoreCase))
            {
                expiry = "24h from first load";
                if (DateTime.TryParse(reader.GetString(7), System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.RoundtripKind, out DateTime listedUtc))
                {
                    DateTime expiresUtc = listedUtc.ToUniversalTime().AddHours(24);
                    TimeSpan remaining = expiresUtc - DateTime.UtcNow;
                    expiry = remaining <= TimeSpan.Zero ? "Expired - removing" :
                        $"{(int)remaining.TotalHours}h {remaining.Minutes:00}m • {expiresUtc.ToLocalTime():MMM d HH:mm}";
                }
            }
            auctions.Add(new AuctionRow(reader.GetString(0), reader.GetString(1), reader.GetInt32(2), reader.GetInt32(3),
                "Fixed price", FormatCopper(buyoutCopper), buyoutCopper, expiry, reader.GetString(5)));
        }
        return auctions;
    }

    private Dictionary<Button, bool> CaptureAndDisableButtonsForSnapshot()
    {
        var states = DescendantControls(this)
            .OfType<Button>()
            .ToDictionary(button => button, button => button.Enabled);
        foreach (Button button in states.Keys)
            button.Enabled = false;
        return states;
    }

    private static void RestoreButtonsAfterSnapshot(IReadOnlyDictionary<Button, bool> states)
    {
        foreach ((Button button, bool enabled) in states)
        {
            if (!button.IsDisposed)
                button.Enabled = enabled;
        }
    }

    private static IEnumerable<Control> DescendantControls(Control root)
    {
        foreach (Control child in root.Controls)
        {
            yield return child;
            foreach (Control descendant in DescendantControls(child))
                yield return descendant;
        }
    }

    private void UpdateXpRateControls(DashboardSnapshot snapshot)
    {
        _loadingXpRates = true;
        try
        {
            SelectXpRate(_playerXpRate, snapshot.PlayerXpRate);
            SelectXpRate(_botXpRate, snapshot.BotXpRate);
            bool editable = snapshot.ServerState == "Stopped" && !_savingXpRates;
            _playerXpRate.Enabled = editable;
            _botXpRate.Enabled = editable;
            _makeMeGm.Checked = snapshot.MakeMeGm;
            _makeMeGm.Enabled = editable;
            if (_savingXpRates)
            {
                _xpSettingsStatus.ForeColor = DaocTheme.Success;
                return;
            }
            _xpSettingsStatus.Text = editable
                ? "Server stopped — choose a rate to save it for the next startup."
                : "XP RATE LOCKED — stop the server before changing these settings.";
            _xpSettingsStatus.ForeColor = editable ? DaocTheme.Success : DaocTheme.Muted;
        }
        finally
        {
            _loadingXpRates = false;
        }
    }

    private static void SelectXpRate(ComboBox selector, double rate)
    {
        XpRateOption? match = selector.Items.Cast<XpRateOption>()
            .FirstOrDefault(option => Math.Abs(option.Multiplier - rate) < 0.001);
        if (match == null)
        {
            match = new XpRateOption(rate, $"{rate:0.###}×  Custom");
            selector.Items.Add(match);
        }
        selector.SelectedItem = match;
    }

    private async Task SaveXpRateAsync(string key, ComboBox selector)
    {
        if (_loadingXpRates || _savingXpRates || selector.SelectedItem is not XpRateOption option)
            return;
        if (IsServerRunning() || FindExactServerProcess() is not null)
        {
            MessageBox.Show(this, "Experience rates can only be changed while the server is fully stopped.",
                "Server is running", MessageBoxButtons.OK, MessageBoxIcon.Information);
            await RefreshDashboardAsync();
            return;
        }

        _savingXpRates = true;
        ShowXpRateApplying(option.Label, key == "xp_rate");
        try
        {
            await Task.Run(() => PersistXpRate(key, option.Multiplier));
            _xpSettingsStatus.Text = $"APPLIED {option.Label}. It will be used on the next server startup.";
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "Unable to save XP rate", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _savingXpRates = false;
            await RefreshDashboardAsync();
        }
    }

    private async Task SaveGmSettingAsync()
    {
        if (_loadingXpRates || _savingXpRates) return;
        bool enabled = _makeMeGm.Checked;
        if (IsServerRunning() || FindExactServerProcess() is not null)
        {
            MessageBox.Show(this, "Stop the server before changing GM access.", "Server is running");
            await RefreshDashboardAsync();
            return;
        }
        _savingXpRates = true;
        _makeMeGm.Enabled = _playerXpRate.Enabled = _botXpRate.Enabled = _startButton.Enabled = false;
        _xpSettingsStatus.Text = "APPLYING GM SETTING…";
        _xpSettingsStatus.ForeColor = DaocTheme.Success;
        try { await Task.Run(() => PersistGmSetting(enabled)); }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Unable to save GM setting"); }
        finally { _savingXpRates = false; await RefreshDashboardAsync(); }
    }

    private void PersistGmSetting(bool enabled)
    {
        if (IsServerRunning() || FindExactServerProcess() is not null)
            throw new InvalidOperationException("The server must be fully stopped.");
        string account = ReadCredentials().Account;
        using var connection = new SQLiteConnection($"Data Source={_database};Version=3;Pooling=False;Default Timeout=10");
        connection.Open();
        PersistGmSetting(connection, account, enabled);
    }

    private static void PersistGmSetting(SQLiteConnection connection, string account, bool enabled)
    {
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "CREATE TABLE IF NOT EXISTS offline_local_options (Key TEXT PRIMARY KEY,Value TEXT NOT NULL)";
        command.ExecuteNonQuery();
        command.CommandText = "INSERT INTO offline_local_options VALUES ('MakeMeGM',@value) ON CONFLICT(Key) DO UPDATE SET Value=excluded.Value";
        command.Parameters.AddWithValue("@value", enabled ? "true" : "false");
        command.ExecuteNonQuery();
        command.CommandText = "UPDATE Account SET PrivLevel=@privilege WHERE Name=@account";
        command.Parameters.AddWithValue("@privilege", enabled ? 2 : 1);
        command.Parameters.AddWithValue("@account", account);
        if (command.ExecuteNonQuery() != 1)
            throw new InvalidOperationException("Your local account must exist before changing GM access. Enter the game once, then stop the server and retry.");
        transaction.Commit();
    }

    private void ShowXpRateApplying(string label, bool playerRate)
    {
        _playerXpRate.Enabled = false;
        _botXpRate.Enabled = false;
        _makeMeGm.Enabled = false;
        _startButton.Enabled = false;
        _xpSettingsStatus.ForeColor = DaocTheme.Success;
        _xpSettingsStatus.Text = $"APPLYING {label} TO {(playerRate ? "YOUR PLAYER XP" : "AUTONOMOUS BOT XP")}…";
    }

    private void PersistXpRate(string key, double multiplier)
    {
        if (key is not ("xp_rate" or "bot_xp_rate") || multiplier is not (1 or 2 or 3 or 5 or 10))
            throw new InvalidOperationException("Unsupported experience-rate selection.");
        if (IsServerRunning() || FindExactServerProcess() is not null)
            throw new InvalidOperationException("The server started before the rate could be saved. Stop it and try again.");
        if (!File.Exists(_database))
            throw new InvalidOperationException("The prepared world database is missing.");

        string stored = multiplier.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
        string description = key == "xp_rate"
            ? "Real player character experience rate selected in the Offline DAoC launcher."
            : "Autonomous player-bot experience rate selected in the Offline DAoC launcher.";
        using var connection = new SQLiteConnection($"Data Source={_database};Version=3;Pooling=False;Default Timeout=10");
        connection.Open();
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO ServerProperty (Category, `Key`, Description, DefaultValue, Value, LastTimeRowUpdated, ServerProperty_ID)
            VALUES ('rates', @key, @description, '1', @value, @updated, '')
            ON CONFLICT(`Key`) DO UPDATE SET
                Value=excluded.Value,
                LastTimeRowUpdated=excluded.LastTimeRowUpdated
            """;
        command.Parameters.AddWithValue("@key", key);
        command.Parameters.AddWithValue("@description", description);
        command.Parameters.AddWithValue("@value", stored);
        command.Parameters.AddWithValue("@updated", DateTime.UtcNow.ToString("O"));
        command.ExecuteNonQuery();
        transaction.Commit();
    }

    private LiveBotSnapshot? RequestLiveBotSnapshot()
    {
        string requestId = Guid.NewGuid().ToString("N");
        string requestPath = Path.Combine(_serverDirectory, "bot-world.request");
        string temporaryPath = requestPath + $".{Environment.ProcessId}.tmp";
        try
        {
            File.WriteAllText(temporaryPath, requestId);
            File.Move(temporaryPath, requestPath, true);
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
        finally
        {
            try
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
            catch (IOException)
            {
                // One fixed PID-scoped temporary name is overwritten on the
                // next refresh, so an interrupted cleanup cannot accumulate.
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        DateTime deadline = DateTime.UtcNow.AddMilliseconds(LiveBotSnapshotWaitMilliseconds);
        do
        {
            LiveBotSnapshot? snapshot = TryReadLiveBotSnapshot();
            if (snapshot?.RequestId == requestId && IsLiveBotSnapshotFresh(snapshot, DateTime.UtcNow))
                return snapshot;
            Thread.Sleep(50);
        }
        while (DateTime.UtcNow < deadline);
        return null;
    }

    private LiveBotSnapshot? TryReadLiveBotSnapshot()
    {
        string path = Path.Combine(_serverDirectory, "bot-world.json");
        if (!File.Exists(path))
            return null;
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete);
            return JsonSerializer.Deserialize<LiveBotSnapshot>(stream,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (IOException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool IsLiveBotSnapshotFresh(LiveBotSnapshot? snapshot, DateTime nowUtc)
    {
        if (snapshot?.Running != true || snapshot.Bots == null)
            return false;
        TimeSpan age = nowUtc - snapshot.UpdatedUtc.ToUniversalTime();
        return age >= TimeSpan.FromSeconds(-5) &&
               age <= TimeSpan.FromMilliseconds(LiveBotSnapshotMaxAgeMilliseconds);
    }

    private DashboardSnapshot ReadSnapshot()
    {
        if (!File.Exists(_database)) return new DashboardSnapshot("Not configured", [], [], 0, 0, 0, 1, 1);
        var bots = new List<BotRow>();
        var running = IsServerRunning();
        using var serverProcess = FindExactServerProcess();
        var serverProcessPresent = serverProcess is not null;
        LiveBotSnapshot? liveSnapshot = running ? RequestLiveBotSnapshot() : null;
        bool liveSnapshotIsAuthoritative = IsLiveBotSnapshotFresh(liveSnapshot, DateTime.UtcNow);
        Dictionary<long, LiveBotStatus> liveBots = liveSnapshotIsAuthoritative
            ? liveSnapshot!.Bots.ToDictionary(bot => bot.BotId)
            : [];
        var active = 0;
        var memoryMb = serverProcessPresent ? serverProcess!.WorkingSet64 / 1024d / 1024d : 0d;
        var tickP95Ms = 0d;

        using var connection = new SQLiteConnection($"Data Source={_database};Version=3;Read Only=True;Pooling=False;Default Timeout=5");
        connection.Open();
        double playerXpRate = ReadServerRate(connection, "xp_rate", 1);
        double botXpRate = ReadServerRate(connection, "bot_xp_rate", 1);
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT TickP95Ms FROM offline_runtime_status WHERE Id=1";
            using var reader = command.ExecuteReader();
            if (reader.Read())
                tickP95Ms = reader.GetDouble(0);
        }

        using (var command = connection.CreateCommand())
        {
            bool hasObjectiveColumns = ColumnExists(connection, "offline_world_bots", "ObjectiveKind") &&
                                       ColumnExists(connection, "offline_world_bots", "ObjectiveAssignmentId") &&
                                       ColumnExists(connection, "offline_world_bots", "ObjectiveAssignedUtc") &&
                                       ColumnExists(connection, "offline_world_bots", "ObjectivePhase");
            string objectiveColumns = hasObjectiveColumns
                ? "ObjectiveKind, ObjectiveAssignmentId, ObjectiveAssignedUtc, ObjectivePhase"
                : "'' AS ObjectiveKind, '' AS ObjectiveAssignmentId, '' AS ObjectiveAssignedUtc, '' AS ObjectivePhase";
            objectiveColumns += ColumnExists(connection, "offline_world_bots", "ObjectiveExpiresUtc")
                ? ", COALESCE(ObjectiveExpiresUtc, '')" : ", '' AS ObjectiveExpiresUtc";
            command.CommandText = $"SELECT BotId, Name, Realm, RaceName, Gender, ClassName, Level, COALESCE(ZoneName, '—'), Activity, CurrentGoal, TargetName, TravelDestination, ObjectiveProgress, IsOnline, IsRetired, COALESCE(ItineraryJson, ''), {objectiveColumns} FROM offline_world_bots ORDER BY Realm, Level DESC, Name";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                long botId = reader.GetInt64(0);
                bool hasLiveStatus = liveBots.TryGetValue(botId, out LiveBotStatus? live);
                bool isOnline = running && (liveSnapshotIsAuthoritative ? hasLiveStatus : reader.GetBoolean(13));
                bool deletionQueued = reader.GetBoolean(14);
                int level = hasLiveStatus ? live!.Level : reader.GetInt32(6);
                string zoneName = hasLiveStatus ? live!.ZoneName : reader.GetString(7);
                string rawActivity = hasLiveStatus ? live!.Activity : reader.GetString(8);
                string currentGoal = hasLiveStatus ? live!.CurrentGoal : reader.GetString(9);
                string targetName = hasLiveStatus ? live!.TargetName : reader.GetString(10);
                string destination = hasLiveStatus ? live!.TravelDestination : reader.GetString(11);
                string progress = hasLiveStatus ? live!.ObjectiveProgress : reader.GetString(12);
                string itinerary = hasLiveStatus ? live!.ItineraryJson : reader.GetString(15);
                string objectiveKind = hasLiveStatus ? live!.ObjectiveKind : reader.GetString(16);
                string assignmentId = hasLiveStatus ? live!.ObjectiveAssignmentId : reader.GetString(17);
                string assignedUtc = hasLiveStatus ? live!.ObjectiveAssignedUtc : reader.GetString(18);
                string objectivePhase = hasLiveStatus ? live!.ObjectivePhase : reader.GetString(19);
                string objectiveExpiresUtc = hasLiveStatus ? live!.ObjectiveExpiresUtc : reader.GetString(20);
                string activity = deletionQueued
                    ? running || serverProcessPresent
                        ? "Deletion requested — waiting for safe server removal"
                        : "Deletion pending — Delete is available while the server is stopped"
                    : FormatBotActivity(rawActivity, currentGoal, targetName, destination, progress, isOnline);
                GroupMetadata? group = isOnline ? ParseGroupMetadata(itinerary) : null;
                bots.Add(new BotRow(botId, reader.GetString(1), RealmName(reader.GetInt32(2)), reader.GetString(3), GenderName(reader.GetInt32(4)), reader.GetString(5), level, zoneName, activity, isOnline, true, deletionQueued,
                    group?.GroupId ?? string.Empty, group?.Phase ?? string.Empty, group?.SharedGoal ?? string.Empty, group?.Status ?? string.Empty,
                    objectiveKind, assignmentId, assignedUtc, objectivePhase)
                {
                    ObjectiveExpiresUtc = objectiveExpiresUtc,
                    HasGroupTaskClock = group?.HasTaskClock == true,
                    TaskTimerPaused = group?.TaskTimerPaused == true,
                    TaskRemainingMilliseconds = group?.TaskRemainingMilliseconds ?? 0,
                    GroupTaskExpiresUtc = group?.TaskExpiresUtc ?? string.Empty,
                    MeetUpDeadlineUtc = group?.MeetUpDeadlineUtc ?? string.Empty,
                    GroupLeaderName = group?.LeaderName ?? string.Empty,
                    GroupRendezvousName = group?.RendezvousName ?? string.Empty,
                    GroupPullerName = group?.PullerName ?? string.Empty
                    ,GroupRole = group?.MemberRole ?? string.Empty
                });
            }
        }
        if (TableExists(connection, "bot_profiles"))
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT Name, ClassId, Level, IsActive FROM bot_profiles ORDER BY Name";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var classId = reader.GetInt32(1);
                var classInfo = ClassInfo(classId);
                bots.Add(new BotRow(null, reader.GetString(0), classInfo.Realm, "—", "—", classInfo.Name, reader.GetInt32(2), "With owner", "Companion", running && reader.GetBoolean(3), false, false, string.Empty, string.Empty, string.Empty, string.Empty,
                    string.Empty, string.Empty, string.Empty, string.Empty));
            }
        }

        active = bots.Count(bot => bot.IsOnline);
        List<GroupRow> groups = bots
            .Where(bot => bot.IsOnline && !bot.DeletionQueued && bot.GroupId.Length > 0)
            .GroupBy(bot => bot.GroupId, StringComparer.Ordinal)
            .Select(group =>
            {
                List<BotRow> members = group.ToList();
                // Status writes are deliberately asynchronous. During the tiny
                // boundary after a leader leaves, prefer a row whose published
                // leader is actually present. Never advertise an absent bot as
                // the active leader while the server elects and republishes.
                BotRow first = members.First();
                BotRow? coherent = members.FirstOrDefault(member => members.Any(candidate =>
                    candidate.Name.Equals(member.GroupLeaderName, StringComparison.OrdinalIgnoreCase)));
                BotRow source = coherent ?? first;
                string leader = coherent != null ? source.GroupLeaderName : "Re-electing…";
                return new GroupRow(group.Key, source.Realm, source.GroupPhase, source.GroupGoal, source.GroupStatus,
                    leader, source.GroupRendezvousName, source.GroupPullerName, members);
            })
            // The server dissolves these immediately; avoid flashing stale
            // one-member metadata during the asynchronous snapshot boundary.
            .Where(group => group.Members.Count >= 2)
            .ToList();
        var serverState = running ? "Running" : serverProcessPresent ? "Starting" : "Stopped";
        bool makeMeGm = false;
        if (TableExists(connection, "offline_local_options"))
        {
            using var gm = connection.CreateCommand();
            gm.CommandText = "SELECT Value FROM offline_local_options WHERE Key='MakeMeGM'";
            makeMeGm = string.Equals(gm.ExecuteScalar()?.ToString(), "true", StringComparison.OrdinalIgnoreCase);
        }
        return new DashboardSnapshot(serverState, bots, groups, active, memoryMb, tickP95Ms, playerXpRate, botXpRate, makeMeGm, ReadRvrWorld());
    }

    private static double ReadServerRate(SQLiteConnection connection, string key, double fallback)
    {
        if (!TableExists(connection, "ServerProperty"))
            return fallback;
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Value FROM ServerProperty WHERE `Key`=@key LIMIT 1";
        command.Parameters.AddWithValue("@key", key);
        object? value = command.ExecuteScalar();
        return value != null && double.TryParse(Convert.ToString(value), System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out double parsed) && parsed > 0
            ? parsed
            : fallback;
    }

    private static GroupMetadata? ParseGroupMetadata(string raw)
    {
        const string prefix = "offline-group-v1:";
        if (string.IsNullOrWhiteSpace(raw) || !raw.StartsWith(prefix, StringComparison.Ordinal))
            return null;
        try
        {
            GroupMetadata? metadata = JsonSerializer.Deserialize<GroupMetadata>(raw[prefix.Length..]);
            return string.IsNullOrWhiteSpace(metadata?.GroupId) ? null : metadata;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static void ApplyRvrStatusStyle(DataGridViewCellStyle style, string? status)
    {
        string state = status?.Trim() ?? string.Empty;
        Color color = state.Contains("rally", StringComparison.OrdinalIgnoreCase) ? DaocTheme.GoldLight
            : state.StartsWith("SIEGE", StringComparison.OrdinalIgnoreCase) || state.StartsWith("DROPPED", StringComparison.OrdinalIgnoreCase) || state.StartsWith("RAID", StringComparison.OrdinalIgnoreCase)
                ? DaocTheme.Danger
            : state.StartsWith("ESCORT", StringComparison.OrdinalIgnoreCase) || state.StartsWith("INTERCEPT", StringComparison.OrdinalIgnoreCase)
                ? Color.FromArgb(196, 160, 240)
            : DaocTheme.Text;
        style.ForeColor = color;
        // Selection must not replace the status color with the generic gold.
        style.SelectionForeColor = color;
        style.SelectionBackColor = Color.FromArgb(55, 49, 42);
    }

    private void RenderActiveRvr()
    {
        string realm = _rvrRealm.SelectedItem?.ToString() ?? "All realms";
        string search = _rvrSearch.Text.Trim();
        List<BotRow> activeRvr = _bots
            .Where(bot => bot.IsOnline && !bot.DeletionQueued && bot.ObjectiveKind.Equals("RvR", StringComparison.OrdinalIgnoreCase))
            .Where(bot => (realm == "All realms" || bot.Realm == realm) &&
                (search.Length == 0 || $"{bot.Name} {bot.ClassName} {bot.ZoneName} {bot.Activity}".Contains(search, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(bot => bot.Realm, StringComparer.OrdinalIgnoreCase)
            .ThenBy(bot => bot.GroupId.Length == 0 ? "~" : bot.GroupId, StringComparer.OrdinalIgnoreCase)
            .ThenByDescending(bot => bot.Level)
            .ThenBy(bot => bot.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        object Key(BotRow bot) => _rvrSortProperty switch
        {
            "Level" => bot.Level,
            "TaskRemaining" => bot.RemainingMilliseconds ?? long.MaxValue,
            _ => typeof(BotRow).GetProperty(_rvrSortProperty)?.GetValue(bot)?.ToString() ?? bot.Name
        };
        activeRvr = (_rvrSortAscending ? activeRvr.OrderBy(Key) : activeRvr.OrderByDescending(Key)).ThenBy(bot => bot.Name).ToList();
        _rvrSource.DataSource = activeRvr;
        _rvrSource.ResetBindings(false);
        foreach (DataGridViewColumn column in _rvrGrid.Columns)
            column.HeaderCell.SortGlyphDirection = column.DataPropertyName == _rvrSortProperty
                ? _rvrSortAscending ? SortOrder.Ascending : SortOrder.Descending : SortOrder.None;
        RenderRealmEvents();
        _rvrUpdated.Text = _rvrWorld == null ? "REALM EVENTS — awaiting the server's first snapshot"
            : $"REALM EVENTS  ·  {(_rvrServerRunning ? DateTime.UtcNow - _rvrWorld.UpdatedUtc > TimeSpan.FromMinutes(6) ? "Old snapshot" : "Snapshot" : "Server stopped — last known state")} {_rvrWorld.UpdatedUtc.ToLocalTime():g}  ·  Red: battle/drop  ·  Purple: relic carrier  ·  Gold: rally";
    }

    private RvrWorldSnapshot? ReadRvrWorld()
    {
        try
        {
            string path = Path.Combine(_serverDirectory, "rvr-world.json");
            if (!File.Exists(path) || new FileInfo(path).Length > 4_000_000) return null;
            var snapshot = System.Text.Json.JsonSerializer.Deserialize<RvrWorldSnapshot>(File.ReadAllText(path));
            _keepRelicResetUtc ??= KeepRelicReset.ReadResetUtc(_database);
            return snapshot?.UpdatedUtc.ToUniversalTime() < _keepRelicResetUtc.Value ? null : snapshot;
        }
        catch (SQLiteException) { return null; }
        catch (IOException) { return null; }
        catch (System.Text.Json.JsonException) { return null; }
    }

    private void ApplyFilter()
    {
        var realm = _realmFilter.SelectedItem?.ToString() ?? "All realms";
        var search = _search.Text.Trim();
        IEnumerable<BotRow> filtered = _bots.Where(bot =>
            (!_onlineOnly.Checked || bot.IsOnline) &&
            (realm == "All realms" || bot.Realm == realm) &&
            (search.Length == 0 || $"{bot.Name} {bot.Realm} {bot.RaceName} {bot.Gender} {bot.ClassName} {bot.ZoneName} {bot.Activity}".Contains(search, StringComparison.OrdinalIgnoreCase)));
        _botSource.DataSource = SortBots(filtered).ToList();
        _botSource.ResetBindings(false);
        UpdateBotSortGlyph();
    }

    private void SortBotsByColumn(int columnIndex)
    {
        if (columnIndex < 0 || columnIndex >= _grid.Columns.Count)
            return;

        string property = _grid.Columns[columnIndex].DataPropertyName;
        if (string.IsNullOrWhiteSpace(property))
            return;

        if (_botSortProperty.Equals(property, StringComparison.Ordinal))
            _botSortAscending = !_botSortAscending;
        else
        {
            _botSortProperty = property;
            _botSortAscending = true;
        }

        ApplyFilter();
    }

    private IEnumerable<BotRow> SortBots(IEnumerable<BotRow> bots)
    {
        static IOrderedEnumerable<BotRow> TextAscending(IEnumerable<BotRow> rows, Func<BotRow, string> selector) =>
            rows.OrderBy(selector, StringComparer.OrdinalIgnoreCase).ThenBy(bot => bot.Name, StringComparer.OrdinalIgnoreCase);
        static IOrderedEnumerable<BotRow> TextDescending(IEnumerable<BotRow> rows, Func<BotRow, string> selector) =>
            rows.OrderByDescending(selector, StringComparer.OrdinalIgnoreCase).ThenBy(bot => bot.Name, StringComparer.OrdinalIgnoreCase);

        return (_botSortProperty, _botSortAscending) switch
        {
            ("TaskRemaining", true) => bots.OrderBy(bot => bot.RemainingMilliseconds ?? long.MaxValue).ThenBy(bot => bot.Name, StringComparer.OrdinalIgnoreCase),
            ("TaskRemaining", false) => bots.OrderByDescending(bot => bot.RemainingMilliseconds ?? -1).ThenBy(bot => bot.Name, StringComparer.OrdinalIgnoreCase),
            ("Level", true) => bots.OrderBy(bot => bot.Level).ThenBy(bot => bot.Name, StringComparer.OrdinalIgnoreCase),
            ("Level", false) => bots.OrderByDescending(bot => bot.Level).ThenBy(bot => bot.Name, StringComparer.OrdinalIgnoreCase),
            ("BotId", true) => bots.OrderBy(bot => bot.BotId).ThenBy(bot => bot.Name, StringComparer.OrdinalIgnoreCase),
            ("BotId", false) => bots.OrderByDescending(bot => bot.BotId).ThenBy(bot => bot.Name, StringComparer.OrdinalIgnoreCase),
            ("State", true) => TextAscending(bots, bot => bot.State),
            ("State", false) => TextDescending(bots, bot => bot.State),
            ("Realm", true) => TextAscending(bots, bot => bot.Realm),
            ("Realm", false) => TextDescending(bots, bot => bot.Realm),
            ("RaceName", true) => TextAscending(bots, bot => bot.RaceName),
            ("RaceName", false) => TextDescending(bots, bot => bot.RaceName),
            ("Gender", true) => TextAscending(bots, bot => bot.Gender),
            ("Gender", false) => TextDescending(bots, bot => bot.Gender),
            ("ClassName", true) => TextAscending(bots, bot => bot.ClassName),
            ("ClassName", false) => TextDescending(bots, bot => bot.ClassName),
            ("ZoneName", true) => TextAscending(bots, bot => bot.ZoneName),
            ("ZoneName", false) => TextDescending(bots, bot => bot.ZoneName),
            ("Activity", true) => TextAscending(bots, bot => bot.Activity),
            ("Activity", false) => TextDescending(bots, bot => bot.Activity),
            (_, true) => TextAscending(bots, bot => bot.Name),
            _ => TextDescending(bots, bot => bot.Name),
        };
    }

    private void UpdateBotSortGlyph()
    {
        foreach (DataGridViewColumn column in _grid.Columns)
        {
            column.HeaderCell.SortGlyphDirection = column.DataPropertyName.Equals(_botSortProperty, StringComparison.Ordinal)
                ? _botSortAscending ? SortOrder.Ascending : SortOrder.Descending
                : SortOrder.None;
        }
    }

    private void ApplyAuctionFilter()
    {
        var search = _auctionSearch.Text.Trim();
        string realm = _auctionRealmTabs.SelectedTab?.Text ?? "Albion";
        IEnumerable<AuctionRow> filtered = _auctions.Where(row => row.State.Equals(realm, StringComparison.OrdinalIgnoreCase) &&
            (search.Length == 0 || $"{row.ItemName} {row.Seller} {row.State}".Contains(search, StringComparison.OrdinalIgnoreCase)));
        _auctionSource.DataSource = SortAuctions(filtered).ToList();
        _auctionSource.ResetBindings(false);
        UpdateAuctionSortGlyph();
    }

    private void SortAuctionsByColumn(int columnIndex)
    {
        if (columnIndex < 0 || columnIndex >= _auctionGrid.Columns.Count)
            return;

        string property = _auctionGrid.Columns[columnIndex].DataPropertyName;
        if (string.IsNullOrWhiteSpace(property))
            return;

        if (_auctionSortProperty.Equals(property, StringComparison.Ordinal))
            _auctionSortAscending = !_auctionSortAscending;
        else
        {
            _auctionSortProperty = property;
            _auctionSortAscending = true;
        }

        ApplyAuctionFilter();
    }

    private IEnumerable<AuctionRow> SortAuctions(IEnumerable<AuctionRow> rows)
    {
        static IOrderedEnumerable<AuctionRow> TextAscending(IEnumerable<AuctionRow> source, Func<AuctionRow, string> selector) =>
            source.OrderBy(selector, StringComparer.OrdinalIgnoreCase)
                .ThenBy(row => row.ItemName, StringComparer.OrdinalIgnoreCase);
        static IOrderedEnumerable<AuctionRow> TextDescending(IEnumerable<AuctionRow> source, Func<AuctionRow, string> selector) =>
            source.OrderByDescending(selector, StringComparer.OrdinalIgnoreCase)
                .ThenBy(row => row.ItemName, StringComparer.OrdinalIgnoreCase);

        return (_auctionSortProperty, _auctionSortAscending) switch
        {
            ("ItemLevel", true) => rows.OrderBy(row => row.ItemLevel).ThenBy(row => row.ItemName, StringComparer.OrdinalIgnoreCase),
            ("ItemLevel", false) => rows.OrderByDescending(row => row.ItemLevel).ThenBy(row => row.ItemName, StringComparer.OrdinalIgnoreCase),
            ("Quantity", true) => rows.OrderBy(row => row.Quantity).ThenBy(row => row.ItemName, StringComparer.OrdinalIgnoreCase),
            ("Quantity", false) => rows.OrderByDescending(row => row.Quantity).ThenBy(row => row.ItemName, StringComparer.OrdinalIgnoreCase),
            ("Buyout", true) => rows.OrderBy(row => row.BuyoutCopper).ThenBy(row => row.ItemName, StringComparer.OrdinalIgnoreCase),
            ("Buyout", false) => rows.OrderByDescending(row => row.BuyoutCopper).ThenBy(row => row.ItemName, StringComparer.OrdinalIgnoreCase),
            ("Seller", true) => TextAscending(rows, row => row.Seller),
            ("Seller", false) => TextDescending(rows, row => row.Seller),
            ("CurrentBid", true) => TextAscending(rows, row => row.CurrentBid),
            ("CurrentBid", false) => TextDescending(rows, row => row.CurrentBid),
            ("Expires", true) => TextAscending(rows, row => row.Expires),
            ("Expires", false) => TextDescending(rows, row => row.Expires),
            ("State", true) => TextAscending(rows, row => row.State),
            ("State", false) => TextDescending(rows, row => row.State),
            (_, true) => TextAscending(rows, row => row.ItemName),
            _ => TextDescending(rows, row => row.ItemName),
        };
    }

    private void UpdateAuctionSortGlyph()
    {
        foreach (DataGridViewColumn column in _auctionGrid.Columns)
        {
            column.HeaderCell.SortGlyphDirection = column.DataPropertyName.Equals(_auctionSortProperty, StringComparison.Ordinal)
                ? _auctionSortAscending ? SortOrder.Ascending : SortOrder.Descending
                : SortOrder.None;
        }
    }

    private IReadOnlyList<BotCharacterGenerator.Identity> GenerateBotCharacters(int realm, int count, int level = 1)
    {
        if (!File.Exists(_database))
            throw new InvalidOperationException("The prepared world database is missing.");
        if (count is not (1 or 10 or 100))
            throw new ArgumentOutOfRangeException(nameof(count));
        if (level is not (1 or 50)) throw new ArgumentOutOfRangeException(nameof(level));

        using var connection = new SQLiteConnection($"Data Source={_database};Version=3;Pooling=False;Default Timeout=10");
        connection.Open();
        using (var busy = connection.CreateCommand())
        {
            busy.CommandText = "PRAGMA busy_timeout=10000; PRAGMA foreign_keys=OFF;";
            busy.ExecuteNonQuery();
        }

        using var transaction = connection.BeginTransaction();
        var reserved = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using (var names = connection.CreateCommand())
        {
            names.Transaction = transaction;
            names.CommandText = "SELECT Name FROM offline_world_bots UNION SELECT Name FROM DOLCharacters";
            using var reader = names.ExecuteReader();
            while (reader.Read()) reserved.Add(reader.GetString(0));
        }

        var identities = new List<BotCharacterGenerator.Identity>(count);
        for (int index = 0; index < count; index++)
        {
            BotCharacterGenerator.Identity identity = BotCharacterGenerator.Generate(realm, reserved);
            BotStartingLocation start = level == 50
                ? CapitalBotStartingLocation(identity.Realm)
                : ChooseBotStartingLocation(connection, transaction, identity.Realm, identity.RaceId, identity.ClassId);
            identities.Add(identity);
            string now = DateTime.UtcNow.ToString("O");
            using var insert = connection.CreateCommand();
            insert.Transaction = transaction;
            insert.CommandText = """
                INSERT INTO offline_world_bots
                    (Name, Realm, ClassId, ClassName, RaceId, RaceName, Gender, Level, Experience, RealmPoints,
                     ZoneName, Activity, IsOnline, IsAlive, LastUpdateUtc, MoneyCopper, InventoryRevision,
                     ZoneId, X, Y, Z, RegionId, Health, Mana, Endurance, BindRegionId, BindX, BindY, BindZ,
                     CurrentGoal, ObjectiveProgress, LastMeaningfulProgressUtc, IsRetired)
                VALUES
                    (@name, @realm, @classId, @className, @raceId, @raceName, @gender, @level, @xp, 0,
                     @zoneName, 'Queued at randomized starting location', 0, 1, @now, @money, 0,
                     @zoneId, @x, @y, @z, @region, 1, 0, 0, @region, @x, @y, @z,
                     'Awaiting staggered login queue', @placement, @now, 0)
                """;
            insert.Parameters.AddWithValue("@name", identity.Name);
            insert.Parameters.AddWithValue("@level", level);
            insert.Parameters.AddWithValue("@xp", level == 50 ? 169999999950L : 0L);
            insert.Parameters.AddWithValue("@money", level == 50 ? 100000000L : 0L);
            insert.Parameters.AddWithValue("@realm", identity.Realm);
            insert.Parameters.AddWithValue("@classId", identity.ClassId);
            insert.Parameters.AddWithValue("@className", identity.ClassName);
            insert.Parameters.AddWithValue("@raceId", identity.RaceId);
            insert.Parameters.AddWithValue("@raceName", identity.RaceName);
            insert.Parameters.AddWithValue("@gender", identity.Gender);
            insert.Parameters.AddWithValue("@now", now);
            insert.Parameters.AddWithValue("@region", start.RegionId);
            insert.Parameters.AddWithValue("@x", start.X);
            insert.Parameters.AddWithValue("@y", start.Y);
            insert.Parameters.AddWithValue("@z", start.Z);
            insert.Parameters.AddWithValue("@zoneId", start.ZoneId);
            insert.Parameters.AddWithValue("@zoneName", level == 50 ? start.ZoneName : start.RegionId is 51 or 151 or 181
                ? "Shrouded Isles starting area"
                : "Classic starting area");
            insert.Parameters.AddWithValue("@placement",
                $"Launcher assigned a realm/race/class-valid random start in region {start.RegionId} before server startup");
            insert.ExecuteNonQuery();
            if (level == 50)
            {
                long botId = connection.LastInsertRowId;
                using var gear = connection.CreateCommand();
                gear.Transaction = transaction;
                gear.CommandText = """
                    INSERT INTO Inventory (Inventory_ID,OwnerID,ITemplate_Id,SlotPosition,Count,Condition,Durability,LastTimeRowUpdated)
                    SELECT lower(hex(randomblob(16))),@owner,l.TemplateId,l.SlotPosition,1,t.MaxCondition,t.MaxDurability,@now
                    FROM offline_level50_loadouts l JOIN ItemTemplate t ON t.Id_nb=l.TemplateId WHERE l.ClassId=@class
                    """;
                gear.Parameters.AddWithValue("@owner", $"offlinebot:{botId}");
                gear.Parameters.AddWithValue("@now", now);
                gear.Parameters.AddWithValue("@class", identity.ClassId);
                if (gear.ExecuteNonQuery() < 15) throw new InvalidOperationException("The level-50 class template is missing or incomplete. No bots were added.");
            }
        }

        UpdatePopulationTarget(connection, transaction);
        transaction.Commit();
        return identities;
    }

    private sealed record BotStartingLocation(int RegionId, int X, int Y, int Z, int ZoneId = 0, string ZoneName = "");

    // Match the server's established capital recovery anchors. Its normal
    // login validation grounds these positions on the current navmesh.
    private static BotStartingLocation CapitalBotStartingLocation(int realm) => realm switch
    {
        1 => new(10, 35990, 30298, 8000, 26, "City of Camelot"),
        2 => new(101, 32020, 28294, 8819, 120, "Jordheim"),
        3 => new(201, 33197, 31200, 8000, 209, "Tir na Nog"),
        _ => throw new ArgumentOutOfRangeException(nameof(realm)),
    };

    private static BotStartingLocation ChooseBotStartingLocation(
        SQLiteConnection connection,
        SQLiteTransaction transaction,
        int realm,
        int raceId,
        int classId)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT Region, XPos, YPos, ZPos
            FROM StartupLocation
            WHERE MinVersion <= 168
              AND (RealmID=0 OR RealmID=@realm)
              AND (RaceID=0 OR RaceID=@race)
              AND (ClassID=0 OR ClassID=@class)
              AND (XPos<>0 OR YPos<>0 OR ZPos<>0)
              AND ((@realm=1 AND Region IN (1,51))
                OR (@realm=2 AND Region IN (100,151))
                OR (@realm=3 AND Region IN (200,181)))
            GROUP BY Region, XPos, YPos, ZPos
            """;
        command.Parameters.AddWithValue("@realm", realm);
        command.Parameters.AddWithValue("@race", raceId);
        command.Parameters.AddWithValue("@class", classId);

        var starts = new List<BotStartingLocation>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            starts.Add(new BotStartingLocation(
                reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2), reader.GetInt32(3)));
        }

        if (starts.Count == 0)
        {
            throw new InvalidOperationException(
                $"No authoritative Classic/Shrouded Isles starting location exists for realm {realm}, race {raceId}, class {classId}. No capital fallback was used.");
        }

        return starts[Random.Shared.Next(starts.Count)];
    }

    private async Task DeleteSelectedBotAsync()
    {
        BotRow? bot = SelectedBot();
        bool serverRunning = IsServerRunning() || FindExactServerProcess() is not null;
        if (bot?.CanDelete != true || bot.BotId is null || bot.DeletionQueued && serverRunning)
            return;

        string warning = $"Permanently delete {bot.Name}, the level {bot.Level} {bot.RaceName} {bot.ClassName}?\n\n" +
                         "This removes the character, equipment, backpack, bank contents, coins, and Realm Exchange listings. This cannot be undone.";
        if (MessageBox.Show(this, warning, "Delete bot character forever", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
            return;

        try
        {
            bool queued = QueueOrDeleteBot(bot.BotId.Value);
            await RefreshDashboardAsync();
            MessageBox.Show(this,
                queued ? $"{bot.Name} was marked for deletion. The server will boot the live bot first, then permanently remove its character data." : $"{bot.Name} and all owned character data were permanently deleted.",
                queued ? "Deletion queued" : "Character deleted", MessageBoxButtons.OK,
                queued ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "Unable to delete character", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task TeleportToSelectedBotAsync()
    {
        BotRow? bot = SelectedBot();
        if (bot is not { BotId: not null, IsOnline: true, DeletionQueued: false } || !IsServerRunning())
        {
            MessageBox.Show(this, "The server and selected playerbot must both be online.", "Teleport unavailable",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            string account = ReadCredentials().Account;
            await Task.Run(() => QueueTeleportCommand(bot.BotId.Value, account));
            _footer.Text = $"Teleport requested — your logged-in character will move to {bot.Name}.";
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "Unable to teleport", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void QueueTeleportCommand(long botId, string account)
    {
        using var connection = new SQLiteConnection($"Data Source={_database};Version=3;Pooling=False;Default Timeout=5");
        connection.Open();
        InsertOrReplaceTeleportCommand(connection, botId, account, DateTime.UtcNow);
    }

    internal static void InsertOrReplaceTeleportCommand(SQLiteConnection connection, long botId, string account, DateTime requestedUtc)
    {
        if (connection == null || connection.State != System.Data.ConnectionState.Open)
            throw new InvalidOperationException("The local game database is not open.");
        if (string.IsNullOrWhiteSpace(account))
            throw new InvalidOperationException("The launcher account is not configured.");
        if (!TableExists(connection, "offline_world_bots") || !TableExists(connection, "offline_bot_commands") ||
            !ColumnExists(connection, "offline_bot_commands", "RequestedByAccount"))
            throw new InvalidOperationException("The updated server is still starting. Wait until Enter Realm is available, then try again.");

        using var transaction = connection.BeginTransaction();
        using (var target = connection.CreateCommand())
        {
            target.Transaction = transaction;
            target.CommandText = "SELECT COUNT(*) FROM offline_world_bots WHERE BotId=@id AND IsOnline=1 AND IsRetired=0";
            target.Parameters.AddWithValue("@id", botId);
            if (Convert.ToInt32(target.ExecuteScalar()) != 1)
                throw new InvalidOperationException("The selected playerbot is no longer online.");
        }

        string now = requestedUtc.ToString("O");
        int replaced;
        using (var replace = connection.CreateCommand())
        {
            replace.Transaction = transaction;
            replace.CommandText = """
                UPDATE offline_bot_commands
                SET BotId=@id, RequestedUtc=@now, State='Pending', CompletedUtc=NULL, Error=NULL
                WHERE CommandId = (
                    SELECT CommandId FROM offline_bot_commands
                    WHERE CommandType='TeleportToBot' AND RequestedByAccount=@account AND State<>'Processing'
                    ORDER BY CASE WHEN State='Pending' THEN 0 ELSE 1 END, CommandId DESC
                    LIMIT 1)
                """;
            replace.Parameters.AddWithValue("@id", botId);
            replace.Parameters.AddWithValue("@now", now);
            replace.Parameters.AddWithValue("@account", account.Trim());
            replaced = replace.ExecuteNonQuery();
        }

        if (replaced == 0)
        {
            using var insert = connection.CreateCommand();
            insert.Transaction = transaction;
            insert.CommandText = """
                INSERT INTO offline_bot_commands
                    (BotId, CommandType, RequestedUtc, State, RequestedByAccount)
                VALUES (@id, 'TeleportToBot', @now, 'Pending', @account)
                """;
            insert.Parameters.AddWithValue("@id", botId);
            insert.Parameters.AddWithValue("@now", now);
            insert.Parameters.AddWithValue("@account", account.Trim());
            insert.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    private async Task DeleteAllBotsAsync()
    {
        bool serverRunning = IsServerRunning() || FindExactServerProcess() is not null;
        int count = _bots.Count(bot => bot.BotId.HasValue && (!bot.DeletionQueued || !serverRunning));
        if (count <= 0)
            return;

        string warning = $"Are you sure you want to permanently delete all {count:N0} bot characters?\n\n" +
                         "This removes every bot's character progress, equipment, backpack, bank contents, coins, and associated Realm Exchange records. " +
                         "Your player account and player characters are not affected. This cannot be undone.";
        if (MessageBox.Show(this, warning, "Delete every bot forever?", MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
            return;

        try
        {
            bool queued = QueueOrDeleteAllBots();
            await RefreshDashboardAsync();
            MessageBox.Show(this,
                queued
                    ? "All bot characters were marked for permanent deletion. The running server will remove each live world actor before deleting its saved data."
                    : "All bot characters and all of their owned data were permanently deleted.",
                queued ? "Delete-all queued" : "All bots deleted", MessageBoxButtons.OK,
                queued ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "Unable to delete all bots", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private bool QueueOrDeleteBot(long botId)
    {
        using var connection = new SQLiteConnection($"Data Source={_database};Version=3;Pooling=False;Default Timeout=10");
        connection.Open();
        using var transaction = connection.BeginTransaction();
        bool serverRunning = IsServerRunning() || FindExactServerProcess() is not null;
        if (serverRunning)
        {
            using var retire = connection.CreateCommand();
            retire.Transaction = transaction;
            retire.CommandText = "UPDATE offline_world_bots SET IsRetired=1, Activity='Deletion requested', CurrentGoal='', LastUpdateUtc=@now WHERE BotId=@id";
            retire.Parameters.AddWithValue("@id", botId);
            retire.Parameters.AddWithValue("@now", DateTime.UtcNow.ToString("O"));
            if (retire.ExecuteNonQuery() != 1)
                throw new InvalidOperationException("The selected character no longer exists.");

            using var queue = connection.CreateCommand();
            queue.Transaction = transaction;
            queue.CommandText = "INSERT INTO offline_bot_commands (BotId, CommandType, RequestedUtc, State) VALUES (@id, 'Delete', @now, 'Pending')";
            queue.Parameters.AddWithValue("@id", botId);
            queue.Parameters.AddWithValue("@now", DateTime.UtcNow.ToString("O"));
            queue.ExecuteNonQuery();
        }
        else
        {
            PurgeBotData(connection, transaction, botId);
        }

        UpdatePopulationTarget(connection, transaction);
        transaction.Commit();
        return serverRunning;
    }

    private bool QueueOrDeleteAllBots()
    {
        using var connection = new SQLiteConnection($"Data Source={_database};Version=3;Pooling=False;Default Timeout=10");
        connection.Open();
        using var transaction = connection.BeginTransaction();
        bool serverRunning = IsServerRunning() || FindExactServerProcess() is not null;
        string now = DateTime.UtcNow.ToString("O");
        if (serverRunning)
        {
            using (var queue = connection.CreateCommand())
            {
                queue.Transaction = transaction;
                queue.CommandText = """
                    INSERT INTO offline_bot_commands (BotId, CommandType, RequestedUtc, State)
                    SELECT b.BotId, 'Delete', @now, 'Pending'
                    FROM offline_world_bots b
                    WHERE b.IsRetired=0
                      AND NOT EXISTS (
                          SELECT 1 FROM offline_bot_commands c
                          WHERE c.BotId=b.BotId AND c.CommandType='Delete' AND c.State IN ('Pending','Processing'))
                    """;
                queue.Parameters.AddWithValue("@now", now);
                queue.ExecuteNonQuery();
            }

            using var retire = connection.CreateCommand();
            retire.Transaction = transaction;
            retire.CommandText = "UPDATE offline_world_bots SET IsRetired=1, Activity='Deletion requested', CurrentGoal='', LastUpdateUtc=@now WHERE IsRetired=0";
            retire.Parameters.AddWithValue("@now", now);
            retire.ExecuteNonQuery();
        }
        else
        {
            foreach (string sql in new[]
            {
                "DELETE FROM Inventory WHERE OwnerID LIKE 'offlinebot:%'",
                "DELETE FROM offline_auction_escrow WHERE BidderBotId IN (SELECT BotId FROM offline_world_bots)",
                "DELETE FROM offline_auction_ledger WHERE SellerBotId IN (SELECT BotId FROM offline_world_bots) OR BuyerBotId IN (SELECT BotId FROM offline_world_bots)",
                "DELETE FROM offline_auction_listings WHERE SellerBotId IN (SELECT BotId FROM offline_world_bots) OR HighBidderBotId IN (SELECT BotId FROM offline_world_bots)",
                "DELETE FROM offline_bot_commands WHERE BotId IN (SELECT BotId FROM offline_world_bots)",
                "DELETE FROM offline_world_bots",
            })
            {
                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = sql;
                command.ExecuteNonQuery();
            }
        }

        UpdatePopulationTarget(connection, transaction);
        transaction.Commit();
        return serverRunning;
    }

    private static void PurgeBotData(SQLiteConnection connection, SQLiteTransaction transaction, long botId)
    {
        string owner = $"offlinebot:{botId}";
        foreach (string sql in new[]
        {
            "DELETE FROM Inventory WHERE OwnerID=@owner",
            "DELETE FROM offline_auction_escrow WHERE BidderBotId=@id",
            "DELETE FROM offline_auction_ledger WHERE SellerBotId=@id OR BuyerBotId=@id",
            "DELETE FROM offline_auction_listings WHERE SellerBotId=@id OR HighBidderBotId=@id",
            "DELETE FROM offline_bot_commands WHERE BotId=@id",
            "DELETE FROM offline_world_bots WHERE BotId=@id",
        })
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            command.Parameters.AddWithValue("@id", botId);
            command.Parameters.AddWithValue("@owner", owner);
            command.ExecuteNonQuery();
        }
    }

    private static void UpdatePopulationTarget(SQLiteConnection connection, SQLiteTransaction transaction)
    {
        int count;
        using (var countCommand = connection.CreateCommand())
        {
            countCommand.Transaction = transaction;
            countCommand.CommandText = "SELECT COUNT(*) FROM offline_world_bots WHERE IsRetired=0";
            count = Convert.ToInt32(countCommand.ExecuteScalar());
        }
        int target = count;
        using (var settings = connection.CreateCommand())
        {
            settings.Transaction = transaction;
            settings.CommandText = "UPDATE offline_population_settings SET Value=CASE Key WHEN 'PopulationEnabled' THEN @enabled WHEN 'ActiveTarget' THEN @target WHEN 'HardActiveCap' THEN '0' ELSE Value END WHERE Key IN ('PopulationEnabled','ActiveTarget','HardActiveCap')";
            settings.Parameters.AddWithValue("@enabled", count > 0 ? "true" : "false");
            settings.Parameters.AddWithValue("@target", target.ToString());
            settings.ExecuteNonQuery();
        }
        using (var serverProperties = connection.CreateCommand())
        {
            serverProperties.Transaction = transaction;
            serverProperties.CommandText = "UPDATE ServerProperty SET Value=CASE Key WHEN 'population_enabled' THEN @enabled WHEN 'active_target' THEN @target WHEN 'hard_active_cap' THEN '0' ELSE Value END WHERE Key IN ('population_enabled','active_target','hard_active_cap')";
            serverProperties.Parameters.AddWithValue("@enabled", count > 0 ? "true" : "false");
            serverProperties.Parameters.AddWithValue("@target", target.ToString());
            serverProperties.ExecuteNonQuery();
        }
    }

    private BotRow? SelectedBot() => _grid.CurrentRow?.DataBoundItem as BotRow;

    private void UpdateDeleteButton()
    {
        bool serverRunning = IsServerRunning() || FindExactServerProcess() is not null;
        if (_deleteBotButton != null)
            _deleteBotButton.Enabled = SelectedBot() is { CanDelete: true } selected &&
                                       (!selected.DeletionQueued || !serverRunning);
        if (_deleteAllBotsButton != null)
            _deleteAllBotsButton.Enabled = _bots.Any(bot => bot.BotId.HasValue &&
                                                            (!bot.DeletionQueued || !serverRunning));
    }

    private void SelectBot(string name)
    {
        foreach (DataGridViewRow row in _grid.Rows)
        {
            if (row.DataBoundItem is BotRow bot && bot.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                row.Selected = true;
                _grid.CurrentCell = row.Cells[0];
                break;
            }
        }
    }

    private BotGoalsSettingsControl? _botGoalsSettings;

    private bool BotGoalsServerStopped()
    {
        if (_stoppingServer || _serverProcess is { HasExited: false } || IsServerRunning()) return false;
        using var process = FindExactServerProcess();
        return process is null;
    }

    private void StartServer()
    {
        // The owned process check closes the small window between Process.Start
        // and the TCP listener coming online.  Without it, a second click can
        // start another CoreServer while the first one is still loading the
        // world, and both processes then contend for the same SQLite database.
        if (_resettingKeepsRelics || _savingXpRates || _serverProcess is { HasExited: false } || IsServerRunning() || FindExactServerProcess() is not null) return;
        if (_botGoalsSettings?.HasUnsavedChanges == true)
        {
            MessageBox.Show(this, "Save or undo your edits in Bot Goals Setting before starting the server.", "Unsaved bot goals");
            return;
        }
        try { OfflineDaoc.Configuration.BotGoalSettings.Load(Path.Combine(_serverDirectory, OfflineDaoc.Configuration.BotGoalSettings.FileName)); }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Correct Bot Goals Setting before starting:\n" + ex.Message, "Invalid bot goals");
            return;
        }
        if (!File.Exists(_serverExecutable))
        {
            MessageBox.Show(this, "The compiled server package is missing.", "Cannot start", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        // Run once more immediately before CoreServer starts.  This covers a
        // launcher that was left open while its database was being prepared,
        // without ever allowing a failed cosmetic migration to block startup.
        EnsureRealmExchangeGuardPositions();

        var availableGb = AvailableMemoryBytes() / 1024d / 1024d / 1024d;
        if (availableGb < 4 && MessageBox.Show(this,
                $"Only {availableGb:F1} GB of memory is currently available. Close some programs before running a large bot population. Start anyway?",
                "Low available memory", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
        {
            return;
        }

        _startButton.Enabled = false;
        try
        {
            Directory.CreateDirectory(_logsDirectory);
            _serverLog?.Dispose();
            _serverLog = new RollingServerLog(Path.Combine(_logsDirectory, "server-console.log"));
            var startInfo = new ProcessStartInfo(_serverExecutable)
            {
                WorkingDirectory = _serverDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            _serverProcess = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
            _serverProcess.OutputDataReceived += (_, args) => { if (args.Data is not null) _serverLog?.WriteLine(args.Data); };
            _serverProcess.ErrorDataReceived += (_, args) => { if (args.Data is not null) _serverLog?.WriteLine("ERROR: " + args.Data); };
            _serverProcess.Exited += (_, _) => BeginInvoke(async () =>
            {
                _serverReadinessPoll.Stop();
                _stoppingServer = false;
                await RefreshDashboardAsync();
            });
            _serverProcess.Start();
            ShowStartingState();
            _serverProcess.BeginOutputReadLine();
            _serverProcess.BeginErrorReadLine();

            // Runtime status is dashboard metadata, not a prerequisite for the
            // server.  During startup the server may briefly own SQLite schema
            // and world-load write locks, so a failed status update must never
            // crash the launcher and close CoreServer's redirected stdin.
            TryUpdateRuntimeState("Starting", _serverProcess.Id);
            BeginServerReadinessPolling();
        }
        catch (Exception exception)
        {
            _serverReadinessPoll.Stop();
            _serverProcess?.Dispose();
            _serverProcess = null;
            _startButton.Enabled = true;
            MessageBox.Show(this, exception.Message, "Unable to start server", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _ = RefreshDashboardAsync();
        }
    }

    private void EnsureRealmExchangeGuardPositions()
    {
        // Never write a world row while any CoreServer owns a database.  If a
        // launcher is opened beside a running server, the next stopped launch
        // will retry the same idempotent migration safely.
        if (_serverProcess is { HasExited: false } || FindExactServerProcess() is not null)
            return;
        EnsureRealmExchangeGuardPositions(_database);
    }

    private static void EnsureRealmExchangeGuardPositions(string databasePath)
    {
        if (!File.Exists(databasePath)) return;

        try
        {
            using var connection = new SQLiteConnection($"Data Source={databasePath};Version=3;Pooling=False;Default Timeout=5");
            connection.Open();
            if (!TableExists(connection, "Mob") ||
                !ColumnExists(connection, "Mob", "Mob_ID") ||
                !ColumnExists(connection, "Mob", "PackageID") ||
                !ColumnExists(connection, "Mob", "Region") ||
                !ColumnExists(connection, "Mob", "X") ||
                !ColumnExists(connection, "Mob", "Y"))
                return;

            using var transaction = connection.BeginTransaction();
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                UPDATE Mob
                   SET X = @x, Y = @y
                 WHERE Mob_ID = @id
                   AND Region = 201
                   AND PackageID = 'offline_realm_exchange'
                """;
            command.Parameters.Add("@x", System.Data.DbType.Int32);
            command.Parameters.Add("@y", System.Data.DbType.Int32);
            command.Parameters.Add("@id", System.Data.DbType.String);

            foreach (var guard in new[]
            {
                (Id: "offline-realm-exchange-hibernia-guard-left", X: 33197, Y: 31240),
                (Id: "offline-realm-exchange-hibernia-guard-right", X: 33197, Y: 31440),
            })
            {
                command.Parameters["@x"].Value = guard.X;
                command.Parameters["@y"].Value = guard.Y;
                command.Parameters["@id"].Value = guard.Id;
                command.ExecuteNonQuery();
            }
            transaction.Commit();
        }
        catch (SQLiteException exception) when (exception.ResultCode is SQLiteErrorCode.Busy or SQLiteErrorCode.Locked)
        {
            // A server/world-load lock is transient.  The second call before
            // startup (and the next launcher start) retries safely.
        }
        catch
        {
            // A missing/older optional table must never make the launcher
            // close or prevent the server from starting.
        }
    }

    private void ShowStartingState()
    {
        _serverState.Text = "STARTING…";
        _serverState.ForeColor = DaocTheme.Gold;
        _startButton.Enabled = false;
        _stopButton.Enabled = false;
        _playButton.Enabled = false;
        _footer.Text = "Server is starting — Enter Realm will unlock automatically when it is ready…";
    }

    private async Task StopServerAsync()
    {
        _serverReadinessPoll.Stop();
        Process? externalProcess = null;
        try
        {
            bool ownsRunningProcess = _serverProcess is { HasExited: false };
            if (!ownsRunningProcess)
                externalProcess = FindExactServerProcess();

            if (!ownsRunningProcess && externalProcess is not null &&
                MessageBox.Show(this, "Gracefully stopping is unavailable because this launcher did not start the process. Force-close this exact local CoreServer process?", "Stop server", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            if (!ownsRunningProcess && externalProcess is null)
            {
                _stoppingServer = false;
                TryUpdateRuntimeState("Stopped", null);
                return;
            }

            ShowStoppingState();
            TryUpdateRuntimeState("Stopping", ownsRunningProcess ? _serverProcess!.Id : externalProcess!.Id);

            if (ownsRunningProcess)
            {
                _serverProcess!.StandardInput.WriteLine("exit");
                _serverProcess.StandardInput.Flush();
                if (!await WaitForExitWithinAsync(_serverProcess, 10_000))
                {
                    MessageBox.Show(this, "The server is still shutting down. It has not been force-closed.", "Shutdown pending", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
            }
            else
            {
                externalProcess!.Kill(true);
                if (!await WaitForExitWithinAsync(externalProcess, 5_000))
                {
                    MessageBox.Show(this, "The server process has not exited yet.", "Shutdown pending", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
            }
            _stoppingServer = false;
            TryUpdateRuntimeState("Stopped", null);
        }
        catch (Exception exception)
        {
            _stoppingServer = false;
            MessageBox.Show(this, exception.Message, "Unable to stop server", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            externalProcess?.Dispose();
            await RefreshDashboardAsync();
        }
    }

    private void ShowStoppingState()
    {
        _stoppingServer = true;
        _serverState.Text = "STOPPING…";
        _serverState.ForeColor = DaocTheme.Danger;
        _startButton.Enabled = false;
        _stopButton.Enabled = false;
        _playButton.Enabled = false;
        _footer.Text = "Server is shutting down safely…";
    }

    private static async Task<bool> WaitForExitWithinAsync(Process process, int timeoutMilliseconds)
    {
        if (process.HasExited)
            return true;
        using var timeout = new CancellationTokenSource(timeoutMilliseconds);
        try
        {
            await process.WaitForExitAsync(timeout.Token);
            return true;
        }
        catch (OperationCanceledException)
        {
            return process.HasExited;
        }
    }

    private void LaunchClient()
    {
        if (!IsServerRunning())
        {
            MessageBox.Show(this, "Start the local server first.", "Server stopped", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (!File.Exists(_clientConnector) || !File.Exists(Path.Combine(_clientDirectory, "game.dll")))
        {
            MessageBox.Show(this, "The isolated client or connection utility is missing.", "Client not ready", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        var credentials = ReadCredentials();
        EnsureBorderlessFullscreen();
        ClientSessionDiagnostics.Prepare(_logsDirectory);
        Process? client = Process.Start(new ProcessStartInfo(_clientConnector)
        {
            WorkingDirectory = _clientDirectory,
            UseShellExecute = false,
            ArgumentList = { "game.dll", "127.0.0.1", credentials.Account, credentials.Password },
        });
        if (client != null)
            ClientSessionDiagnostics.Start(client, _clientDirectory, _logsDirectory);
    }

    private (string Account, string Password) ReadCredentials()
    {
        return PortableCredentials.ReadOrCreate(_root);
    }

    private bool IsServerRunning()
    {
        return IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners().Any(endpoint => endpoint.Port == 10300);
    }

    private void BeginServerReadinessPolling()
    {
        _serverReadinessPoll.Stop();
        _serverReadinessPoll.Start();
    }

    private async Task RefreshWhenServerReadyAsync()
    {
        bool processExited = _serverProcess is { HasExited: true };
        bool serverReady = IsServerRunning();
        if (!serverReady && !processExited)
            return;

        // Do not stop the probe while another refresh owns the snapshot gate.
        // Its next lightweight tick will perform the one readiness refresh.
        if (_refreshing)
            return;

        await RefreshDashboardAsync();
        // A short SQLite startup lock may make the first snapshot fail. Keep
        // probing until the successful refresh actually enables Enter Realm.
        if (processExited || _playButton.Enabled || !File.Exists(_clientConnector))
            _serverReadinessPoll.Stop();
    }

    private void EnsureBorderlessFullscreen()
    {
        // Use this distribution's client profile, not another installed copy's preferences.
        string profile = "OfflineDAoCGitHub03";
        string pathsFile = Path.Combine(_clientDirectory, "paths.dat");
        if (File.Exists(pathsFile))
        {
            string? value = File.ReadLines(pathsFile).FirstOrDefault(line => line.TrimStart().StartsWith("settings=", StringComparison.OrdinalIgnoreCase));
            if (value != null)
            {
                string candidate = value[(value.IndexOf('=') + 1)..].Trim();
                if (candidate.Length > 0 && candidate.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '-')) profile = candidate;
            }
        }
        var profileDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Electronic Arts", "Dark Age of Camelot", profile);
        Directory.CreateDirectory(profileDirectory);
        var path = Path.Combine(profileDirectory, "user.dat");
        var lines = File.Exists(path) ? File.ReadAllLines(path).ToList() : new List<string>();
        var mainIndex = lines.FindIndex(line => line.Trim().Equals("[main]", StringComparison.OrdinalIgnoreCase));
        if (mainIndex < 0)
        {
            if (lines.Count > 0 && lines[^1].Length != 0) lines.Add(string.Empty);
            lines.Add("[main]");
            lines.Add("fullscreen_windowed=1");
        }
        else
        {
            var sectionEnd = lines.FindIndex(mainIndex + 1, line => line.TrimStart().StartsWith('['));
            if (sectionEnd < 0) sectionEnd = lines.Count;
            var settingIndex = -1;
            for (var index = mainIndex + 1; index < sectionEnd; index++)
            {
                if (lines[index].TrimStart().StartsWith("fullscreen_windowed=", StringComparison.OrdinalIgnoreCase))
                {
                    settingIndex = index;
                    break;
                }
            }
            if (settingIndex >= 0) lines[settingIndex] = "fullscreen_windowed=1";
            else lines.Insert(sectionEnd, "fullscreen_windowed=1");
        }
        File.WriteAllLines(path, lines);
    }

    private Process? FindExactServerProcess()
    {
        foreach (var process in Process.GetProcessesByName("CoreServer"))
        {
            try
            {
                if (string.Equals(process.MainModule?.FileName, _serverExecutable, StringComparison.OrdinalIgnoreCase)) return process;
            }
            catch
            {
                process.Dispose();
            }
        }
        return null;
    }

    private bool TryUpdateRuntimeState(string state, int? pid)
    {
        try
        {
            using var connection = new SQLiteConnection($"Data Source={_database};Version=3;Pooling=False;Default Timeout=1");
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "UPDATE offline_runtime_status SET ServerState=@state, ServerPid=@pid, LastHeartbeatUtc=@now WHERE Id=1";
            command.Parameters.AddWithValue("@state", state);
            command.Parameters.AddWithValue("@pid", pid is null ? DBNull.Value : pid.Value);
            command.Parameters.AddWithValue("@now", DateTime.UtcNow.ToString("O"));
            command.ExecuteNonQuery();
            return true;
        }
        catch (SQLiteException exception) when (exception.ResultCode is SQLiteErrorCode.Busy or SQLiteErrorCode.Locked)
        {
            _footer.Text = "Server state is changing; dashboard status will catch up on refresh.";
            return false;
        }
        catch (Exception exception)
        {
            _footer.Text = $"Server state metadata could not be updated: {exception.Message}";
            return false;
        }
    }

    private static bool TableExists(SQLiteConnection connection, string table)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=@name";
        command.Parameters.AddWithValue("@name", table);
        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    private static bool ColumnExists(SQLiteConnection connection, string table, string column)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info([{table.Replace("]", "]]", StringComparison.Ordinal)}])";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            if (reader.GetString(1).Equals(column, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private static (string Realm, string Name) ClassInfo(int id) => id switch
    {
        1 => ("Albion", "Paladin"), 2 => ("Albion", "Armsman"), 3 => ("Albion", "Scout"), 4 => ("Albion", "Minstrel"),
        5 => ("Albion", "Theurgist"), 6 => ("Albion", "Cleric"), 7 => ("Albion", "Wizard"), 8 => ("Albion", "Sorcerer"),
        9 => ("Albion", "Infiltrator"), 10 => ("Albion", "Friar"), 11 => ("Albion", "Mercenary"), 12 => ("Albion", "Necromancer"),
        13 => ("Albion", "Cabalist"), 19 => ("Albion", "Reaver"),
        21 => ("Midgard", "Thane"), 22 => ("Midgard", "Warrior"), 23 => ("Midgard", "Shadowblade"), 24 => ("Midgard", "Skald"),
        25 => ("Midgard", "Hunter"), 26 => ("Midgard", "Healer"), 27 => ("Midgard", "Spiritmaster"), 28 => ("Midgard", "Shaman"),
        29 => ("Midgard", "Runemaster"), 30 => ("Midgard", "Bonedancer"), 31 => ("Midgard", "Berserker"), 32 => ("Midgard", "Savage"),
        40 => ("Hibernia", "Eldritch"), 41 => ("Hibernia", "Enchanter"), 42 => ("Hibernia", "Mentalist"), 43 => ("Hibernia", "Blademaster"),
        44 => ("Hibernia", "Hero"), 45 => ("Hibernia", "Champion"), 46 => ("Hibernia", "Warden"), 47 => ("Hibernia", "Druid"),
        48 => ("Hibernia", "Bard"), 49 => ("Hibernia", "Nightshade"), 50 => ("Hibernia", "Ranger"), 55 => ("Hibernia", "Animist"),
        56 => ("Hibernia", "Valewalker"),
        _ => ("Unknown", $"Class {id}"),
    };

    private static string RealmName(int value) => value switch { 1 => "Albion", 2 => "Midgard", 3 => "Hibernia", _ => "Unknown" };
    private static string GenderName(int value) => value switch { 1 => "Male", 2 => "Female", _ => "—" };

    private static string FormatBotActivity(string activity, string goal, string target, string destination, string progress, bool isOnline)
    {
        activity = activity?.Trim() ?? string.Empty;
        goal = goal?.Trim() ?? string.Empty;
        target = target?.Trim() ?? string.Empty;
        destination = destination?.Trim() ?? string.Empty;
        progress = progress?.Trim() ?? string.Empty;

        var details = new List<string>(3);
        if (goal.Length > 0) details.Add(goal);
        if (destination.Length > 0 && !goal.Contains(destination, StringComparison.OrdinalIgnoreCase)) details.Add(destination);
        if (target.Length > 0 && !goal.Contains(target, StringComparison.OrdinalIgnoreCase)) details.Add($"Target: {target}");
        if (progress.Length > 0) details.Add(progress);
        string live = details.Count > 0 ? string.Join(" — ", details) : activity.Length > 0 ? activity : "Choosing next activity";
        if (isOnline && activity.Length > 0 && !live.StartsWith(activity, StringComparison.OrdinalIgnoreCase))
            live = $"{activity}: {live}";
        return isOnline ? live : $"Parked — resumes {live}";
    }

    internal static string FormatCopper(long copper)
    {
        long platinum = copper / 1_000_000;
        long gold = copper % 1_000_000 / 10_000;
        long silver = copper % 10_000 / 100;
        long remainder = copper % 100;
        var parts = new List<string>(4);
        if (platinum > 0) parts.Add($"{platinum}p");
        if (gold > 0) parts.Add($"{gold}g");
        if (silver > 0) parts.Add($"{silver}s");
        if (remainder > 0 || parts.Count == 0) parts.Add($"{remainder}c");
        return string.Join(" ", parts);
    }

    private static string RealmRosterValue(IReadOnlyCollection<BotRow> bots, string realm)
    {
        int total = bots.Count(bot => bot.Realm == realm && !bot.DeletionQueued);
        int online = bots.Count(bot => bot.Realm == realm && bot.IsOnline && !bot.DeletionQueued);
        return $"{total:N0} ROSTER\n{online:N0} ONLINE";
    }

    private static string FormatExpiry(string value)
    {
        if (!DateTime.TryParse(value, null, System.Globalization.DateTimeStyles.RoundtripKind, out var expires)) return value;
        var remaining = expires.ToUniversalTime() - DateTime.UtcNow;
        if (remaining <= TimeSpan.Zero) return "Expired";
        if (remaining.TotalDays >= 1) return $"{(int)remaining.TotalDays}d {remaining.Hours}h";
        if (remaining.TotalHours >= 1) return $"{(int)remaining.TotalHours}h {remaining.Minutes}m";
        return $"{Math.Max(0, remaining.Minutes)}m";
    }

    private static Button ActionButton(string text, Color color) => new RuneButton
    {
        Text = text,
        Accent = color,
    };

    private static Panel Card(string caption, Label value, Color accent, Control? action = null)
    {
        var panel = new InsetPanel { Dock = DockStyle.Fill, BackColor = DaocTheme.Panel, Margin = new Padding(3), Accent = accent };
        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = action == null ? 2 : 3,
            ColumnCount = 1,
            Padding = new Padding(9, 3, 6, 4),
            BackColor = Color.Transparent,
        };
        // Constrain nested layouts to the card instead of their preferred width.
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 18));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        if (action != null)
            content.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));

        var captionLabel = new Label
        {
            Text = caption,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = DaocTheme.Muted,
            Font = new Font("Georgia", 8f, FontStyle.Bold),
        };
        value.Dock = DockStyle.Fill;
        value.Padding = new Padding(0);
        value.Margin = new Padding(3, 0, 0, 0);
        value.TextAlign = ContentAlignment.MiddleLeft;
        value.Font = action == null
            ? new Font("Georgia", 15f, FontStyle.Bold)
            : new Font("Georgia", 10f, FontStyle.Bold);
        content.Controls.Add(captionLabel, 0, 0);
        content.Controls.Add(value, 0, 1);
        if (action != null)
        {
            action.Margin = new Padding(0, 1, 0, 0);
            content.Controls.Add(action, 0, 2);
        }

        panel.Controls.Add(content);
        var accentBar = new Panel { Dock = DockStyle.Left, Width = 3, BackColor = accent };
        panel.Controls.Add(accentBar);
        accentBar.BringToFront();
        return panel;
    }

    private static Label CardValue() => new()
    {
        Text = "0",
        Dock = DockStyle.Fill,
        Padding = new Padding(11, 4, 0, 0),
        TextAlign = ContentAlignment.TopLeft,
        Font = new Font("Georgia", 14f, FontStyle.Bold),
        ForeColor = DaocTheme.GoldLight,
    };

    private static DataGridViewTextBoxColumn TextColumn(string header, string property, int width, DataGridViewAutoSizeColumnMode mode = DataGridViewAutoSizeColumnMode.None) => new()
    {
        HeaderText = header,
        DataPropertyName = property,
        Width = width,
        AutoSizeMode = mode,
        SortMode = DataGridViewColumnSortMode.Automatic,
    };

    private static void StyleInput(Control input)
    {
        input.BackColor = Color.FromArgb(59, 53, 43);
        input.ForeColor = DaocTheme.Text;
        input.Font = new Font("Georgia", 8.25f);
        input.Margin = new Padding(0, 0, 8, 0);
        input.Height = 27;
        if (input is TextBox textBox) textBox.BorderStyle = BorderStyle.FixedSingle;
    }

    private void OpenFolder(string path)
    {
        Directory.CreateDirectory(path);
        Process.Start(new ProcessStartInfo("explorer.exe", $"\"{path}\"") { UseShellExecute = true });
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MemoryStatus
    {
        public uint Length;
        public uint MemoryLoad;
        public ulong TotalPhysical;
        public ulong AvailablePhysical;
        public ulong TotalPageFile;
        public ulong AvailablePageFile;
        public ulong TotalVirtual;
        public ulong AvailableVirtual;
        public ulong AvailableExtendedVirtual;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatus status);

    private const int VkLeftShift = 0xA0;
    private const int VkLeftControl = 0xA2;

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int virtualKey);

    private static ulong AvailableMemoryBytes()
    {
        var status = new MemoryStatus { Length = (uint)Marshal.SizeOf<MemoryStatus>() };
        return GlobalMemoryStatusEx(ref status) ? status.AvailablePhysical : 0;
    }

    private sealed record BotRow(long? BotId, string Name, string Realm, string RaceName, string Gender, string ClassName, int Level, string ZoneName, string Activity, bool IsOnline, bool CanDelete, bool DeletionQueued,
        string GroupId, string GroupPhase, string GroupGoal, string GroupStatus, string ObjectiveKind, string ObjectiveAssignmentId, string ObjectiveAssignedUtc, string ObjectivePhase)
    {
        public string ObjectiveExpiresUtc { get; init; } = string.Empty;
        public bool HasGroupTaskClock { get; init; }
        public bool TaskTimerPaused { get; init; }
        public long TaskRemainingMilliseconds { get; init; }
        public string GroupTaskExpiresUtc { get; init; } = string.Empty;
        public string MeetUpDeadlineUtc { get; init; } = string.Empty;
        public string GroupLeaderName { get; init; } = string.Empty;
        public string GroupRendezvousName { get; init; } = string.Empty;
        public string GroupPullerName { get; init; } = string.Empty;
        public string GroupRole { get; init; } = string.Empty;
        public long? AssemblyRemainingMilliseconds => IsOnline &&
            GroupPhase is "Leader staging" or "Meeting up"
                ? TaskTimerDisplay.Remaining(MeetUpDeadlineUtc, DateTime.UtcNow) : null;
        public long? RemainingMilliseconds => !IsOnline || DeletionQueued || GroupPhase == "Player-led" ? null :
            AssemblyRemainingMilliseconds ?? (HasGroupTaskClock && TaskTimerPaused
                ? TaskRemainingMilliseconds
                : TaskTimerDisplay.Remaining(HasGroupTaskClock ? GroupTaskExpiresUtc : ObjectiveExpiresUtc, DateTime.UtcNow));
        public string TaskRemaining => !IsOnline ? "Offline" : GroupPhase == "Player-led" ? "Player-led" :
            GroupPhase == "Leader staging" && AssemblyRemainingMilliseconds is long staging
                ? TaskTimerDisplay.Format(staging) + " (staging)" :
            GroupPhase == "Meeting up" && AssemblyRemainingMilliseconds is long meetup
                ? TaskTimerDisplay.Format(meetup) + " (meetup)" :
            ObjectiveAssignmentId.StartsWith("between-pve-services-", StringComparison.Ordinal)
                ? "Town " + TaskTimerDisplay.Format(RemainingMilliseconds ?? 0) :
            RemainingMilliseconds is long remaining
                ? TaskTimerDisplay.Format(remaining) + (HasGroupTaskClock && TaskTimerPaused ? " (paused)" : string.Empty) : "—";
        public string GroupTimerText => GroupPhase switch
        {
            "Leader staging" => "LEADER STAGING: " + TaskTimerDisplay.Format(AssemblyRemainingMilliseconds ?? 0),
            "Meeting up" => "MEETUP LEFT: " + TaskTimerDisplay.Format(AssemblyRemainingMilliseconds ?? 0),
            _ => "TASK LEFT: " + TaskRemaining,
        };
        public string NameWithMeetUpTimer => IsOnline && GroupPhase == "Meeting up" &&
            TaskTimerDisplay.Remaining(MeetUpDeadlineUtc, DateTime.UtcNow) is long remaining
                ? $"{Name} ({TaskTimerDisplay.Format(remaining)})" : Name;
        public string State => DeletionQueued ? "Deleting…" : IsOnline ? "Online" : "Offline";
        public string RvrFormation => GroupId.Length > 0 ? $"Warband {GroupId}" : "Solo roamer";
    }

    private sealed record AuctionRow(string ItemName, string Seller, int ItemLevel, int Quantity, string CurrentBid, string Buyout, long BuyoutCopper, string Expires, string State);
    private sealed record GroupRow(string GroupId, string Realm, string Phase, string SharedGoal, string Status,
        string LeaderName, string RendezvousName, string PullerName, List<BotRow> Members);
    private sealed class GroupMetadata
    {
        public string GroupId { get; set; } = string.Empty;
        public string Phase { get; set; } = string.Empty;
        public string SharedGoal { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool HasTaskClock { get; set; }
        public bool TaskTimerPaused { get; set; }
        public long TaskRemainingMilliseconds { get; set; }
        public string TaskExpiresUtc { get; set; } = string.Empty;
        public string MeetUpDeadlineUtc { get; set; } = string.Empty;
        public string LeaderName { get; set; } = string.Empty;
        public string RendezvousName { get; set; } = string.Empty;
        public string PullerName { get; set; } = string.Empty;
        public string MemberRole { get; set; } = string.Empty;
    }
    private sealed record DashboardSnapshot(string ServerState, List<BotRow> Bots, List<GroupRow> Groups,
        int Active, double ServerMemoryMb, double TickP95Ms, double PlayerXpRate, double BotXpRate, bool MakeMeGm = false, RvrWorldSnapshot? RvrWorld = null);

    private sealed record LiveBotStatus(long BotId, int Level, string ZoneName, string Activity,
        string CurrentGoal, string TargetName, string TravelDestination, string ObjectiveProgress,
        bool IsAlive, string ItineraryJson, string ObjectiveKind, string ObjectiveAssignmentId,
        string ObjectiveAssignedUtc, string ObjectivePhase, string ObjectiveExpiresUtc);
    private sealed record LiveBotSnapshot(DateTime UpdatedUtc, bool Running, string RequestId, List<LiveBotStatus> Bots);

    private sealed record RvrObjective(string Kind, string Name, string Owner, string State, string Location, string Carrier, string Forces, string Id = "", long CooldownMilliseconds = 0, bool IsCatalogOnly = false, long PhaseRemainingMilliseconds = 0, string Phase = "")
    {
        // Countdown reaching zero does not end an event: arrivals, landing and
        // interior staging can still be in progress. Older keep/relic snapshots
        // provide State rather than Phase, so recognize their published states too.
        public bool IsActiveEvent => !IsCatalogOnly && !IsProtectedPortal &&
            (Phase is "Muster" or "Staging" or "Waiting" or "Battle" ||
             State.StartsWith("Raid rally", StringComparison.OrdinalIgnoreCase) ||
             State.StartsWith("Keep siege rally", StringComparison.OrdinalIgnoreCase) ||
             State.StartsWith("Relic siege rally", StringComparison.OrdinalIgnoreCase) ||
             State.StartsWith("SIEGE", StringComparison.OrdinalIgnoreCase) ||
             State.StartsWith("RAID —", StringComparison.OrdinalIgnoreCase) ||
             State.StartsWith("ESCORT", StringComparison.OrdinalIgnoreCase) ||
             State.StartsWith("DROPPED", StringComparison.OrdinalIgnoreCase) ||
             State.StartsWith("Under attack", StringComparison.OrdinalIgnoreCase));
        public string Marker => Kind == "Dragon" ? "◆ DRAGON" : Kind == "Epic dungeon" ? "◆ DUNGEON" : Kind == "Relic" ? "◆ RELIC" : Kind == "Relic keep" ? "▣ RELIC KEEP" : "▣ KEEP";
        // Compatibility with the last-known JSON generated by older servers.
        // Current publishers filter by the authoritative server keep flag.
        public bool IsProtectedPortal => Kind.Equals("Portal keep", StringComparison.OrdinalIgnoreCase) ||
            Name.EndsWith("Portal Keep", StringComparison.OrdinalIgnoreCase);
    }
    private sealed record EventParticipant(string EventId, string GroupId, string Name, string Realm, string Location, string Activity, int X, int Y, int Z);
    private sealed record RvrWorldSnapshot(DateTime UpdatedUtc, bool Running, List<RvrObjective> Objectives, List<EventParticipant>? Participants = null);

    private sealed record XpRateOption(double Multiplier, string Label)
    {
        public override string ToString() => Label;
    }
}
