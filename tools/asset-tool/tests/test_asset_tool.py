"""Only temporary client copies are modified. Live files are hashed, never written."""
import io
import json
from pathlib import Path
import shutil
import struct
import sys
import tempfile
import unittest
from unittest import mock

from PIL import Image

sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
import asset_tool as tool
from archive import Entry, read, write, verify_memory_image

LIVE_HOME=tool.HOME
LIVE_CLIENT=tool.CLIENT


class WorkflowTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.profiles=tool.profiles()
        cls.paths={'gamedata.mpk'}
        for profile in cls.profiles.values():
            cls.paths.update(profile['guards'])
            cls.paths.update(tool.profile_files(profile))
        cls.live_hashes={p:tool.sha((LIVE_CLIENT/p).read_bytes()) for p in cls.paths}

    @classmethod
    def tearDownClass(cls):
        actual={p:tool.sha((LIVE_CLIENT/p).read_bytes()) for p in cls.paths}
        if actual!=cls.live_hashes:
            raise AssertionError('LIVE ASSETS CHANGED DURING READ-ONLY FIXTURE TESTING')

    def setUp(self):
        self.temp=tempfile.TemporaryDirectory(prefix='daoc-private-texture-test-')
        self.root=Path(self.temp.name)
        self.home=self.root/'Offline DAoC'/'OFFLINE DAOC ASSET TOOL'
        self.client=self.home.parent/'runtime/client-opendaoc/app'
        self.home.mkdir(parents=True)
        shutil.copy2(LIVE_HOME/'assets.json',self.home/'assets.json')
        for p in self.paths:
            dest=self.client/p;dest.parent.mkdir(parents=True,exist_ok=True)
            shutil.copy2(LIVE_CLIENT/p,dest)
        self.home_patch=mock.patch.object(tool,'HOME',self.home);self.home_patch.start()
        self.client_patch=mock.patch.object(tool,'CLIENT',self.client);self.client_patch.start()
        self.stop_patch=mock.patch.object(tool,'stopped');self.stop_patch.start()
        self.image=self.root/'edited texture.png'
        Image.new('RGBA',(512,512),(110,82,57,0)).save(self.image)

    def tearDown(self):
        self.stop_patch.stop();self.client_patch.stop();self.home_patch.stop()
        self.temp.cleanup()

    def prepare(self,key='undead-hero-body'):
        project=tool.export_project(key)
        staged=tool.build(project,self.image,256)
        return project,staged

    def hashes(self):
        return {p:tool.sha((self.client/p).read_bytes()) for p in self.paths}

    def test_profiles_accept_working_registration(self):
        for p in self.profiles.values():tool.check_profile(p)

    def test_body_export_build_install_rollback(self):
        before=self.hashes();project,staged=self.prepare()
        self.assertEqual(before,self.hashes())
        self.assertTrue((project/'EDIT THIS TEXTURE.png').exists())
        backup=tool.install(staged,reviewed=True)
        self.assertEqual(tool.load(backup/'receipt.json')['state'],'installed')
        self.assertNotEqual(before['figures/skins/skin099.mpk'],self.hashes()['figures/skins/skin099.mpk'])
        tool.rollback(backup)
        self.assertEqual(before,self.hashes())
        self.assertEqual(tool.load(backup/'receipt.json')['state'],'restored')

    def test_sword_both_files_restore_and_originals_unchanged(self):
        before=self.hashes();_,staged=self.prepare('undead-hero-sword')
        backup=tool.install(staged,reviewed=True)
        changed={p for p,h in before.items() if self.hashes()[p]!=h}
        self.assertEqual(changed,{'items/uh_sword001.dds','items/uh_sword001.tga'})
        tool.rollback(backup);self.assertEqual(before,self.hashes())

    def test_import_alpha_preserved_and_legacy_header_retained(self):
        project,staged=self.prepare()
        _,entries=read((staged/'payload/figures/skins/skin099.mpk').read_bytes())
        data=entries[0].data;ref=(project/'reference-original.dds').read_bytes()
        self.assertEqual(Image.open(io.BytesIO(data)).convert('RGBA').getchannel('A').getextrema(),(255,255))
        a=bytearray(data[:128]);b=bytearray(ref[:128])
        for i in (12,16,20,28):a[i:i+4]=b[i:i+4]
        self.assertEqual(a,b)

    def test_no_review_no_install(self):
        _,staged=self.prepare();before=self.hashes()
        with self.assertRaisesRegex(ValueError,'Preview'):tool.install(staged)
        self.assertEqual(before,self.hashes())

    def test_running_game_blocks_install(self):
        _,staged=self.prepare();before=self.hashes()
        with mock.patch.object(tool,'stopped',side_effect=ValueError('Close game')):
            with self.assertRaisesRegex(ValueError,'Close game'):tool.install(staged,True)
        self.assertEqual(before,self.hashes())
        self.assertFalse((self.home/'INSTALL IN PROGRESS.lock').exists())

    def test_process_guard_detects_game_and_launcher(self):
        self.stop_patch.stop()
        try:
            for name in ['game.dll','CoreServer.exe','OfflineDAoC.exe','connect.exe']:
                with mock.patch.object(tool.subprocess,'check_output',return_value=f'"{name}","123"'):
                    with self.assertRaisesRegex(ValueError,'Close the game'):tool.stopped()
        finally:self.stop_patch.start()

    def test_lock_blocks_second_installer(self):
        _,staged=self.prepare()
        (self.home/'INSTALL IN PROGRESS.lock').write_text('test-lock')
        with self.assertRaisesRegex(ValueError,'Another installation'):tool.install(staged,True)

    def test_aspect_ratio_change_rejected(self):
        project=tool.export_project('undead-hero-body')
        Image.new('RGB',(512,256)).save(self.image)
        with self.assertRaisesRegex(ValueError,'Aspect ratio'):tool.build(project,self.image)

    def test_changed_mesh_rejected(self):
        project=tool.export_project('undead-hero-body')
        mesh=self.root/'fake.nif';mesh.write_bytes(b'not the original rig')
        with self.assertRaisesRegex(ValueError,'New/modified meshes'):tool.build(project,self.image,mesh=mesh)

    def test_reference_tamper_rejected(self):
        project=tool.export_project('undead-hero-body')
        (project/'reference-original.dds').write_bytes(b'changed')
        with self.assertRaisesRegex(ValueError,'Reference file changed'):tool.build(project,self.image)

    def test_stale_export_rejected(self):
        project=tool.export_project('undead-hero-sword')
        (self.client/'items/uh_sword001.tga').write_bytes(b'later edit')
        with self.assertRaisesRegex(ValueError,'Live texture changed'):tool.build(project,self.image)

    def test_payload_tamper_rejected(self):
        _,staged=self.prepare()
        (staged/'payload/figures/skins/skin099.mpk').write_bytes(b'changed')
        with self.assertRaisesRegex(ValueError,'Staged file changed'):tool.install(staged,True)

    def test_archive_other_entries_preserved_and_checked_at_install(self):
        p=self.client/'figures/skins/skin099.mpk'
        name,entries=read(p.read_bytes());entries.append(Entry('untouched.txt',b'leave me alone'))
        p.write_bytes(write(name,entries))
        _,staged=self.prepare()
        payload=staged/'payload/figures/skins/skin099.mpk'
        name,entries=read(payload.read_bytes())
        self.assertEqual(entries[1].data,b'leave me alone')
        entries[1].data=b'bad edit';payload.write_bytes(write(name,entries))
        report=tool.load(staged/'build.json');report['outputs']['figures/skins/skin099.mpk']=tool.sha(payload.read_bytes())
        tool.save(staged/'build.json',report)
        with self.assertRaisesRegex(ValueError,'Unrelated archive texture'):tool.install(staged,True)

    def test_model_entry_after_blank_is_rejected(self):
        p=self.client/'gamedata.mpk';name,entries=read(p.read_bytes())
        entry=next(e for e in entries if e.name=='monnifs.csv')
        rows=entry.data.splitlines(keepends=True);i=next(i for i,r in enumerate(rows) if r.startswith(b'986,'))
        rows.insert(i,b',,,,,,,,,,,,,,,,,,,,,,,,,,,,\r\n');entry.data=b''.join(rows)
        p.write_bytes(write(name,entries))
        with self.assertRaisesRegex(ValueError,'behind a blank'):tool.check_profile(self.profiles['undead-hero-body'])

    def test_install_io_failure_restores_both_files(self):
        before=self.hashes();_,staged=self.prepare('undead-hero-sword')
        real=tool.os.replace;calls=[]
        def fail_second(a,b):
            calls.append(b)
            if len(calls)==2:raise OSError('simulated write failure')
            return real(a,b)
        with mock.patch.object(tool.os,'replace',side_effect=fail_second):
            with self.assertRaisesRegex(OSError,'simulated'):tool.install(staged,True)
        self.assertEqual(before,self.hashes())
        receipt=next((self.home/'backups').glob('*/receipt.json'))
        self.assertEqual(tool.load(receipt)['state'],'failed-restored')

    def test_rollback_refuses_later_edits(self):
        _,staged=self.prepare('undead-hero-sword');backup=tool.install(staged,True)
        (self.client/'items/uh_sword001.tga').write_bytes(b'later edit')
        before=self.hashes()
        with self.assertRaisesRegex(ValueError,'Later change'):tool.rollback(backup)
        self.assertEqual(before,self.hashes())

    def test_rollback_io_failure_keeps_installed_pair(self):
        _,staged=self.prepare('undead-hero-sword');backup=tool.install(staged,True)
        before=self.hashes();real=tool.shutil.copy2;calls=[]
        def fail_second(a,b):
            calls.append(b)
            if len(calls)==2:raise OSError('simulated restore failure')
            return real(a,b)
        with mock.patch.object(tool.shutil,'copy2',side_effect=fail_second):
            with self.assertRaisesRegex(OSError,'simulated'):tool.rollback(backup)
        self.assertEqual(before,self.hashes())
        self.assertEqual(tool.load(backup/'receipt.json')['state'],'installed')

    def test_gui_constructs_and_shows_built_preview(self):
        import tkinter as tk
        from asset_tool_gui import App
        root=tk.Tk();root.withdraw()
        try:
            app=App(root);project,staged=self.prepare()
            app.project_ready(project);app.build_ready(staged)
            app.reviewed.set(True);app.update_install();root.update_idletasks()
            self.assertIsNotNone(app.photo)
            self.assertEqual(str(app.install_button.cget('state')),'normal')
            app.installed(self.home/'backups/example');app.update_install()
            self.assertEqual(str(app.install_button.cget('state')),'disabled')
        finally:root.destroy()


