# OpenDAoC
[![Build and Release](https://github.com/OpenDAoC/OpenDAoC-Core/actions/workflows/build-and-release.yml/badge.svg)](https://github.com/OpenDAoC/OpenDAoC-Core/actions/workflows/build-and-release.yml)

## About

OpenDAoC is an emulator for Dark Age of Camelot (DAoC) servers, originally a fork of the [DOLSharp](https://github.com/Dawn-of-Light/DOLSharp) project.

Now completely rewritten with ECS architecture, OpenDAoC ensures performance and scalability for many players, providing a robust platform for creating and managing DAoC servers.

While the project focuses on recreating the DAoC 1.65 experience, it can be adapted for any patch level.

## Documentation

The easiest way to get started with OpenDAoC is to use Docker. Check out the `docker-compose.yml` file in the repository root for an example setup.

For detailed instructions and additional setup options, refer to the full [OpenDAoC Documentation](https://www.opendaoc.com/docs/).

## Releases

Releases for OpenDAoC are available at [OpenDAoC Releases](https://github.com/OpenDAoC/OpenDAoC-Core/releases).

OpenDAoC is also available as a Docker image, which can be pulled from the following registries:

- [GitHub Container Registry](https://ghcr.io/opendaoc/opendaoc-core) (recommended): `ghcr.io/opendaoc/opendaoc-core/opendaoc:latest`
- [Docker Hub](https://hub.docker.com/repository/docker/claitz/opendaoc/): `claitz/opendaoc:latest`

For detailed instructions and additional setup options, refer to the documentation.

## Companion Repositories

Several companion repositories are part of the [OpenDAoC project](https://github.com/OpenDAoC).

Some of the main repositories include:

- [OpenDAoC Database v1.65](https://github.com/OpenDAoC/OpenDAoC-Database)
- [Account Manager](https://github.com/OpenDAoC/opendaoc-accountmanager)
- [Client Launcher](https://github.com/OpenDAoC/OpenDAoC-Launcher)

## License

OpenDAoC is licensed under the [GNU General Public License (GPL)](https://choosealicense.com/licenses/gpl-3.0/) v3 to serve the DAoC community and promote open-source development.  
See the [LICENSE](LICENSE) file for more details.

## DaoC-Code-Bots

This fork adds **player-controlled bot companions** to OpenDAoC. Bots function like real players -- they have proper class specs, stats, equipment, spells, styles, and a full combat AI brain. They join your group, follow you, heal, tank, nuke, and fight autonomously based on their class.

### Features

- **37 supported classes** across all three realms (Albion, Hibernia, Midgard)
- **Full class identity** -- proper stats, specializations, spell lines, styles, and abilities
- **Equipment system** -- bots equip appropriate weapons, armor, and jewelry for their class and level
- **Combat AI (BotBrain)** -- FSM-based brain ticked by the game loop with 4 states: IDLE, FOLLOW, AGGRO, PASSIVE
  - Healers prioritize emergency heals, cures, and group healing
  - Casters use offensive spells, bolts, crowd control, and quickcast when interrupted
  - Melee classes use positional styles, chain styles, taunts, and defensive abilities
  - All bots assist the owner and defend group members automatically
- **Persistence** -- bots save to database and auto-respawn on owner login
- **Group integration** -- bots are auto-invited to group and auto-follow on spawn

### Bot Commands

All commands use `/bot` and are available to all players.

| Command | Description |
|---------|-------------|
| `/bot create <name> <classId> <raceId> <genderId>` | Create a new bot, auto-spawns and follows you |
| `/bot spawn <name>` | Spawn a saved bot from the database |
| `/bot despawn <name>` | Remove bot from world (keeps in database) |
| `/bot delete <name>` | Permanently delete a bot |
| `/bot list` | List all your saved and active bots |
| `/bot invite <name>` | Invite a bot to your group |
| `/bot follow [name]` | Bot follows you (resumes AI) |
| `/bot stay [name]` | Bot holds position (IDLE state) |
| `/bot hold [name]` | Suspend bot AI entirely (PASSIVE state) |
| `/bot resume [name]` | Resume bot AI (FOLLOW state) |

Commands that take `[name]` are optional -- if omitted, the command targets your currently selected bot.

### Example Usage

```
/bot create Aylia 6 1 0      -- Create a level-matched Cleric (class 6), Briton (race 1), Male (gender 0)
/bot create Thorin 2 1 0      -- Create an Armsman
/bot create Shadowfax 23 9 1  -- Create an Infiltrator, Saracen, Female
/bot list                      -- See all your bots
/bot hold Aylia                -- Pause Aylia's AI
/bot resume Aylia              -- Resume Aylia's AI
/bot despawn Thorin            -- Despawn Thorin (saved for later)
/bot spawn Thorin              -- Bring Thorin back
/bot delete Shadowfax          -- Permanently delete Shadowfax
```

### Class IDs (Quick Reference)

**Albion:** Armsman (2), Cabalist (13), Cleric (6), Friar (10), Infiltrator (9), Mercenary (11), Minstrel (4), Paladin (1), Reaver (19), Scout (3), Sorcerer (8), Theurgist (5), Wizard (7)

**Hibernia:** Bard (48), Blademaster (43), Champion (45), Druid (47), Eldritch (40), Enchanter (41), Hero (44), Mentalist (42), Nightshade (49), Ranger (50), Valewalker (56), Warden (46)

**Midgard:** Berserker (31), Bonedancer (30), Healer (26), Hunter (25), Runemaster (29), Savage (32), Shadowblade (23), Shaman (28), Skald (24), Spiritmaster (27), Thane (21), Warrior (22)

### Race IDs (Quick Reference)

| ID | Race | Realm |
|----|------|-------|
| 1 | Briton | Albion |
| 2 | Avalonian | Albion |
| 3 | Highlander | Albion |
| 4 | Saracen | Albion |
| 13 | Inconnu | Albion |
| 16 | Half Ogre | Albion |
| 19 | Minotaur (Korazh) | Albion |
| 5 | Norseman | Midgard |
| 6 | Troll | Midgard |
| 7 | Dwarf | Midgard |
| 8 | Kobold | Midgard |
| 14 | Valkyn | Midgard |
| 17 | Frostalf | Midgard |
| 20 | Minotaur (Deifrang) | Midgard |
| 9 | Celt | Hibernia |
| 10 | Firbolg | Hibernia |
| 11 | Elf | Hibernia |
| 12 | Lurikeen | Hibernia |
| 15 | Sylvan | Hibernia |
| 18 | Shar | Hibernia |
| 21 | Minotaur (Graoch) | Hibernia |
