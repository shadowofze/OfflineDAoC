using System.Data.SQLite;
using System.Globalization;

namespace OfflineDaoc.Launcher;

/// <summary>On-demand, read-only view; no polling or work added to bot turns.</summary>
internal sealed class ExchangeSalesForm : Form
{
    private readonly string _database;
    private readonly DataGridView _grid = new();
    private readonly ComboBox _realm = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 135 };
    private readonly Label _status = new() { Dock = DockStyle.Fill, ForeColor = DaocTheme.GoldLight, TextAlign = ContentAlignment.MiddleLeft };
    private readonly Button _refresh = new() { Text = "REFRESH", AutoSize = true, BackColor = DaocTheme.Panel, ForeColor = DaocTheme.GoldLight };
    private List<SaleRow> _rows = [];
    private bool _loading;

    public ExchangeSalesForm(string database)
    {
        _database = database;
        Text = "Realm Exchange — 50 Most Recently Sold";
        Size = new Size(1150, 600);
        MinimumSize = new Size(850, 400);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = DaocTheme.Panel;
        ForeColor = DaocTheme.GoldLight;
        Font = new Font("Georgia", 9f);
        AutoScaleMode = AutoScaleMode.Dpi;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(10) };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Fill };
        _realm.Items.AddRange(["All realms", "Albion", "Midgard", "Hibernia"]);
        _realm.SelectedIndex = 0;
        _realm.SelectedIndexChanged += (_, _) => BindRows();
        _refresh.Click += async (_, _) => await RefreshAsync();
        toolbar.Controls.Add(_realm);
        toolbar.Controls.Add(_refresh);
        toolbar.Controls.Add(new Label { Text = "Completed purchases only • newest first • saved across restarts", AutoSize = true, Margin = new Padding(12, 7, 0, 0) });
        layout.Controls.Add(toolbar, 0, 0);
        _grid.Dock = DockStyle.Fill;
        _grid.ReadOnly = true;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.AllowUserToResizeRows = false;
        _grid.AutoGenerateColumns = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.RowHeadersVisible = false;
        _grid.BackgroundColor = Color.FromArgb(205, 187, 144);
        _grid.EnableHeadersVisualStyles = false;
        _grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(80, 59, 35), ForeColor = DaocTheme.GoldLight, Font = new Font("Georgia", 9f, FontStyle.Bold) };
        _grid.DefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(215, 199, 157), ForeColor = DaocTheme.Ink, SelectionBackColor = Color.FromArgb(117, 85, 45), SelectionForeColor = Color.White, Padding = new Padding(2) };
        _grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(201, 182, 138) };
        _grid.RowTemplate.Height = 27;
        AddColumn("Sold (local time)", nameof(SaleRow.Sold), 155);
        AddColumn("Realm", nameof(SaleRow.Realm), 85);
        AddColumn("Item", nameof(SaleRow.ItemName), 210);
        AddColumn("Qty", nameof(SaleRow.Quantity), 45);
        AddColumn("Seller", nameof(SaleRow.Seller), 180);
        AddColumn("Buyer", nameof(SaleRow.Buyer), 180);
        AddColumn("Total paid", nameof(SaleRow.Price), 125);
        _grid.Columns[2].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        _grid.Columns[2].MinimumWidth = 170;
        layout.Controls.Add(_grid, 0, 1);
        layout.Controls.Add(_status, 0, 2);
        Controls.Add(layout);
        Shown += async (_, _) => await RefreshAsync();
    }

    private void AddColumn(string title, string property, int width) => _grid.Columns.Add(new DataGridViewTextBoxColumn
    {
        HeaderText = title, DataPropertyName = property, Width = width, SortMode = DataGridViewColumnSortMode.NotSortable,
    });

    private async Task RefreshAsync()
    {
        if (_loading) return;
        _loading = true;
        _refresh.Enabled = false;
        _status.Text = "Reading completed sales…";
        try
        {
            List<SaleRow> rows = await Task.Run(ReadSales);
            if (IsDisposed) return;
            _rows = rows;
            BindRows();
        }
        catch (Exception error)
        {
            if (!IsDisposed) _status.Text = "Could not refresh sale history: " + error.Message;
        }
        finally
        {
            _loading = false;
            if (!IsDisposed) _refresh.Enabled = true;
        }
    }

    private List<SaleRow> ReadSales()
    {
        if (!File.Exists(_database)) return [];
        using var connection = new SQLiteConnection($"Data Source={_database};Version=3;Read Only=True;Pooling=False;Default Timeout=5");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='realm_exchange_sales'";
        if (Convert.ToInt32(command.ExecuteScalar()) == 0) return [];
        command.CommandText = """
            SELECT SoldUtc, Realm, ItemName, Quantity, SellerId, SellerName, BuyerId, BuyerName, PriceCopper
            FROM realm_exchange_sales WHERE SaleSequence > 0
            ORDER BY SaleSequence DESC LIMIT 50
            """;
        using var reader = command.ExecuteReader();
        List<SaleRow> rows = [];
        while (reader.Read())
        {
            string sold = DateTime.TryParse(reader.GetString(0), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime utc)
                ? utc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") : reader.GetString(0);
            string realm = reader.GetInt32(1) switch { 1 => "Albion", 2 => "Midgard", 3 => "Hibernia", _ => "Unknown" };
            rows.Add(new SaleRow(sold, realm, reader.GetString(2), reader.GetInt32(3),
                Participant(reader.GetString(4), reader.GetString(5)), Participant(reader.GetString(6), reader.GetString(7)),
                MainForm.FormatCopper(reader.GetInt64(8))));
        }
        return rows;
    }

    private void BindRows()
    {
        string selected = _realm.SelectedItem?.ToString() ?? "All realms";
        var visible = _rows.Where(row => selected == "All realms" || row.Realm == selected).ToList();
        _grid.DataSource = visible;
        _status.Text = _rows.Count == 0
            ? "No completed sales recorded yet. Tracking begins with the updated server; earlier trades cannot be reconstructed."
            : $"Showing {visible.Count} of the latest {_rows.Count} recorded sales across all realms. Refresh reads history only; no automatic polling.";
    }

    private static string Participant(string id, string name) => name + (id.StartsWith("offlinebot:", StringComparison.OrdinalIgnoreCase) ? " (bot)" : " (player)");

    private sealed record SaleRow(string Sold, string Realm, string ItemName, int Quantity, string Seller, string Buyer, string Price);
}
