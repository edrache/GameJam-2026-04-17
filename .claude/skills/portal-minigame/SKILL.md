---
name: portal-minigame
description: Use this skill whenever the user wants to create a new portal mini-game in the GameJam-2026-04-17 Unity project. Trigger when the user says things like "dodaj mini grę", "nowa mini gra do portalu", "stwórz mini grę", "add mini-game", "create mini-game for portal", or describes a new interactive mechanic that happens while the player's hand is inside a portal. This skill explains the full architecture and provides a ready-to-use template.
---

# Portal Mini-Game System

## Architecture Overview

When the player holds the **Reach** button (LPM), the arm animation plays:
```
Idle → Arm reach → Arm portal (loops) → Arm loot (on TriggerLoot) → Idle
                                       → Idle (on button release or out of range)
```

`ArmReachController` (on Main Camera child of Player) detects when the Animator enters the **"Arm portal"** state, then finds the nearest `PortalAnimator` within `reachDistance`, detects whether the player used the portal from the front or back, and calls `OnHandEnter` on the active `IPortalMiniGame`.

Portals can use `PortalSideMiniGameRouter` to route front-side and back-side interactions to different mini-game components.

## Key Files

- `Assets/TSF/Scripts/IPortalMiniGame.cs` — the interface every mini-game implements
- `Assets/TSF/Scripts/ArmReachController.cs` — detects hand entering/exiting portal state, calls the mini-game
- `Assets/TSF/Scripts/PortalAnimator.cs` — lives on the portal GameObject alongside the mini-game
- `Assets/TSF/Scripts/PortalSliderMiniGame.cs` — example: slider that fills up and triggers loot
- `Assets/TSF/Scripts/PortalSideMiniGameRouter.cs` — routes `PortalSide.Front` and `PortalSide.Back` to separate mini-game components
- `Assets/TSF/Scripts/PortalTetherSliderMiniGame.cs` — example: slider that locks exit, clamps player distance, and forces looking at the portal
- `Assets/TSF/Scripts/PlayerController.cs` — exposes reusable distance constraint API for mini-games
- `Assets/TSF/Scripts/FPSCameraController.cs` — exposes reusable forced-look API for mini-games

## The Interface

```csharp
namespace TSF
{
    public enum PortalSide
    {
        Front,
        Back
    }

    public interface IPortalMiniGame
    {
        void OnHandEnter(ArmReachController reach, PortalSide side);
        void OnHandExit();
    }
}
```

- `OnHandEnter` — called when the hand animation reaches "Arm portal" state. Receives `reach` so you can call `reach.TriggerLoot()` when the mini-game is won, and `side` so front/back interactions can behave differently.
- `OnHandExit` — called when the player releases the button, moves out of range, or the GameObject is disabled. Use this to clean up UI and cancel ongoing logic.

## Mini-Game Lifecycle

```
OnHandEnter(reach, side)
  │
  ├── show UI, start logic
  │
  │   [player wins]
  │       └── reach.TriggerLoot()  →  Arm loot animation plays  →  OnHandExit called
  │
  └── [player releases button / leaves range]
          └── OnHandExit()  →  hide UI, cancel logic
```

`TriggerLoot()` fires an Animator trigger that transitions "Arm portal" → "Arm loot". After the loot animation finishes, the Animator returns to Idle and `OnHandExitPortal` is called automatically.

## Front/Back Routing

Use `PortalSideMiniGameRouter` on a portal when the front and back should launch different mini-games.

Inspector setup:

- `Front Mini Game` — assign a `MonoBehaviour` that implements `IPortalMiniGame`
- `Back Mini Game` — assign a different `IPortalMiniGame`
- `Fallback To Other Side` — when enabled, a missing side falls back to the other assigned mini-game

`ArmReachController` detects the side once when the hand enters the portal and passes it to `OnHandEnter(reach, side)`. The current portal mesh has its forward direction opposite to the visible front, so `ArmReachController.GetPortalSide` already accounts for that. Do not duplicate front/back detection inside mini-games unless the mechanic truly needs continuous side checks.

## Reusable Player Locks

Mini-games can reuse built-in lock mechanics instead of writing one-off movement or camera code.

### Locking Portal Exit

Use this when the player must stay in a portal mini-game until it completes:

```csharp
reach.LockMiniGameExit(this);
```

While locked, `ArmReachController` keeps `IsReaching = true` and ignores normal exit caused by releasing Reach or leaving the portal animation. Unlock when the mini-game has completed or when cleaning up:

```csharp
reach.UnlockMiniGameExit(this);
```

Always unlock in completion and defensive cleanup paths such as `OnHandExit` or `OnDisable`.

### Clamping Player Distance

Use `PlayerController.SetDistanceConstraint` to keep the player within a planar distance band from a target:

