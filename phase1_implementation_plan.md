# Phase 1 Implementation Plan — Pre-Production Finalization

*Granular tickets targeting ~4 hours engineering time each*

---

## Epic 1: Unity Project Foundation

### STILL-001: Create Unity 6.2 Project with 2D URP Template
**Estimate:** 2 hours

- Create new Unity 6.2 project using 2D URP template
- Verify URP 2D Renderer is active
- Set project to linear color space
- Configure initial quality settings for target platforms (PC, Steam Deck)

**Acceptance Criteria:**
- Project opens without errors
- 2D URP pipeline renders a test sprite correctly

---

### STILL-002: Establish Folder Structure
**Estimate:** 2 hours

Create the following folder hierarchy under `Assets/`:
```
Art/Palettes, Tiles/Ground, Tiles/Water, Tiles/Props, Tiles/FX, Sprites/Characters, Sprites/NPC, Sprites/UI, Animations
Audio/Music, SFX
Code/Core, Framework, Fishing, World, EchoSystem, Anomalies, NPC, Journal, UI, Tools
Data/FishDefinitions, EchoTables, AnomalyDefinitions, JournalEntries, ZoneConfigs
Scenes/Boot, Title, Main, Test
Settings/URP, Input, Quality
```

**Acceptance Criteria:**
- All folders exist and are committed
- README or .keep files prevent empty folder loss in git

---

### STILL-003: Create Assembly Definitions
**Estimate:** 3 hours

Create assembly definitions for each code module:
- `Stillwater.Core.asmdef`
- `Stillwater.Framework.asmdef`
- `Stillwater.Fishing.asmdef`
- `Stillwater.World.asmdef`
- `Stillwater.Echo.asmdef`
- `Stillwater.Anomalies.asmdef`
- `Stillwater.NPC.asmdef`
- `Stillwater.Journal.asmdef`
- `Stillwater.UI.asmdef`
- `Stillwater.Tools.asmdef` (Editor only)

Configure references:
- Core/Framework referenced by all feature assemblies
- No cyclic dependencies

**Acceptance Criteria:**
- All asmdefs compile without errors
- Dependency graph is acyclic

---

### STILL-004: Configure Pixel-Perfect Camera
**Estimate:** 3 hours

- Add Pixel Perfect Camera component to main camera
- Set Assets Pixels Per Unit (PPU) — determine value based on art direction (16, 24, or 32)
- Set Reference Resolution (e.g., 320x180 or 640x360)
- Enable Upscale Render Texture mode
- Test with placeholder sprite at various screen resolutions

**Acceptance Criteria:**
- Sprites render pixel-crisp at target resolution
- No sub-pixel jitter when camera moves
- Works correctly at 1080p and 720p

---

### STILL-005: Configure Sorting Layers
**Estimate:** 2 hours

Create sorting layers in order:
1. Background
2. Water
3. Ground
4. Props_Back
5. Characters
6. Props_Front
7. FX
8. UI

Document layer usage in code comments or README.

**Acceptance Criteria:**
- Sorting layers configured in Project Settings
- Test scene demonstrates correct layering

---

## Epic 2: Tilemap & Isometric Rendering

### STILL-006: Configure Isometric Grid Settings
**Estimate:** 3 hours

- Create Grid GameObject with Isometric cell layout
- Set cell size to match art tile dimensions
- Configure cell swizzle for isometric projection
- Document grid settings in architecture notes

**Acceptance Criteria:**
- Grid displays isometric guidelines in Scene view
- Tile placement snaps correctly to isometric positions

---

### STILL-007: Create Tilemap Layer GameObjects
**Estimate:** 2 hours

Create child Tilemaps under the Grid:
- `Tilemap_Ground`
- `Tilemap_Water`
- `Tilemap_Props`
- `Tilemap_Interactables`
- `Tilemap_FX`

Assign appropriate sorting layers and order-in-layer values.

**Acceptance Criteria:**
- All tilemap layers exist as scene hierarchy
- Sorting order renders Ground < Water < Props < Interactables < FX

