namespace OfflineDaoc.Launcher;

internal sealed partial class MainForm
{
    private Button _resetKeepsRelics = null!;
    private bool _resettingKeepsRelics;
    private DateTime? _keepRelicResetUtc;

    private Control BuildActiveRvrHeader()
    {
        var header = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty, BackColor = DaocTheme.Panel, Font = Font };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        header.Controls.Add(new Label { Text = "ACTIVE RVR — roaming bots and warbands · keeps and relics are in Realm Events", Dock = DockStyle.Fill,
            ForeColor = DaocTheme.GoldLight, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true }, 0, 0);
        _resetKeepsRelics = ActionButton("Reset Keeps && Relics", DaocTheme.Gold);
        _resetKeepsRelics.AccessibleName = "Reset Keeps & Relics";
        _resetKeepsRelics.AutoSize = true;
        _resetKeepsRelics.MinimumSize = new Size(220, 32);
        _resetKeepsRelics.Anchor = AnchorStyles.Right;
        _resetKeepsRelics.Enabled = false;
        _resetKeepsRelics.Click += async (_, _) => await ResetKeepsRelicsAsync();
        header.Controls.Add(_resetKeepsRelics, 1, 0);
        return header;
    }

    private async Task ResetKeepsRelicsAsync()
    {
        if (_resettingKeepsRelics || !BotGoalsServerStopped())
        {
            MessageBox.Show(this, "Stop the server completely before resetting keeps and relics.", "Server must be stopped");
            return;
        }
        if (MessageBox.Show(this, "Return all keeps to their original realms, clear keep guild claims, and return all six relics to their home shrines?\n\nCharacters, bots, inventories, coins, Realm Exchange and event records are not changed. A small backup of the keep/relic rows will be saved.",
            "Reset Keeps & Relics", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
        _resettingKeepsRelics = true;
        _resetKeepsRelics.Enabled = _startButton.Enabled = false;
        try
        {
            var result = await Task.Run(() => KeepRelicReset.Apply(_database,
                Path.Combine(_root, "data", "keep-relic-reset-backups"), BotGoalsServerStopped));
            _keepRelicResetUtc = result.UpdatedUtc;
            _rvrWorld = null; // Never display old ownership or rally attendance after a reset.
            MessageBox.Show(this, $"Reset complete: {result.Keeps} keeps and {result.Relics} relics.\n\nStart the server to load the restored ownership and refresh Realm Events.\n\nBackup: {result.Backup}", "Keeps & relics restored");
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Reset failed — no partial reset", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        finally { _resettingKeepsRelics = false; await RefreshDashboardAsync(); }
    }
}
