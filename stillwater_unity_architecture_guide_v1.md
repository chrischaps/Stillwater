# Stillwater — Unity Project Architecture Guide (v1.0)

*A practical, opinionated structure for building Stillwater in Unity 6.2 with 2D URP and isometric tilemaps.*

---

## 0. Document Information

**Project:** Stillwater  
**Document:** Unity Project Architecture Guide  
**Version:** 1.0 (Pre-Production)  
**Engine Target:** Unity 6.2, 2D URP  
**Companion Docs:** GDD v1.0, TDD v1.0, Production Roadmap v1.0  

---

## 1. Goals & Principles

This guide defines how the Unity project is structured so that:

- Core systems (Fishing, Lake Watcher, Echo System, Anomalies) are **modular and testable**.  
- Isometric tilemaps are **clean and scalable**.  
- Async multiplayer is **decoupled** from moment-to-moment gameplay.  
- Designers can add content with minimal engineering help.  
- The project supports **long-term maintainability** and **experimentation**.

Key principles:

1. **Separation of Concerns** – Systems, presentation, and data are separated.  
2. **ScriptableObjects for Data** – Fish, anomalies, moods, etc. are defined as assets.  
3. **Event-Driven Logic** – Use an in-process event bus for decoupling.  
4. **Minimal Singletons** – At most one or two “root” managers (e.g., `GameRoot`).  
5. **Addressables for Content** – Future-friendly for DLC/patches.

---

## 2. Unity Version & Core Project Settings

### 2.1 Unity Version

- **Unity 6.2** (or latest stable LTS).  
- Template: **2D (URP)**.

### 2.2 Graphics & URP

- Create a **2D Renderer Data** asset and assign it to URP.  
- Enable:
  - Pixel snapping (where appropriate).  
  - Camera sorting layers for transparency stacking.

### 2.3 Pixel-Perfect Camera

- Add **Pixel Perfect Camera** to main camera.  
- Set:
  - Assets Pixels Per Unit (PPU) to match your art (e.g., 16, 24, or 32).  
  - Reference Resolution (e.g., 320x180, 640x360).  
- Choose **Upscale Render Texture** for crisp scaling.

### 2.4 Physics

- Use 2D physics with gravity disabled or low, since movement is mostly top-down.  
- Collision used primarily for interaction volumes, not dynamic physics.

---

## 3. Folder Structure

Suggested base layout under `Assets/`:

```text
Assets/
  Art/
    Palettes/
    Tiles/
      Ground/
      Water/
      Props/
      FX/
    Sprites/
      Characters/
      NPC/
      UI/
    Animations/
  Audio/
    Music/
    SFX/
  Code/
    Core/
    Framework/
    Fishing/
    World/
    EchoSystem/
    Anomalies/
    NPC/
    Journal/
    UI/
    Tools/
  Data/
    FishDefinitions/
    EchoTables/
    AnomalyDefinitions/
    JournalEntries/
    ZoneConfigs/
  Scenes/
    Boot/
    Title/
    Main/
    Test/
  Settings/
    URP/
    Input/
    Quality/
  Resources/        (keep minimal, prefer Addressables)
  Addressables/     (groups config)
```

---

## 4. Namespaces & Assembly Definitions

### 4.1 Namespace Convention

Use a root namespace like `Stillwater`:

- `Stillwater.Core`
- `Stillwater.Framework`
- `Stillwater.Fishing`
- `Stillwater.World`
- `Stillwater.Echo`
- `Stillwater.Anomalies`
- `Stillwater.NPC`
- `Stillwater.Journal`
- `Stillwater.UI`

### 4.2 Assembly Definitions

Create assembly definitions in each major code folder:

```text
Code/Core/Stillwater.Core.asmdef
Code/Framework/Stillwater.Framework.asmdef
Code/Fishing/Stillwater.Fishing.asmdef
Code/World/Stillwater.World.asmdef
Code/EchoSystem/Stillwater.Echo.asmdef
Code/Anomalies/Stillwater.Anomalies.asmdef
Code/NPC/Stillwater.NPC.asmdef
Code/Journal/Stillwater.Journal.asmdef
Code/UI/Stillwater.UI.asmdef
Code/Tools/Stillwater.Tools.asmdef
```

