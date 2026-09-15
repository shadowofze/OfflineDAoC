using System.Reflection;
using NUnit.Framework;
using OfflineDaoc.Configuration;

[TestFixture, NonParallelizable, Apartment(ApartmentState.STA)]
public class BotGoalsSettingsTests
{
    private string _folder;
    private Control _panel;
    private bool _stopped;
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    private T Field<T>(string name) => (T)_panel.GetType().GetField(name, Hidden)!.GetValue(_panel)!;
    private void Call(string name) => _panel.GetType().GetMethod(name, Hidden | BindingFlags.Public)!.Invoke(_panel, null);
    [SetUp] public void Setup()
    {
        _folder = Path.Combine(Path.GetTempPath(), "goal-ui-" + Guid.NewGuid().ToString("N"));
        _stopped = true;
        var type = Assembly.Load("OfflineDAoC").GetType("OfflineDaoc.Launcher.BotGoalsSettingsControl")!;
        _panel = (Control)Activator.CreateInstance(type, Path.Combine(_folder, BotGoalSettings.FileName), (Func<bool>)(() => _stopped))!;
    }
    [TearDown] public void Cleanup() { _panel.Dispose(); if (Directory.Exists(_folder)) Directory.Delete(_folder, true); }

    [Test] public void TotalsAndServerStateGateSavingAndKeepLowLevelRvrDisabled()
    {
        var values = Field<NumericUpDown[,]>("_values");
        Assert.That(values[0, 2].Enabled, Is.False);
        values[2, 0].Value = 0;
        Assert.That(Field<Button>("_save").Enabled, Is.False);
        values[2, 1].Value = 0;
        values[2, 2].Value = 100;
        Assert.That(Field<Button>("_save").Enabled, Is.True);
        _stopped = false;
        Call("UpdateState");
        Assert.That(Field<Button>("_save").Enabled, Is.False);
        Assert.That(values.Cast<NumericUpDown>().All(value => !value.Enabled), Is.True);
        Call("SaveSettings");
        Assert.That(File.Exists(Path.Combine(_folder, BotGoalSettings.FileName)), Is.False);
        _stopped = true;
        Call("SaveSettings");
        Assert.That(BotGoalSettings.Load(Path.Combine(_folder, BotGoalSettings.FileName)).Level50.RvR, Is.EqualTo(100));
        Assert.That(Field<Label>("_status").Text, Does.StartWith("Saved."));
    }

    [Test] public void UndoRestoresAllThreeSavedBrackets()
    {
        Call("SaveSettings");
        Field<NumericUpDown[,]>("_values")[1, 0].Value = 99;
        Call("LoadSettings");
        Assert.That(Field<NumericUpDown[,]>("_values")[1, 0].Value, Is.EqualTo(40));
        Assert.That(_panel.GetType().GetProperty("HasUnsavedChanges")!.GetValue(_panel), Is.False);
    }

    [Test] public void RenderPanelWithoutStartingServerOrLauncher()
    {
        using var host = new Form { ClientSize = new Size(960, 530), StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000), ShowInTaskbar = false };
        host.Controls.Add(_panel);
        host.Show();
        Application.DoEvents();
        using var bitmap = new Bitmap(host.ClientSize.Width, host.ClientSize.Height);
        _panel.DrawToBitmap(bitmap, new Rectangle(Point.Empty, host.ClientSize));
        bitmap.Save(Path.Combine(TestContext.CurrentContext.WorkDirectory, "bot-goals-preview.png"));
        Assert.That(Field<Button>("_save").Bounds.Width, Is.GreaterThan(60));
        host.Controls.Remove(_panel);
    }
}
