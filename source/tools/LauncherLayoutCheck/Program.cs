using System.Reflection;
using System.Drawing.Imaging;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        Application.SetHighDpiMode(HighDpiMode.DpiUnaware);
        var type = Assembly.Load("OfflineDAoC").GetType("OfflineDaoc.Launcher.MainForm", true)!;
        // Never show the form: its Shown handler reads the live database.
        using var form = (Form)Activator.CreateInstance(type, true)!;
        ((Label)type.GetField("_serverState", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(form)!).Text = "STARTING…";
        foreach (string field in new[] { "_albionValue", "_midgardValue", "_hiberniaValue" })
            ((Label)type.GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(form)!).Text = "3,334 ROSTER\n3,334 ONLINE";
        var cards = Descendants(form).OfType<TableLayoutPanel>().Single(p => p.ColumnCount == 6);
        cards.Parent.Controls.Remove(cards);
        cards.Dock = DockStyle.None;
        foreach (int width in new[] { 960, 1100, 1440 })
        {
            cards.Size = new Size(width - 48, 110);
            cards.CreateControl();
            Layout(cards);
            using var bitmap = new Bitmap(cards.Width, cards.Height);
            cards.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
            foreach (var button in Descendants(cards).OfType<Button>().Where(b => b.Text.StartsWith("+ Lv.")))
            {
                if (!button.Parent!.ClientRectangle.Contains(button.Bounds))
                    throw new Exception($"Clipped button at {width}: {button.Text} {button.Bounds}");
                Control split = button.Parent;
                if (!split.Parent!.ClientRectangle.Contains(split.Bounds))
                    throw new Exception($"Clipped generation row at {width}: {split.Bounds}");
                var textSize = TextRenderer.MeasureText(button.Text, button.Font);
                if (textSize.Width > button.Width - 6 || textSize.Height > button.Height - 4)
                    throw new Exception($"Button text does not fit at {width}: {button.Text} {button.Size}");
            }
            bitmap.Save(Path.Combine(AppContext.BaseDirectory, $"launcher-{width}.png"), ImageFormat.Png);
            Console.WriteLine($"PASS {width}: all six generation buttons fit; rendered launcher-{width}.png");
        }
        using var rvrForm = (Form)Activator.CreateInstance(type, true)!;
        var hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        var snapshotType = type.GetNestedType("RvrWorldSnapshot", BindingFlags.NonPublic)!;
        string json = """
            {"UpdatedUtc":"2026-09-04T15:00:00Z","Running":true,"Objectives":[
              {"Kind":"Keep","Name":"Dun Crimthainn","Owner":"Hibernia","State":"SIEGE — under attack","Location":"Hibernia","Carrier":"","Forces":"Albion: 80 · Hibernia: 64 · third realm: 48 · cap 128/realm"},
              {"Kind":"Relic","Name":"Thor's Hammer","Owner":"In transit","State":"ESCORT / INTERCEPTION","Location":"Odin's Gate","Carrier":"Test carrier","Forces":"Original realm: Midgard · Strength"},
              {"Kind":"Relic keep","Name":"Grallarhorn Faste","Owner":"Midgard","State":"Relic siege rally","Location":"Midgard","Carrier":"","Forces":"Albion: 96 · Midgard: 64 · third realm: 40 · cap 192/realm"},
              {"Kind":"Keep","Name":"Caer Benowyc","Owner":"Albion","State":"Secure","Location":"Albion","Carrier":"","Forces":"No organized rally"}]}
            """;
        type.GetField("_rvrWorld", hidden)!.SetValue(rvrForm, System.Text.Json.JsonSerializer.Deserialize(json, snapshotType));
        type.GetField("_rvrServerRunning", hidden)!.SetValue(rvrForm, true);
        type.GetMethod("RenderActiveRvr", hidden)!.Invoke(rvrForm, null);
        var grid = (DataGridView)type.GetField("_rvrObjectivesGrid", hidden)!.GetValue(rvrForm)!;
        Control panel = grid.Parent!;
        panel.Parent!.Controls.Remove(panel);
        panel.Dock = DockStyle.None;
        panel.Size = new Size(1020, 390);
        panel.CreateControl();
        Layout(panel);
        using var rvrBitmap = new Bitmap(panel.Width, panel.Height);
        panel.DrawToBitmap(rvrBitmap, new Rectangle(Point.Empty, panel.Size));
        rvrBitmap.Save(Path.Combine(AppContext.BaseDirectory, "rvr-panel.png"), ImageFormat.Png);
        Console.WriteLine("PASS: rendered Active RvR mock data without opening the launcher or reading the database");
    }
    static IEnumerable<Control> Descendants(Control root)
    {
        foreach (Control child in root.Controls)
        {
            yield return child;
            foreach (Control nested in Descendants(child)) yield return nested;
        }
    }
    static void Layout(Control root)
    {
        root.PerformLayout();
        foreach (Control child in root.Controls) Layout(child);
    }
}
