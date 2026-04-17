# Portal-Revealing Flashlight — Design Spec

**Date:** 2026-04-17  
**Status:** Approved

## Overview

Replace the current standard flashlight with one that reveals portals: a portal is visible only within the circular cone of the flashlight. The flashlight emits no normal scene light.

## Approach

Global shader properties broadcast from `FlashlightController` each frame. The `PortalVortex` shader reads these properties per-fragment to compute a reveal mask based on whether the fragment falls inside the flashlight cone.

## Data Flow

Each frame, `FlashlightController` sets three global shader properties:

| Property | Type | Value |
|---|---|---|
| `_FlashlightWorldPos` | `Vector4` | World-space position of the flashlight |
| `_FlashlightWorldDir` | `Vector4` | Normalized world-space forward direction |
| `_FlashlightCosHalfAngle` | `Float` | `cos(spotAngle / 2)` — cone threshold |

When the flashlight is toggled off, `_FlashlightCosHalfAngle` is set to `-1`, making the reveal factor 0 everywhere (portal fully hidden).

The `Light` component keeps `intensity = 0` at all times — it does not illuminate the scene. Toggle uses `flashlight.enabled` as before.

## Shader Changes — `PortalVortex.shader`

### New global properties (declared outside CBUFFER)
```hlsl
float4 _FlashlightWorldPos;
float4 _FlashlightWorldDir;
float  _FlashlightCosHalfAngle;
```

### Varyings
Add `float3 positionWS : TEXCOORD1` to pass world-space fragment position from vertex to fragment stage.

### Vertex shader
```hlsl
output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
```

### Fragment shader — reveal mask (applied before returning)
```hlsl
float3 toFrag = normalize(input.positionWS - _FlashlightWorldPos.xyz);
float cosAngle = dot(toFrag, _FlashlightWorldDir.xyz);
float edgeSoftness = 0.05;
float reveal = smoothstep(_FlashlightCosHalfAngle - edgeSoftness, _FlashlightCosHalfAngle, cosAngle);

return half4(finalColor, alpha * reveal);
```

The `edgeSoftness` of `0.05` produces a natural soft boundary on the reveal circle. Can be promoted to a shader property `_FlashlightEdgeSoftness` if needed.

## Script Changes — `FlashlightController.cs`

### Awake
Set `flashlight.intensity = 0f` — light component no longer illuminates the scene.

### Update — broadcast per frame
```csharp
if (flashlight.enabled)
{
    Shader.SetGlobalVector("_FlashlightWorldPos", flashlight.transform.position);
    Shader.SetGlobalVector("_FlashlightWorldDir", flashlight.transform.forward);
    Shader.SetGlobalFloat("_FlashlightCosHalfAngle", Mathf.Cos(spotAngle * 0.5f * Mathf.Deg2Rad));
}
else
{
    Shader.SetGlobalFloat("_FlashlightCosHalfAngle", -1f);
}
```

## Files to Modify

- `Assets/TSF/Scripts/FlashlightController.cs`
- `Assets/TSF/Shaders/PortalVortex.shader`

## Out of Scope

- No changes to `PortalAnimator.cs`
- No changes to scene hierarchy or materials
- No new render features or passes
