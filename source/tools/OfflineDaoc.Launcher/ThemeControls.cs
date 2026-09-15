using System.Drawing.Drawing2D;
using System.ComponentModel;
using System.Reflection;

namespace OfflineDaoc.Launcher;

internal static class DaocTheme
{
    internal static readonly Color Void = Color.FromArgb(18, 15, 12);
    internal static readonly Color Stone = Color.FromArgb(47, 43, 36);
    internal static readonly Color StoneDark = Color.FromArgb(25, 24, 21);
    internal static readonly Color StoneLight = Color.FromArgb(79, 71, 56);
    internal static readonly Color Iron = Color.FromArgb(102, 100, 90);
    internal static readonly Color Gold = Color.FromArgb(194, 157, 77);
    internal static readonly Color GoldLight = Color.FromArgb(232, 205, 135);
    internal static readonly Color Parchment = Color.FromArgb(215, 199, 157);
    internal static readonly Color Ink = Color.FromArgb(43, 32, 22);
    internal static readonly Color Panel = Color.FromArgb(35, 31, 26);
    internal static readonly Color PanelAlt = Color.FromArgb(42, 37, 30);
    internal static readonly Color Text = Color.FromArgb(224, 215, 190);
    internal static readonly Color Muted = Color.FromArgb(166, 154, 128);
    internal static readonly Color Success = Color.FromArgb(139, 191, 104);
    internal static readonly Color Danger = Color.FromArgb(232, 95, 83);
    internal static readonly Color Albion = Color.FromArgb(171, 64, 58);
    internal static readonly Color Midgard = Color.FromArgb(62, 94, 144);
    internal static readonly Color Hibernia = Color.FromArgb(57, 117, 66);
}

internal sealed class StoneSurface : Panel
{
    public StoneSurface()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        Padding = new Padding(14);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        using var background = new LinearGradientBrush(ClientRectangle, Color.FromArgb(54, 50, 42), Color.FromArgb(25, 23, 20), 90f);
        e.Graphics.FillRectangle(background, ClientRectangle);

        // Subtle deterministic banding gives the surface the compressed stone texture
        // common to small fan launchers without shipping a large modern art asset.
        for (var y = 5; y < Height; y += 7)
        {
            var shade = 33 + ((y * 17) % 13);
            using var texture = new Pen(Color.FromArgb(65, shade + 12, shade + 8, shade));
            e.Graphics.DrawLine(texture, 2, y, Width - 3, y);
        }

        using var outer = new Pen(Color.FromArgb(12, 10, 8), 2f);
        using var middle = new Pen(DaocTheme.StoneLight);
        using var inner = new Pen(Color.FromArgb(112, 91, 61));
        e.Graphics.DrawRectangle(outer, 1, 1, Math.Max(0, Width - 3), Math.Max(0, Height - 3));
        e.Graphics.DrawRectangle(middle, 3, 3, Math.Max(0, Width - 7), Math.Max(0, Height - 7));
        e.Graphics.DrawRectangle(inner, 6, 6, Math.Max(0, Width - 13), Math.Max(0, Height - 13));
    }
}

internal sealed class InsetPanel : Panel
{
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color Accent { get; init; } = DaocTheme.Gold;

    public InsetPanel()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        BackColor = DaocTheme.Panel;
        Padding = new Padding(2);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var shadow = new Pen(Color.FromArgb(10, 9, 7), 2f);
        using var edge = new Pen(DaocTheme.StoneLight);
        using var accent = new Pen(Accent);
        e.Graphics.DrawRectangle(shadow, 1, 1, Math.Max(0, Width - 3), Math.Max(0, Height - 3));
        e.Graphics.DrawLine(edge, 3, 3, Width - 4, 3);
        e.Graphics.DrawLine(accent, 3, Height - 4, Width - 4, Height - 4);
    }
}

internal sealed class RuneButton : Button
{
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool ShowOrnaments { get; set; } = true;
    private bool _hovered;
    private bool _pressed;
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color Accent { get; init; } = DaocTheme.Gold;

    public RuneButton()
    {
        DoubleBuffered = true;
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        Cursor = Cursors.Hand;
        Font = new Font("Georgia", 8.25f, FontStyle.Bold);
        ForeColor = DaocTheme.GoldLight;
        Width = 104;
        Height = 31;
        Margin = new Padding(4, 0, 0, 0);
    }

