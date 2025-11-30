# Fishing Vertical Slice — Polish Plan

*A focused plan to achieve a tight, polished core fishing loop before adding atmospheric systems.*

---

## Philosophy

**Core Loop First, Depth Later**

The Lake Watcher, Echo System, and Anomaly systems add mechanical and narrative depth, but they are ancillary to the moment-to-moment feel. A player should find the basic act of fishing satisfying *before* any meta-systems enhance it.

This plan focuses on:
1. **Fishing Spot Interaction** — Natural approach and engagement
2. **Visual Polish** — Animations, line, lure, particles, effects
3. **Audio Design** — SFX that reinforce every action
4. **HUD/UI** — Minimal, clear feedback
5. **Game Feel** — Timing, weight, responsiveness

---

## Current State Assessment

### What Works
- Complete FSM with all 12 states
- FishDefinition ScriptableObjects (3 fish)
- Basic lure spawning/despawning
- Player movement (track-locked isometric)
- Input system configured

### What's Missing for Polish
- No fishing spot detection or interaction flow
- No player fishing animations
- No visible fishing line
- Lure is placeholder (no bobber behavior)
- No visual effects (splashes, ripples, particles)
- No fishing-related SFX
- No HUD during fishing
- No fish reveal/catch celebration
- State transitions feel mechanical, not organic

---

## Epic 1: Fishing Spot System

### STIL-50: Define Fishing Spot Architecture
**Estimate:** 3 hours

Design the fishing spot system:
- Fishing spots are shore-adjacent water tiles
- Player must be on valid "shore" tile facing water
- Interaction prompt appears when conditions met
- Entering fishing mode locks player position and facing

**Deliverables:**
- Architecture diagram in code comments
- `FishingSpot` component specification
- Shore tile detection strategy (tilemap collision or marker tiles)

**Acceptance Criteria:**
- Clear documentation of how spots work
- Decision made: trigger colliders vs tilemap query

---

### STIL-51: Create Shore and Water Tile Detection
**Estimate:** 4 hours

Implement detection system:
- Tag or layer shore tiles in tilemap
- Create `ShoreDetector` component on player
- Detect when player stands on shore tile
- Detect water tile in facing direction
- Expose `CanFish` boolean and `FishingDirection` vector

**Acceptance Criteria:**
- Player on shore facing water: CanFish = true
- Player on shore facing land: CanFish = false
- Player in water or on land: CanFish = false

---

### STIL-52: Implement Fishing Interaction Flow
**Estimate:** 4 hours

Create interaction state machine:
- **Exploring** — Normal movement, check for fishing spots
- **FishingReady** — At valid spot, show prompt, await input
- **Fishing** — Locked in place, fishing FSM active
- **Exiting** — Transition back to exploring

Interaction triggers:
- Press Interact at valid spot → Enter Fishing
- Press Cancel while fishing (in Idle state) → Exit Fishing

**Acceptance Criteria:**
- Smooth transition into fishing mode
- Player cannot move while fishing
- Can exit only from Idle state
- Camera adjusts appropriately

---

### STIL-53: Create Interaction Prompt UI
**Estimate:** 3 hours

Build minimal interaction prompt:
- Small UI element appears near player when at valid fishing spot
- Shows input hint (e.g., "E" or gamepad button icon)
- Subtle fade in/out animation
- Disappears when fishing starts

**Acceptance Criteria:**
- Prompt only visible at valid spots
- Correct button shown for current input device
- Non-intrusive visual style

---

### STIL-54: Position Player for Fishing
**Estimate:** 3 hours

When entering fishing mode:
- Snap player to optimal position on shore tile
- Face player toward water
- Lock movement input
- Adjust camera framing if needed

**Acceptance Criteria:**
- Player always faces water when fishing
- Position feels natural, not jarring
- Works for all 4 cardinal + 4 diagonal directions

---

## Epic 2: Player Fishing Animations

### STIL-55: Define Player Animation State Machine
**Estimate:** 3 hours

