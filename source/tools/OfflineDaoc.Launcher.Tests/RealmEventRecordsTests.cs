using DOL.GS;
using System.Reflection;
using NUnit.Framework;

[TestFixture,NonParallelizable,Apartment(ApartmentState.STA)]
public class RealmEventRecordsTests
{
    private string _directory,_path;
    [SetUp]public void Setup(){_directory=Path.Combine(Path.GetTempPath(),"daoc-record-tests-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(_directory);_path=Path.Combine(_directory,"records.sqlite3");}
    [TearDown]public void Cleanup(){Directory.Delete(_directory,true);}
    private static RealmEventRecord Example(string id,string kind="Dragon",string realm="Albion",string outcome="In progress")=>
        new(id,"dragon-albion","Golestandt",kind,realm,"2026-09-14T00:00:00.0000000Z","","Rally",outcome,"Automatic rally",300,0);
    [Test]public void ReadMissingLedgerDoesNotCreateFiles()
    {Assert.That(RealmEventRecordStore.Read(_path,"","","","",0).Total,Is.Zero);Assert.That(File.Exists(_path),Is.False);}
    [Test]public void OneAttemptUpdatesButASecondAttemptIsNotLost()
    {
        RealmEventRecordStore.Save(_path,[Example("a")]);
        RealmEventRecordStore.Save(_path,[Example("a") with {Outcome="Boss defeated",EndedUtc="2026-09-14T01:00:00Z",Phase="Ended",Details="Dragon died",Present=200},Example("b")]);
        var results=RealmEventRecordStore.Read(_path,"","","","",0);
        Assert.That(results.Total,Is.EqualTo(2));Assert.That(results.Rows.Single(r=>r.Id=="a").Outcome,Is.EqualTo("Boss defeated"));
        RealmEventRecordStore.CloseInterrupted(_path);
        results=RealmEventRecordStore.Read(_path,"","","","",0);
        Assert.That(results.Rows.Single(r=>r.Id=="a").Outcome,Is.EqualTo("Boss defeated"));
        Assert.That(results.Rows.Single(r=>r.Id=="b").Outcome,Is.EqualTo("Interrupted"));
        Assert.That(results.Rows.Single(r=>r.Id=="b").EndedUtc,Is.Empty,"An unclean stop has no known exact finish time");
    }
    [Test]public void AllKindsOutcomesPagingAndLiteralSearchAreReadOnly()
    {
        var records=Enumerable.Range(0,205).Select(i=>Example(i.ToString("D4"),i%2==0?"Relic keep":"Epic dungeon",i%2==0?"Hibernia":"Midgard",i%2==0?"Captured":"Failed rally") with {Name=i==10?"100% _ ' test":"Encounter"});
        RealmEventRecordStore.Save(_path,records);var before=File.ReadAllBytes(_path);
        Assert.That(RealmEventRecordStore.Read(_path,"","","","",0).Rows.Count,Is.EqualTo(100));
        Assert.That(RealmEventRecordStore.Read(_path,"","","","",2).Rows.Count,Is.EqualTo(5));
        Assert.That(RealmEventRecordStore.Read(_path,"Hibernia","Relic keep","Captured","",0).Total,Is.EqualTo(103));
        Assert.That(RealmEventRecordStore.Read(_path,"","","","% _ '",0).Total,Is.EqualTo(1));
        Assert.That(RealmEventRecordStore.Read(_path,"","","","' OR 1=1 --",0).Total,Is.Zero);
        Assert.That(File.ReadAllBytes(_path),Is.EqualTo(before));
    }
    [Test]public async Task RecordsPanelReadsRealLedgerWithoutStartingServerOrLauncherServices()
    {
        RealmEventRecordStore.Save(_path,[Example("a",outcome:"Boss defeated"),Example("b","Relic keep","Hibernia","Captured")]);
        var type=Assembly.Load("OfflineDAoC").GetType("OfflineDaoc.Launcher.RealmEventRecordsControl")!;
        using var panel=(Control)Activator.CreateInstance(type,BindingFlags.Instance|BindingFlags.NonPublic,null,[_path],null)!;
        using var host=new Form{ClientSize=new Size(1050,500),ShowInTaskbar=false,Opacity=0,Location=new Point(-20000,-20000),StartPosition=FormStartPosition.Manual};
        host.Controls.Add(panel);host.Show();
        await (Task)type.GetMethod("RefreshAsync",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(panel,null)!;
        var grid=(DataGridView)type.GetField("_grid",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(panel)!;
        Assert.That(grid.Rows.Count,Is.EqualTo(2));Assert.That(grid.ReadOnly,Is.True);
        using var image=new Bitmap(panel.Width,panel.Height);panel.DrawToBitmap(image,new Rectangle(Point.Empty,image.Size));
        string preview=Path.Combine(TestContext.CurrentContext.WorkDirectory,"realm-event-records.png");image.Save(preview);TestContext.AddTestAttachment(preview);
    }
}
