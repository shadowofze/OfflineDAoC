"""Read-only validation for the verified bounty journal UI patch."""

import argparse
from pathlib import Path
import xml.etree.ElementTree as ET

from patch_bounty_journal_ui import EXPECTED_SHA256, RELATIVE_NAME, patch_ui_bytes


def validate(client_app: Path) -> None:
    for theme in EXPECTED_SHA256:
        source = client_app / "ui" / theme / RELATIVE_NAME
        original = source.read_bytes()
        patched = patch_ui_bytes(theme, original)
        root = ET.fromstring(patched)
        assert len(root.findall(".//ButtonTemplate[Name='bounty_map_button']")) == 1
        assert len(root.findall(".//ButtonDef[Label='BOUNTY MAP']")) == 1
        assert root.find(".//ButtonDef[Label='BOUNTY MAP']/OnClickEvent").text == "ToggleMap"
        try:
            patch_ui_bytes(theme, original[:-1] + bytes([original[-1] ^ 1]))
        except ValueError as error:
            assert "Unsupported" in str(error)
        else:
            raise AssertionError(f"{theme}: modified client UI passed the hash guard")
        print(f"PASS: {theme} BOUNTY MAP opens the current map; unknown input rejected")
    print("No files were written.")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("client_app", type=Path)
    validate(parser.parse_args().client_app)