Document animation states needed:
- **Idle_Walk** — Existing movement animations
- **Fish_Ready** — Holding rod, waiting to cast
- **Fish_Cast** — Wind up and throw
- **Fish_Waiting** — Holding rod, watching bobber
- **Fish_Hook** — Sharp jerk to set hook
- **Fish_Reel** — Cranking reel, body tension
- **Fish_Struggle** — Fighting fish, rod bending
- **Fish_Catch** — Celebration, holding fish up
- **Fish_Lost** — Disappointment reaction

Create animation state diagram and frame count estimates.

**Acceptance Criteria:**
- All states documented
- Transition triggers defined
- Frame budget established (4-8 frames per animation for pixel art)

---

### STIL-56: Create Placeholder Animation Sprites
**Estimate:** 6 hours

Create simple programmer art animations:
- Silhouette or stick figure style is fine
- Focus on clear poses and timing
- 4-8 directions as needed (or 4 with flipping)
- Export as sprite sheets

**Acceptance Criteria:**
- All animation states have placeholder sprites
- Timing feels appropriate for game pace
- Can be replaced with final art later

---

### STIL-57: Implement Player Animation Controller
**Estimate:** 4 hours

Create `PlayerFishingAnimator` component:
- Subscribe to fishing state events
- Trigger appropriate animations on state change
- Handle blend transitions
- Support animation events for SFX/VFX sync

**Acceptance Criteria:**
- Animations play at correct times
- Transitions are smooth
- Animation events fire reliably

---

### STIL-58: Sync Animation with Fishing States
**Estimate:** 4 hours

Map FSM states to animations:
| FSM State | Animation | Notes |
|-----------|-----------|-------|
| Idle | Fish_Ready | Waiting to cast |
| Casting | Fish_Cast | Wind up → throw |
| LureDrift | Fish_Waiting | Watching lure settle |
| Stillness | Fish_Waiting | Subtle idle variation |
| MicroTwitch | Fish_Waiting | Small hand movement |
| BiteCheck | Fish_Waiting | Building anticipation |
| HookOpportunity | Fish_Waiting → Fish_Hook | Quick reaction window |
| Hooked | Fish_Hook → Fish_Reel | Transition to fight |
| Reeling | Fish_Reel / Fish_Struggle | Based on tension |
| SlackEvent | Fish_Reel (relaxed) | Release tension pose |
| Caught | Fish_Catch | Victory pose |
| Lost | Fish_Lost | Disappointment |

**Acceptance Criteria:**
- Every state has corresponding visual
- Transitions feel connected
- Player actions have clear feedback

---

## Epic 3: Fishing Line Rendering

### STIL-59: Research Line Rendering Approaches
**Estimate:** 2 hours

Evaluate options:
- **LineRenderer** — Built-in, simple curves
- **Sprite chain** — Pixel-art friendly segments
- **Custom shader** — Most control, more complex
- **Bezier with sprites** — Balance of control and simplicity

Document pros/cons for pixel-art aesthetic.

**Acceptance Criteria:**
- Approach selected and justified
- Prototype plan defined

---

### STIL-60: Implement Basic Line Renderer
**Estimate:** 4 hours

Create `FishingLineRenderer` component:
- Connects rod tip position to lure position
- Updates every frame
- Basic curve/sag based on line length
- Pixel-art appropriate thickness (1-2 pixels)

**Acceptance Criteria:**
- Line visible from rod to lure
- Line follows lure movement
- Looks acceptable in pixel-art style

---

### STIL-61: Add Line Tension Visualization
**Estimate:** 4 hours

Enhance line based on fishing state:
- **Slack** — More sag, relaxed curve
- **Normal** — Gentle curve
- **Taut** — Nearly straight, slight vibration
- **Strained** — Straight, color shift or pulse

Visual feedback during reeling:
- Line tightens when reeling
- Line slackens on SlackEvent
- Extreme tension warning (line about to break)

**Acceptance Criteria:**
- Tension clearly readable from line appearance
- Matches current FSM state
- Feels responsive to input

---

### STIL-62: Animate Line During Cast
**Estimate:** 3 hours

