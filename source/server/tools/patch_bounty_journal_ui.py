"""Build the optional quest-journal BOUNTY MAP button in a separate output tree.

The v0.3 and v0.31 public clients use the same two verified journal XML files.
This script refuses unknown or already-modified layouts and never edits its
input directory. The button invokes the client's existing ToggleMap action;
the server marker is visible only in the bounty target's zone or dungeon.
"""

import argparse
import hashlib
from pathlib import Path


BASE_HASHES = {
    'isles': 'd43872c2f96608a20aaad4710fe6e25663305d492a252a3decd0b4d45080d98c',
    'atlantis': 'b1d07d306cfb51f50299d3c81ae6c9491c8ba205715d054bdc96bfe1fca46864',
}
JOURNAL_NAME = 'new_quest_journal_window.xml'


def build_patch(source: bytes, skin: str) -> bytes:
    expected = BASE_HASHES[skin]
    actual = hashlib.sha256(source).hexdigest()
    if actual != expected:
        raise ValueError(f'Unverified {skin} journal XML ({actual}); refusing to patch')

    newline = b'\r\n'
    if source.count(newline) != source.count(b'\n'):
        raise ValueError('Unexpected XML line endings')

    template = newline.join((
        b'\t<ButtonTemplate>',
        b'\t\t<Name>bounty_map_button</Name>',
        b'\t\t<Parent>none</Parent>',
        b'\t\t<Size><X>100</X><Y>16</Y></Size>',
        b'\t\t<Font>',
        b'\t\t\t<Name>button_large</Name>',
        b'\t\t\t<ColorNormal><R>192</R><G>192</G><B>192</B><A>255</A></ColorNormal>',
        b'\t\t\t<ColorPressed><R>255</R><G>192</G><B>0</B><A>255</A></ColorPressed>',
        b'\t\t\t<ColorHighlit><R>255</R><G>255</G><B>255</B><A>255</A></ColorHighlit>',
        b'\t\t\t<ColorDisabled><R>128</R><G>128</G><B>128</B><A>255</A></ColorDisabled>',
        b'\t\t</Font>',
        b'\t\t<Texture>',
        b'\t\t\t<TextureName>page3</TextureName>',
        b'\t\t\t<Normal><X>155</X><Y>222</Y></Normal>',
        b'\t\t\t<Pressed><X>155</X><Y>239</Y></Pressed>',
        b'\t\t\t<NormalHighlit><X>155</X><Y>205</Y></NormalHighlit>',
        b'\t\t\t<PressedHighlit><X>155</X><Y>239</Y></PressedHighlit>',
        b'\t\t\t<Disabled><X>155</X><Y>222</Y></Disabled>',
        b'\t\t</Texture>',
        b'\t</ButtonTemplate>',
        b'',
    ))
    button = newline.join((
        b'\t\t<ButtonDef>',
        b'\t\t\t<Alignment>',
        b'\t\t\t\t<OffsetRight>true</OffsetRight>',
        b'\t\t\t</Alignment>',
        b'\t\t\t<TemplateName>bounty_map_button</TemplateName>',
        b'\t\t\t<OnClickEvent>ToggleMap</OnClickEvent>',
        b'\t\t\t<ControlId>1006</ControlId>',
        b'\t\t\t<Label>BOUNTY MAP</Label>',
        b'\t\t\t<Position>',
        b'\t\t\t\t<X>20</X>',
        b'\t\t\t\t<Y>24</Y>',
        b'\t\t\t</Position>',
        b'\t\t</ButtonDef>',
        b'',
    ))

    window_anchor = b'\t<WindowTemplate>' + newline + b'\t\t<Name>new_quest_journal</Name>'
    if source.count(window_anchor) != 1:
        raise ValueError('Journal window anchor was not unique')
    result = source.replace(window_anchor, template + window_anchor)

    remove_anchor = b'<OnClickEvent>RemoveQuest</OnClickEvent>'
    if result.count(remove_anchor) != 1:
        raise ValueError('RemoveQuest button anchor was not unique')
    start = result.index(remove_anchor)
    end = result.find(b'\t\t</ButtonDef>' + newline, start)
    if end < 0:
        raise ValueError('RemoveQuest button did not close')
    end += len(b'\t\t</ButtonDef>' + newline)
    result = result[:end] + button + result[end:]

    if result.count(b'<Name>bounty_map_button</Name>') != 1 or result.count(b'<Label>BOUNTY MAP</Label>') != 1:
        raise ValueError('Bounty button insertion failed')
    return result


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('source_ui', type=Path, help='read-only client app/ui directory')
    parser.add_argument('output_ui', type=Path, help='new output directory')
    args = parser.parse_args()
    if args.source_ui.resolve() == args.output_ui.resolve():
        raise SystemExit('Input and output directories must differ')
    if args.output_ui.exists():
        raise SystemExit('Output directory already exists')

    results = {}
    for skin in BASE_HASHES:
        source = args.source_ui / skin / JOURNAL_NAME
        results[skin] = build_patch(source.read_bytes(), skin)
    for skin, data in results.items():
        destination = args.output_ui / skin / JOURNAL_NAME
        destination.parent.mkdir(parents=True, exist_ok=True)
        destination.write_bytes(data)
        print(f'{skin}: {destination} ({hashlib.sha256(data).hexdigest()})')


if __name__ == '__main__':
    main()
