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

## Development Workflow

This project uses JIRA for task management and follows a structured git workflow for feature development.

### Working on JIRA Tickets

**IMPORTANT: JIRA MCP Authentication:**
- The JIRA MCP integration occasionally loses authentication permissions
- If you encounter authentication errors (e.g., "Unauthorized", "Authentication failed", "accessibleResources.filter is not a function"), you should:
    1. **PAUSE your current work immediately**
    2. **Inform the user** that JIRA MCP authentication has been lost
    3. **Request that the user re-authenticate** the MCP before continuing
    4. **Do NOT attempt to continue** working on JIRA tickets without authentication
- Once the user has re-authenticated, you can resume the ticket workflow

**JIRA Status Flow:**
- **To Do** → **In Progress** → **In Review** → **Done**

**Ticket Selection Process:**
1. Check JIRA for tickets in "To Do" status that have no blocking dependencies
2. Select the **highest priority** ticket available
3. If multiple tickets have the same priority, select the one with the **lowest ticket number** (e.g., STIL-10 before STIL-15)

**Implementation Workflow:**

1. **Move Ticket to In Progress**
    - Update the JIRA ticket status from "To Do" to "In Progress"
    - This signals that work has started on this ticket

2. **Create Feature Branch**
    - Create a new git feature branch from the appropriate base branch
    - Branch naming convention: `feature/STIL-{ticket-number}-{brief-description}`
    - Example: `feature/STIL-10-unity-project-setup` or `feature/STIL-24-fishing-state-enum`

   **For Regular Tickets:**
   ```bash
   # Create feature branch from main
   git checkout main
   git pull origin main
   git checkout -b feature/STIL-10-unity-project-setup
   ```

   **For Epic Tickets (e.g., Fishing State Machine STIL-4):**
   ```bash
   # First ticket of epic - create epic branch from main
   git checkout main
   git pull origin main
   git checkout -b epic/STIL-4-fishing-state-machine
   git push -u origin epic/STIL-4-fishing-state-machine

   # For subsequent tickets in epic - create feature branch from epic branch
   git checkout epic/STIL-4-fishing-state-machine
   git pull origin epic/STIL-4-fishing-state-machine
   git checkout -b feature/STIL-24-fishing-state-enum
   ```

3. **Implement the Feature**
    - Work through the tasks listed in the JIRA ticket description
    - Follow the technical specifications and acceptance criteria
    - Write clean, well-commented code following C# best practices and Unity conventions
    - Make regular commits with clear, descriptive messages
   ```bash
   git add .
   git commit -m "STIL-10: Set up initial Unity project structure"
   ```

4. **Test the Implementation**
    - **CRITICAL:** Always test before committing or creating PRs
    - **Write unit tests** for new functionality when possible:
        - Create test classes in the appropriate `Tests/` assembly
        - Cover core logic, edge cases, and error conditions
        - Aim for tests that validate acceptance criteria
    - **Run the Unity Test Runner** (Window > General > Test Runner):
        - Execute all tests in Edit Mode and Play Mode
        - **All new and existing tests must pass** before proceeding
        - Fix any test failures before creating a PR
    - **Manual testing** in Unity Editor:
        - Check Console for compiler errors (must be zero)
        - Enter Play Mode and verify the feature works as expected
        - Watch Console for runtime errors and Debug.Log output
        - Test edge cases and verify no regressions
    - **Only proceed to PR if all tests pass and manual testing confirms changes work correctly**
   ```
   # Testing workflow:
   # 1. Write unit tests for new functionality (when applicable)
   # 2. Open Window > General > Test Runner
   # 3. Run All Tests - ALL must pass (new and existing)
   # 4. Check Console for compiler errors (must be zero)
   # 5. Enter Play Mode and manually verify the feature
   # 6. Document test coverage in PR description
   ```

5. **Open Pull Request**
    - Push the feature branch to GitHub
    - Open a pull request to merge the feature branch back into the appropriate base branch
    - **Regular tickets**: PR to `main`
    - **Epic tickets**: PR to the epic branch (e.g., `epic/STIL-4-fishing-state-machine`)
    - PR title should reference the ticket: "STIL-10: Create Unity 6.2 project with 2D URP"
    - PR description should include:
        - Link to the JIRA ticket (e.g., `https://chrischappelear.atlassian.net/browse/STIL-10`)
        - Summary of changes
        - **Manual testing performed** (Phase 1: document all test steps and results)
        - Any notes or considerations
    - **IMPORTANT:** Do NOT merge the PR - wait for code review and approval

   **Regular Ticket:**
   ```bash
   git push origin feature/STIL-10-unity-project-setup
   # Create PR to 'main' branch
   ```

   **Epic Ticket:**
   ```bash
   git push origin feature/STIL-24-fishing-state-enum
   # Create PR to 'epic/STIL-4-fishing-state-machine' branch
   ```