Cast sequence:
1. Line starts at rod tip (coiled/short)
2. On cast, line extends along arc trajectory
3. Line settles into water with lure
4. Transition to normal fishing line state

**Acceptance Criteria:**
- Cast feels like throwing a line
- Arc trajectory visible briefly
- Smooth transition to waiting state

---

## Epic 4: Lure/Bobber Polish

### STIL-63: Create Bobber Sprite and Animation
**Estimate:** 3 hours

Design bobber visual:
- Simple float design (red/white classic or stylized)
- Idle floating animation (subtle bob on water)
- Disturbed animation (ripples, wobble)
- Dunk animation (pulled under)

**Acceptance Criteria:**
- Bobber clearly visible against water
- Floating feels natural
- Dunk is unmistakable

---

### STIL-64: Implement Bobber Water Physics
**Estimate:** 4 hours

Enhance `LureController`:
- Bobber floats at water surface
- Gentle drift based on (future) wind/current
- Subtle random bobbing motion
- Constrained to water tiles

**Acceptance Criteria:**
- Bobber stays on water surface
- Movement feels organic, not mechanical
- Respects water tile boundaries

---

### STIL-65: Add Bobber State Behaviors
**Estimate:** 4 hours

State-specific bobber behavior:
| State | Bobber Behavior |
|-------|-----------------|
| LureDrift | Settling, ripples dissipating |
| Stillness | Gentle float, minimal motion |
| MicroTwitch | Small disturbance, returns to still |
| BiteCheck | Occasional subtle nibble animation |
| HookOpportunity | Sharp dunk, urgent visual |
| Hooked | Pulled under, line taut |
| Reeling | Dragging through water toward shore |
| Caught | Bobber retracts to rod |
| Lost | Bobber pops back up, slack line |

**Acceptance Criteria:**
- Each state has distinct bobber behavior
- Bite is immediately recognizable
- Lost/Caught have clear resolution visuals

---

### STIL-66: Implement Cast Landing
**Estimate:** 4 hours

Cast landing sequence:
- Calculate landing position based on cast power/direction
- Animate lure arc through air
- Splash effect on water impact
- Bobber settles into floating position

**Acceptance Criteria:**
- Cast has satisfying arc
- Landing splash is visible
- Smooth transition to drift state

---

## Epic 5: Visual Effects (VFX)

### STIL-67: Create Water Ripple Effect
**Estimate:** 4 hours

Ripple system for water interactions:
- Expanding ring sprite or shader effect
- Configurable size and duration
- Can spawn at any water position

Use cases:
- Lure landing
- Bobber movement
- Fish activity
- Catch/escape splashes

**Acceptance Criteria:**
- Ripples look natural on water tiles
- Multiple ripples can exist simultaneously
- Performance acceptable (object pooling)

---

### STIL-68: Create Splash Effect
**Estimate:** 3 hours

Splash particle system:
- Water droplet particles
- Configurable intensity (small splash vs big splash)
- Brief white foam sprite

Use cases:
- Cast landing (medium)
- Hook set (small)
- Fish struggle (medium, repeated)
- Catch (large celebration splash)
- Escape (large disappointed splash)

**Acceptance Criteria:**
- Splashes feel impactful
- Scale matches event importance
- Pixel-art appropriate particle style

---

### STIL-69: Create Bite Indicator Effect
**Estimate:** 3 hours

Visual cue when bite occurs:
- Exclamation mark or subtle flash
- Brief screen shake or pulse (very subtle)
- Bobber dunk combined with effect
- Quick enough to create urgency

**Acceptance Criteria:**
- Bite is unmissable but not obnoxious
- Creates "react now!" feeling
- Works with audio cue (STIL-77)

---

### STIL-70: Create Catch Celebration Effect
**Estimate:** 3 hours

Success feedback:
- Sparkle/shine particles
- Fish sprite reveal (pulled from water)
- Brief slow-motion or pause (50-100ms)
- Screen flash or vignette pulse

**Acceptance Criteria:**
- Catching a fish feels rewarding
- Effect scales with fish rarity (optional)
- Transitions smoothly to fish info display

---

