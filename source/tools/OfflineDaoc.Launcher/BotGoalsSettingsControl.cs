using OfflineDaoc.Configuration;

namespace OfflineDaoc.Launcher;

internal sealed class BotGoalsSettingsControl : UserControl
{
    private readonly string _path;
    private readonly Func<bool> _serverStopped;
    private readonly NumericUpDown[,] _values = new NumericUpDown[3, 3];
    private readonly Label[] _totals = new Label[3];
    private readonly Label _status = new() { AutoSize = true, MaximumSize = new Size(850, 0) };
    private readonly Button _save = new() { Text = "Save settings", AutoSize = true };
    private readonly Button _undo = new() { Text = "Undo edits", AutoSize = true };
    private readonly Button _defaults = new() { Text = "Restore defaults", AutoSize = true };
    private readonly System.Windows.Forms.Timer _poll = new() { Interval = 2000 };
    private BotGoalSettings _saved = BotGoalSettings.Defaults;
    private bool _loading, _loadFailed, _savedFile;
    private string _notice = "";
    public bool HasUnsavedChanges => ReadValues() != _saved || _loadFailed;

    public BotGoalsSettingsControl(string path, Func<bool> serverStopped)
    {
        _path = path;
        _serverStopped = serverStopped;
        Dock = DockStyle.Fill;
        BackColor = DaocTheme.Panel;
        ForeColor = DaocTheme.Text;
        AutoScroll = true;
        var body = new FlowLayoutPanel
        {
            Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.TopDown,
            WrapContents = false, Padding = new Padding(16),
        };
        body.Controls.Add(new Label { Text = "Bot Goals Setting", AutoSize = true,
            Font = new Font(Font.FontFamily, 15, FontStyle.Bold), ForeColor = DaocTheme.GoldLight });
        body.Controls.Add(new Label { AutoSize = true, MaximumSize = new Size(850, 0), Margin = new Padding(3, 10, 3, 15),
            Text = "Choose the goal mix for each level bracket. Each row must total 100%.\n0% disables a goal; 100% selects only that goal. Applies to autonomous gamebots in all three realms." });
        var table = new TableLayoutPanel { AutoSize = true, ColumnCount = 5, RowCount = 4, Margin = new Padding(3, 3, 3, 15) };
        foreach (int width in new[] { 150, 135, 135, 135, 190 }) table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, width));
        string[] headings = ["Level bracket", "Solo PvE %", "Group PvE %", "RvR %", "Row total"];
        for (int col = 0; col < 5; col++) table.Controls.Add(new Label { Text = headings[col], AutoSize = true, Padding = new Padding(0, 4, 0, 8) }, col, 0);
        for (int row = 0; row < 3; row++)
        {
            table.Controls.Add(new Label { Text = new[] { "Levels 1–19", "Levels 20–49", "Level 50" }[row], AutoSize = true, Padding = new Padding(0, 8, 0, 8) }, 0, row + 1);
            for (int col = 0; col < 3; col++)
            {
                var value = new NumericUpDown { Minimum = 0, Maximum = 100, Width = 108,
                    Margin = new Padding(3, 5, 3, 8), AccessibleName = headings[col] + " " + row,
                    BackColor = DaocTheme.Panel, ForeColor = DaocTheme.Text };
                _values[row, col] = value;
                value.ValueChanged += (_, _) => { if (!_loading) { _notice = ""; UpdateState(); } };
                table.Controls.Add(value, col + 1, row + 1);
            }
            _totals[row] = new Label { AutoSize = true, Padding = new Padding(0, 8, 0, 8) };
            table.Controls.Add(_totals[row], 4, row + 1);
        }
        body.Controls.Add(table);
        body.Controls.Add(new Label { AutoSize = true, MaximumSize = new Size(850, 0),
            Text = "Levels 1–19 cannot do RvR. Saved settings replace forced PvE after RvR, allowing back-to-back RvR.\n" +
                "Percentages are population targets, not exact head counts at every moment. Existing allowed tasks finish normally.\n" +
                "Group-only bots wait for a complete eight-member role roster; they never fall back to a 0% goal.\n" +
                "Normal training, selling, recovery and town breaks remain. Player characters and /spawn companions are unchanged." });
        var buttons = new FlowLayoutPanel { AutoSize = true, Margin = new Padding(0, 14, 0, 10) };
        buttons.Controls.AddRange([_save, _undo, _defaults]);
        body.Controls.Add(buttons);
        body.Controls.Add(_status);
        Controls.Add(body);
        _save.Click += (_, _) => SaveSettings();
        _undo.Click += (_, _) => LoadSettings();
        _defaults.Click += (_, _) => { SetValues(BotGoalSettings.Defaults); _notice = "Default percentages loaded. Click Save settings to apply."; UpdateState(); };
        LoadSettings();
        _poll.Tick += (_, _) => UpdateState();
        _poll.Start();
    }

    private BotGoalSettings ReadValues() => new()
    {
        Levels1To19 = Row(0), Levels20To49 = Row(1), Level50 = Row(2),
    };
    private BotGoalWeights Row(int row) => new((int)_values[row, 0].Value, (int)_values[row, 1].Value, (int)_values[row, 2].Value);

    private void SetValues(BotGoalSettings settings)
    {
        _loading = true;
        BotGoalWeights[] rows = [settings.Levels1To19, settings.Levels20To49, settings.Level50];
        for (int row = 0; row < 3; row++)
        {
            _values[row, 0].Value = rows[row].SoloPve;
            _values[row, 1].Value = rows[row].GroupPve;
            _values[row, 2].Value = rows[row].RvR;
        }
        _loading = false;
    }

    private void LoadSettings()
    {
        try
        {
            _saved = BotGoalSettings.Load(_path);
            _savedFile = File.Exists(_path);
            _loadFailed = false;
            _notice = "";
            SetValues(_saved);
        }
        catch (Exception ex)
        {
            _loadFailed = true;
            SetValues(BotGoalSettings.Defaults);
            _notice = "Cannot read saved goals: " + ex.Message + " Review these values and Save to repair the settings file.";
        }
        UpdateState();
    }

    private void SaveSettings()
    {
        try
        {
            var values = ReadValues();
            values.Save(_path, _serverStopped);
            _saved = values;
            _savedFile = true;
            _loadFailed = false;
            _notice = "Saved. These goals will apply on the next server start.";
        }
        catch (Exception ex) { _notice = "Not saved: " + ex.Message; }
        UpdateState();
    }

    public void UpdateState()
    {
        bool stopped = _serverStopped();
        bool valid = true;
        for (int row = 0; row < 3; row++)
        {
            int total = Row(row).Total;
            valid &= total == 100;
            _totals[row].Text = total == 100 ? "100% — ready" : $"{total}% — needs 100%";
            _totals[row].ForeColor = total == 100 ? Color.LightGreen : Color.Salmon;
            for (int col = 0; col < 3; col++) _values[row, col].Enabled = stopped && !(row == 0 && col == 2);
        }
        _save.Enabled = stopped && valid;
        _defaults.Enabled = _undo.Enabled = stopped;
        _status.Text = !stopped ? "Locked: stop the server to change bot goals." :
            _notice.Length > 0 ? _notice : !valid ? "Not saved. Adjust each row to exactly 100%." :
            HasUnsavedChanges ? "Unsaved changes — click Save settings before starting the server." :
            _savedFile ? "Saved settings ready. Start the server when ready." :
            "Current default percentages shown. Click Save settings to enable this goal policy (including back-to-back RvR).";
        _status.ForeColor = stopped && valid && !_loadFailed ? DaocTheme.GoldLight : Color.Salmon;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _poll.Dispose();
        base.Dispose(disposing);
    }
}
