"""Use the verified isolated character provisioner with Necromancer-specific values."""
from pathlib import Path
source = (Path(__file__).parent / 'create_raidtester.py').read_text()
changes = {
    'Raidtester': 'Abomtest', 'Mercenary': 'Necromancer',
    'Class=11': 'Class=12', 'ClassId=11': 'ClassId=12', "'11' in": "'12' in", 'classId=11': 'classId=12',
    'Strength=115, Dexterity=93, Constitution=85, Quickness=60': 'Strength=60, Dexterity=93, Constitution=60, Quickness=75',
    'Intelligence=60': 'Intelligence=115', 'Mana=0': 'Mana=1000', 'ActiveWeaponSlot=1': 'ActiveWeaponSlot=2',
    "Slash|50;Dual Wield|50;Parry|28": "Deathsight|49;Painworking|22"
}
for before, after in changes.items():
    assert before in source, before
    source = source.replace(before, after)
exec(compile(source, str(Path(__file__)), 'exec'))