### STIL-71: Create Fish Escape Effect
**Estimate:** 3 hours

Failure feedback:
- Big splash where fish was
- Line goes slack (snaps back)
- Bobber pops up
- Brief camera shake (optional, subtle)

**Acceptance Criteria:**
- Escape feels like a loss
- Clear visual of "fish got away"
- Not frustrating, just disappointing

---

### STIL-72: Create Line Tension Warning Effect
**Estimate:** 2 hours

Visual warning during reel:
- Line color shifts toward red when over-tensioned
- Screen edge vignette pulse
- Rod bending animation intensifies

**Acceptance Criteria:**
- Player knows when they're about to lose fish
- Creates tension without being annoying
- Gives time to react (slack)

---

## Epic 6: HUD and UI

### STIL-73: Design Fishing HUD Layout
**Estimate:** 3 hours

Design minimal fishing HUD:
- **Tension Meter** — Only visible during Hooked/Reeling states
- **Hook Window Indicator** — Brief timing cue during HookOpportunity
- **State Indicator** — Subtle icon or text (optional, debug-friendly)
- **Fish Info Panel** — Appears on catch

Principles:
- Invisible when not needed
- Diegetic where possible (line tension = line visual)
- Quick fade in/out transitions

**Acceptance Criteria:**
- Mockup or wireframe complete
- Placement doesn't obscure action
- Consistent with game's minimal aesthetic

---

### STIL-74: Implement Tension Meter UI
**Estimate:** 4 hours

Create tension meter:
- Horizontal or arc bar
- Green → Yellow → Red gradient
- Animated fill based on tension value
- Pulse/shake at critical levels
- Only visible during appropriate states

**Acceptance Criteria:**
- Tension clearly readable at a glance
- Matches actual line tension state
- Smooth animation

---

### STIL-75: Implement Hook Timing Indicator
**Estimate:** 3 hours

Brief timing cue during HookOpportunity:
- Quick shrinking circle or filling bar
- Shows remaining time to react
- Success/failure feedback
- Disappears on transition

Consider: Making this subtle enough that skilled players ignore it but new players can use it.

**Acceptance Criteria:**
- Helps player learn timing
- Not required to play (visual bobber is primary cue)
- Clean appear/disappear

---

### STIL-76: Implement Fish Caught Panel
**Estimate:** 4 hours

Display caught fish info:
- Fish sprite/icon (large)
- Fish name
- Brief flavor text (from FishDefinition)
- "New catch!" badge for first-time catches (future: requires save system)
- Dismiss with any input

**Acceptance Criteria:**
- Panel appears after catch celebration
- Information clearly presented
- Smooth transitions

---

## Epic 7: Sound Effects (SFX)

### STIL-77: Define SFX Requirements List
**Estimate:** 2 hours

Document all needed sounds:
| Event | Sound Description | Priority |
|-------|-------------------|----------|
| Cast wind-up | Whoosh, rod swing | High |
| Cast release | Line whip | High |
| Lure splash | Water impact, small | High |
| Bobber floating | Subtle water lap (ambient?) | Low |
| Nibble | Small underwater thump | Medium |
| Bite | Sharp underwater tug, alert tone | High |
| Hook set | Rod snap, line tension | High |
| Reel cranking | Mechanical click/whir | High |
| Line tension | Creaking, strain | Medium |
| Fish struggle | Splashing, thrashing | High |
| Catch success | Celebration jingle, splash | High |
| Fish escape | Splash, line snap, sad tone | High |
| Enter fishing mode | Subtle ready sound | Low |
| Exit fishing mode | Subtle pack-up sound | Low |

**Acceptance Criteria:**
- Complete sound list documented
- Priority assigned for phased implementation
- Style notes (lo-fi, naturalistic, etc.)

---

### STIL-78: Source or Create Placeholder SFX
**Estimate:** 4 hours

Gather temporary sounds:
- Use free sound libraries (freesound.org, etc.)
- Record simple foley if needed
- Process to match lo-fi aesthetic
- Organize in Audio/SFX/Fishing/

