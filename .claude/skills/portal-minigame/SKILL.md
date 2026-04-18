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

`ArmReachController` (on Main Camera child of Player) detects when the Animator enters the **"Arm portal"** state, then finds the nearest `PortalAnimator` within `reachDistance` and calls `OnHandEnter` on any `IPortalMiniGame` component on that portal's GameObject.

## Key Files

- `Assets/TSF/Scripts/IPortalMiniGame.cs` — the interface every mini-game implements
- `Assets/TSF/Scripts/ArmReachController.cs` — detects hand entering/exiting portal state, calls the mini-game
- `Assets/TSF/Scripts/PortalAnimator.cs` — lives on the portal GameObject alongside the mini-game
- `Assets/TSF/Scripts/PortalSliderMiniGame.cs` — example: slider that fills up and triggers loot

## The Interface

```csharp
namespace TSF
{
    public interface IPortalMiniGame
    {
        void OnHandEnter(ArmReachController reach);
        void OnHandExit();
    }
}
```

- `OnHandEnter` — called when the hand animation reaches "Arm portal" state. Receives `reach` so you can call `reach.TriggerLoot()` when the mini-game is won.
- `OnHandExit` — called when the player releases the button, moves out of range, or the GameObject is disabled. Use this to clean up UI and cancel ongoing logic.

## Mini-Game Lifecycle

```
OnHandEnter(reach)
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

        public void OnHandEnter(ArmReachController reach)
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

## Setup in Unity

1. Place the script as a **new `.cs` file** in `Assets/TSF/Scripts/`
2. In Unity, select the **Portal GameObject** (the one with `PortalAnimator`)
3. **Add Component** → your new mini-game script
4. Assign any serialized fields (UI elements, etc.) in the Inspector

The portal already has a `MeshCollider` — no extra collider needed. `ArmReachController` uses proximity detection (not raycast), so the player just needs to be within `reachDistance` (default 100, set on Main Camera's `ArmReachController`).

## Rules

- Always implement **both** `OnHandEnter` and `OnHandExit` — `OnHandExit` can be called at any time, even mid-game
- Call `reach.TriggerLoot()` **once** when the player wins — it sets `_active = false` style cleanup before you call it to avoid double-fire
- Never store state between sessions in these components — reset everything in `OnHandExit`
- If you use a UI Canvas, keep the reference serialized so it can be assigned per-portal in the Inspector
- Namespace: `TSF`

## Example: PortalSliderMiniGame

Read `Assets/TSF/Scripts/PortalSliderMiniGame.cs` for a complete working example. It shows:
- Showing/hiding a `Slider` UI on enter/exit
- Filling the slider over time in `Update`
- Calling `TriggerLoot()` when full
- Setting `_completed` to prevent double-triggering
