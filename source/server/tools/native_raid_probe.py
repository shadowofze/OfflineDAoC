"""Build an isolated native UI binding proof, not a finished raid implementation.

Default: stage a patched client and custom9 XML. --install backs up and installs
the probe on this explicitly verified client. No server or group limits change.
The forty bars are TEST VALUES, never represented as real raid members.
"""
import argparse
import copy
import hashlib
import json
import struct
import subprocess
import xml.etree.ElementTree as ET
from pathlib import Path
import pefile

CLIENT = Path(r'C:\Users\thedo\Desktop\Offline DAoC\runtime\client-opendaoc\app')
OUTPUT = Path(__file__).resolve().parents[1] / 'build/native-raid-probe'
HOOK = 0x4DA938
EXPECTED = bytes.fromhex('51 d9 ee 51 d9 5c 24 04')

def build(image):
    pe = pefile.PE(data=image)
    base = pe.OPTIONAL_HEADER.ImageBase
    assert base == 0x400000 and pe.FILE_HEADER.Machine == 0x14c
    assert not any(s.Name.rstrip(b'\0') == b'.raidp' for s in pe.sections)
    offset = pe.get_offset_from_rva(HOOK-base)
    assert image[offset:offset+8] == EXPECTED, 'Unexpected UI initialization code.'
    align = lambda n, a: (n+a-1)//a*a
    last = pe.sections[-1]
    rva = align(last.VirtualAddress+max(last.Misc_VirtualSize,last.SizeOfRawData), pe.OPTIONAL_HEADER.SectionAlignment)
    address = base+rva
    # Fixed code/data separation within this PROTOTYPE section. Forty independent
    # registered adapters use the game's normal float-adapter constructor.
    slots = address+0x2000
    names = address+0x2200
    code = bytearray(b'\x9c\x60')  # preserve flags and all integer registers
    strings = bytearray()
    def imm(value):
        code.extend(b'\x68'+struct.pack('<I',value))
    for i in range(40):
        label = f'raid_probe_health{i}'.encode()+b'\0'
        name = names+len(strings)
        strings.extend(label)
        imm(0)  # minimum
        imm(struct.unpack('<I',struct.pack('<f',100.0))[0])  # maximum
        imm(name)
        imm(slots+i*4)
        code.extend(b'\x57')  # verified EDI = native UI data store here
        code.extend(b'\xe8'+struct.pack('<i',0x4B6BC0-(address+len(code)+5)))
        code.extend(b'\xa1'+struct.pack('<I',slots+i*4))
        # Allocation failure: don't dereference null. Registration owns lifetime.
        code.extend(bytes.fromhex('85 c0 74 0b c7 40 10'))
        code.extend(struct.pack('<f',float(20+i*2)))
        code.extend(bytes.fromhex('c6 40 04 01'))
    code.extend(b'\x61\x9d'+EXPECTED)
    code.extend(b'\xe9'+struct.pack('<i',HOOK+8-(address+len(code)+5)))
    assert len(code)<0x2000
    payload=code+bytes(0x2200-len(code))+strings
    raw=align(len(image),pe.OPTIONAL_HEADER.FileAlignment)
    size=align(len(payload),pe.OPTIONAL_HEADER.FileAlignment)
    header=pe.sections[0].get_file_offset()+40*pe.FILE_HEADER.NumberOfSections
    assert header+40<=min(s.PointerToRawData for s in pe.sections if s.PointerToRawData)
    assert not any(image[header:header+40])
    result=bytearray(image)
    result.extend(bytes(raw+size-len(result)))
    result[raw:raw+len(payload)]=payload
    result[header:header+40]=struct.pack('<8sIIIIIIHHI',b'.raidp\0\0',len(payload),rva,size,raw,0,0,0,0,0xE0000060)
    struct.pack_into('<H',result,pe.FILE_HEADER.get_field_absolute_offset('NumberOfSections'),pe.FILE_HEADER.NumberOfSections+1)
    struct.pack_into('<I',result,pe.OPTIONAL_HEADER.get_field_absolute_offset('SizeOfImage'),align(rva+len(payload),pe.OPTIONAL_HEADER.SectionAlignment))
    result[offset:offset+8]=b'\xe9'+struct.pack('<i',address-HOOK-5)+b'\x90'*3
    check=pefile.PE(data=result)
    struct.pack_into('<I',result,pe.OPTIONAL_HEADER.get_field_absolute_offset('CheckSum'),check.generate_checksum())
    return bytes(result), dict(hook=hex(HOOK),codeAddress=hex(address),codeLength=len(code),slots=hex(slots),count=40)

