"""Build the bounty button XML for the verified v0.3/v0.31 client UI.

This creates separate output files and never changes the input client. The
button opens the current map; the server marker is visible only inside the
assigned zone or dungeon. Do not use this on an unknown or customized UI.
"""

import argparse
import hashlib
from pathlib import Path
import xml.etree.ElementTree as ET


EXPECTED_SHA256 = {
    "isles": "d43872c2f96608a20aaad4710fe6e25663305d492a252a3decd0b4d45080d98c",
    "atlantis": "b1d07d306cfb51f50299d3c81ae6c9491c8ba205715d054bdc96bfe1fca46864",
}
RELATIVE_NAME = Path("new_quest_journal_window.xml")

BUTTON_TEMPLATE = """\t<ButtonTemplate>
\t\t<Name>bounty_map_button</Name>
\t\t<Parent>none</Parent>
\t\t<Size><X>100</X><Y>16</Y></Size>
\t\t<Font>
\t\t\t<Name>button_large</Name>
\t\t\t<ColorNormal><R>192</R><G>192</G><B>192</B><A>255</A></ColorNormal>
\t\t\t<ColorPressed><R>255</R><G>192</G><B>0</B><A>255</A></ColorPressed>
\t\t\t<ColorHighlit><R>255</R><G>255</G><B>255</B><A>255</A></ColorHighlit>
\t\t\t<ColorDisabled><R>128</R><G>128</G><B>128</B><A>255</A></ColorDisabled>
\t\t</Font>
\t\t<Texture>
\t\t\t<TextureName>page3</TextureName>
\t\t\t<Normal><X>155</X><Y>222</Y></Normal>
\t\t\t<Pressed><X>155</X><Y>239</Y></Pressed>
\t\t\t<NormalHighlit><X>155</X><Y>205</Y></NormalHighlit>
\t\t\t<PressedHighlit><X>155</X><Y>239</Y></PressedHighlit>
\t\t\t<Disabled><X>155</X><Y>222</Y></Disabled>
\t\t</Texture>
\t</ButtonTemplate>
"""

BUTTON_DEF = """\t\t<ButtonDef>
\t\t\t<Alignment><OffsetRight>true</OffsetRight></Alignment>
\t\t\t<TemplateName>bounty_map_button</TemplateName>
\t\t\t<OnClickEvent>ToggleMap</OnClickEvent>
\t\t\t<ControlId>1006</ControlId>
\t\t\t<Label>BOUNTY MAP</Label>
\t\t\t<Position><X>20</X><Y>24</Y></Position>
\t\t</ButtonDef>
"""


def patch_ui_bytes(theme: str, original: bytes) -> bytes:
    actual = hashlib.sha256(original).hexdigest()
    expected = EXPECTED_SHA256[theme]
    if actual != expected:
        raise ValueError(f"Unsupported {theme} journal SHA-256 {actual}; expected {expected}")

    text = original.decode("latin-1")
    newline = "\r\n" if "\r\n" in text else "\n"
    template_anchor = "\t<WindowTemplate>\n\t\t<Name>new_quest_journal</Name>"
    button_anchor = "\t\t</ButtonDef>\n\t\t<HorizontalResizeImageDef>"
    normalized = text.replace("\r\n", "\n")
    if normalized.count(template_anchor) != 1 or normalized.count(button_anchor) != 1:
        raise ValueError(f"Unexpected {theme} journal structure; refusing patch")
    normalized = normalized.replace(template_anchor, BUTTON_TEMPLATE + template_anchor, 1)
    normalized = normalized.replace(button_anchor,
                                    "\t\t</ButtonDef>\n" + BUTTON_DEF +
                                    "\t\t<HorizontalResizeImageDef>", 1)
    result = normalized.replace("\n", newline).encode("latin-1")
    root = ET.fromstring(result)
    assert root.find(".//ButtonTemplate[Name='bounty_map_button']") is not None
    assert root.find(".//ButtonDef[Label='BOUNTY MAP']/OnClickEvent").text == "ToggleMap"
    return result


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("client_app", type=Path, help="verified client app folder")
    parser.add_argument("output", type=Path, help="new output folder")
    arguments = parser.parse_args()
    prepared = {}
    for theme in EXPECTED_SHA256:
        source = arguments.client_app / "ui" / theme / RELATIVE_NAME
        destination = arguments.output / "ui" / theme / RELATIVE_NAME
        if source.resolve() == destination.resolve() or destination.exists():
            raise ValueError(f"Output must be a new file: {destination}")
        prepared[destination] = patch_ui_bytes(theme, source.read_bytes())
    for destination, data in prepared.items():
        destination.parent.mkdir(parents=True, exist_ok=True)
        destination.write_bytes(data)
        print(f"Prepared: {destination} ({len(data)} bytes)")
