# Fishing Spot System Architecture

*Design document for STIL-53: Define Fishing Spot Architecture*

---

## Overview

This document defines how players interact with fishing locations in Stillwater. The system determines when a player can fish based on their position relative to water tiles and manages the transition between exploration and fishing modes.

---

## Design Decision: Shore Detection Strategy

### Chosen Approach: Tilemap Query (Hybrid)

**Primary Method: Automatic Shore Detection via Tilemap Query**

The system queries existing tilemaps to detect valid fishing positions:
- Player must be on a non-water tile (ground/shore)
- Player must be facing a water tile
- No additional GameObjects needed per fishing spot

**Secondary Method: Optional FishingSpot Markers**

For special curated spots with unique properties:
- Custom fish populations
- Modified bite rates
- Quest/narrative locations

### Why Tilemap Query?

| Criteria | Tilemap Query | Trigger Colliders |
|----------|---------------|-------------------|
| Setup effort | Automatic (uses existing tiles) | Manual placement per spot |
| Performance | No collision overhead | Physics queries each frame |
| Scalability | Scales with map | More objects = more overhead |
| Flexibility | Less per-spot customization | Rich per-spot data |
| Existing patterns | Matches WaterShoreDetector | New pattern |

**Decision**: Tilemap query leverages existing infrastructure and matches the pattern already established in `WaterShoreDetector.cs`. Special spots can use optional markers when needed.

---

## Component Specifications

### 1. ShoreDetector

**Purpose**: Detect when player is on shore facing water.

**Attach to**: Player GameObject

**Dependencies**:
- `Tilemap` references (Water, Ground)
- Player position (`Rigidbody2D.position`)
- Player facing direction (needs to be exposed by `PlayerController`)

**Public Interface**:
```csharp
public interface IShoreDetector
{
    bool CanFish { get; }
    Vector2 FishingDirection { get; }
    Vector2 TargetWaterPosition { get; }
    FishingSpotData ActiveSpotData { get; }  // Null for generic shore
    event Action<bool> OnCanFishChanged;
}
```

**Detection Algorithm**:
```
IsOnShore(playerCell):
    1. If playerCell is water tile → return false
    2. For each of 8 neighbor cells:
       - If neighbor is water tile → return true
    3. return false

CanFishInDirection(playerCell, facingDirection):
    1. Get cell in facing direction from playerCell
    2. Return true if that cell is a water tile
```

**Update Frequency**: Each frame (or on position/facing change)

---

### 2. FishingSpotMarker (Optional)

**Purpose**: Mark special curated fishing spots with custom data.

**Attach to**: Empty GameObject at fishing location

**Data Structure**:
```csharp
public class FishingSpotData
{
    string SpotId;
    string DisplayName;
    FishDefinition[] AvailableFish;      // Override zone defaults
    float BiteProbabilityModifier;        // 1.0 = normal
    string ZoneIdOverride;                // Optional zone override
}
```

**Editor Visualization**: Gizmo showing spot location and radius

---

### 3. FishingInteractionController

**Purpose**: Manage state transitions between exploration and fishing.

**Attach to**: Player GameObject or Systems root

**Dependencies**:
- `IShoreDetector`
- `PlayerController` (for movement lock)
- `FishingController` (for FSM state)
- `EventBus` (for input events)

**Public Interface**:
```csharp
public interface IFishingInteractionService
{
    FishingInteractionState CurrentState { get; }
    bool CanStartFishing { get; }
    bool CanExitFishing { get; }
    bool TryStartFishing();
    bool TryExitFishing();
    event Action<FishingInteractionState> OnStateChanged;
}
```

---

## Interaction State Machine

```
┌─────────────┐                              ┌──────────────┐
│  Exploring  │───(CanFish becomes true)────►│ FishingReady │
│             │◄──(CanFish becomes false)────│              │
└─────────────┘                              └──────┬───────┘
       ▲                                           │
       │                                    (Interact input)
       │                                           │
       │                                           ▼
┌──────┴──────┐                              ┌─────────────┐
│   Exiting   │◄────(Cancel in Idle state)───│   Fishing   │
└─────────────┘                              └─────────────┘
```

### State Behaviors

| State | Player Movement | Actions |
|-------|-----------------|---------|
| **Exploring** | Enabled | Monitor ShoreDetector.CanFish |
| **FishingReady** | Enabled | Show prompt, listen for Interact |
| **Fishing** | Disabled | FSM active, listen for Cancel (Idle only) |
| **Exiting** | Disabled→Enabled | Transition animation, cleanup |