---

### STILL-008: Create Placeholder Isometric Tiles
**Estimate:** 4 hours

Create simple programmer art tiles for testing:
- 1 ground tile (grass/dirt)
- 1 water tile (blue)
- 1 prop tile (rock or tree)
- 1 interactable tile (fishing spot indicator)

Import as sprites with correct PPU and pivot points (bottom-center for isometric).

**Acceptance Criteria:**
- Tiles paint correctly onto isometric tilemaps
- No visual gaps between adjacent tiles

---

### STILL-009: Implement Z-as-Y Depth Sorting
**Estimate:** 4 hours

- Configure Transparency Sort Mode to Custom Axis (0, 1, 0)
- Create `DepthSortByY` component for dynamic sprites (player, NPCs)
- Component updates sprite renderer sortingOrder based on transform.position.y
- Test with multiple sprites overlapping at different Y positions

**Acceptance Criteria:**
- Sprites closer to camera bottom render in front
- Player walks behind props when above them, in front when below

---

### STILL-010: Build Starting Lake Test Layout
**Estimate:** 4 hours

Using placeholder tiles, paint a minimal Starting Lake zone:
- Shoreline with ground tiles
- Water body with water tiles
- 3-5 prop elements (trees, rocks)
- 2-3 designated fishing spots

**Acceptance Criteria:**
- Zone is visually coherent in isometric view
- Fishing spots are clearly identifiable
- Serves as testbed for fishing system

---

## Epic 3: Core Framework

### STILL-011: Implement Event Bus System
**Estimate:** 4 hours

Create `EventBus` class in `Stillwater.Core`:
- Generic publish/subscribe pattern
- Support for typed events (structs or classes)
- Methods: `Subscribe<T>(Action<T>)`, `Unsubscribe<T>(Action<T>)`, `Publish<T>(T)`
- Thread-safe subscription management

Define initial event types:
- `FishingStateChangedEvent`
- `FishCaughtEvent`
- `FishLostEvent`
- `MoodUpdatedEvent`

**Acceptance Criteria:**
- Unit tests pass for subscribe/publish/unsubscribe
- Events can be fired and received across assemblies

---

### STILL-012: Implement Service Locator
**Estimate:** 3 hours

Create `ServiceLocator` class in `Stillwater.Framework`:
- `Register<T>(T instance)`
- `Get<T>() : T`
- `TryGet<T>(out T) : bool`
- Clear method for test cleanup

**Acceptance Criteria:**
- Services can be registered and retrieved
- Missing service throws clear exception
- Works across assembly boundaries

---

### STILL-013: Create GameRoot Bootstrap
**Estimate:** 4 hours

Create `GameRoot` MonoBehaviour in `Stillwater.Core`:
- Lives in Boot scene, marked DontDestroyOnLoad
- Initializes and registers core services:
  - EventBus
  - (Future: LakeWatcher, EchoClient, etc.)
- Handles scene loading for Title → Main flow
- Exposes `IsInitialized` flag

**Acceptance Criteria:**
- Boot scene loads, GameRoot initializes services
- Services accessible via ServiceLocator after initialization
- Main scene can load additively

---

### STILL-014: Create Scene Structure
**Estimate:** 3 hours

Create scenes:
- `Boot.unity` — Contains GameRoot, handles initialization
- `Main_Base.unity` — Tilemaps, environment, lighting
- `Main_Systems.unity` — System managers (placeholder)
- `Main_UI.unity` — HUD canvas (placeholder)
- `Test_Fishing.unity` — Sandbox for fishing development

Configure Boot to additively load Main scenes.

**Acceptance Criteria:**
- Play from Boot scene loads full game
- Each scene has clear single responsibility
- Test scene can run independently

---

## Epic 4: Fishing State Machine

### STILL-015: Define Fishing State Enum and Interfaces
**Estimate:** 3 hours

In `Stillwater.Fishing`:

Create `FishingState` enum:
```
Idle, Casting, LureDrift, Stillness, MicroTwitch, BiteCheck, HookOpportunity, Hooked, Reeling, SlackEvent, Caught, Lost
```

