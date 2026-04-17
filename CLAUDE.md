# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Game Jam project (started 2026-04-17). First-person perspective game built in Unity 6 (6000.3.10f1) with a toon/stylized aesthetic. All code and comments must be written in English.

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
- **Feel / MMFeedbacks** (`Assets/Feel/`) — use `MMFeedbacks` components for juicy response to player actions (hit, pickup, UI transitions, etc.)
- **DOTween** (`Assets/Plugins/Demigiant/DOTween/`) — tweening for animations and UI

### Key Scenes
- `Assets/Scenes/Game.unity` — main game scene (currently minimal: DirectionalLight + MainCamera)
- `Assets/TSF/stage.unity` — stage with animated mascot characters (samba animation), toon materials, and geometry

### Asset Conventions
- Game-specific scripts, models, materials, and textures go in `Assets/TSF/`
- Third-party packages live in their own top-level folders (`Assets/Feel/`, `Assets/Rewired/`, `Assets/Quibli/`, etc.) — do not modify

### AI / Navigation
- **Unity AI Navigation 2.0.10** is available (`com.unity.ai.navigation`)

### Timeline
- **Unity Timeline 1.8.10** is available for cutscenes and scripted sequences