- `Core` and `Framework` referenced by most others.  
- Higher-level systems (e.g., `UI`) may depend on multiple feature assemblies.  
- Avoid cyclic dependencies.

---

## 5. Scene Structure & Loading

### 5.1 Core Scenes

Recommended scenes:

- `Boot` – Initial scene loaded by the player. Handles:
  - Config initialization  
  - Save/load  
  - Scene loading for Title/Main  
- `Title` – Main menu, settings.  
- `Main` – Core gameplay scene:
  - Tilemaps  
  - Player  
  - Systems (LakeWatcher, EchoSystem, etc.)  
- `Test_*` – One-off test scenes (fishing sandbox, anomaly playground).

### 5.2 Additive Loading

Use **additive loading** to separate concerns:

- `Main_Base` – Tilemaps, lighting, environment.  
- `Main_Systems` – Game systems managers.  
- `Main_UI` – HUD, menus.  

The `Boot` scene loads the right combination additively.

---

## 6. Core Systems & Managers

### 6.1 Root Object

In `Main_Systems`:

- `GameRoot` (MonoBehaviour)
  - Responsible for:
    - Initializing systems  
    - Wiring dependencies (service locator or DI container)  
    - Managing global lifecycle (pause, quit, etc.)

### 6.2 Service Locator / DI

Use a simple service locator (or a lightweight DI solution) to avoid scattering singletons:

- Example services:
  - `IFishingService`
  - `ILakeWatcherService`
  - `IEchoService`
  - `IWorldTimeService`
  - `IAnomalyService`
  - `IJournalService`
  - `IEventBus`

### 6.3 In-Process Event Bus

Implement an event bus in `Stillwater.Core`:

- Use C# events or a simple message system for:
  - `OnFishCaught`
  - `OnFishLost`
  - `OnAnomalyTriggered`
  - `OnEchoUpdated`
  - `OnZoneChanged`
  - `OnJournalEntryCreated`

This keeps Fishing/Anomalies/Journal decoupled.

---

## 7. Scripting Patterns

### 7.1 MonoBehaviours vs ScriptableObjects

- **MonoBehaviours:** Runtime entities in scenes (player, NPC, interactables).  
- **ScriptableObjects:** Define data and configuration:
  - Fish types  
  - Anomaly definitions  
  - Zone configurations  
  - Echo tuning curves  
  - Journal templates  

### 7.2 Data-Driven Design

Examples:

```csharp
[CreateAssetMenu(menuName = "Stillwater/Fish Definition")]
public class FishDefinition : ScriptableObject {
    public string id;
    public string displayName;
    public AnimationCurve biteWindow;
    public float rarityBase;
    public Sprite icon;
    public string flavorTextId;
}
```

This allows designers to add fish without code changes.

---

## 8. Fishing System Architecture

**Key Components:**

- `FishingController` (MonoBehaviour on player)  
- `LureController` (MonoBehaviour on lure/prefab)  
- `FishingStateMachine` (class in `Stillwater.Fishing`)  
- Supporting SO assets:
  - `FishingStateConfig`
  - `FishingTuningConfig`

Flow:

1. Player input routed to `FishingController`.  
2. `FishingController` manipulates `FishingStateMachine`.  
3. `FishingStateMachine` drives `LureController` and issues events.  
4. Events consumed by:
   - `LakeWatcher`  
   - `JournalSystem`  
   - `AnomalyManager`  

---

## 9. Lake Watcher Architecture

