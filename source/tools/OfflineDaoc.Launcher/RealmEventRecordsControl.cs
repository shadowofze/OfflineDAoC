using DOL.GS;
using System.Globalization;

namespace OfflineDaoc.Launcher;

internal sealed class RealmEventRecordsControl : UserControl
{
    private readonly string _path;
    private readonly ComboBox _realm=new(){DropDownStyle=ComboBoxStyle.DropDownList,Width=110};
    private readonly ComboBox _kind=new(){DropDownStyle=ComboBoxStyle.DropDownList,Width=125};
    private readonly ComboBox _outcome=new(){DropDownStyle=ComboBoxStyle.DropDownList,Width=165};
    private readonly TextBox _search=new(){Width=200,PlaceholderText="Search event or outcome details"};
    private readonly Button _refresh=new(){Text="REFRESH",AutoSize=true},_previous=new(){Text="Previous",AutoSize=true},_next=new(){Text="Next",AutoSize=true};
    private readonly DataGridView _grid=new();
    private readonly Label _status=new(){AutoSize=true,ForeColor=DaocTheme.GoldLight};
    private readonly TextBox _details=new(){ReadOnly=true,Multiline=true,Dock=DockStyle.Fill,ScrollBars=ScrollBars.Vertical,BackColor=DaocTheme.StoneDark,ForeColor=DaocTheme.Text};
    private readonly System.Windows.Forms.Timer _debounce=new(){Interval=300};
    private int _page,_request;
    internal RealmEventRecordsControl(string path)
    {
        _path=path;Dock=DockStyle.Fill;BackColor=DaocTheme.Panel;ForeColor=DaocTheme.Text;Font=new Font("Georgia",9);
        var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=4,Padding=new Padding(8)};
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute,70));layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var toolbar=new FlowLayoutPanel{Dock=DockStyle.Fill,AutoSize=true,WrapContents=true};
        _realm.Items.AddRange(["All realms","Albion","Midgard","Hibernia"]);
        _kind.Items.AddRange(["All events","Dragon","Epic dungeon","Keep","Relic keep","Relic"]);
        _outcome.Items.AddRange(["All outcomes","In progress","Boss defeated","Failed rally","Timed out","Defended (timeout)","Captured","Captured / returned","Relic taken","Interrupted","Ended (unconfirmed)","Encounter unavailable"]);
        _realm.SelectedIndex=_kind.SelectedIndex=_outcome.SelectedIndex=0;
        toolbar.Controls.AddRange([_realm,_kind,_outcome,_search,_refresh]);layout.Controls.Add(toolbar,0,0);
        _grid.Dock=DockStyle.Fill;_grid.ReadOnly=true;_grid.AllowUserToAddRows=false;_grid.AllowUserToDeleteRows=false;
        _grid.AutoGenerateColumns=false;_grid.RowHeadersVisible=false;_grid.MultiSelect=false;_grid.SelectionMode=DataGridViewSelectionMode.FullRowSelect;
        _grid.BackgroundColor=DaocTheme.StoneDark;_grid.EnableHeadersVisualStyles=false;
        _grid.ColumnHeadersDefaultCellStyle=new(){BackColor=DaocTheme.Panel,ForeColor=DaocTheme.GoldLight};
        _grid.DefaultCellStyle=new(){BackColor=DaocTheme.StoneDark,ForeColor=DaocTheme.Text,SelectionBackColor=Color.FromArgb(90,65,32),SelectionForeColor=Color.White};
        foreach(var (title,property,width) in new[]{("Started (local)","Started",150),("Ended (local)","Ended",150),("Type","Kind",105),("Event","Name",170),("Initiating realm","Realm",115),("Outcome","Outcome",160)})
            _grid.Columns.Add(new DataGridViewTextBoxColumn{HeaderText=title,DataPropertyName=property,Width=width,SortMode=DataGridViewColumnSortMode.NotSortable});
        _grid.Columns[3].AutoSizeMode=DataGridViewAutoSizeColumnMode.Fill;_grid.Columns[3].MinimumWidth=130;
        _grid.SelectionChanged+=(_,_)=>_details.Text=_grid.CurrentRow?.DataBoundItem is Row row ? row.Record.Details+"\r\nEvent: "+row.Record.EventId+" | Record: "+row.Record.Id : "";
        layout.Controls.Add(_grid,0,1);layout.Controls.Add(_details,0,2);
        var footer=new FlowLayoutPanel{Dock=DockStyle.Fill,AutoSize=true,WrapContents=true};footer.Controls.AddRange([_previous,_next,_status]);layout.Controls.Add(footer,0,3);Controls.Add(layout);
        foreach(var filter in new[]{_realm,_kind,_outcome})filter.SelectedIndexChanged+=async(_,_)=>{_page=0;await RefreshAsync();};
        _search.TextChanged+=(_,_)=>{_debounce.Stop();_debounce.Start();};
        _debounce.Tick+=async(_,_)=>{_debounce.Stop();_page=0;await RefreshAsync();};
        _refresh.Click+=async(_,_)=>await RefreshAsync();
        _previous.Click+=async(_,_)=>{_page=Math.Max(0,_page-1);await RefreshAsync();};
        _next.Click+=async(_,_)=>{_page++;await RefreshAsync();};
        _status.Text="Event history is saved separately from accounts and the Realm Exchange. Refresh to read records.";
        _previous.Enabled=_next.Enabled=false;
    }
    private static string Filter(ComboBox box)=>box.SelectedIndex<=0?"":box.SelectedItem?.ToString()??"";
    internal async Task RefreshAsync()
    {
        int request=++_request;string realm=Filter(_realm),kind=Filter(_kind),outcome=Filter(_outcome),search=_search.Text.Trim();int page=_page;
        _refresh.Enabled=_next.Enabled=_previous.Enabled=false;_status.Text="Reading event records…";
        try
        {
            var result=await Task.Run(()=>RealmEventRecordStore.Read(_path,realm,kind,outcome,search,page));
            if(IsDisposed||request!=_request)return;
            if(page>0&&result.Rows.Count==0){_page=0;await RefreshAsync();return;}
            _grid.DataSource=result.Rows.Select(r=>new Row(r)).ToList();
            _details.Text=result.Rows.Count==0?"No matching records. Older events may have incomplete evidence; interrupted is not a defeat.":result.Rows[0].Details;
            _previous.Enabled=page>0;_next.Enabled=(page+1)*100<result.Total;
            _status.Text=$"{result.Total:N0} matching records · Page {page+1} · newest first · saved across restarts";
        }
        catch(Exception ex) when(ex is System.Data.SQLite.SQLiteException or IOException or UnauthorizedAccessException)
        {if(!IsDisposed&&request==_request)_status.Text="Could not read records: "+ex.Message;}
        finally{if(!IsDisposed&&request==_request)_refresh.Enabled=true;}
    }
    protected override void Dispose(bool disposing){if(disposing){_request++;_debounce.Dispose();}base.Dispose(disposing);}
    private sealed record Row(RealmEventRecord Record)
    {
        private static string Local(string value)=>DateTimeOffset.TryParse(value,CultureInfo.InvariantCulture,DateTimeStyles.AssumeUniversal,out var time)?time.ToLocalTime().ToString("g"):"Unknown / not ended";
        public string Started=>Local(Record.StartedUtc);public string Ended=>Local(Record.EndedUtc);
        public string Name=>Record.Name;public string Kind=>Record.Kind;public string Realm=>Record.Realm;public string Outcome=>Record.Outcome;
    }
}
