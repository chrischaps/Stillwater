# Stillwater — Game Design Document (Pre‑Production Draft 1.0)
*A contemplative isometric fishing game about loneliness, connection, and quiet surrealism.*  
*(Includes diagrams and pre-production structure)*

---

# 0. Document Information
**Project:** Stillwater  
**Draft:** Pre‑Production GDD v1.0  
**Engine:** Unity (2022+ LTS, 2D Isometric)  
**Multiplayer:** Asynchronous Strand System  
**Primary Sources:** Vision Document, GDD Draft 0.2 & 0.3

---

# 1. High‑Level Overview

## 1.1 Core Concept
Stillwater is a meditative, narrative-light fishing game built around an esoteric, symbolic fishing system and atmospheric exploration. Players inhabit parallel worlds that softly influence one another through asynchronous data exchange.

## 1.2 Tone & Themes
Calm, melancholic, dreamlike, subtly surreal.  
Primary themes include existential loneliness, connection, stillness, and the unknowable.

## 1.3 Engine & Rendering
- Unity 2D URP  
- Pixel-perfect camera  
- True isometric tilemaps  
- Dynamic lighting & shader-based water  
- Custom depth sorting and grid layers  

---

# 2. Core Pillars

### 2.1 Mechanical Storytelling
The fishing system is the narrative. Ritual patterns, waiting, stillness, and subtle decision-making create meaning.

### 2.2 Esoteric but Optional Complexity
Deep and strange mechanics enrich the system—never mandatory for progression.

### 2.3 Asynchronous Connection
Players influence each other indirectly via “Echo Currents,” residues, rarity shifts, and journal fragments.

### 2.4 Subtle Surrealism
Escalating anomalies triggered by both personal and global emotional patterns.

---

# 3. Gameplay Structure

## 3.1 Moment-to-Moment Loop
```mermaid
flowchart TD
    A[Move to Fishing Tile] --> B[Cast Line]
    B --> C[Wait]
    C -->|Time Distortion, Echo Influence| D[Hook]
    D --> E[Reel Phase]
    E --> F{Result}
    F -->|Fish| G[Flavor Text]
    F -->|Object| H[Interpretation]
    G --> I[Keep / Release]
    H --> I
    I --> A
```

## 3.2 Meta Loop
```mermaid
flowchart LR
    A[Fishing Behaviors] --> B[Lake Watcher Memory]
    B --> C[World Shifts]
    C --> D[Anomalies / Weather / NPCs]
    D --> A
    B --> E[Journal Updates]
    E --> A
```

---

# 4. Fishing System (Esoteric Core)

## 4.1 System Overview
Fishing is composed of five expressive layers:
1. **Casting (Ritual)**
2. **Lure Behavior (Expression)**
3. **Waiting (Temporal)**
4. **Hooking (Intuition)**
5. **Reeling (Confrontation)**

## 4.2 State Machine
```mermaid
stateDiagram-v2
    [*] --> Idle
    Idle --> Casting : Input Press
    Casting --> LureDrift : Cast Lands
    LureDrift --> Stillness : No Input
    Stillness --> MicroTwitch : Small Input
    MicroTwitch --> Stillness
    Stillness --> BiteCheck : Time Passes
    LureDrift --> BiteCheck : Hidden Trigger
    BiteCheck --> HookOpportunity : Bite Detected
    HookOpportunity --> Hooked : Player Reacts Correctly
    HookOpportunity --> Lost : Wrong Reaction
    Hooked --> Reeling
    Reeling --> SlackEvent : Player Releases Reel
    SlackEvent --> Reeling : Correct Slack Use
    Reeling --> Caught : Success Condition
    Reeling --> Lost : Line Silence / Moment Slip
    Caught --> [*]
    Lost --> [*]
```

## 4.3 Hidden Logic Layers
- **Echo Mood Conditions** (Stillness, Loss, Curiosity, Disruption)  
- **Fish Route Tables** — invisible paths influenced by global and personal state  
- **Temporal Windows** — some bites occur during liminal temporal frames  
- **Anomaly Hooks** — rare surreal events tied to ritualistic behavior  

---

# 5. Asynchronous Multiplayer (“Strand System”)