**Acceptance Criteria:**
- All high-priority sounds have placeholders
- Sounds imported and organized
- Legal/license compliance noted

---

### STIL-79: Implement Fishing Audio Controller
**Estimate:** 4 hours

Create `FishingAudioController`:
- Subscribe to fishing events
- Play appropriate sounds on state changes
- Handle looping sounds (reel cranking)
- Support sound variation (multiple clips per event)

**Acceptance Criteria:**
- All fishing actions have audio feedback
- Sounds play at correct times
- No overlapping/clipping issues

---

### STIL-80: Add Ambient Water Audio
**Estimate:** 3 hours

Environmental audio for fishing:
- Gentle water lapping loop
- Plays when in fishing mode
- Subtle volume/filter changes based on state
- Crossfade with exploration ambient

**Acceptance Criteria:**
- Water sounds enhance atmosphere
- Not intrusive or repetitive
- Smooth transitions

---

## Epic 8: Game Feel Polish

### STIL-81: Tune Cast Timing and Feel
**Estimate:** 3 hours

Refine cast mechanic:
- Hold to charge cast distance
- Release to throw
- Animation timing matches input
- Satisfying "weight" to the throw

**Acceptance Criteria:**
- Casting feels intentional
- Distance control is intuitive
- Animation sync is tight

---

### STIL-82: Tune Hook Window Timing
**Estimate:** 3 hours

Balance hook opportunity:
- Window duration (experiment: 0.5s - 1.5s)
- Visual/audio cue lead time
- Success/failure thresholds
- Difficulty curve (easier fish = longer window)

**Acceptance Criteria:**
- Hook timing feels fair
- Creates tension without frustration
- Skill expression possible

---

### STIL-83: Tune Reel Tension Mechanics
**Estimate:** 4 hours

Balance reeling phase:
- Tension accumulation rate
- Recovery rate when holding steady
- Slack event trigger conditions
- Line break threshold
- Fish progress toward shore

Test with different fish "fight" values.

**Acceptance Criteria:**
- Reeling feels like a mini-game
- Skill matters (not just spam reel)
- Different fish feel different

---

### STIL-84: Add Micro-Feedback Throughout
**Estimate:** 4 hours

Small touches that add feel:
- Input buffer for hook timing (few frames of forgiveness)
- Controller rumble on bite/hook/catch (if supported)
- Subtle camera movements (zoom slightly during tension)
- Screen shake intensity tuning
- Button press visual feedback

**Acceptance Criteria:**
- Actions feel responsive
- Feedback layer is cohesive
- Nothing feels "dead" or unresponsive

---

### STIL-85: Playtest and Iterate
**Estimate:** 6 hours (multiple sessions)

Structured playtesting:
- Session 1: Flow and frustration points
- Session 2: Timing and difficulty
- Session 3: Visual clarity
- Session 4: Audio balance
- Session 5: First-time player observation

Document findings and prioritize fixes.

**Acceptance Criteria:**
- Core loop feels satisfying to repeat
- No major friction points
- "One more cast" feeling achieved

---

## Epic 9: Integration and Cleanup

### STIL-86: Create Vertical Slice Scene
**Estimate:** 4 hours

Build polished test environment:
- Starting Lake zone (existing layout or improved)
- Multiple fishing spots
- Player spawn point
- Proper lighting
- Background elements

**Acceptance Criteria:**
- Scene demonstrates full fishing loop
- Environment supports the mood
- Can be shown as proof of concept

---

### STIL-87: Performance Optimization Pass
**Estimate:** 3 hours

Ensure smooth performance:
- Profile VFX systems
- Object pooling for particles/ripples
- Audio source management
- Draw call optimization

**Acceptance Criteria:**
- Stable 60fps on target hardware
- No GC spikes during normal play
- Memory usage reasonable

---

### STIL-88: Bug Fixing Sprint
**Estimate:** 4 hours

Address issues found during playtesting:
- State machine edge cases
- Animation glitches
- Audio timing issues
- UI edge cases

**Acceptance Criteria:**
- No critical bugs
- No jarring glitches
- Polish feels complete

---

