# AGENTS.md - Guidance for AI Agents

This file provides guidance for AI coding agents working in the OpenDAoC-Core-Bots repository.

## Project Overview

OpenDAoC-Core-Bots is a .NET 10.0 Dark Age of Camelot server emulator with player-controlled bot companions. It's a fork of [OpenDAoC-Core](https://github.com/OpenDAoC/OpenDAoC-Core).

## Build Commands

```bash
# Build the solution (Debug)
dotnet build "Dawn of Light.sln"

# Build in Release mode
dotnet build "Dawn of Light.sln" -c Release

# Run the server
dotnet run --project CoreServer

# Run all tests (NUnit)
dotnet test Tests/Tests.csproj

# Run a single test by name
dotnet test Tests/Tests.csproj --filter "FullyQualifiedName~UT_SessionIdAllocator.Allocate_ShouldAllocateUniqueSessionIds_Concurrently"

# Run tests matching a pattern
dotnet test Tests/Tests.csproj --filter "FullyQualifiedName~UT_CharacterStat"

# Docker (local dev with MariaDB)
docker-compose up
```

## Solution Structure

| Directory | Purpose |
|-----------|---------|
| CoreBase/ | Shared utilities: logging (log4net), networking, threading, MPK file handling |
| CoreDatabase/ | ORM layer with `DataObject` base class, MySQL and SQLite support |
| CoreServer/ | Entry point (`MainClass.cs`) |
| GameServer/ | All game logic (~2300 files). Most development happens here |
| Tests/ | NUnit tests in `UnitTests/` and `IntegrationTests/` |
| Pathing/ | Detour-based pathfinding library |
| GameServer/bots/ | Bot system (GameBot, BotManager, BotBrain, BotCommand, etc.) |
| GameServer/ECS-*/ | Entity-Component-System architecture (Components, Services, Effects) |

## Code Style Guidelines

### Formatting (from .editorconfig)

- **Indentation**: 4 spaces (no tabs)
- **Line endings**: CRLF
- **Namespace style**: Block-scoped (not file-scoped)
- **Braces**: Prefer braces when multiline
- **Expression bodies**: Use for properties, indexers, accessors, lambdas. Avoid for methods, constructors, operators, local functions.

### Naming Conventions

- **Types/Classes**: PascalCase (e.g., `GameBot`, `BotManager`)
- **Interfaces**: Prefix with `I` (e.g., `IGamePlayer`, `ICharacterClass`)
- **Properties/Methods/Events**: PascalCase
- **Private fields**: Can use `_camelCase` or `m_camelCase`

### Import Style

- Place `using` statements outside the namespace declaration
- Prefer simple using statements where appropriate
- Order: System namespaces first, then project namespaces

### Example Class Structure

```csharp
using System;
using System.Collections.Generic;
using DOL.Database;

namespace DOL.GS
{
    public class MyClass : BaseClass
    {
        private readonly ILog log = LogManager.GetLogger(typeof(MyClass));
        
        public string MyProperty { get; private set; }
        
        public void MyMethod()
        {
            // code here
        }
    }
}
```

### Logging

Use log4net for logging:
```csharp
using log4net;

private static readonly ILog log = LogManager.GetLogger(typeof(MyClass));

if (log.IsErrorEnabled)
    log.Error("Error message");
```

### Error Handling

- Wrap service ticks in try-catch blocks
- Log exceptions with appropriate severity
- Use `GameServiceUtils.HandleServiceException` for ECS service errors

### Database Objects

- Inherit from `DataObject`
- Use `[DataTable]` and `[DataElement]` attributes
- Save via `GameServer.Database.SaveObject()` or `AddObject()`

### Command System

- Classes with `[CmdAttribute]` go in `GameServer/commands/`
- Player commands: `playercommands/`
- GM commands: `gmcommands/`
- Admin commands: `admincommands/`

### ECS System

Services use `GameLoop.GetListForTick<T>()` or `ServiceObjectStore` to get entities for processing. Services should:
- Inherit from `GameServiceBase`
- Implement `Tick()` method
- Handle exceptions gracefully

### Bot System

- `GameBot.cs` extends `GameNPC`, implements `IGamePlayer`
- Bot AI in `BotBrain.cs` and subclasses (BotMeleeAI, BotHealerAI, etc.)
- Bot commands handled by `BotCommand.cs`
- Bot data persisted via `BotDatabase.cs`

## Important Patterns

- **Static singletons**: `GameServer.Instance`, `BotManager` methods are static
- **Event system**: Handlers registered via `GameEventMgr`
- **Game loop**: Services tick on `GameLoop` intervals
- **Components**: ECS components attached to game objects (AttackComponent, EffectListComponent, etc.)

## Key Files

- `GameServer/GameServer.cs` - Main game server class
- `GameServer/GamePlayer.cs` - Player character base class
- `GameServer/bots/GameBot.cs` - Bot implementation
- `GameServer/commands/playercommands/PlayerCommand.cs` - Player command handler base

## Testing

- Use NUnit with `[TestFixture]` attribute
- Test methods use `[Test]` attribute
- Use `Assert.That(actual, Is.EqualTo(expected))` style assertions

```csharp
using NUnit.Framework;

[TestFixture]
public class UT_MyTest
{
    [Test]
    public void MyTestMethod()
    {
        Assert.That(result, Is.EqualTo(expected));
    }
}
```

## Common Issues

- Bots must implement `IGamePlayer` interface for full player functionality
- ECS components on GameLiving use lowercase names but expose PascalCase via explicit interface implementation
- Database operations require `GameServer.Database` access
- Thread safety important for concurrent game loop processing