---

## Integration Points

### PlayerController

**Required Additions**:
1. Expose `FacingDirection` property (currently not tracked)
2. Add `SetFacingDirection(FacingDirection dir)` method
3. Update facing based on movement direction

**Existing Methods to Use**:
- `DisableMovement()` - Lock player during fishing
- `EnableMovement()` - Unlock after fishing
- `StopMovement()` - Halt velocity immediately

**Suggested Addition**:
```csharp
// Add to PlayerController
public FacingDirection FacingDirection { get; private set; }

public void SetFacingDirection(FacingDirection direction)
{
    FacingDirection = direction;
    // Update sprite facing if applicable
}
```

---

### FishingController

**Required Additions**:
1. `IsInIdleState` property - Check if can exit
2. `SetActive(bool)` method - Enable/disable FSM
3. `ApplySpotData(FishingSpotData)` - Apply spot modifiers

**Suggested Addition**:
```csharp
public bool IsInIdleState => _stateMachine.CurrentState == FishingState.Idle;

public void SetActive(bool active)
{
    enabled = active;
    if (active) _stateMachine.Initialize(FishingState.Idle);
}

public void ApplySpotData(FishingSpotData data)
{
    if (data == null) return;
    _currentZoneId = data.ZoneIdOverride ?? _currentZoneId;
    _biteProbabilityModifier = data.BiteProbabilityModifier;
    // Set available fish pool
}
```

---

### EventBus (New Events)

```csharp
// Player enters valid fishing spot range
public struct FishingSpotDetectedEvent
{
    public Vector2 SpotPosition;
    public FishingSpotData SpotData;  // Null for generic shore
}

// Player leaves valid fishing spot range
public struct FishingSpotLostEvent { }

// Player enters fishing mode
public struct FishingModeEnteredEvent
{
    public Vector2 PlayerPosition;
    public Vector2 FishingDirection;
    public FishingSpotData SpotData;
}

// Player exits fishing mode
public struct FishingModeExitedEvent { }
```

---

### Input System

**Existing Events (already wired)**:
- `InteractInputEvent` - Use to start fishing
- `CancelInputEvent` - Use to exit fishing

**No changes needed** - InputService already publishes these events.

---

## Tilemap Configuration

### Required Tilemaps

| Tilemap | Purpose | Already Exists |
|---------|---------|----------------|
| `Tilemap_Water` | Water tile detection | Yes |
| `Tilemap_Ground` | Shore/walkable detection | Yes |

### Detection Logic

```
Water Tile: Any tile present on Tilemap_Water
Shore Tile: Any tile on Tilemap_Ground adjacent to Tilemap_Water
Valid Fishing Position: Player on shore + facing direction has water
```

No special tile tagging required for MVP. Tile presence is sufficient.

---

## Positioning Player for Fishing

When entering fishing mode:

1. **Snap to optimal position**
   - Center of current shore tile
   - Slight offset away from water edge

2. **Set facing direction**
   - Face toward target water tile
   - Support all 8 directions

3. **Lock movement**
   - Call `PlayerController.DisableMovement()`
   - Store original position for potential return

4. **Adjust camera** (optional)
   - Slight zoom or reframe for fishing view

---

## Future Considerations

### Phase 2 Enhancements

- **Directional spots**: Some shores only fishable from certain angles
- **Water depth**: Deep vs shallow affects fish availability
- **Time-based spots**: Night fishing, dawn/dusk specials
- **Weather effects**: Rain opens new areas
- **Unlockable spots**: Progression-gated premium locations

---

## File Structure (When Implemented)

```
Assets/Code/Fishing/
├── FishingSpotArchitecture.md    ← This document
├── IShoreDetector.cs             ← Interface (STIL-54)
├── ShoreDetector.cs              ← Implementation (STIL-54)
├── FishingSpotData.cs            ← Data class (STIL-54)
├── FishingSpotMarker.cs          ← Optional marker (STIL-54)
├── IFishingInteractionService.cs ← Interface (STIL-55)
├── FishingInteractionController.cs ← Implementation (STIL-55)
└── ... existing files ...
```

---

## Acceptance Criteria Checklist

- [x] Clear documentation of how fishing spots work
- [x] Decision made: Tilemap query approach (with optional markers)
- [x] Component interfaces defined (IShoreDetector, IFishingInteractionService)
- [x] Integration points with PlayerController identified
- [x] Integration points with FishingController identified
- [x] Event definitions specified
- [x] State machine documented

---

*Last Updated: STIL-53*