```csharp
PlayerController player = reach.GetComponentInParent<PlayerController>();
player.SetDistanceConstraint(this, portalTransform, minDistance, maxDistance);
```

Clear it with:

```csharp
player.ClearDistanceConstraint(this);
```

`PortalTetherSliderMiniGame` uses this with `minDistance = 0.5f` and `maxDistance = the player's distance when the mini-game started`, so the player cannot move farther away than their starting distance or closer than 0.5.

### Forcing Look Direction

Use `FPSCameraController.SetForcedLookAt` to keep the player looking at a target:

```csharp
FPSCameraController cameraController = player.GetComponentInChildren<FPSCameraController>();
cameraController.SetForcedLookAt(this, lookTarget);
```

Clear it with:

```csharp
cameraController.ClearForcedLookAt(this);
```

This rotates the player root toward the target and sets camera pitch toward it. Prefer this API over directly setting player or camera rotations from a mini-game.

## Template

```csharp
using UnityEngine;

namespace TSF
{
    public class MyMiniGame : MonoBehaviour, IPortalMiniGame
    {
        // Serialized references to UI or other components go here
        // [SerializeField] private SomeUI ui;

        private ArmReachController _reach;
        private bool _active;

        public void OnHandEnter(ArmReachController reach, PortalSide side)
        {
            _reach = reach;
            _active = true;
            // show UI, start coroutines, play sounds, etc.
        }

        public void OnHandExit()
        {
            _active = false;
            _reach = null;
            // hide UI, stop coroutines, reset state
        }

        void Update()
        {
            if (!_active) return;

            // mini-game logic here...

            // when the player wins:
            // _active = false;
            // _reach.TriggerLoot();
        }
    }
}
```

## Template With Locked Exit

Use this pattern when the player must finish the mini-game once it starts:

```csharp
using UnityEngine;

namespace TSF
{
    public class MyLockedMiniGame : MonoBehaviour, IPortalMiniGame
    {
        private ArmReachController _reach;
        private bool _active;

        public void OnHandEnter(ArmReachController reach, PortalSide side)
        {
            _reach = reach;
            _active = true;
            _reach.LockMiniGameExit(this);
        }

        public void OnHandExit()
        {
            _reach?.UnlockMiniGameExit(this);
            _active = false;
            _reach = null;
        }

        private void OnDisable()
        {
            _reach?.UnlockMiniGameExit(this);
        }

        private void Complete()
        {
            if (!_active) return;

            _active = false;
            _reach.UnlockMiniGameExit(this);
            _reach.TriggerLoot();
        }
    }
}
```

## Setup in Unity

1. Place the script as a **new `.cs` file** in `Assets/TSF/Scripts/`
2. In Unity, select the **Portal GameObject** (the one with `PortalAnimator`)
3. **Add Component** → your new mini-game script
4. If the portal uses `PortalSideMiniGameRouter`, assign your component to either `Front Mini Game` or `Back Mini Game`
5. Assign any serialized fields (UI elements, etc.) in the Inspector

The portal already has a `MeshCollider` — no extra collider needed. `ArmReachController` uses proximity detection (not raycast), so the player just needs to be within `reachDistance` (default 100, set on Main Camera's `ArmReachController`).

## Rules

- Always implement **both** `OnHandEnter` and `OnHandExit` — `OnHandExit` can be called at any time, even mid-game
- Call `reach.TriggerLoot()` **once** when the player wins — it sets `_active = false` style cleanup before you call it to avoid double-fire
- If you call `reach.LockMiniGameExit(this)`, always call `reach.UnlockMiniGameExit(this)` before `TriggerLoot`, in `OnHandExit`, and in `OnDisable`
- Use `PlayerController.SetDistanceConstraint/ClearDistanceConstraint` for distance locks instead of moving the player manually from each mini-game
- Use `FPSCameraController.SetForcedLookAt/ClearForcedLookAt` for forced camera/player facing instead of custom rotation code
- Never store state between sessions in these components — reset everything in `OnHandExit`
- If you use a UI Canvas, keep the reference serialized so it can be assigned per-portal in the Inspector
- Namespace: `TSF`

## Example: PortalSliderMiniGame

Read `Assets/TSF/Scripts/PortalSliderMiniGame.cs` for a complete working example. It shows:
- Showing/hiding a `Slider` UI on enter/exit
- Filling the slider over time in `Update`
- Calling `TriggerLoot()` when full
- Setting `_completed` to prevent double-triggering

## Example: PortalTetherSliderMiniGame

Read `Assets/TSF/Scripts/PortalTetherSliderMiniGame.cs` for a complete locked mini-game example. It shows:

- Using `reach.LockMiniGameExit(this)` so the player cannot cancel the mini-game
- Clamping the player between `minimumPortalDistance` and their starting distance from the portal
- Forcing the camera/player to look at the portal while the slider fills
- Clearing all locks before triggering loot and in cleanup paths
