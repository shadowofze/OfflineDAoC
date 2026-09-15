"""Test asset-tool source against read-only fixtures in an extracted distribution.

All editing/rollback tests use temporary copies. Neither this command nor the tests
deploy to the supplied client. Source and complete-download layouts are both supported.
"""
from pathlib import Path
import argparse, sys, unittest

p=argparse.ArgumentParser()
p.add_argument('--distribution',type=Path,required=True)
args=p.parse_args()
source=Path(__file__).resolve().parent/'asset-tool'
sys.path.insert(0,str(source))
import asset_tool
asset_tool.CLIENT=args.distribution.resolve()/'runtime/client-opendaoc/app'
suite=unittest.defaultTestLoader.discover(str(source/'tests'))
result=unittest.TextTestRunner(verbosity=2).run(suite)
sys.exit(0 if result.wasSuccessful() else 1)
