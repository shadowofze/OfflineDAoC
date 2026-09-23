"""Focused ownership checks for the optional class's static DB export."""

from __future__ import annotations

import sqlite3
import unittest

from build_overlay import EQUIPMENT_TEMPLATE_IDS, select_rows


class EquipmentOverlayTests(unittest.TestCase):
    def test_only_named_sluaghbinder_templates_are_exported(self) -> None:
        with sqlite3.connect(":memory:") as db:
            db.execute(
                "CREATE TABLE NPCEquipment (TemplateID TEXT, Slot INTEGER, Model INTEGER, Effect INTEGER)"
            )
            for index, template in enumerate(EQUIPMENT_TEMPLATE_IDS):
                db.execute(
                    "INSERT INTO NPCEquipment VALUES (?, ?, ?, ?)",
                    (template, 10, 400 + index, 54 if "dullahan" in template else 0),
                )
            db.execute("INSERT INTO NPCEquipment VALUES ('some_other_pet', 10, 999, 0)")
            db.execute("INSERT INTO NPCEquipment VALUES ('sluagh_unrelated_name', 10, 998, 0)")

            selected = select_rows(db, "NPCEquipment")

        self.assertEqual(len(selected), len(EQUIPMENT_TEMPLATE_IDS))
        self.assertEqual({r["TemplateID"] for r in selected}, set(EQUIPMENT_TEMPLATE_IDS))
        self.assertNotIn("some_other_pet", {r["TemplateID"] for r in selected})


if __name__ == "__main__":
    unittest.main()