### STIL-89: Documentation Update
**Estimate:** 2 hours

Update project documentation:
- Mark completed tickets
- Update CLAUDE.md with new systems
- Document new components and their usage
- Note any technical debt for future

**Acceptance Criteria:**
- Documentation reflects current state
- New developers can understand fishing system
- Known issues documented

---

## Summary

| Epic | Tickets | Estimated Hours |
|------|---------|-----------------|
| 1. Fishing Spot System | 5 | 17 |
| 2. Player Animations | 4 | 17 |
| 3. Fishing Line Rendering | 4 | 13 |
| 4. Lure/Bobber Polish | 4 | 15 |
| 5. Visual Effects | 6 | 18 |
| 6. HUD and UI | 4 | 14 |
| 7. Sound Effects | 4 | 13 |
| 8. Game Feel Polish | 5 | 20 |
| 9. Integration | 4 | 13 |
| **Total** | **40** | **140 hours** |

---

## Dependencies Graph

```
STIL-50 → STIL-51 → STIL-52 → STIL-53, STIL-54

STIL-55 → STIL-56 → STIL-57 → STIL-58

STIL-59 → STIL-60 → STIL-61, STIL-62

STIL-63 → STIL-64 → STIL-65
STIL-64 → STIL-66

STIL-67 → STIL-68, STIL-69, STIL-70, STIL-71
STIL-61 → STIL-72

STIL-73 → STIL-74, STIL-75, STIL-76

STIL-77 → STIL-78 → STIL-79
STIL-79 → STIL-80

STIL-52, STIL-58, STIL-65, STIL-79 → STIL-81, STIL-82, STIL-83, STIL-84

STIL-84 → STIL-85 → STIL-88

STIL-85 → STIL-86, STIL-87

STIL-88 → STIL-89
```

---

## Critical Path

The minimum path to a playable polish slice:

1. **STIL-51** — Shore/water detection
2. **STIL-52** — Fishing interaction flow
3. **STIL-60** — Basic fishing line
4. **STIL-64** — Bobber water physics
5. **STIL-66** — Cast landing
6. **STIL-67** — Water ripple effect
7. **STIL-68** — Splash effect
8. **STIL-69** — Bite indicator
9. **STIL-74** — Tension meter
10. **STIL-78** — Placeholder SFX
11. **STIL-79** — Audio controller
12. **STIL-85** — Playtest

This critical path is approximately **50-60 hours** and delivers a playable, polished fishing loop without full animation or UI polish.

---

## Exit Criteria (Vertical Slice - Fishing Polish)

The fishing vertical slice is complete when:

- [ ] Player can walk to shore and initiate fishing naturally
- [ ] Cast feels weighty and satisfying
- [ ] Fishing line renders from rod to bobber
- [ ] Bobber floats realistically and dunks on bite
- [ ] Bite moment creates urgency (visual + audio)
- [ ] Hook timing feels fair and skill-based
- [ ] Reeling creates tension through mechanics and feedback
- [ ] Catching a fish feels rewarding
- [ ] Losing a fish feels disappointing but fair
- [ ] All actions have audio feedback
- [ ] Core loop is repeatable without frustration
- [ ] "One more cast" engagement achieved

---

## Post-Slice: What Comes Next

After the fishing slice is polished:

1. **Save System** — Persist caught fish, progress
2. **Journal System** — Flavor text, fish collection
3. **Lake Watcher** — Mood tracking, atmospheric influence
4. **Echo System** — Async multiplayer simulation
5. **Anomaly System** — Surreal events
6. **Content Expansion** — More fish, zones, NPCs

The polished core loop makes all future additions more impactful.

---

## Notes on Art Style

Since this plan involves placeholder art, consider:

- **Consistency over quality** — All placeholders should match the target style (pixel art, limited palette)
- **Clear silhouettes** — Actions readable even without detail
- **Animation principles** — Anticipation, follow-through, squash/stretch
- **Reference games** — Stardew Valley fishing, Moonlighter, Eastward

The placeholder art created here can serve as animation timing reference for final art.

---

# END OF DOCUMENT