Create interfaces:
- `IFishingState` — `Enter()`, `Exit()`, `Update()`, `GetNextState()`
- `IFishingContext` — Exposes lure, input, timing data to states

**Acceptance Criteria:**
- Enum and interfaces compile
- States can transition based on returned next state

---

### STILL-016: Implement FishingStateMachine Core
**Estimate:** 4 hours

Create `FishingStateMachine` class:
- Holds current `IFishingState`
- `Update()` calls current state, handles transitions
- Publishes `FishingStateChangedEvent` on transition
- Accepts `IFishingContext` for state access to game data

**Acceptance Criteria:**
- State machine transitions correctly in unit tests
- Events fire on state change
- No null reference errors on edge cases

---

### STILL-017: Implement Idle and Casting States
**Estimate:** 4 hours

`IdleState`:
- Waits for cast input
- Transitions to Casting on input

`CastingState`:
- Plays cast animation timing
- Calculates landing position based on input direction/power
- Transitions to LureDrift when cast completes

**Acceptance Criteria:**
- Player can initiate cast from idle
- Cast landing position calculated correctly
- State transitions happen at correct timing

---

### STILL-018: Implement LureDrift and Stillness States
**Estimate:** 4 hours

`LureDriftState`:
- Lure moves based on drift velocity (wind + echo influence)
- Transitions to Stillness when input released and velocity low

`StillnessState`:
- Lure stationary, accumulates stillness time
- Can transition to MicroTwitch on small input
- Transitions to BiteCheck after time threshold

**Acceptance Criteria:**
- Lure drifts visually
- Stillness time tracked accurately
- Micro-twitch returns to stillness

---

### STILL-019: Implement BiteCheck and HookOpportunity States
**Estimate:** 4 hours

`BiteCheckState`:
- Evaluates bite probability based on time, mood, fish tables
- If bite occurs, transitions to HookOpportunity
- If no bite, may return to Stillness or timeout to Idle

`HookOpportunityState`:
- Short timing window for player reaction
- Correct input → Hooked
- Wrong/late input → Lost

**Acceptance Criteria:**
- Bites occur with appropriate randomness
- Hook window feels responsive but challenging
- Lost state triggers on missed hook

---

### STILL-020: Implement Reeling and Result States
**Estimate:** 4 hours

`ReelingState`:
- Tension meter based on input timing
- May trigger SlackEvent requiring input release
- Success threshold → Caught
- Failure (line break, escape) → Lost

`CaughtState`:
- Publishes `FishCaughtEvent`
- Returns to Idle after delay

`LostState`:
- Publishes `FishLostEvent`
- Returns to Idle after delay

**Acceptance Criteria:**
- Reeling feels tactile with tension feedback
- Caught/Lost events fire correctly
- Full fishing loop completable

---

### STILL-021: Create FishingController MonoBehaviour
**Estimate:** 4 hours

Create `FishingController` attached to Player:
- Reads player input (cast, reel, slack)
- Implements `IFishingContext`
- Owns `FishingStateMachine` instance
- Calls `Update()` on state machine each frame
- References `LureController`

**Acceptance Criteria:**
- Player can fish using keyboard/gamepad input
- State machine driven correctly by input
- Debug log shows state transitions

---

### STILL-022: Create LureController and Lure Prefab
**Estimate:** 4 hours

Create `LureController` MonoBehaviour:
- Manages lure GameObject position
- Applies drift velocity each frame
- Exposes position to FishingController
- Spawns/despawns on cast/catch/lost

Create Lure prefab:
- Sprite renderer with placeholder art
- LureController component

**Acceptance Criteria:**
- Lure appears on cast at correct position
- Lure drifts based on velocity
- Lure disappears on catch/lost

---

### STILL-023: Create FishDefinition ScriptableObject
**Estimate:** 3 hours

