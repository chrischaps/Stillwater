# Stillwater — Technical Design Document (Pre-Production Draft 1.0)
*Matching TDD for the Pre-Production GDD v1.0*

---

# 0. Document Information
**Project:** Stillwater  
**Document:** Technical Design Document  
**Draft:** v1.0 (Pre-Production)  
**Engine:** Unity 6.2  
**Primary Architecture:** 2D URP, Isometric Tilemap  
**Multiplayer:** Asynchronous Strand System (cloud-backed)

---

# 1. Technical Overview

## 1.1 Core Technologies
- **Unity 6.2**  
- **URP (2D Renderer)**  
- **Unity Tilemap System** (Isometric Z as Y)  
- **Unity Addressables** for content management  
- **Unity Jobs + Burst** for lightweight background tasks (optional)  
- **Cloud Backend:**  
  - Unity Cloud Code / AWS Lambda / Firebase Cloud Functions  
  - Firestore / DynamoDB / Supabase for storage  
- **Networking:**  
  - REST-style async polling  
  - Lightweight JSON packets (<1 KB)  

## 1.2 Target Hardware
- PC (Windows/macOS) minimum  
- Steam Deck-friendly  
- Future-proofed for Switch  

---

# 2. Project Architecture

## 2.1 Folder Structure
```
Assets/
  Art/
    Tiles/
    Sprites/
    Animation/
  Code/
    Core/
    Fishing/
    World/
    EchoSystem/
    UI/
    NPC/
  Data/
    FishDefinitions/
    EchoTables/
    JournalEntries/
  Scenes/
    Overworld/
    Title/
    TestScenes/
  Resources/
  Addressables/
```

## 2.2 Essential Subsystems
1. **Tilemap & Rendering System**  
2. **Fishing FSM**  
3. **Lake Watcher (World Mood Manager)**  
4. **Echo System (Async Multiplayer)**  
5. **Anomaly Manager**  
6. **NPC Manager**  
7. **Journal/Flavor Text System**  
8. **Event Bus** (Decoupled communication)  

---

# 3. Isometric Tilemap & Rendering

## 3.1 Tilemaps
- **Grid Type:** Isometric  
- **Tilemap Layers:**
```
Tilemap_Ground
Tilemap_Props
Tilemap_Water
Tilemap_Interactables
Tilemap_FX
```

## 3.2 Sorting
- Use the “Z as Y” trick: sprite position.y determines render order.
- All character and prop sprites pivot at feet.

## 3.3 Pixel-Perfect Camera
- Orthographic size tuned so 1 Unity unit = 1 tile = constant pixel size.
- Enable Pixel Perfect Camera component.

---

# 4. Fishing Technical Specification

## 4.1 Fishing State Machine (Core)
Implemented via ScriptableObjects representing:
- CastingState
- DriftState
- StillnessState
- MicroTwitchState
- BiteCheckState
- HookOpportunityState
- ReelingState
- SlackEventState
- CaughtState
- LostState

## 4.2 Lure Physics (Lightweight)
- Lure is a GameObject with:
  - Position on water plane
  - Drift velocity
  - Ripple emitter (shader-driven)
- Drift logic:
```
driftVelocity = windVector * windStrength * driftFactor
driftVelocity += echoInfluenceVector
```

## 4.3 Bite Detection Logic
- Every fish has bite windows: `[minWait, maxWait]`
- Additional modifiers:
  - Echo mood
  - Player ritual history
  - Lure state sequence
  - Global rarity drift
- Bite events may be:
  - Normal
  - False ripple
  - Memory tug (camera shift)
  - Silent bite (no VFX)

## 4.4 Reeling Logic
- Curves define tension vs. input timing.
- Slack events validated via:
```
abs(reelInputChange) < slackThreshold
```

---

# 5. The Lake Watcher (World Mood Manager)

## 5.1 Purpose
- Track player behaviors  
- Shape world mood  
- Drive anomaly probability  
- Influence fish tables  
- Provide seed values for time distortions  

