"""Small local GUI for the private DAoC texture workflow."""
import os
from pathlib import Path
import queue
import threading
import tkinter as tk
from tkinter import ttk, filedialog, messagebox

from PIL import Image, ImageTk
import asset_tool as core


class App:
    def __init__(self, root):
        self.root=root
        root.title('Offline DAoC - Private Texture Tool')
        root.geometry('1000x790');root.minsize(860,720)
        root.configure(bg='#211e18')
        self.busy=False;self.project=None;self.staged=None;self.photo=None
        self.events=queue.Queue()
        self.catalog=core.profiles();self.keys=list(self.catalog)
        self.asset=tk.StringVar(value=self.catalog[self.keys[0]]['label'])
        self.art=tk.StringVar();self.maximum=tk.StringVar(value='2048')
        self.reviewed=tk.BooleanVar(value=False)
        self.status=tk.StringVar(value='Ready. Export a reference texture to begin. No game files changed.')
        self.project_text=tk.StringVar(value='No project selected')
        style=ttk.Style(root);style.theme_use('clam')
        style.configure('.',font=('Segoe UI',10),background='#29251e',foreground='#ead8af')
        style.configure('TButton',padding=7)
        style.configure('TEntry',fieldbackground='#fffdf6',foreground='#222222')
        style.configure('TCombobox',fieldbackground='#fffdf6',foreground='#222222')
        style.configure('Title.TLabel',font=('Segoe UI',19,'bold'),foreground='#e6bd68')
        self.body=ttk.Frame(root,padding=16);self.body.pack(fill='both',expand=True)
        f=self.body
        ttk.Label(f,text='OFFLINE DAoC  |  TEXTURE TOOL',style='Title.TLabel').pack(anchor='w')
        ttk.Label(f,text='Private artwork only. No new meshes, gameplay edits or original draugr replacements.').pack(anchor='w',pady=(2,12))
        row=ttk.Frame(f);row.pack(fill='x')
        ttk.Label(row,text='Asset:').pack(side='left')
        self.combo=ttk.Combobox(row,textvariable=self.asset,values=[self.catalog[k]['label'] for k in self.keys],state='readonly',width=44)
        self.combo.pack(side='left',padx=8);self.combo.bind('<<ComboboxSelected>>',self.asset_changed)
        self.controls=[]
        self.button(row,'1. Export reference',self.export).pack(side='left',padx=4)
        self.button(row,'Open project...',self.open_project).pack(side='left',padx=4)
        ttk.Label(f,textvariable=self.project_text,wraplength=910).pack(anchor='w',pady=(8,12))
        row=ttk.Frame(f);row.pack(fill='x')
        self.button(row,'2. Choose edited image...',self.choose_art).pack(side='left')
        ttk.Entry(row,textvariable=self.art,state='readonly').pack(side='left',fill='x',expand=True,padx=8)
        ttk.Label(row,text='Max size:').pack(side='left')
        self.size_combo=ttk.Combobox(row,textvariable=self.maximum,values=['256','512','1024','2048'],state='readonly',width=6)
        self.size_combo.pack(side='left',padx=(5,0))
        row=ttk.Frame(f);row.pack(fill='x',pady=10)
        self.button(row,'3. Build and preview',self.build).pack(side='left')
        self.button(row,'Open project folder',self.open_folder).pack(side='left',padx=8)
        self.button(row,'Rollback an install...',self.rollback).pack(side='right')
        ttk.Label(f,text='Reference texture (left)  |  Converted game texture (right) - check that all sections still line up').pack(anchor='w')
        self.preview=tk.Label(f,text='Your comparison preview will appear here.',background='#151515',foreground='#d9c8a6',height=15)
        self.preview.pack(fill='both',expand=True,pady=8)
        self.preview.bind('<Double-Button-1>',self.open_preview)
        self.check=ttk.Checkbutton(f,text='I reviewed the texture layout. I will verify the pet visually in-game after installing.',variable=self.reviewed,command=self.update_install)
        self.check.pack(anchor='w')
        row=ttk.Frame(f);row.pack(fill='x',pady=10)
        self.install_button=ttk.Button(row,text='4. Install private textures',command=self.install,state='disabled')
        self.install_button.pack(side='left')
        ttk.Label(row,text='Close the game, server and launcher first. Only affected texture files are backed up.').pack(side='left',padx=12)
        self.progress=ttk.Progressbar(f,mode='indeterminate');self.progress.pack(fill='x')
        ttk.Label(f,textvariable=self.status,wraplength=910).pack(anchor='w',pady=(8,0))
        root.protocol('WM_DELETE_WINDOW',self.close)
        root.after(100,self.poll)

    def key(self):
        return next(k for k in self.keys if self.catalog[k]['label']==self.asset.get())

    def button(self,parent,text,command):
        b=ttk.Button(parent,text=text,command=command);self.controls.append(b);return b

    def reset_build(self):
        self.staged=None;self.reviewed.set(False);self.photo=None
        self.preview.configure(image='',text='Build the selected artwork to see a new comparison.',height=15)
        self.update_install()

    def asset_changed(self,_=None):
        self.project=None;self.project_text.set('No project selected');self.reset_build()

    def update_install(self):
        enabled=self.staged is not None and self.reviewed.get() and not self.busy
        self.install_button.configure(state='normal' if enabled else 'disabled')

    def run(self,label,job,done):
        if self.busy:return
        self.busy=True;self.status.set(label);self.progress.start(12)
        for b in self.controls:b.configure(state='disabled')
        self.combo.configure(state='disabled');self.size_combo.configure(state='disabled');self.check.configure(state='disabled')
        self.update_install()
        def work():
            try:self.events.put((True,job(),done))
            except Exception as error:self.events.put((False,str(error),None))
        threading.Thread(target=work,daemon=False).start()

    def poll(self):
        try:
            ok,value,done=self.events.get_nowait()
            self.busy=False;self.progress.stop()
            for b in self.controls:b.configure(state='normal')
            self.combo.configure(state='readonly');self.size_combo.configure(state='readonly');self.check.configure(state='normal')
            if ok:
                try:done(value)
                except Exception as error:
                    self.status.set('Operation completed, but the display could not update: '+str(error))
                    messagebox.showerror('Display error',str(error),parent=self.root)
            else:
                self.status.set('STOPPED: '+value)
                messagebox.showerror('Operation stopped',value,parent=self.root)
            self.update_install()
        except queue.Empty:pass
        self.root.after(100,self.poll)

    def export(self):
        key=self.key()
        self.run('Exporting the working texture and checking protected assets...',lambda:core.export_project(key),self.project_ready)

    def project_ready(self,folder):
        self.project=Path(folder);self.project_text.set(str(self.project));self.reset_build()
        self.status.set('Reference exported. Give EDIT THIS TEXTURE.png to your image editor, then choose the returned image.')

    def open_project(self):
        file=filedialog.askopenfilename(title='Select asset-project.json',initialdir=core.HOME/'projects',filetypes=[('Asset project','asset-project.json')])
        if not file:return
        try:
            meta=core.load(file);key=meta['asset']
            core.require(key in self.catalog,'This project uses an unregistered asset')
            self.asset.set(self.catalog[key]['label']);self.project_ready(Path(file).parent)
            self.status.set('Project opened. Choose artwork and build a fresh preview.')
        except Exception as error:messagebox.showerror('Cannot open project',str(error),parent=self.root)

    def choose_art(self):
        file=filedialog.askopenfilename(title='Select edited texture atlas',filetypes=[('Texture artwork','*.png *.tga *.dds *.bmp *.jpg *.jpeg *.webp')])
        if file:self.art.set(file);self.reset_build()

    def build(self):
        if not self.project or not self.art.get():
            messagebox.showinfo('Choose inputs','Export/open a project and choose your edited image first.',parent=self.root);return
        project=self.project;art=self.art.get();maximum=int(self.maximum.get())
        self.reset_build()
        self.run('Converting DDS and mipmaps, preserving alpha, checking private archive...',lambda:core.build(project,art,maximum),self.build_ready)

    def build_ready(self,folder):
        self.staged=Path(folder)
        with Image.open(self.staged/'COMPARE ORIGINAL LEFT - IMPORT RIGHT.png') as im:
            im.thumbnail((900,300));self.photo=ImageTk.PhotoImage(im.copy(),master=self.root)
        self.preview.configure(image=self.photo,text='',height=0)
        report=core.load(self.staged/'build.json')
        self.status.set(f'BUILD READY: {report["dds"]}. Preview only - nothing installed. Review the layout and check the confirmation box.')

    def install(self):
        if not self.staged or not self.reviewed.get():return
        if not messagebox.askyesno('Install private textures?',
                'This replaces only the selected private textures and saves their previous versions.\n\nClose the game, server and launcher before continuing. Install now?',parent=self.root):return
        staged=self.staged
        self.run('Checking stopped processes, saving texture backup and installing...',lambda:core.install(staged,reviewed=True),self.installed)

    def installed(self,backup):
        self.staged=None;self.reviewed.set(False)
        self.status.set('INSTALLED AND FILE-VERIFIED. Restart the game and resummon the pet to check appearance. Backup: '+str(backup))

    def rollback(self):
        folder=filedialog.askdirectory(title='Select a timestamped tool backup (contains receipt.json)',initialdir=core.HOME/'backups')
        if not folder:return
        if not messagebox.askyesno('Restore previous textures?',
                'Restore this texture backup? Later edits will not be overwritten. Close the game, server and launcher first.',parent=self.root):return
        def done(backup):
            self.reset_build();self.status.set('RESTORED AND FILE-VERIFIED: '+str(backup))
        self.run('Checking backup and restoring previous private textures...',lambda:core.rollback(folder),done)

    def open_folder(self):
        os.startfile(str(self.project or core.HOME))

    def open_preview(self,_=None):
        if self.staged and not self.busy:
            os.startfile(str(self.staged/'COMPARE ORIGINAL LEFT - IMPORT RIGHT.png'))

    def close(self):
        if self.busy:
            messagebox.showinfo('Operation in progress','Please wait for the current operation to finish before closing.',parent=self.root);return
        self.root.destroy()


def main():
    root=tk.Tk()
    try:App(root)
    except Exception as error:
        root.withdraw();messagebox.showerror('Texture tool could not start',str(error),parent=root);root.destroy();return
    root.mainloop()

if __name__=='__main__':main()