class FormatTests(unittest.TestCase):
    def test_dxt1_and_dxt5_full_mips(self):
        for size in (256,512,2048):
            for alpha,codec in [(255,'DXT1'),(100,'DXT5')]:
                with self.subTest(size=size,codec=codec):
                    data=tool.dds(Image.new('RGBA',(size,size),(50,100,90,alpha)))
                    self.assertEqual(tool.validate_dds(data)[:3],(size,size,codec))
                    self.assertEqual(struct.unpack_from('<I',data,20)[0],(size//4)**2*(8 if alpha==255 else 16))

    def test_bad_size_and_truncated_dds_rejected(self):
        data=bytearray(tool.dds(Image.new('RGBA',(256,256),(0,0,0,255))))
        struct.pack_into('<I',data,20,8204)
        with self.assertRaisesRegex(ValueError,'byte count'):tool.validate_dds(data)
        with self.assertRaisesRegex(ValueError,'Truncated'):tool.validate_dds(b'DDS ')

    def test_archive_offsets_crc_and_growth(self):
        raw=write(b'test.mpk',[Entry('one',b'abc'*1000),Entry('two',b'test')])
        name,entries=read(raw);entries[0].data+=b'growth'*800
        changed=write(name,entries)
        self.assertEqual(verify_memory_image(changed),2)
        self.assertEqual(read(changed)[1][1].data,b'test')
        corrupt=bytearray(changed);corrupt[-1]^=1
        with self.assertRaises(ValueError):read(corrupt)

    def test_path_escape_rejected(self):
        with self.assertRaisesRegex(ValueError,'escapes'):tool.safe(Path(tempfile.gettempdir())/'tool','../escape')


if __name__=='__main__':unittest.main(verbosity=2)
