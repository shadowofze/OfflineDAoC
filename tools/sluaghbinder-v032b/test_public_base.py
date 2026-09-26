"""Synthetic checks for the public v0.32 world release gate."""

from __future__ import annotations

import sqlite3
import subprocess
import tempfile
import unittest
from pathlib import Path

from check_public_base import ORO_ID, validate_public_base


PATCHER = (
    Path(__file__).resolve().parents[2]
    / "source/tools/OfflineDaoc.SluaghbinderPatch/bin/Release/net10.0/OfflineDaoc.SluaghbinderPatch.exe"
)


class PublicBaseTests(unittest.TestCase):
    def setUp(self) -> None:
        self.folder = tempfile.TemporaryDirectory()
        self.addCleanup(self.folder.cleanup)
        self.database = Path(self.folder.name) / "world.sqlite3.db"
        with sqlite3.connect(self.database) as db:
            for table in ("Account", "DOLCharacters", "Inventory", "offline_world_bots"):
                db.execute(f'CREATE TABLE "{table}" (Name TEXT)')
            db.execute(
                "CREATE TABLE Mob (Mob_ID TEXT, Name TEXT, Region INTEGER, ClassType TEXT, Level INTEGER)"
            )
            db.execute(
                "INSERT INTO Mob VALUES (?, 'High Lord Oro', 249, 'DOL.GS.HighLordOro', 69)",
                (ORO_ID,),
            )

    def test_clean_public_world_passes(self) -> None:
        validate_public_base(self.database)

    def test_private_account_is_rejected(self) -> None:
        with sqlite3.connect(self.database) as db:
            db.execute("INSERT INTO Account VALUES ('private')")
        with self.assertRaisesRegex(ValueError, "private Account"):
            validate_public_base(self.database)

    def test_isolated_boss_level_is_rejected(self) -> None:
        with sqlite3.connect(self.database) as db:
            db.execute("UPDATE Mob SET Level=67 WHERE Mob_ID=?", (ORO_ID,))
        with self.assertRaisesRegex(ValueError, "level 69"):
            validate_public_base(self.database)

    @unittest.skipUnless(PATCHER.is_file(), "Build the Release patcher to check its read-only guard")
    def test_release_patcher_accepts_clean_world_read_only(self) -> None:
        before = self.database.read_bytes()
        result = subprocess.run(
            [str(PATCHER), "--verify-v032-world", "69", "--database", str(self.database)],
            capture_output=True, text=True, check=False,
        )
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(self.database.read_bytes(), before)

    @unittest.skipUnless(PATCHER.is_file(), "Build the Release patcher to check its read-only guard")
    def test_release_patcher_rejects_isolated_boss_level(self) -> None:
        with sqlite3.connect(self.database) as db:
            db.execute("UPDATE Mob SET Level=67 WHERE Mob_ID=?", (ORO_ID,))
        result = subprocess.run(
            [str(PATCHER), "--verify-v032-world", "69", "--database", str(self.database)],
            capture_output=True, text=True, check=False,
        )
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("level 69", result.stderr)


if __name__ == "__main__":
    unittest.main()
