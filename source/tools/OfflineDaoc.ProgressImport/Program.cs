namespace OfflineDaoc.ProgressImport;

internal static class Program
{
    [STAThread]
    static int Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        if(args.Length>=4 && args[0]=="--import" && args[3]=="--replace-progress")
        {
            string report=args.Length>4?args[4]:Path.Combine(args[2],"import-test-result.txt");
            try { var lines=new List<string>();string backup=ImportEngine.Import(args[1],args[2],lines.Add);lines.Add("SUCCESS "+backup);File.WriteAllLines(report,lines);return 0; }
            catch(Exception e){File.WriteAllText(report,e.ToString());return 1;}
        }
        string root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"..",".."));
        using var form=new ImportForm(root);
        if(args.Length==2 && args[0]=="--render-test")
        {
            form.Show();Application.DoEvents();using var image=new Bitmap(form.Width,form.Height);form.DrawToBitmap(image,new Rectangle(Point.Empty,form.Size));image.Save(args[1]);form.Close();return 0;
        }
        Application.Run(form);return 0;
    }
}

public sealed class ImportForm : Form
{
    readonly TextBox source=new(){Dock=DockStyle.Fill,ReadOnly=true};
    readonly Button browse=new(){Text="Choose OLD folder…",AutoSize=true};
    readonly Button transfer=new(){Text="IMPORT PROGRESS",AutoSize=true,Enabled=false};
    readonly Label summary=new(){AutoSize=true,Text="Choose your old Offline DAoC folder to inspect its saved progress."};
    readonly Label status=new(){AutoSize=true,Text="Nothing has been changed."};
    readonly ProgressBar bar=new(){Dock=DockStyle.Fill,Style=ProgressBarStyle.Continuous};
    readonly string destination;
    public ImportForm(string root)
    {
        destination=root;Text="Offline DAoC 0.2 — Transfer saved progress";ClientSize=new(780,445);MinimumSize=new(700,460);
        StartPosition=FormStartPosition.CenterScreen;BackColor=Color.FromArgb(31,29,24);ForeColor=Color.Wheat;
        Font=new Font("Segoe UI",10);AutoScaleMode=AutoScaleMode.Dpi;
        var layout=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(22),ColumnCount=1,RowCount=9};
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        foreach(int height in new[]{42,55,48,60,55,26,45,28,30})layout.RowStyles.Add(new RowStyle(SizeType.Absolute,height));
        layout.Controls.Add(new Label{Text="BRING YOUR ADVENTURE WITH YOU",AutoSize=true,Font=new Font("Georgia",16,FontStyle.Bold)});
        layout.Controls.Add(new Label{Text="Close both launchers, both game clients, and stop the server first.\nYour OLD folder is read only. The NEW folder keeps its updated game/world files.",AutoSize=true});
        var pick=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2};pick.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));pick.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));pick.Controls.Add(source);pick.Controls.Add(browse);layout.Controls.Add(pick);
        layout.Controls.Add(summary);
        layout.Controls.Add(new Label{Text="Destination (this new copy):\n"+root,AutoSize=true,MaximumSize=new Size(725,0)});
        layout.Controls.Add(new Label{Text="XP stays at 1×. GM stays OFF. This replaces saved progress; it does not merge rosters.",AutoSize=true});
        layout.Controls.Add(transfer);layout.Controls.Add(bar);layout.Controls.Add(status);Controls.Add(layout);
        foreach(var button in new[]{browse,transfer}){button.BackColor=Color.FromArgb(78,57,32);button.ForeColor=Color.Wheat;button.FlatStyle=FlatStyle.Flat;button.Padding=new Padding(7);}
        browse.Click+=(_,_)=>
        {
            using var dialog=new FolderBrowserDialog{Description="Select the OLD Offline DAoC folder",UseDescriptionForTitle=true,ShowNewFolderButton=false};
            if(dialog.ShowDialog(this)!=DialogResult.OK)return;
            try { var data=ImportEngine.Inspect(dialog.SelectedPath);source.Text=dialog.SelectedPath;summary.Text=$"Found {data.Accounts:N0} account(s), {data.Characters:N0} character(s), {data.Bots:N0} bots\nand {data.InventoryItems:N0} real inventory/equipment entries.";transfer.Enabled=true;status.Text="Ready. Your old folder will not be changed."; }
            catch(Exception e){transfer.Enabled=false;MessageBox.Show(this,e.Message,"Cannot use this folder",MessageBoxButtons.OK,MessageBoxIcon.Warning);}
        };
        transfer.Click+=async(_,_)=>
        {
            if(MessageBox.Show(this,"Replace progress in THIS NEW copy with progress from:\n"+source.Text+"\n\nA rollback backup is created first. The old folder is not changed. Continue?","Confirm progress transfer",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)!=DialogResult.Yes)return;
            browse.Enabled=transfer.Enabled=false;bar.Style=ProgressBarStyle.Marquee;ControlBox=false;
            string oldFolder=source.Text;
            try
            {
                string backup=await Task.Run(()=>ImportEngine.Import(oldFolder,destination,message=>BeginInvoke(()=>status.Text=message)));
                status.Text="Import complete. Close this window, then use START OFFLINE DAOC.cmd.";
                MessageBox.Show(this,"Progress imported successfully.\n\nYou can now launch this new copy normally.\nXP is 1× and GM is off.\n\nRollback backup and verification notes:\n"+backup,"Transfer complete",MessageBoxButtons.OK,MessageBoxIcon.Information);
            }
            catch(Exception e){status.Text="Import failed — see the message; previous progress is retained.";MessageBox.Show(this,e.Message,"Import not completed",MessageBoxButtons.OK,MessageBoxIcon.Error);}
            finally{browse.Enabled=true;ControlBox=true;bar.Style=ProgressBarStyle.Continuous;}
        };
    }
}
