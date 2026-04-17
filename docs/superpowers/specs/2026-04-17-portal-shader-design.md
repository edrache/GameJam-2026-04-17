# Portal Vortex Shader — Design Spec

**Date:** 2026-04-17  
**Project:** GameJam-2026-04-17  
**Status:** Approved

## Overview

A Rick and Morty-style animated portal effect implemented as a URP Shader Graph applied to a flat quad. The portal is a visual prop (no gameplay teleport mechanic). Colors are fully configurable via material properties. A lightweight `PortalAnimator.cs` script drives animation parameters through DOTween.

## Files

| Path | Purpose |
|------|---------|
| `Assets/TSF/Shaders/PortalVortex.shadergraph` | Main Shader Graph |
| `Assets/TSF/Materials/Portal.mat` | Material using the shader |
| `Assets/TSF/Scripts/PortalAnimator.cs` | C# animator script |

A noise/Voronoi texture will use Unity's built-in Shader Graph procedural nodes (no external texture asset required).

## Shader Architecture — 6 Layers

Layers are composited in order inside the Shader Graph. The shader uses the **URP Unlit** target with **Alpha Blending** (surface type: Transparent).

### Layer 1 — Circular Alpha Mask
- Compute distance from UV center `(0.5, 0.5)`
- `smoothstep` with `_EdgeSoftness` parameter → soft circular clip
- Drives the final Alpha output

### Layer 2 — Twirl UV Distortion
- Apply the built-in **Twirl** Shader Graph node to the input UVs
- Center: `(0.5, 0.5)`, Strength: `_TwirlStrength`, Offset: `(_TwirlOffset, 0)`
- `_TwirlOffset` is incremented each frame by `PortalAnimator` → continuous spiral motion
- Output: distorted UVs fed into all subsequent layers

### Layer 3 — Swirl Vortex (Noise + Polar UV)
- Convert twirlUVs → polar coordinates `(angle/2π, radius)`
- Sample a **Simple Noise** node on polar UVs with `Time * _ScrollSpeed` offset
- Layer a second octave at 2× frequency, 0.5× amplitude (fbm-style depth)
- Output: float `noiseValue ∈ [0,1]`

### Layer 4 — Color Gradient
- Compute `t = distance from center` (0 = center, 1 = edge)
- Blend three HDR colors: `_ColorCenter → _ColorMid → _ColorEdge` using two `lerp` nodes
- Modulate by `noiseValue` to break up the gradient with organic variation
- Output: `RGB`

### Layer 5 — Sparkle Dots
- Sample **Voronoi** node at high frequency (scale ~20) on twirlUVs
- `step(1 - _SparkleThreshold, voronoi)` → binary white dots
- Mask to edge ring: multiply by `smoothstep(0.5, 0.65, radius)`
- Add to base color with white tint

### Layer 6 — Edge Glow (Emission)
- `edgeMask = smoothstep(0.55, 0.7, radius) * smoothstep(0.85, 0.7, radius)` → ring at ~r=0.7
- Multiply `_ColorEdge * _GlowIntensity` → HDR emission
- Add to final color — triggers URP Bloom post-processing

## Exposed Shader Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `_ColorCenter` | HDR Color | `(0.02, 0.05, 0.02)` | Dark center color |
| `_ColorMid` | HDR Color | `(0.1, 0.6, 0.15)` | Mid swirl color |
| `_ColorEdge` | HDR Color | `(0.5, 2.0, 0.5)` | Bright edge (HDR for bloom) |
| `_TwirlStrength` | Float | `3.0` | Spiral distortion amount |
| `_TwirlOffset` | Float | `0.0` | Animated by PortalAnimator |
| `_ScrollSpeed` | Float | `0.3` | Noise scroll speed |
| `_SparkleThreshold` | Float | `0.15` | Size/density of sparkle dots |
| `_GlowIntensity` | Float | `2.0` | Edge glow multiplier |
| `_EdgeSoftness` | Float | `0.05` | Softness of circular clip |
| `_OpenAmount` | Float | `1.0` | 0=closed, 1=fully open (scales radius) |

## PortalAnimator.cs

Lightweight `MonoBehaviour` attached to the same `GameObject` as the portal quad.

```
Responsibilities:
- Increment _TwirlOffset each frame: offset += _rotationSpeed * Time.deltaTime
- Expose Open() / Close() methods that tween _OpenAmount 0↔1 via DOTween
- Expose SetIntensity(float) for MMFeedbacks integration
```

```csharp
// Public API
void Open(float duration = 1f)   // DOTween: _OpenAmount → 1
void Close(float duration = 0.5f) // DOTween: _OpenAmount → 0
void SetIntensity(float value)    // instant _GlowIntensity override
float RotationSpeed               // serialized field, default 1.5
```

The script uses `material.SetFloat()` on `Awake`-cached `MaterialPropertyBlock` to avoid creating material instances.

## Geometry Setup

- GameObject: `Portal` with `MeshFilter` (Quad) + `MeshRenderer`
- Scale: adjust in scene; shader is UV-space, scale-independent
- Layer: default (no special render layer needed)
- Requires **URP Bloom** post-processing volume in the scene for glow to be visible

## Out of Scope

- 3D depth / scene visible through portal
- Collision / teleport gameplay
- Sound effects (wire up separately via MMFeedbacks)