    protected override void OnMouseEnter(EventArgs e) { _hovered = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hovered = false; _pressed = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { _pressed = true; Invalidate(); base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { _pressed = false; Invalidate(); base.OnMouseUp(e); }
    protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
        var top = _pressed ? Color.FromArgb(30, 27, 23) : _hovered ? Color.FromArgb(77, 68, 52) : Color.FromArgb(58, 52, 42);
        var bottom = _pressed ? Color.FromArgb(51, 45, 36) : Color.FromArgb(27, 25, 22);
        using var fill = new LinearGradientBrush(bounds, top, bottom, 90f);
        e.Graphics.FillRectangle(fill, bounds);
        using var outer = new Pen(Color.FromArgb(12, 10, 8));
        using var inner = new Pen(Enabled ? Accent : Color.FromArgb(86, 82, 72));
        using var shine = new Pen(Color.FromArgb(112, 102, 81));
        e.Graphics.DrawRectangle(outer, bounds);
        e.Graphics.DrawRectangle(inner, 2, 2, Width - 5, Height - 5);
        e.Graphics.DrawLine(shine, 3, 3, Width - 4, 3);

        using var ornament = new SolidBrush(Enabled ? Accent : Color.FromArgb(86, 82, 72));
        Point[] leftDiamond = [new(8, Height / 2), new(11, Height / 2 - 3), new(14, Height / 2), new(11, Height / 2 + 3)];
        Point[] rightDiamond = [new(Width - 9, Height / 2), new(Width - 12, Height / 2 - 3), new(Width - 15, Height / 2), new(Width - 12, Height / 2 + 3)];
        if (ShowOrnaments)
        {
            e.Graphics.FillPolygon(ornament, leftDiamond);
            e.Graphics.FillPolygon(ornament, rightDiamond);
        }

        var offset = _pressed ? 1 : 0;
        TextRenderer.DrawText(e.Graphics, Text, Font, new Rectangle(offset, offset, Width, Height),
            Enabled ? ForeColor : Color.FromArgb(111, 106, 94), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }
}

internal sealed class FantasyBanner : Panel
{
    private static readonly Lazy<Image?> Artwork = new(() =>
    {
        using Stream? stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("OfflineDaoc.Launcher.Assets.offline-daoc-header.png");
        if (stream == null)
            return null;
        using var loaded = new Bitmap(stream);
        return new Bitmap(loaded);
    });

    public FantasyBanner()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        Padding = new Padding(4);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        e.Graphics.Clear(Color.FromArgb(18, 19, 18));
        Image? artwork = Artwork.Value;
        if (artwork != null && Width > 0 && Height > 0)
        {
            float scale = Math.Max(Width / (float)artwork.Width, Height / (float)artwork.Height);
            float sourceWidth = Width / scale;
            float sourceHeight = Height / scale;
            float sourceX = Math.Max(0, (artwork.Width - sourceWidth) / 2f);
            float sourceY = Math.Max(0, (artwork.Height - sourceHeight) / 2f);
            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            e.Graphics.DrawImage(artwork, ClientRectangle,
                sourceX, sourceY, sourceWidth, sourceHeight, GraphicsUnit.Pixel);
        }

        using var shade = new LinearGradientBrush(ClientRectangle,
            Color.FromArgb(190, 10, 9, 8), Color.FromArgb(80, 10, 9, 8), 0f);
        e.Graphics.FillRectangle(shade, ClientRectangle);
        using var lowerShade = new LinearGradientBrush(ClientRectangle,
            Color.FromArgb(5, 0, 0, 0), Color.FromArgb(105, 0, 0, 0), 90f);
        e.Graphics.FillRectangle(lowerShade, ClientRectangle);

        using var outer = new Pen(Color.FromArgb(8, 7, 5), 2f);
        using var gold = new Pen(Color.FromArgb(113, 83, 43));
        e.Graphics.DrawRectangle(outer, 1, 1, Math.Max(0, Width - 3), Math.Max(0, Height - 3));
        e.Graphics.DrawRectangle(gold, 3, 3, Math.Max(0, Width - 7), Math.Max(0, Height - 7));
    }
}

internal sealed class RealmShieldPicture : PictureBox
{
    private static readonly Lazy<Image?> Shield = new(() =>
    {
        using Stream? stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("OfflineDaoc.Launcher.Assets.offline-daoc-realm-shield.png");
        if (stream == null)
            return null;
        using var loaded = new Bitmap(stream);
        return new Bitmap(loaded);
    });

    public RealmShieldPicture()
    {
        BackColor = Color.Transparent;
        Image = Shield.Value;
        SizeMode = PictureBoxSizeMode.Zoom;
        TabStop = false;
    }
}

internal sealed class DaocTabControl : TabControl
{
    public DaocTabControl()
    {
        DrawMode = TabDrawMode.OwnerDrawFixed;
        SizeMode = TabSizeMode.Fixed;
        ItemSize = new Size(172, 29);
        Padding = new Point(8, 3);
        Font = new Font("Georgia", 9f, FontStyle.Bold);
    }

    protected override void OnDrawItem(DrawItemEventArgs e)
    {
        var selected = e.Index == SelectedIndex;
        var rectangle = GetTabRect(e.Index);
        rectangle.Inflate(-1, 0);
        using var fill = new LinearGradientBrush(rectangle,
            selected ? Color.FromArgb(79, 65, 42) : Color.FromArgb(47, 43, 36),
            selected ? Color.FromArgb(39, 32, 23) : Color.FromArgb(25, 24, 21), 90f);
        e.Graphics.FillRectangle(fill, rectangle);
        using var border = new Pen(selected ? DaocTheme.Gold : DaocTheme.Iron);
        e.Graphics.DrawRectangle(border, rectangle.X, rectangle.Y, rectangle.Width - 1, rectangle.Height - 1);
        TextRenderer.DrawText(e.Graphics, TabPages[e.Index].Text.ToUpperInvariant(), Font, rectangle,
            selected ? DaocTheme.GoldLight : DaocTheme.Muted,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }

    protected override void WndProc(ref Message message)
    {
        base.WndProc(ref message);
        if (message.Msg != 0x000F || !IsHandleCreated || TabCount == 0) return;

        using var graphics = Graphics.FromHwnd(Handle);
        var lastTab = GetTabRect(TabCount - 1);
        var headerHeight = Math.Max(lastTab.Bottom + 2, DisplayRectangle.Top);
        using var fill = new SolidBrush(DaocTheme.StoneDark);
        graphics.FillRectangle(fill, lastTab.Right, 0, Math.Max(0, Width - lastTab.Right), headerHeight);
        using var edge = new Pen(Color.FromArgb(116, 88, 48));
        graphics.DrawLine(edge, lastTab.Right, headerHeight - 1, Width, headerHeight - 1);
    }
}