Create `FishDefinition` SO in `Stillwater.Fishing`:
```csharp
public string id;
public string displayName;
public Sprite icon;
public AnimationCurve biteWindowCurve;
public float rarityBase;
public float minWaitTime;
public float maxWaitTime;
public string flavorTextId;
```

Create 2-3 test fish definitions with varied parameters.

**Acceptance Criteria:**
- FishDefinition assets can be created via Create menu
- Test fish have distinct bite behaviors
- Fishing system can reference definitions

---

### STILL-024: Integrate Fish Selection into BiteCheck
**Estimate:** 4 hours

Modify `BiteCheckState` to:
- Load available `FishDefinition` assets
- Weight selection by rarity and mood modifiers
- Use selected fish's bite window for timing
- Pass selected fish to Caught state for event data

**Acceptance Criteria:**
- Different fish are caught based on probability
- `FishCaughtEvent` contains fish definition reference
- Rarity affects catch frequency appropriately

---

## Epic 5: Lake Watcher (Mood System)

### STILL-025: Create LakeWatcherState Data Structure
**Estimate:** 2 hours

In `Stillwater.World`, create:
```csharp
public struct LakeWatcherState {
    public float stillnessScore;
    public float curiosityScore;
    public float lossScore;
    public float disruptionScore;
    public double lastUpdateTime;
    public int sessionIndex;
}
```

Add utility methods for normalization and mood category calculation.

**Acceptance Criteria:**
- Struct compiles and is serializable
- Mood scores can be read and modified

---

### STILL-026: Implement LakeWatcher Service
**Estimate:** 4 hours

Create `LakeWatcher` class implementing `ILakeWatcherService`:
- Holds `LakeWatcherState`
- Subscribes to EventBus:
  - `FishCaughtEvent` → adjust scores
  - `FishLostEvent` → increase lossScore
  - `FishingStateChangedEvent` → track stillness time
- Methods: `GetCurrentMood()`, `GetAnomalyProbability()`, `GetFishRarityModifier()`
- Publishes `MoodUpdatedEvent` on significant changes

**Acceptance Criteria:**
- Mood scores change based on fishing actions
- Stillness time in Stillness state increases stillnessScore
- Service accessible via ServiceLocator

---

### STILL-027: Create Mood Debug UI Panel
**Estimate:** 4 hours

Create editor window or runtime debug panel showing:
- Current values of all mood scores
- Calculated anomaly probability
- Rarity modifier
- Manual override sliders for testing

**Acceptance Criteria:**
- Panel displays live mood data during play
- Sliders can force mood values for testing
- Panel accessible from Window menu or debug key

---

## Epic 6: Echo System (Local Simulation)

### STILL-028: Define EchoPacket and EchoCurrent Data Structures
**Estimate:** 3 hours

In `Stillwater.Echo`, create:

```csharp
public class EchoPacket {
    public string playerIdHash;
    public int sessionTime;
    public float avgStillness;
    public List<string> rareFishReleased;
    public List<string> ritualPatterns;
    public Dictionary<string, float> zoneTimeDistribution;
}

public class EchoCurrent {
    public float globalStillness;
    public float globalLoss;
    public Dictionary<string, float> fishRarity;
    public int weatherSeed;
    public float anomalyIntensity;
}
```

Add JSON serialization support.

**Acceptance Criteria:**
- Structures serialize/deserialize to JSON correctly
- Unit tests validate round-trip serialization

---

### STILL-029: Implement Mock EchoClient
**Estimate:** 4 hours

Create `EchoClient` class:
- `GatherPacket()` — collects data from LakeWatcher into EchoPacket
- `SimulateSend()` — stores packet locally (no network)
- `SimulateReceive()` — generates fake EchoCurrent with slight randomization
- Configurable update interval (default: 5 minutes simulated)

**Acceptance Criteria:**
- EchoClient can produce packets from current session
- Simulated EchoCurrent values are plausible
- No actual network calls made

---

### STILL-030: Implement EchoApplier
**Estimate:** 4 hours

Create `EchoApplier` class:
- Receives `EchoCurrent` from EchoClient
- Modifies:
  - Fish rarity multipliers in fishing system
  - Anomaly intensity in AnomalyManager
  - Weather seed (stored for future use)
