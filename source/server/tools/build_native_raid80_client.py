"""Stage the native raid UI extension for the verified offline x86 client.

No installation side effects. Native UI data is independent of the eight-member
group arrays. Reserved DebugMode packets carry bounded, fixed-size raid updates.
"""
import copy
import hashlib
import json
from pathlib import Path
import struct
import sys
import xml.etree.ElementTree as ET
import pefile
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'build/native-raid-probe/deps'))
from keystone import Ks, KS_ARCH_X86, KS_MODE_32
import native_raid_probe as probe

OUT=Path(__file__).resolve().parents[1]/'build/native-raid80'
BASE_IMAGE=probe.CLIENT/'rollback-native-raid-probe/game.dll'
BASE_SHA='00d8f943e9c7cfdaf9136dad96b768541717f023ae0590404d1f3dd760b8a26c'

def build():
    original=BASE_IMAGE.read_bytes()
    assert hashlib.sha256(original).hexdigest()==BASE_SHA
    pe=pefile.PE(data=original);base=pe.OPTIONAL_HEADER.ImageBase
    align=lambda n,a:(n+a-1)//a*a
    last=pe.sections[-1]
    rva=align(last.VirtualAddress+max(last.Misc_VirtualSize,last.SizeOfRawData),pe.OPTIONAL_HEADER.SectionAlignment)
    va=base+rva
    hp=va+0xa000;power=hp+320;names=power+320;ids=names+320;active=ids+320
    constants=va+0xb000
    strings=bytearray()
    def string(value):
        address=constants+len(strings);strings.extend(value.encode('ascii')+b'\0');return address
    empty=string('')
    health_names=[string(f'raid_health{i}') for i in range(80)]
    power_names=[string(f'raid_power{i}') for i in range(80)]
    name_names=[string(f'raid_name{i}') for i in range(80)]
    events=[string(f'RaidMember{i:02}') for i in range(80)]
    ks=Ks(KS_ARCH_X86,KS_MODE_32)
    payload=bytearray(0xb000)
    blocks={}
    def assemble(label,offset,source):
        encoded,_=ks.asm(source,addr=va+offset)
        assert offset+len(encoded)<=0xa000
        assert all(offset+len(encoded)<=b['address']-va or offset>=b['address']-va+b['length'] for b in blocks.values())
        payload[offset:offset+len(encoded)]=bytes(encoded)
        blocks[label]=dict(address=va+offset,length=len(encoded))
    init=['pushfd','pushad',f'mov dword ptr [{active}], 0']
    for i in range(80):
        init += [f'mov dword ptr [{ids+4*i}], 0']
        for location,label in [(hp+4*i,health_names[i]),(power+4*i,power_names[i])]:
            init += ['push 0','push 0x42c80000',f'push {label}',f'push {location}','push edi','call 0x4b6bc0']
        init += [f'push {name_names[i]}',f'push {names+4*i}','push edi','call 0x4b6c84']
    init += ['popad','popfd','push ecx','fldz','push ecx','fstp dword ptr [esp+4]','jmp 0x4da940']
    assemble('init',0,';'.join(init))

    # EBX is the payload, EBP+18 is its length, proven in the existing dispatcher.
    # No debug permission or existing observer-flight state is touched by raid data.
    packet=f'''
        cmp dword ptr [ebp+0x18], 128
        jb legacy
        cmp byte ptr [ebx+1], 0x52
        jne legacy
        cmp byte ptr [ebx+2], 1
        jne legacy
        pushfd
        pushad
        mov esi, ebx
        movzx eax, byte ptr [esi+3]
        cmp eax, 1
        je row
        cmp eax, 2
        je show
        cmp eax, 3
        je hide
        jmp done
    row:
        movzx edi, byte ptr [esi+4]
        cmp edi, 79
        ja done
        cmp byte ptr [esi+5], 100
        ja done
        cmp byte ptr [esi+6], 100
        ja done
        cmp byte ptr [esi+43], 0
        jne done
        mov ecx, dword ptr [{hp}+edi*4]
        test ecx, ecx
        jz done
        movzx eax, byte ptr [esi+5]
        push eax
        fild dword ptr [esp]
        fstp dword ptr [esp]
        call 0x524370
        mov ecx, dword ptr [{power}+edi*4]
        test ecx, ecx
        jz done
        movzx eax, byte ptr [esi+6]
        push eax
        fild dword ptr [esp]
        fstp dword ptr [esp]
        call 0x524370
        mov ebx, dword ptr [{names}+edi*4]
        test ebx, ebx
        jz done
        lea eax, [esi+12]
        push eax
        call 0x524436
        movzx eax, word ptr [esi+8]
        mov dword ptr [{ids}+edi*4], eax
        jmp done
    show:
        mov ecx, dword ptr [0x104c2bc]
        test ecx, ecx
        jz done
        push 0
        push 0x76
        mov eax, dword ptr [ecx]
        call dword ptr [eax+8]
        mov ecx, dword ptr [0x104c2bc]
        push 0
        push 0x77
        mov eax, dword ptr [ecx]
        call dword ptr [eax+8]
        mov dword ptr [{active}], 40
        mov edx, 0x76
        cmp byte ptr [esi+4], 80
        jne open_window
        mov dword ptr [{active}], 80
        mov edx, 0x77
    open_window:
        mov ecx, dword ptr [0x104c2bc]
        push 1
        push edx
        mov eax, dword ptr [ecx]
        call dword ptr [eax+8]
        jmp done
    hide:
        mov dword ptr [{active}], 0
        mov ecx, dword ptr [0x104c2bc]
        test ecx, ecx
        jz done
        push 0
        push 0x76
        mov eax, dword ptr [ecx]
        call dword ptr [eax+8]
        mov ecx, dword ptr [0x104c2bc]
        push 0
        push 0x77
        mov eax, dword ptr [ecx]
        call dword ptr [eax+8]
    done:
        popad
        popfd
        jmp 0x4113ad
    legacy:
        movsx eax, byte ptr [ebx]
        mov dword ptr [0xf96fa0], eax
        jmp 0x411209
    '''
    assemble('packet',0x4000,packet)
    mapper=[]
    for i,event in enumerate(events):
        mapper += [f'push {event}','push esi','call 0x77156e','add esp, 8','test eax, eax',f'jnz next{i}',f'mov eax, {0x600+i}','ret',f'next{i}:']
    mapper += ['push 0x94e5a4','jmp 0x4e99eb']
    assemble('eventNames',0x5000,';'.join(mapper))
    event_handler=f'''
        pushfd
        pushad
        mov ecx, dword ptr [eax+8]
        sub ecx, 0x600
        cmp ecx, 79
        ja legacy
        cmp ecx, dword ptr [{active}]
        jae handled
        lea eax, [{ids}+ecx*4]
        cmp word ptr [eax], 0
        je handled
        call 0x41ab40
    handled:
        popad
        popfd
        mov al, 1
        ret
    legacy:
        popad
        popfd
        push esi
        mov esi, dword ptr [eax+8]
        cmp esi, 0x12e
        jmp 0x4e04e8
    '''
    assemble('eventHandler',0x7000,event_handler)
    payload.extend(strings)
    data=bytearray(original)
    patches=[(0x4da938,probe.EXPECTED,va),(0x411201,bytes.fromhex('0f be 03 a3 a0 6f f9 00'),va+0x4000),
             (0x4e99e6,bytes.fromhex('68 a4 e5 94 00'),va+0x5000),
             (0x4e04de,bytes.fromhex('56 8b 70 08 81 fe 2e 01 00 00'),va+0x7000)]
    for address,expected,target in patches:
        offset=pe.get_offset_from_rva(address-base)
        assert data[offset:offset+len(expected)]==expected,hex(address)
        data[offset:offset+len(expected)]=b'\xe9'+struct.pack('<i',target-address-5)+b'\x90'*(len(expected)-5)
    raw=align(len(data),pe.OPTIONAL_HEADER.FileAlignment)
    size=align(len(payload),pe.OPTIONAL_HEADER.FileAlignment)
    header=pe.sections[0].get_file_offset()+40*pe.FILE_HEADER.NumberOfSections
    assert header+40<=min(s.PointerToRawData for s in pe.sections if s.PointerToRawData)
    assert not any(data[header:header+40])
    data.extend(bytes(raw+size-len(data)));data[raw:raw+len(payload)]=payload
    data[header:header+40]=struct.pack('<8sIIIIIIHHI',b'.raid\0\0\0',len(payload),rva,size,raw,0,0,0,0,0xe0000060)
    struct.pack_into('<H',data,pe.FILE_HEADER.get_field_absolute_offset('NumberOfSections'),pe.FILE_HEADER.NumberOfSections+1)
    struct.pack_into('<I',data,pe.OPTIONAL_HEADER.get_field_absolute_offset('SizeOfImage'),align(rva+len(payload),pe.OPTIONAL_HEADER.SectionAlignment))
    changed=pefile.PE(data=data)
    struct.pack_into('<I',data,pe.OPTIONAL_HEADER.get_field_absolute_offset('CheckSum'),changed.generate_checksum())
    return bytes(data),dict(blocks=blocks,hp=hp,power=power,names=names,ids=ids,active=active,
                            originalSha256=BASE_SHA,sha256=hashlib.sha256(data).hexdigest())

