# AGENTS.md

Guidance for Codex and other AI agents working in this repository.

## Project Overview

Game Jam project (started 2026-04-17). First-person perspective game built in Unity 6 (6000.3.10f1) with a toon/stylized aesthetic. All code and comments must be written in **English**.

## Unity Version

**Unity 6000.3.10f1** — open the project via Unity Hub.

## Architecture

### Rendering
- **Universal Render Pipeline (URP) 17.3.0** — all materials must be URP-compatible
- **Quibli** — toon/stylized shaders; custom toon materials live in `Assets/TSF/Materials/`
- **TSF.shader** — custom MatCap-based toon shader in `Assets/TSF/Shaders/`
- Quality tiers: `Assets/Settings/PC_RPAsset.asset` (desktop) and `Assets/Settings/Mobile_RPAsset.asset` (mobile)

### Input
- **Rewired** (primary, `Assets/Rewired/`) — advanced multi-platform input with remapping support
- **Unity Input System 1.18.0** — actions defined in `Assets/InputSystem_Actions.inputactions`
  - Player map: Move, Look, Attack, Interact, Crouch
- Prefer Rewired for game logic; Input System actions are available as fallback

### Game Feel
- **Feel / MMFeedbacks** (`Assets/Feel/`) — use `MMFeedbacks` components for player action responses (hit, pickup, UI transitions, etc.)
- **DOTween** (`Assets/Plugins/Demigiant/DOTween/`) — tweening for animations and UI

### Key Scenes
- `Assets/Scenes/Game.unity` — main game scene (DirectionalLight + MainCamera)
- `Assets/TSF/stage.unity` — stage with animated mascot characters, toon materials, and geometry

### AI / Navigation
- **Unity AI Navigation 2.0.10** (`com.unity.ai.navigation`)

### Timeline
- **Unity Timeline 1.8.10** — for cutscenes and scripted sequences

## Asset Conventions

- Game-specific scripts, models, materials, and textures go in `Assets/TSF/`
- Third-party packages live in their own top-level folders (`Assets/Feel/`, `Assets/Rewired/`, `Assets/Quibli/`, etc.) — **do not modify**

## Coding Guidelines

- All C# code and comments must be in **English**
- Scripts go in `Assets/TSF/Scripts/`
- Follow Unity component-based patterns; avoid singletons unless necessary
- Use `MMFeedbacks` for game-feel responses instead of ad-hoc animations
- Use DOTween for UI and procedural tweening

## What Agents Should NOT Do

- Do not modify third-party package folders
- Do not create materials outside `Assets/TSF/Materials/`
- Do not use the legacy Built-in Render Pipeline — URP only
- Do not use Unity's old Input Manager — use Rewired or Input System