- Publishes `EchoUpdatedEvent`

**Acceptance Criteria:**
- Fish rarity modifiers reflect EchoCurrent values
- Anomaly probability affected by echo intensity
- Event fires when echo data applied

---

### STILL-031: Integrate Echo System into GameRoot
**Estimate:** 3 hours

- Register EchoClient and EchoApplier with ServiceLocator
- Schedule periodic echo simulation (coroutine or timer)
- Log echo updates to console for debugging

**Acceptance Criteria:**
- Echo simulation runs automatically during play
- Console logs show echo packet contents
- Fishing behavior subtly affected by echo

---

## Epic 7: Anomaly System (Prototype)

### STILL-032: Create AnomalyDefinition ScriptableObject
**Estimate:** 3 hours

Create `AnomalyDefinition` SO in `Stillwater.Anomalies`:
```csharp
public string id;
public string displayName;
public float moodThreshold;
public float echoIntensityMin;
public AnomalyType type; // Visual, Temporal, Spatial, Wildlife, Fishing
public float duration;
public float weight;
```

**Acceptance Criteria:**
- AnomalyDefinition assets can be created via Create menu
- All fields serializable and editable in inspector

---

### STILL-033: Implement AnomalyManager
**Estimate:** 4 hours

Create `AnomalyManager` class:
- Loads all `AnomalyDefinition` assets
- Each update tick:
  - Sample mood state from LakeWatcher
  - Sample echo intensity
  - Calculate trigger probability
  - Weighted random selection if triggered
- Publishes `AnomalyTriggeredEvent`

**Acceptance Criteria:**
- Manager runs each frame or fixed interval
- Anomalies trigger based on mood/echo thresholds
- Event contains anomaly definition reference

---

### STILL-034: Create Test Anomaly (Visual)
**Estimate:** 4 hours

Implement one simple visual anomaly:
- Screen vignette darkens briefly
- Subtle color shift or grain effect
- Triggered by high stillness + echo intensity
- Duration: 3-5 seconds

Create corresponding AnomalyDefinition asset.

**Acceptance Criteria:**
- Anomaly visually noticeable but subtle
- Triggers under correct conditions
- Fades in/out smoothly

---

### STILL-035: Create Anomaly Debug Panel
**Estimate:** 3 hours

Create editor window or runtime panel:
- List of all anomaly definitions
- Button to force-trigger each anomaly
- Current trigger probability display
- Recent anomaly history log

**Acceptance Criteria:**
- Any anomaly can be triggered on demand
- Probability updates in real-time
- History shows last 10 triggered anomalies

---

## Epic 8: Player & Input

### STILL-036: Create Player Prefab with Movement
**Estimate:** 4 hours

Create Player prefab:
- Sprite renderer with placeholder character art
- Rigidbody2D (kinematic or dynamic as needed)
- Collider for interaction detection
- `PlayerController` script for movement

Isometric movement:
- WASD/analog stick input
- Convert to isometric directions
- Smooth movement with configurable speed

**Acceptance Criteria:**
- Player moves in isometric directions
- Movement feels responsive
- Player renders at correct depth

---

### STILL-037: Configure Input System
**Estimate:** 3 hours

Using Unity's new Input System:
- Create Input Actions asset
- Define actions:
  - Move (Vector2)
  - Cast (Button)
  - Reel (Button, hold)
  - Slack (Button)
  - Interact (Button)
  - Cancel (Button)
- Support keyboard and gamepad

**Acceptance Criteria:**
- All actions defined and mapped
- Player controller responds to input actions
- Gamepad works correctly

---

### STILL-038: Implement Fishing Spot Detection
**Estimate:** 3 hours

- Create `FishingSpot` component for interactable tiles
- Player detects nearby fishing spots via trigger collider
- UI indicator when fishing spot in range
- Cast action only available when at valid spot

