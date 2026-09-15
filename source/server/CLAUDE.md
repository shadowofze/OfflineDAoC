# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

OpenDAoC-Core-Bots is a fork of [OpenDAoC-Core](https://github.com/OpenDAoC/OpenDAoC-Core), a Dark Age of Camelot (DAoC) server emulator built on .NET 9.0. This fork adds **player-controlled bot companions** that can join groups, follow players, and execute role-based AI (melee, tank, healer, caster, ranged).

## Build & Run Commands

```bash
# Build the solution
dotnet build "Dawn of Light.sln"

# Build in Release mode
dotnet build "Dawn of Light.sln" -c Release

# Run the server
dotnet run --project CoreServer

# Run all tests (NUnit)
dotnet test Tests/Tests.csproj

# Run a single test by name
dotnet test Tests/Tests.csproj --filter "FullyQualifiedName~TestClassName.TestMethodName"

# Docker (local dev with MariaDB)
docker-compose up
```

## Solution Structure

- **CoreBase/** — Shared utilities: logging (log4net), networking, threading, MPK file handling
- **CoreDatabase/** — ORM layer with `DataObject` base class, supports MySQL and SQLite. Query building via `WhereClause`
- **CoreServer/** — Entry point (`MainClass.cs`). Boots the server via `dotnet CoreServer.dll`
- **GameServer/** — All game logic (~2300 files). This is where nearly all development happens
- **Tests/** — NUnit tests split into `UnitTests/` and `IntegrationTests/`
- **Pathing/** — Detour-based pathfinding library

## Architecture

### Game Object Hierarchy

`GameObject` → `GameLiving` → `GameNPC` → specialized NPCs (merchants, guards, pets, etc.)
`GameObject` → `GameLiving` → `GamePlayer` (player characters)
`GameObject` → `GameLiving` → `GameNPC` → `GameBot` (player-controlled bots)

### ECS System

The server uses Entity-Component-System architecture under `GameServer/ECS-*/`:
- **ECS-Components/** — Data components (AttackComponent, CastingComponent, EffectListComponent, etc.)
- **ECS-Services/** — Processing services that operate on components each tick (AttackService, CastingService, MovementService, EffectService, etc.)
- **ECS-Effects/** — Effect lifecycle management (buffs, debuffs, crowd control)

Services run on `GameLoop` ticks. Use `GameLoop.GetListForTick<T>()` to get entities for processing.

### Bot System (`GameServer/bots/`)

The bot system added by this fork:
- **GameBot.cs** — Extends `GameNPC`. Owned by a `GamePlayer`, has a `BotRole` and role-specific AI
- **BotManager.cs** — Static singleton. Creates, spawns, despawns, deletes bots. Max 15 bots per player. Uses `ConcurrentDictionary` for thread safety
- **BotCommand.cs** — `/bot` command handler (create, spawn, despawn, follow, stay, hold, resume, invite, list, delete)
- **BotAI.cs** — Abstract base; subclasses: `BotMeleeAI`, `BotTankAI`, `BotHealerAI`, `BotCasterAI`, `BotRangedAI`
- **database/** — `BotProfile` (bot_profiles table) and `BotSettings` (bot_settings table) DataObjects, with `BotDatabase` helper

### AI / Brain System (`GameServer/ai/brain/`)

NPCs use a "brain" pattern for AI behavior. Key brains: `StandardMobBrain`, `ControlledNPCBrain` (pets), specialized class brains. The bot AI system is separate from the brain system but coexists with it.

### Key Patterns

- **Static singletons** — `GameServer.Instance`, `BotManager` methods are static
- **Database objects** — Inherit from `DataObject`, use `[DataTable]` and `[DataElement]` attributes. Persist via `GameServer.Database.SaveObject()`/`AddObject()`
- **Command system** — Classes with `[CmdAttribute]` in `GameServer/commands/`. Player commands go in `playercommands/`, GM in `gmcommands/`, admin in `admincommands/`
- **Event system** — `GameServer/events/` with event handlers registered via `GameEventMgr`

## Code Style

- **Indentation**: 4 spaces, CRLF line endings
- **Naming**: PascalCase for types, properties, methods. Interfaces prefixed with `I`
- **Braces**: Prefer braces when multiline (`when_multiline`)
- **Namespaces**: Block-scoped (not file-scoped)
- **Expression bodies**: Use for properties, indexers, accessors, lambdas; avoid for methods, constructors, operators