def window():
    tree=ET.parse(CLIENT/'ui/atlantis/new_group_window.xml')
    old=tree.getroot().find('WindowTemplate')
    root=ET.Element('Root_Element',ID='DAOCUi')
    new=ET.SubElement(root,'WindowTemplate')
    for child in old:
        if len(child)==0: new.append(copy.deepcopy(child))
    for key,value in dict(Name='custom9_window',WindowId='Custom9',Width='620',Height='250',CloseButton='true',MoveButton='true').items():
        new.find(key).text=value
    background=copy.deepcopy(old.find('FullResizeImageDef'))
    background.find('Width').text='620'; background.find('Height').text='250'
    new.append(background)
    for i in range(40):
        x=12+(i//8)*122; y=25+(i%8)*27
        label=copy.deepcopy(old.find('LabelDef'))
        label.find('Position/X').text=str(x);label.find('Position/Y').text=str(y)
        label.find('Width').text='112'
        label.find('Data').text=f'TEST slot {i+1:02d}'
        for key in ('Adapter','ColorAdapter'):
            element=label.find(key)
            if element is not None: element.text=''
        new.append(label)
        bar=copy.deepcopy(old.find('StatusBarDef'))
        bar.find('Position/X').text=str(x);bar.find('Position/Y').text=str(y+14)
        bar.find('Width').text='110'
        bar.find('AdapterName').text=f'raid_probe_health{i}'
        new.append(bar)
    ET.indent(root)
    return ET.tostring(root,encoding='iso-8859-1',xml_declaration=True)

def main():
    parser=argparse.ArgumentParser()
    parser.add_argument('--install',action='store_true')
    parser.add_argument('--restore',action='store_true')
    parser.add_argument('--install-button',action='store_true')
    args=parser.parse_args()
    if args.install_button:
        running=subprocess.check_output(['powershell','-NoProfile','-Command',
            'Get-Process CoreServer,game,camelot -ErrorAction SilentlyContinue | Select-Object -ExpandProperty ProcessName; exit 0'],text=True)
        assert not running.strip(), 'Close client/server before installing test button.'
        backup=CLIENT/'rollback-native-raid-probe'
        assert backup.is_dir()
        for skin in ('atlantis','isles'):
            path=CLIENT/'ui'/skin/'command_window.xml'
            if not path.exists(): continue
            original=path.read_bytes()
            assert b'RaidProbeButton' not in original
            saved=backup/(skin+'-command_window.xml')
            assert not saved.exists()
            saved.write_bytes(original)
            root=ET.fromstring(original)
            panel=root.find('WindowTemplate')
            height=int(panel.find('Height').text)
            panel.find('Height').text=str(height+20)
            background=panel.find('FullResizeImageDef/Height')
            if background is not None: background.text=str(height+20)
            button=copy.deepcopy(panel.find('ButtonDef'))
            button.find('ControlId').text='RaidProbeButton'
            button.find('OnClickEvent').text='ShowCustom9'
            button.find('Label').text='RAID TEST'
            button.find('Position/X').text='6'
            button.find('Position/Y').text=str(height-1)
            panel.append(button)
            ET.indent(root)
            path.write_bytes(ET.tostring(root,encoding='iso-8859-1',xml_declaration=True))
        print('Native RAID TEST button added to the Commands window.')
        return
    if args.restore:
        assert not args.install
        running=subprocess.check_output(['powershell','-NoProfile','-Command',
            'Get-Process CoreServer,game,camelot -ErrorAction SilentlyContinue | Select-Object -ExpandProperty ProcessName; exit 0'],text=True)
        assert not running.strip(), 'Close client/server before restoring.'
        backup=CLIENT/'rollback-native-raid-probe'
        report=json.loads((backup/'manifest.json').read_text())
        assert hashlib.sha256((CLIENT/'game.dll').read_bytes()).hexdigest()==report['patchedSha256'], 'Client changed since probe installation; do not overwrite it.'
        original=(backup/'game.dll').read_bytes()
        assert hashlib.sha256(original).hexdigest()==report['originalSha256']
        (CLIENT/'game.dll').write_bytes(original)
        (CLIENT/'ui/uimain.xml').write_bytes((backup/'uimain.xml').read_bytes())
        for skin in ('atlantis','isles'):
            saved=backup/(skin+'-command_window.xml')
            if saved.exists(): (CLIENT/'ui'/skin/'command_window.xml').write_bytes(saved.read_bytes())
        print('Original client restored; probe XML is now unreferenced. Rollback retained.')
        return
    original=(CLIENT/'game.dll').read_bytes()
    patched,report=build(original)
    xml=window()
    main=(CLIENT/'ui/uimain.xml').read_bytes()
    assert b'custom9_window.xml' not in main
    newmain=main.replace(b'</XML>',b'\t<Include>custom9_window.xml</Include>\r\n</XML>')
    assert main != newmain
    report.update(originalSha256=hashlib.sha256(original).hexdigest(),patchedSha256=hashlib.sha256(patched).hexdigest(),
                  purpose='Native UI binding probe ONLY; all forty health values are synthetic test values.')
    OUTPUT.mkdir(parents=True,exist_ok=True)
    (OUTPUT/'game.dll').write_bytes(patched)
    (OUTPUT/'custom9_window.xml').write_bytes(xml)
    (OUTPUT/'uimain.xml').write_bytes(newmain)
    (OUTPUT/'manifest.json').write_text(json.dumps(report,indent=2))
    if args.install:
        running=subprocess.check_output(['powershell','-NoProfile','-Command',
            'Get-Process CoreServer,game,camelot -ErrorAction SilentlyContinue | Select-Object -ExpandProperty ProcessName; exit 0'],text=True)
        assert not running.strip(), 'Close client/server before installation.'
        backup=CLIENT/'rollback-native-raid-probe'
        assert not backup.exists(), 'Rollback already exists; inspect before replacing it.'
        assert not (CLIENT/'ui/atlantis/custom9_window.xml').exists()
        assert not (CLIENT/'ui/isles/custom9_window.xml').exists()
        backup.mkdir()
        (backup/'game.dll').write_bytes(original)
        (backup/'uimain.xml').write_bytes(main)
        (backup/'manifest.json').write_text(json.dumps(report,indent=2))
        try:
            (CLIENT/'ui/atlantis/custom9_window.xml').write_bytes(xml)
            (CLIENT/'ui/isles/custom9_window.xml').write_bytes(xml)
            (CLIENT/'ui/uimain.xml').write_bytes(newmain)
            (CLIENT/'game.dll').write_bytes(patched)
        except Exception:
            (CLIENT/'game.dll').write_bytes(original)
            (CLIENT/'ui/uimain.xml').write_bytes(main)
            raise
    print(json.dumps(report,indent=2))
    print('INSTALLED PROBE ONLY' if args.install else 'STAGED ONLY: original client unchanged')

if __name__=='__main__': main()