**Acceptance Criteria:**
- Player can only fish at designated spots
- Visual feedback shows valid fishing position
- Attempting to fish elsewhere does nothing

---

## Epic 9: Testing & Validation

### STILL-039: Create Fishing Integration Test Scene
**Estimate:** 4 hours

Build dedicated test scene containing:
- Player at fishing spot
- All fishing states exercisable
- On-screen debug info:
  - Current state
  - Lure position
  - Tension meter
  - Selected fish

**Acceptance Criteria:**
- Full fishing loop testable in isolation
- Debug overlay shows all relevant data
- Scene loadable independently

---

### STILL-040: Create System Integration Test Scene
**Estimate:** 4 hours

Build scene combining all Phase 1 systems:
- Starting Lake layout
- Player with fishing
- LakeWatcher active
- Echo simulation running
- Anomaly manager active
- All debug panels accessible

**Acceptance Criteria:**
- All systems run together without errors
- Mood affects fishing and anomalies
- Echo simulation influences gameplay

---

### STILL-041: Write Unit Tests for Core Systems
**Estimate:** 4 hours

Create test assemblies and write tests for:
- EventBus subscribe/publish/unsubscribe
- ServiceLocator registration and retrieval
- FishingStateMachine transitions
- LakeWatcherState mood calculations
- EchoPacket/EchoCurrent serialization

**Acceptance Criteria:**
- All tests pass
- Tests run in Unity Test Runner
- Coverage for critical paths

---

### STILL-042: Playtest Session and Tuning Pass
**Estimate:** 4 hours

Conduct internal playtest:
- Fish for 15-20 minutes
- Note friction points in fishing feel
- Observe mood progression
- Verify anomaly triggering

Adjust tuning values:
- Bite timing windows
- Reel tension curves
- Mood accumulation rates
- Anomaly thresholds

**Acceptance Criteria:**
- Fishing feels meditative, not frustrating
- Mood changes noticeably over session
- At least one anomaly triggers during test

---

## Summary

| Epic | Tickets | Total Hours |
|------|---------|-------------|
| 1. Unity Project Foundation | 5 | 12 |
| 2. Tilemap & Isometric Rendering | 5 | 17 |
| 3. Core Framework | 4 | 14 |
| 4. Fishing State Machine | 10 | 38 |
| 5. Lake Watcher | 3 | 10 |
| 6. Echo System | 4 | 14 |
| 7. Anomaly System | 4 | 14 |
| 8. Player & Input | 3 | 10 |
| 9. Testing & Validation | 4 | 16 |
| **Total** | **42** | **145 hours** |

Estimated calendar time at 1 developer: 4-5 weeks
Estimated calendar time at 2 developers: 2-3 weeks

---

## Dependencies

```
STILL-001 → STILL-002 → STILL-003
STILL-001 → STILL-004, STILL-005
STILL-006 → STILL-007 → STILL-008 → STILL-010
STILL-004 → STILL-009
STILL-003 → STILL-011 → STILL-012 → STILL-013
STILL-013 → STILL-014
STILL-011 → STILL-015 → STILL-016 → STILL-017 → STILL-018 → STILL-019 → STILL-020
STILL-016 → STILL-021 → STILL-022
STILL-003 → STILL-023 → STILL-024
STILL-011 → STILL-025 → STILL-026 → STILL-027
STILL-026 → STILL-028 → STILL-029 → STILL-030 → STILL-031
STILL-026 → STILL-032 → STILL-033 → STILL-034, STILL-035
STILL-009 → STILL-036
STILL-003 → STILL-037 → STILL-038
STILL-010, STILL-021, STILL-036 → STILL-039
STILL-039, STILL-031, STILL-033 → STILL-040
STILL-016, STILL-026, STILL-029 → STILL-041
STILL-040 → STILL-042
```

---

## Exit Criteria (Phase 1)

Per Production Roadmap:
- [ ] Fishing prototype feels correct (STILL-042)
- [ ] Tilemaps render properly (STILL-010)
- [ ] Echo system simulated locally (STILL-031)
- [ ] Scope validated (STILL-042)