def window(capacity=40):
    root=ET.fromstring(probe.window());panel=root.find('WindowTemplate')
    labels=panel.findall('LabelDef');bars=panel.findall('StatusBarDef')
    original=ET.parse(probe.CLIENT/'ui/atlantis/new_group_window.xml').getroot().find('WindowTemplate')
    for i,label in enumerate(labels):
        label.find('Data').text=''
        adapter=label.find('Adapter')
        if adapter is None:adapter=ET.SubElement(label,'Adapter')
        adapter.text=f'raid_name{i}'
        bars[i].find('AdapterName').text=f'raid_health{i}'
        bar=copy.deepcopy(bars[i]);bar.find('TemplateName').text='blue_status'
        bar.find('AdapterName').text=f'raid_power{i}'
        bar.find('Position/Y').text=str(int(bar.find('Position/Y').text)+5)
        panel.append(bar)
        button=copy.deepcopy(original.find('InvisibleButtonDef'))
        button.find('Position/X').text=label.find('Position/X').text
        button.find('Position/Y').text=label.find('Position/Y').text
        button.find('Width').text='112';button.find('Height').text='26'
        button.find('OnClickEvent').text=f'RaidMember{i:02}'
        button.find('Label').text=f'Raid member {i+1}'
        panel.append(button)
    assert capacity in (40,80)
    if capacity == 80:
        panel.find('Name').text='custom10_window'
        panel.find('WindowId').text='Custom10'
        panel.find('Height').text='466'
        panel.find('FullResizeImageDef/Height').text='466'
        for child in list(panel):
            if child.tag not in ('LabelDef','StatusBarDef','InvisibleButtonDef'): continue
            clone=copy.deepcopy(child)
            clone.find('Position/Y').text=str(int(clone.find('Position/Y').text)+216)
            for key in ('Adapter','AdapterName','OnClickEvent'):
                elem=clone.find(key)
                if elem is not None and elem.text:
                    import re
                    elem.text=re.sub(r'\d+$',lambda m:str(int(m.group())+40),elem.text)
            if child.tag=='InvisibleButtonDef':
                clone.find('Label').text='Raid member '+str(int(child.find('Label').text.split()[-1])+40)
            panel.append(clone)
    ET.indent(root)
    return ET.tostring(root,encoding='iso-8859-1',xml_declaration=True)

if __name__=='__main__':
    image,report=build();OUT.mkdir(parents=True,exist_ok=True)
    (OUT/'game.dll').write_bytes(image)
    (OUT/'custom9_window.xml').write_bytes(window(40))
    (OUT/'custom10_window.xml').write_bytes(window(80))
    main=(probe.CLIENT/'ui/uimain.xml').read_bytes()
    if b'custom10_window.xml' not in main:
        assert b'</XML>' in main
        main=main.replace(b'</XML>',b'\t<Include>custom10_window.xml</Include>\r\n</XML>')
    (OUT/'uimain.xml').write_bytes(main)
    (OUT/'manifest.json').write_text(json.dumps(report,indent=2))
    print(json.dumps(report,indent=2));print('STAGED ONLY; not installed.')