6. **Update JIRA Ticket**
    - Add a comment with the PR link
    - Move ticket to "In Review" status
    - Document any issues encountered or deviations from the spec
    - Ticket will move to "Done" after PR is reviewed and merged

### Git Branch Strategy

- **main**: Primary development and integration branch
- **epic/**: Long-lived branches for large features spanning multiple tickets
- **feature/**: Individual feature branches created from main or epic branches

**Standard Workflow (Regular Features):**
- Feature branches are created from and merge back to `main`
- Example: `feature/STIL-45-player-prefab` → PR to `main`

**Epic Workflow (Large Multi-Ticket Features):**
For major features spanning multiple tickets (e.g., Fishing State Machine - STIL-4):
1. Create an epic branch from `main`: `epic/STIL-4-fishing-state-machine`
2. Create feature branches from the epic branch: `feature/STIL-24-fishing-state-enum`
3. Submit PRs from feature branches back to the epic branch
4. When the epic is complete, merge the epic branch to `main`

**Epic Branch Examples:**
- `epic/STIL-4-fishing-state-machine` - For Fishing State Machine (STIL-24 through STIL-33)
- `epic/STIL-6-echo-system` - For Echo System (STIL-37 through STIL-40)

**Important Notes:**
- Regular features: branch from `main`, PR to `main`
- Epic features: branch from epic branch, PR to epic branch
- Never merge PRs yourself - wait for code review and approval

### Commit Message Guidelines

- Prefix commits with ticket number: `STIL-X: Description`
- Use present tense: "Add feature" not "Added feature"
- Be descriptive but concise
- Reference multiple tickets if applicable: `STIL-10, STIL-11: Set up project and folder structure`

### Example Complete Workflow

**Example 1: Regular Feature (Standard Workflow)**
```bash
# 1. Check JIRA, select STIL-10 (highest priority, lowest number)
# 2. Move STIL-10 to "In Progress" in JIRA

# 3. Create feature branch from main
git checkout main
git pull origin main
git checkout -b feature/STIL-10-unity-project-setup

# 4. Implement the feature
# ... work on the feature ...
git add .
git commit -m "STIL-10: Create Unity 6.2 project with 2D URP template"
git add .
git commit -m "STIL-10: Configure project settings for target platforms"
git add .
git commit -m "STIL-10: Verify URP 2D Renderer is active"

# 5. Test the implementation
# - Write unit tests for new functionality
# - Run Unity Test Runner - ALL tests must pass
# - Check Console for compiler errors (must be zero)
# - Enter Play Mode and manually verify the feature
# DO NOT proceed to PR if tests fail or errors occur

# 6. Open pull request
git push origin feature/STIL-10-unity-project-setup
# Create PR on GitHub with:
# - Title: "STIL-10: Create Unity 6.2 project with 2D URP template"
# - Description: JIRA link, changes summary, test coverage, manual testing results
# - Target branch: main
# - DO NOT MERGE - wait for review

# 7. Update JIRA with PR link and move to "In Review"
# Ticket moves to "Done" after PR is reviewed and merged
```

**Example 2: Epic Feature (Fishing State Machine Workflow)**
```bash
# 1. Check JIRA, select STIL-24 (first ticket of STIL-4 epic)
# 2. Move STIL-24 to "In Progress" in JIRA

# 3. Create epic branch (first ticket of epic only)
git checkout main
git pull origin main
git checkout -b epic/STIL-4-fishing-state-machine
git push -u origin epic/STIL-4-fishing-state-machine

# 4. Create feature branch from epic branch
git checkout -b feature/STIL-24-fishing-state-enum

# For subsequent tickets in same epic (STIL-25, 26, etc.):
# git checkout epic/STIL-4-fishing-state-machine
# git pull origin epic/STIL-4-fishing-state-machine
# git checkout -b feature/STIL-25-state-machine-core

# 5. Implement, commit, and test (same as regular workflow)
git add .
git commit -m "STIL-24: Define FishingState enum and interfaces"
# Write unit tests and run Unity Test Runner - ALL tests must pass

# 6. Open pull request to EPIC BRANCH (not main)
git push origin feature/STIL-24-fishing-state-enum
# Create PR on GitHub with:
# - Title: "STIL-24: Define Fishing State Enum and Interfaces"
# - Description: JIRA link, changes summary, test coverage, manual testing results
# - Target branch: epic/STIL-4-fishing-state-machine  ← IMPORTANT!
# - DO NOT MERGE - wait for review

# 7. Update JIRA with PR link and move to "In Review"
# When all epic tickets (STIL-24-33) are complete:
# - Create final PR from epic/STIL-4-fishing-state-machine to main
# - Merge epic into main after approval
```