## 5.2 Internal Data Model
```
struct LakeWatcherState {
  float stillnessScore;
  float curiosityScore;
  float lossScore;
  float disruptionScore;
  double lastUpdateTime;
  int sessionIndex;
}
```

## 5.3 Mood Update Rules
- Each fishing action modifies scores:
```
stillness += timeWaited * weight
curiosity += rareTwitchPatterns
loss += numberOfLostFish
disruption += fast movement / noise
```

Scores feed into:
- Weather seeds  
- Fish rarity multipliers  
- NPC triggers  
- Anomaly triggers  

---

# 6. Echo System (Async Multiplayer)

## 6.1 Data Flow
1. Local client summarizes player behavior every N minutes  
2. Sends `EchoPacket` to cloud  
3. Cloud updates global aggregates  
4. Cloud returns `EchoCurrent`  
5. Local client applies influence on next session/frame

## 6.2 EchoPacket Schema
```
{
  "playerIdHash": "...",
  "sessionTime": 1234,
  "avgStillness": 0.42,
  "rareFishReleased": ["midnight-koi", ...],
  "ritualPatterns": [...],
  "zoneTimeDistribution": { "lake": 0.6, "marsh": 0.3, ... }
}
```

## 6.3 EchoCurrent Schema
```
{
  "globalStillness": 0.38,
  "globalLoss": 0.12,
  "fishRarity": {
    "midnight-koi": 0.74,
    "weeping-perch": 1.25
  },
  "weatherSeed": 55192,
  "anomalyIntensity": 0.22
}
```

## 6.4 Cloud Considerations
- No per-player storage (privacy)  
- Only hashed identifiers  
- Aggregates updated via rolling window  

---

# 7. Anomaly System

## 7.1 Trigger Conditions
- LakeWatcher mood thresholds  
- EchoCurrent anomalyIntensity  
- Time-of-day  
- Player ritual patterns  

## 7.2 Event Types
- Visual anomalies (shader effects)  
- Temporal anomalies (time dilation)  
- Spatial anomalies (tiles shift)  
- Wildlife anomalies  
- Fishing anomalies (memory tugs, impossible reflections)

## 7.3 Manager Loop
```
OnUpdate:
  sampleMoodState()
  sampleEchoCurrent()
  if(chance < anomalyThreshold) triggerRandomAnomaly()
```

---

# 8. NPC System

## 8.1 Architecture
- NPCs controlled by:
  - Behavior ScriptableObjects
  - Mood gating
  - Time gating  
- Appear/disappear via fade-in masks.

## 8.2 Dialogue System
- Fragment-based  
- No branching requirements  
- Seeds for subtle variations based on mood  

---

# 9. Journal & Flavor System

## 9.1 Data Format
Each entry stored as:
```
struct JournalEntry {
  string id;
  string baseText;
  string echoVariant;
  MoodFrame moodFrame;
}
```

## 9.2 Generation Flow
1. Event triggers entry  
2. LakeWatcher mood + EchoCurrent modifies tone  
3. Final entry assigned + written  

---

# 10. UI/UX Technical

## 10.1 UI System
- Unity UI Toolkit or UGUI (both viable)  
- Use pixel-font with crisp rendering  
- Diegetic cues from shaders / animations  

## 10.2 HUD Update Logic
- Float indicator updates per fishing FSM event  
- Ritual feedback uses shader pulses  
- Minimal update cycles to reduce noise  

---

# 11. Performance Considerations

- Use URP 2D Renderer for batching  
- Tilemaps merged where possible  
- Limit overdraw with smart layer ordering  
- Compute anomaly effects with lightweight shaders  
- Networking payload <1KB  
- Echo calculations done on cloud, not client  

---

# 12. Testing & Tools

## 12.1 In-Editor Tools
- Fishing debugger  
- Anomaly simulator  
- Echo injection panel  
- Tilemap painting ruleset preview  

## 12.2 Automated Tests
- FSM transition tests  
- Mood score validator  
- Echo packet validator  
- Tile sorting tests  

---

# END OF DOCUMENT