## 5.1 Data Flow Diagram
```mermaid
sequenceDiagram
    participant P as Player Client
    participant L as Local Lake State
    participant C as Cloud Echo Server
    participant G as Global Echo State

    P->>L: Fishing Actions / Behaviors
    L->>C: Upload Behavioral Summaries
    C->>G: Update Global Echo Currents
    G->>C: Adjust Global Mood / Rarity / Weather Seeds
    C->>P: Download Echo Influence Packet
    P->>L: Update Local Anomalies & States
```

## 5.2 Types of Shared Influence
- **Echo Currents:** Aggregated global behaviors alter anomaly rates, fish migrations.
- **Residue Markers:** Environmental traces left by other players.
- **Shared Journals:** Occasionally receive a variant page shaped by another player.
- **Rarity Drift:** Player choices affect spawning probabilities globally.

---

# 6. World & Environment

## 6.1 Zone Layout
```mermaid
flowchart TB
    A[Starting Lake] --> B[Forest River]
    A --> C[Crescent Cove]
    B --> D[Night Marsh]
    C --> D
    D --> E[Deepwater Inlet]
```

## 6.2 Zone Characteristics
- **Starting Lake:** Calm, safe, minimal surrealism  
- **Forest River:** Narrow, flow-based fish, unusual sound cues  
- **Crescent Cove:** Tides, reflection anomalies  
- **Night Marsh:** Fog, drifting lights, temporal distortions  
- **Deepwater Inlet:** Rare encounters, escalating surrealism  

---

# 7. Narrative & Journal Systems

## 7.1 Narrative Delivery
No explicit plot. Story emerges through:
- Fishing behaviors  
- Environmental shifts  
- Journal fragments  
- Strange objects  

## 7.2 Journal Diagram
```mermaid
flowchart LR
    A[Catches] --> B[Flavor Text]
    B --> C[Journal Entry]
    D[Echo Influence] --> C
    C --> E[Player Reflection Layer]
```

Journal pages combine:
- Player behaviors  
- Echo contributions  
- Procedurally varied emotional framing  

---

# 8. NPC System

### Sparse, enigmatic, symbolic characters that:
- Appear based on Echo states  
- Offer flavor instead of exposition  
- Act as environmental punctuation  

NPCs follow a simple state machine:
```mermaid
stateDiagram-v2
    [*] --> Dormant
    Dormant --> Approach : Echo Trigger
    Approach --> Present : Player Nearby
    Present --> Fade : Time / Echo Shift
    Fade --> Dormant
```

---

# 9. UI & UX

## 9.1 Principles
Minimal, diegetic, warm, slow-paced.  
Visual noise is avoided.

## 9.2 UI Map
```mermaid
flowchart LR
    A[Main HUD] --> B[Float Indicator]
    A --> C[Weather Cue]
    A --> D[Ritual Feedback]
    A --> E[Inventory]
    E --> F[Object Details]
    A --> G[Journal Icon]
    G --> H[Journal UI]
```

---

# 10. Technical Design Notes (Unity)

## 10.1 Systems Overview
- **Isometric Tilemap Grid** — layered rule tiles, smart painting  
- **Pixel Camera** — orthographic, pixel snapping  
- **Fishing FSM** — ScriptableObject-driven  
- **Lake Watcher** — central mood manager  
- **Echo Sync** — periodic async fetch & upload  
- **World Seeds** — control anomalies, weather, rarity  
- **Anomaly Manager** — triggers surreal events  

## 10.2 Data Structures
- `FishDefinition`  
- `EchoPacket` (local → cloud)  
- `EchoCurrent` (cloud → clients)  
- `JournalEntry`  
- `TileResidueMarker`  

## 10.3 Performance Goals
- Low GPU load  
- Tilemap-friendly physics  
- Lightweight networking (<1KB packets)  

---

# 11. Open Design Questions

- How strong should anomaly escalation be?  
- What is the long-term rhythm of global Echo Currents?  
- Should players influence zone unlock order?  
- Should the journal ever hint at a shared emotional arc?  

---

# 12. Appendix

## 12.1 Art Direction Notes
- Muted earth tones  
- 1:1 pixel density  
- Soft dithering and brush texture overlays  
- Limited animation frames for serenity  

## 12.2 Audio Direction Notes
- Lo-fi field recordings  
- Gentle wind, reeds, water  
- Subtle tonal cues tied to Echo states  

---

# END OF DOCUMENT