- `LakeWatcher` (MonoBehaviour or pure C# service)  
- Holds `LakeWatcherState` (struct/class in `Stillwater.World`).  
- Subscribes to event bus:
  - On fish caught  
  - On fish lost  
  - On idle time patterns  
  - On anomaly resolution  

Exposes:

- `GetCurrentMood()`  
- `GetModifiersForZone()`  
- `GetAnomalyProbability()`  

Other systems query this instead of reading raw mood scores.

---

## 10. Echo (Async Multiplayer) Architecture

### 10.1 Client Layer (Unity)

- `EchoClient` (service in `Stillwater.Echo`):
  - Gathers summaries over time  
  - Packages into `EchoPacket`  
  - Sends to backend via HTTP  
  - Receives `EchoCurrent`  

- `EchoApplier`:
  - Takes `EchoCurrent`  
  - Updates:
    - Fish rarity modifiers  
    - Zone biases  
    - Anomaly intensity  
    - Journal variant weights  

### 10.2 Scheduling

- Use a simple timer or `Coroutine` for periodic sync:
  - On startup  
  - Every N minutes  
  - On quit (best effort)  

---

## 11. Anomaly System Architecture

- `AnomalyManager` (service or MonoBehaviour)
  - Subscribes to LakeWatcher + Echo events  
  - Holds a list of `AnomalyDefinition` SOs  
  - Each `AnomalyDefinition` includes:
    - Conditions (mood range, time-of-day)  
    - Visual/sound payload  
    - Optional gameplay effect  

- Uses weighted random selection based on:
  - Local mood  
  - Global Echo  
  - Zone  

---

## 12. NPC & Dialogue Architecture

- `NPCController` (MonoBehaviour, per NPC)  
- `NPCDefinition` (SO):
  - Appearance rules  
  - Dialogue fragment IDs  
  - Conditions (mood, time, zone)  

- `DialogueSystem`:
  - Simple line display  
  - Optional variations based on mood/echo  
  - No complex branching structure needed  

---

## 13. Journal & Text System

- `JournalSystem` (service)
  - Receives events:
    - `OnFishCaught`
    - `OnObjectFound`
    - `OnAnomalyTriggered`
  - Combines:
    - Base template text  
    - Mood & Echo variant  
  - Writes entries to:
    - Save data  
    - UI display  

- `Localization/TextDB`:
  - Store all text & flavor strings in tables or ScriptableObjects.  
  - Optional: integrate with a CSV or external table for easier editing.

---

## 14. UI Architecture

- Use either **UGUI** or **UI Toolkit** (pick one early and stick to it).  
- Organize UI in a single `Main_UI` scene:
  - HUD Canvas  
  - Journal Panel  
  - Inventory Panel  
  - Settings Menu  

- UI controllers subscribe to event bus:
  - `OnFishingStateChanged`  
  - `OnJournalEntryCreated`  
  - `OnEchoUpdated`  

Avoid UI logic directly in gameplay systems.

---

## 15. Save/Load & Persistence

- `SaveSystem` (service in `Stillwater.Core`):
  - Local data: player position, LakeWatcherState, discovered entries, inventory, zone status.  
  - Cloud data: EchoPackets handled separately (no save of other players).  

- Use JSON or binary for local save.  
- Versioning:
  - Include `saveVersion` in root.  
  - Migration code for future updates.

---

## 16. Editor Tools

Create tools in `Code/Tools`:

- **Fishing Debug Panel** – manually set mood, echo, and test fish tables.  
- **Anomaly Tester** – trigger specific anomalies.  
- **Zone Teleporter** – quickly jump between zones in play mode.  
- **Journal Viewer** – preview journal variants given mood & echo.

Use custom inspectors and editor windows where helpful.

---

## 17. Getting Started Checklist

When creating or onboarding the Unity project:

1. [x] Create Unity 6.2 2D URP project.
2. [x] Set up:
   - [x] URP 2D Renderer
   - [x] Pixel Perfect Camera
   - [x] Sorting layers & tilemap layers
3. [x] Create folder structure as defined above.
4. [x] Create assembly definitions and namespaces.
5. [x] Implement:
   - [x] `GameRoot`
   - [x] Event bus
   - [x] `FishingController` + complete state machine (all 12 states)
6. [ ] Implement `LakeWatcher` and hook it to a debug UI.
7. [ ] Prototype a single zone (`Starting Lake`) with placeholder art.
8. [ ] Hook in minimal Echo simulation (local only).

> **Current Status:** Steps 1-5 complete. The Fishing FSM is fully implemented with all 12 states.
> Next priority is the Fishing Vertical Slice (see `fishing_vertical_slice_plan.md`) before returning to steps 6-8.

Once these are in place, the project is architecturally ready to follow the GDD, TDD, and Production Roadmap.

---

# END OF DOCUMENT
