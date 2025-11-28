# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Stillwater is a contemplative isometric fishing game built in Unity 6.2 with 2D URP. The game features an esoteric fishing system, asynchronous multiplayer ("Strand System"), and subtle surrealism driven by player behavior patterns.

## Technical Stack

- **Engine:** Unity 6.2, 2D URP
- **Rendering:** Pixel-perfect camera, isometric tilemaps (Z as Y sorting)
- **Backend:** Cloud functions (Unity Cloud Code/AWS Lambda/Firebase) with lightweight JSON packets (<1KB)
- **Networking:** REST-style async polling, no real-time multiplayer

## Architecture

### Namespace Convention
Root namespace `Stillwater` with sub-namespaces:
- `Stillwater.Core` - Event bus, save system, shared utilities
- `Stillwater.Framework` - Service locator/DI infrastructure
- `Stillwater.Fishing` - Fishing FSM, lure controller, state configs
- `Stillwater.World` - Lake Watcher (mood manager), zone configs
- `Stillwater.Echo` - Async multiplayer client, EchoPacket/EchoCurrent handling
- `Stillwater.Anomalies` - Anomaly manager and definitions
- `Stillwater.NPC` - NPC controllers and dialogue
- `Stillwater.Journal` - Journal system, flavor text
- `Stillwater.UI` - HUD, menus, panels

### Core Systems
1. **Fishing FSM** - ScriptableObject-driven state machine (Casting → Drift → Stillness → BiteCheck → Hook → Reel → Caught/Lost)
2. **Lake Watcher** - Tracks player behavior, maintains mood scores (stillness, curiosity, loss, disruption), drives anomaly probability
3. **Echo System** - Async multiplayer that uploads behavior summaries and receives global influence (fish rarity, weather seeds, anomaly intensity)
4. **Anomaly Manager** - Triggers surreal events based on mood thresholds and echo state

### Design Patterns
- **Event-Driven:** Use in-process event bus (`OnFishCaught`, `OnAnomalyTriggered`, `OnEchoUpdated`, etc.)
- **Data-Driven:** Fish, anomalies, zones, journal entries defined as ScriptableObjects
- **Service Locator:** `GameRoot` initializes and wires dependencies; avoid scattering singletons
- **Additive Scene Loading:** Separate scenes for base world, systems, and UI

## Folder Structure (when Unity project exists)

```
Assets/
  Art/Palettes, Tiles/, Sprites/, Animations/
  Audio/Music/, SFX/
  Code/Core/, Framework/, Fishing/, World/, EchoSystem/, Anomalies/, NPC/, Journal/, UI/, Tools/
  Data/FishDefinitions/, EchoTables/, AnomalyDefinitions/, JournalEntries/, ZoneConfigs/
  Scenes/Boot/, Title/, Main/, Test/
  Settings/URP/, Input/, Quality/
```

## Key Data Structures

- `FishDefinition` - Fish type with bite window curve, rarity, flavor text ID
- `EchoPacket` - Player behavior summary sent to cloud (stillness avg, ritual patterns, zone distribution)
- `EchoCurrent` - Global state received from cloud (fish rarity modifiers, weather seed, anomaly intensity)
- `LakeWatcherState` - Local mood scores (stillness, curiosity, loss, disruption)
- `JournalEntry` - Text with base + echo variant + mood frame

## Tilemap Layers

```
Tilemap_Ground
Tilemap_Props
Tilemap_Water
Tilemap_Interactables
Tilemap_FX
```

Sorting uses "Z as Y" - sprite position.y determines render order. Character/prop sprites pivot at feet.

## Game Zones

Starting Lake → Forest River → Crescent Cove → Night Marsh → Deepwater Inlet

Each zone has distinct fish, anomalies, and echo-influenced variables.
