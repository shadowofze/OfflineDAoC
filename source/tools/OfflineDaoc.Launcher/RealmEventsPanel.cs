using System.Text.Json;

namespace OfflineDaoc.Launcher;

internal sealed partial class MainForm
{
    private readonly ComboBox _eventRealm = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 115 };
    private readonly ComboBox _eventKind = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 125 };
    private readonly ComboBox _eventActingRealm = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 115 };
    private readonly TextBox _eventSearch = new() { Width = 200, PlaceholderText = "Search objective, zone or bot" };
    private readonly CheckBox _eventActiveOnly = new() { Text = "Active Events", AutoSize = true, ForeColor = DaocTheme.GoldLight, Margin = new Padding(6, 5, 3, 3) };
    private readonly DataGridView _eventMembers = new();
    private readonly Label _eventCommandStatus = new() { Dock = DockStyle.Fill, ForeColor = DaocTheme.GoldLight, AutoEllipsis = true };
    private Button? _eventStart;
    private Button? _eventReset;
    private bool _eventRequestPending;
    private string _eventSort = "", _eventMemberSort = "";
    private bool _eventAscending = true, _eventMemberAscending = true;

    private Control BuildRealmEventsPanel()
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, Padding = new Padding(7) };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
        panel.Controls.Add(_rvrUpdated, 0, 0);
        var filters = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = true };
        _eventRealm.Items.AddRange(["All realms", "Albion", "Midgard", "Hibernia"]);
        _eventKind.Items.AddRange(["All events", "Keep", "Relic keep", "Relic", "Dragon", "Epic dungeon"]);
        _eventActingRealm.Items.AddRange(["Albion", "Midgard", "Hibernia"]);
        _eventRealm.SelectedIndex = _eventKind.SelectedIndex = _eventActingRealm.SelectedIndex = 0;
        filters.Controls.AddRange([_eventRealm, _eventKind, _eventSearch, _eventActiveOnly,
            new Label { Text = "Select an event to inspect its assigned bots below", AutoSize = true, ForeColor = DaocTheme.GoldLight, Padding = new Padding(3, 6, 0, 0) }]);
        _eventRealm.SelectedIndexChanged += (_, _) => RenderRealmEvents();
        _eventKind.SelectedIndexChanged += (_, _) => RenderRealmEvents();
        _eventSearch.TextChanged += (_, _) => RenderRealmEvents();
        _eventActiveOnly.CheckedChanged += (_, _) => RenderRealmEvents();
        panel.Controls.Add(filters, 0, 1);
        _rvrObjectivesGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _rvrObjectivesGrid.MultiSelect = false;
        _rvrObjectivesGrid.Columns[1].HeaderText = "Objective";
        _rvrObjectivesGrid.Columns.Add(TextColumn("Cooldown", "CooldownMilliseconds", 90));
        _rvrObjectivesGrid.Columns[^1].DisplayIndex = 4;
        _rvrObjectivesGrid.Columns.Add(TextColumn("Stage / battle", "PhaseRemainingMilliseconds", 125));
        _rvrObjectivesGrid.Columns[^1].DisplayIndex = 4;
        // Keep readiness visible at the default width. Relic carriers remain
        // available at the right; they must not push cooldowns off-screen.
        foreach (DataGridViewColumn column in _rvrObjectivesGrid.Columns)
        {
            switch (column.DataPropertyName)
            {
                case "Marker": column.Width = 95; break;
                case "Name": column.Width = 155; break;
                case "Owner": column.Width = 80; column.HeaderText = "Realm"; break;
                case "State": column.Width = 170; break;
                case "Carrier": column.Width = 95; column.DisplayIndex = 8; break;
                case "Location": column.Width = 105; break;
                case "Forces": column.MinimumWidth = 190; break;
            }
        }
        _rvrObjectivesGrid.CellFormatting += (_, e) =>
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && _rvrObjectivesGrid.Columns[e.ColumnIndex].DataPropertyName == "PhaseRemainingMilliseconds" &&
                _rvrObjectivesGrid.Rows[e.RowIndex].DataBoundItem is RvrObjective timed)
            {
                long elapsed = _rvrServerRunning && _rvrWorld?.Running == true
                    ? (long)Math.Max(0, (DateTime.UtcNow - _rvrWorld.UpdatedUtc.ToUniversalTime()).TotalMilliseconds) : 0;
                e.Value = FormatEventTimer(timed.Phase, Math.Max(0, timed.PhaseRemainingMilliseconds - elapsed));
                e.FormattingApplied = true;
            }
            if (e.ColumnIndex >= 0 && _rvrObjectivesGrid.Columns[e.ColumnIndex].DataPropertyName == "CooldownMilliseconds" && e.Value is long milliseconds)
            {
                var objective = _rvrObjectivesGrid.Rows[e.RowIndex].DataBoundItem as RvrObjective;
                e.Value = objective?.IsCatalogOnly == true ? "Unknown" : milliseconds > 0 ? $"{Math.Ceiling(milliseconds / 60000d)} min" :
                    objective?.State == "Encounter unavailable" ? "Not ready" :
                    objective?.Kind == "Relic" || objective?.State.StartsWith("RAID") == true ||
                    objective?.State.StartsWith("SIEGE") == true || objective?.State.Contains("rally", StringComparison.OrdinalIgnoreCase) == true ? "—" : "Ready";
                e.FormattingApplied = true;
            }
        };
        // SelectionChanged can fire before CurrentRow advances. Listen after
        // the current cell changes so clicking a new event never shows the
        // previous event's participant roster or sends controls to it.
        _rvrObjectivesGrid.CurrentCellChanged += (_, _) => RenderEventParticipants();
        panel.Controls.Add(_rvrObjectivesGrid, 0, 2);
        var controls = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, RowCount = 2, ColumnCount = 1, Margin = Padding.Empty };
        controls.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        controls.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        controls.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = false, Margin = Padding.Empty };
        _eventCommandStatus.AutoSize = true;
        _eventCommandStatus.MinimumSize = new Size(0, Font.Height + 6);
        _eventStart = ActionButton("START EVENT", DaocTheme.Gold);
        _eventReset = ActionButton("RESET COOLDOWN…", DaocTheme.Gold);
        _eventStart.Width = 145;
        _eventReset.Width = 175;
        actions.Controls.AddRange([new Label { Text = "Acting realm:", AutoSize = true, ForeColor = DaocTheme.GoldLight, Padding = new Padding(0, 6, 0, 0) }, _eventActingRealm, _eventStart, _eventReset]);
        _eventStart.Click += async (_, _) => await SendEventCommandAsync("start");
        _eventReset.Click += async (_, _) => await SendEventCommandAsync("reset-cooldown");
        controls.Controls.Add(actions, 0, 0);
        controls.Controls.Add(_eventCommandStatus, 0, 1);
        panel.Controls.Add(controls, 0, 3);
        _eventMembers.Dock = DockStyle.Fill;
        _eventMembers.ReadOnly = true;
        _eventMembers.AllowUserToAddRows = false;
        _eventMembers.AllowUserToDeleteRows = false;
        _eventMembers.AutoGenerateColumns = false;
        _eventMembers.RowHeadersVisible = false;
        _eventMembers.BackgroundColor = DaocTheme.StoneDark;
        _eventMembers.EnableHeadersVisualStyles = false;
        _eventMembers.ColumnHeadersDefaultCellStyle = _rvrGrid.ColumnHeadersDefaultCellStyle.Clone();
        _eventMembers.DefaultCellStyle = _rvrGrid.DefaultCellStyle.Clone();
        foreach (var c in new[] { ("Name", "Name", 130), ("Realm", "Realm", 80), ("Group", "GroupId", 130), ("Zone", "Location", 140), ("X", "X", 70), ("Y", "Y", 70), ("Z", "Z", 65) })
            _eventMembers.Columns.Add(TextColumn(c.Item1, c.Item2, c.Item3));
        _eventMembers.Columns.Add(TextColumn("Current activity", "Activity", 260, DataGridViewAutoSizeColumnMode.Fill));
        foreach (DataGridView grid in new[] { _rvrObjectivesGrid, _eventMembers })
        {
            foreach (DataGridViewColumn column in grid.Columns) column.SortMode = DataGridViewColumnSortMode.Programmatic;
            grid.ColumnHeaderMouseClick += (_, e) =>
            {
                string property = grid.Columns[e.ColumnIndex].DataPropertyName;
                if (grid == _eventMembers)
                { _eventMemberAscending = property != _eventMemberSort || !_eventMemberAscending; _eventMemberSort = property; RenderEventParticipants(); }
                else
                { _eventAscending = property != _eventSort || !_eventAscending; _eventSort = property; RenderRealmEvents(); }
            };
        }
        panel.Controls.Add(_eventMembers, 0, 4);
        return panel;
    }

    // The event catalogue must not disappear merely because the last saved
    // snapshot predates PvE raids, or because no server has been started yet.
    // These are selectable descriptions only, never fabricated live readiness.
    private IEnumerable<RvrObjective> EventObjectives()
    {
        var current = _rvrWorld?.Objectives ?? [];
        foreach (var row in current) yield return row;
        foreach (var (id, name, realm, kind) in new[]
        {
            ("dragon-albion", "Golestandt", "Albion", "Dragon"),
            ("dragon-midgard", "Gjalpinulva", "Midgard", "Dragon"),
            ("dragon-hibernia", "Cuuldurach", "Hibernia", "Dragon"),
            ("epic-albion", "Caer Sidi", "Albion", "Epic dungeon"),
            ("epic-midgard", "Tuscaran Glacier", "Midgard", "Epic dungeon"),
            ("epic-hibernia", "Galladoria", "Hibernia", "Epic dungeon"),
        })
        {
            if (current.Any(row => row.Id == id || row.Kind == kind && row.Name == name)) continue;
            yield return new RvrObjective(kind, name, realm, "Awaiting server snapshot", name, "",
                "Start the updated server and refresh for live readiness and cooldown", id, 0, true);
        }
    }

    private void RenderRealmEvents()
    {
        string? selected = (_rvrObjectivesGrid.CurrentRow?.DataBoundItem as RvrObjective)?.Id;
        string realm = _eventRealm.SelectedItem?.ToString() ?? "All realms";
        string kind = _eventKind.SelectedItem?.ToString() ?? "All events";
        string search = _eventSearch.Text.Trim();
        var matches = _rvrWorld?.Participants?.Where(p => p.Name.Contains(search, StringComparison.OrdinalIgnoreCase)).Select(p => p.EventId).ToHashSet() ?? [];
        var rows = EventObjectives().Where(o => !o.IsProtectedPortal &&
            (!_eventActiveOnly.Checked || o.IsActiveEvent) &&
            (realm == "All realms" || o.Owner == realm || o.Forces.Contains(realm, StringComparison.OrdinalIgnoreCase)) &&
            (kind == "All events" || o.Kind == kind) &&
            (search.Length == 0 || $"{o.Name} {o.Location} {o.State}".Contains(search, StringComparison.OrdinalIgnoreCase) || matches.Contains(o.Id)))
            .OrderByDescending(o => o.State.StartsWith("RAID") || o.State.StartsWith("SIEGE") || o.State.StartsWith("ESCORT") || o.State.StartsWith("DROPPED"))
            .ThenBy(o => o.Owner).ThenBy(o => o.Kind).ThenBy(o => o.Name).ToList();
        _rvrObjectivesGrid.DataSource = SortEventRows(rows, _eventSort, _eventAscending);
        SetEventSortGlyph(_rvrObjectivesGrid, _eventSort, _eventAscending);
        if (!string.IsNullOrEmpty(selected))
            foreach (DataGridViewRow row in _rvrObjectivesGrid.Rows)
                if (row.DataBoundItem is RvrObjective o && o.Id == selected) { _rvrObjectivesGrid.CurrentCell = row.Cells[0]; break; }
        RenderEventParticipants();
    }

    private void RenderEventParticipants()
    {
        var selected = _rvrObjectivesGrid.CurrentRow?.DataBoundItem as RvrObjective;
        _eventMembers.DataSource = SortEventRows(_rvrWorld?.Participants?.Where(p => p.EventId == selected?.Id)
            .OrderBy(p => p.Realm).ThenBy(p => p.GroupId).ThenBy(p => p.Name), _eventMemberSort, _eventMemberAscending);
        SetEventSortGlyph(_eventMembers, _eventMemberSort, _eventMemberAscending);
        bool enabled = !_eventRequestPending && _rvrServerRunning && selected?.IsCatalogOnly == false && selected.Kind != "Relic" && !string.IsNullOrEmpty(selected.Id);
        if (_eventStart != null) _eventStart.Enabled = enabled;
        if (_eventReset != null) _eventReset.Enabled = enabled;
    }

    private sealed record EventCommandResult(string Id, bool Success, string Message, DateTime UpdatedUtc);

    private static string FormatEventTimer(string phase, long milliseconds) => phase == "Waiting" || phase == "Staging" && milliseconds <= 0 ? "Waiting" :
        string.IsNullOrEmpty(phase) ? "—" : $"{phase} {Math.Max(0, milliseconds) / 60000}:{Math.Max(0, milliseconds) / 1000 % 60:00}";

    private static List<T>? SortEventRows<T>(IEnumerable<T>? rows, string property, bool ascending)
    {
        if (rows == null) return null;
        var field = typeof(T).GetProperty(property);
        if (field == null) return rows.ToList();
        return (ascending ? rows.OrderBy(row => field.GetValue(row)) : rows.OrderByDescending(row => field.GetValue(row))).ToList();
    }

    private static void SetEventSortGlyph(DataGridView grid, string property, bool ascending)
    {
        foreach (DataGridViewColumn column in grid.Columns)
            column.HeaderCell.SortGlyphDirection = column.DataPropertyName == property ? ascending ? SortOrder.Ascending : SortOrder.Descending : SortOrder.None;
    }
    private async Task SendEventCommandAsync(string action)
    {
        if (_eventRequestPending || !_rvrServerRunning || _rvrObjectivesGrid.CurrentRow?.DataBoundItem is not RvrObjective target || target.IsCatalogOnly || string.IsNullOrEmpty(target.Id)) return;
        if (action == "start" && target.Kind is "Dragon" or "Epic dungeon" && MessageBox.Show(this,
            "Force this expedition? This requires 300 available level-50 autonomous bots and reassigns their ordinary tasks. Bots already assigned or reserved for another expedition or siege are protected. Departure requires 200 individually present bots; late arrivals join directly.\n\n" +
            "Everyone travels to a safe service hub together, then formed parties advance to the encounter. Preparation lasts at least 45 minutes. At least 200 must arrive, and a dragon must land, before attacking. It fails safely after 90 minutes if those requirements cannot be met.\n\n" +
            "A forced expedition may run alongside another event in this realm, but cannot duplicate the same encounter. This explicit order can override the Bot Goals Setting percentages for its participants. Players, companions and mixed-level parties are excluded.",
            "Force level-50 realm expedition", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        if (action == "reset-cooldown" && MessageBox.Show(this,
                "Reset this event's cooldown? Monsters might not have respawned yet. This does not respawn monsters or reset an active battle.",
                "Reset event cooldown", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        _eventRequestPending = true;
        RenderEventParticipants();
        string id = Guid.NewGuid().ToString();
        _eventCommandStatus.Text = "Sending request…";
        try
        {
            string requestPath = Path.Combine(_serverDirectory, "realm-events.request.json");
            string json = JsonSerializer.Serialize(new { Id = id, Action = action, TargetId = target.Id,
                Realm = _eventActingRealm.SelectedIndex + 1, CreatedUtc = DateTime.UtcNow, Confirmed = action == "reset-cooldown" });
            await File.WriteAllTextAsync(requestPath + ".tmp", json);
            File.Move(requestPath + ".tmp", requestPath, true);
            for (int attempt = 0; attempt < 40; attempt++)
            {
                await Task.Delay(250);
                if (IsDisposed) return;
                string resultPath = Path.Combine(_serverDirectory, "realm-events.result.json");
                try
                {
                    if (!File.Exists(resultPath) || new FileInfo(resultPath).Length > 8192) continue;
                    var result = JsonSerializer.Deserialize<EventCommandResult>(await File.ReadAllTextAsync(resultPath));
                    if (result?.Id != id) continue;
                    _eventCommandStatus.Text = (result.Success ? "Applied: " : "Not applied: ") + result.Message;
                    _nextRvrSnapshotRefreshUtc = DateTime.MinValue;
                    RefreshRvrSnapshotIfDue();
                    return;
                }
                catch (IOException) { }
                catch (JsonException) { }
            }
            _eventCommandStatus.Text = "No acknowledgment yet. The request may still be processed; refresh before retrying.";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        { _eventCommandStatus.Text = "Could not send request: " + exception.Message; }
        finally { _eventRequestPending = false; if (!IsDisposed) RenderEventParticipants(); }
    }
}
