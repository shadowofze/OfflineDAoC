"""Rebuild the verified native raid customization without installing or using author paths.

Run with the complete download's Python runtime. Creates a fresh output directory.
The original client is a baseline binary dependency, not reconstructed source.
"""
from pathlib import Path
import argparse, hashlib, json, sys

parser=argparse.ArgumentParser()
parser.add_argument('--distribution',type=Path,required=True,help='Extracted complete OfflineDAoC distribution')
parser.add_argument('--output',type=Path,required=True,help='New staging directory; must not already exist')
args=parser.parse_args()
root=Path(__file__).resolve().parents[1]
distribution=args.distribution.resolve()
output=args.output.resolve()
if output.exists(): parser.error('Output already exists. Choose a new staging directory; nothing was changed.')
deps=distribution/'tools/client-patches/deps'
sys.path.insert(0,str(deps))
sys.path.insert(0,str(root/'source/server/tools'))
import native_raid_probe as probe
probe.CLIENT=distribution/'runtime/client-opendaoc/app'
import build_native_raid80_client as raid
raid.BASE_IMAGE=distribution/'tools/client-patches/baselines/game-before-native-raid.dll'
image,report=raid.build()  # Includes exact baseline/hash and x86 guards.
expected='67dcf68a37b95a93946a943b99d5e19b4a03e08cd6469275e25c7b909de21e99'
report['matchesReferenceClient']=hashlib.sha256(image).hexdigest()==expected
output.mkdir(parents=True)
(output/'game.dll').write_bytes(image)
(output/'custom9_window.xml').write_bytes(raid.window(40))
(output/'custom10_window.xml').write_bytes(raid.window(80))
main=(probe.CLIENT/'ui/uimain.xml').read_bytes()
if b'custom10_window.xml' not in main:
    assert b'</XML>' in main
    main=main.replace(b'</XML>',b'\t<Include>custom10_window.xml</Include>\r\n</XML>')
(output/'uimain.xml').write_bytes(main)
(output/'manifest.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report,indent=2))
print('Staged only. No client or server was started or modified.')
