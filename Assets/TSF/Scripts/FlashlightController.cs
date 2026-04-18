using System.Collections.Generic;
using UnityEngine;
using Rewired;

namespace TSF
{
    // Attach to the Camera child of the Player GameObject.
    // Requires a child GameObject named "Flashlight" with a Spot Light.
    [ExecuteAlways]
    public class FlashlightController : MonoBehaviour
    {
        private static readonly int FlashlightWorldPos     = Shader.PropertyToID("_FlashlightWorldPos");
        private static readonly int FlashlightWorldDir     = Shader.PropertyToID("_FlashlightWorldDir");
        private static readonly int FlashlightCosHalfAngle = Shader.PropertyToID("_FlashlightCosHalfAngle");
        private static readonly int FlashlightRange        = Shader.PropertyToID("_FlashlightRange");
        private static readonly int FlashlightEditorReveal = Shader.PropertyToID("_FlashlightEditorReveal");

        [Header("Flashlight")]
        [SerializeField] private Light flashlight;
        [SerializeField] private bool onByDefault = true;

        [Header("Light Settings")]
        [SerializeField] private float range = 15f;
        [SerializeField] [Range(1f, 179f)] private float spotAngle = 45f;
        [SerializeField] private float innerSpotAngle = 20f;
        [SerializeField] private Color lightColor = Color.white;

        [Header("Cone Visualization")]
        [SerializeField] private bool showCone = true;
        [SerializeField] private Color coneColor = new Color(1f, 0.95f, 0.75f, 0.12f);
        [SerializeField] [Range(4, 48)] private int coneSegments = 20;

        private Player _player;
        private bool _initialized;

        // Cone — only valid in play mode
        private MeshFilter   _coneMeshFilter;
        private MeshRenderer _coneMeshRenderer;
        private Material     _coneMaterial;

        void Awake()
        {
            if (flashlight == null)
                flashlight = GetComponentInChildren<Light>();

            if (flashlight != null)
            {
                flashlight.type           = LightType.Spot;
                flashlight.intensity      = 0f;
                flashlight.range          = range;
                flashlight.spotAngle      = spotAngle;
                flashlight.innerSpotAngle = innerSpotAngle;
                flashlight.color          = lightColor;
                flashlight.shadows        = LightShadows.Soft;
                flashlight.enabled        = onByDefault;
            }
            else
            {
                Debug.LogWarning("[FlashlightController] No Light found.", this);
            }

            // Cone is only created at runtime to avoid editor prefab issues.
            if (Application.isPlaying)
                SetupCone();

            BroadcastFlashlightGlobals();
        }

        void Update()
        {
            if (Application.isPlaying && ReInput.isReady)
            {
                if (!_initialized) Initialize();

                if (_player.GetButtonDown("Flashlight"))
                    Toggle();
            }

            BroadcastFlashlightGlobals();
        }

        void OnDisable()
        {
            Shader.SetGlobalFloat(FlashlightEditorReveal, Application.isPlaying ? 0f : 1f);
            Shader.SetGlobalFloat(FlashlightCosHalfAngle, -1f);
        }

        void OnValidate()
        {
            if (flashlight == null)
                flashlight = GetComponentInChildren<Light>();

            // OnValidate runs in editor and prefab context — never touch the scene hierarchy here.
            BroadcastFlashlightGlobals();
        }

        private void Initialize()
        {
            _player = ReInput.players.GetPlayer(0);
            _initialized = true;
        }

        public void Toggle()
        {
            if (flashlight != null)
                flashlight.enabled = !flashlight.enabled;
            UpdateConeVisibility();
        }

        // ── Cone ────────────────────────────────────────────────────────────

        private void SetupCone()
        {
            if (flashlight == null) return;

            var coneGO = new GameObject("_LightCone");
            coneGO.transform.SetParent(flashlight.transform, false);

            _coneMeshFilter   = coneGO.AddComponent<MeshFilter>();
            _coneMeshRenderer = coneGO.AddComponent<MeshRenderer>();
            _coneMeshRenderer.shadowCastingMode        = UnityEngine.Rendering.ShadowCastingMode.Off;
            _coneMeshRenderer.receiveShadows           = false;
            _coneMeshRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;

            var shader = Shader.Find("TSF/FlashlightCone");
            if (shader != null)
            {
                _coneMaterial       = new Material(shader) { name = "FlashlightConeMat" };
                _coneMaterial.color = coneColor;
                _coneMeshRenderer.sharedMaterial = _coneMaterial;
            }
            else
            {
                Debug.LogWarning("[FlashlightController] Shader 'TSF/FlashlightCone' not found.", this);
            }

            _coneMeshFilter.mesh = BuildConeMesh(coneSegments, range, spotAngle);
            UpdateConeVisibility();
        }

        private void UpdateConeVisibility()
        {
            if (_coneMeshRenderer != null)
                _coneMeshRenderer.enabled = showCone && (flashlight == null || flashlight.enabled);
        }

        private static Mesh BuildConeMesh(int segments, float height, float angleDeg)
        {
            float halfRad    = angleDeg * 0.5f * Mathf.Deg2Rad;
            float baseRadius = Mathf.Tan(halfRad) * height;

            var verts = new List<Vector3>(segments * 3);
            var uvs   = new List<Vector2>(segments * 3);
            var tris  = new List<int>(segments * 3);

            // Cone sides as quads (two triangles per segment) so the wall has area.
            // Tip is offset slightly forward to avoid near-clip eating the geometry.
            const float tipOffset = 0.05f;
            float       tipRadius = Mathf.Tan(halfRad) * tipOffset;

            for (int i = 0; i < segments; i++)
            {
                float a0 = 2f * Mathf.PI * i / segments;
                float a1 = 2f * Mathf.PI * (i + 1) / segments;

                Vector3 t0 = new Vector3(Mathf.Sin(a0) * tipRadius,  Mathf.Cos(a0) * tipRadius,  tipOffset);
                Vector3 t1 = new Vector3(Mathf.Sin(a1) * tipRadius,  Mathf.Cos(a1) * tipRadius,  tipOffset);
                Vector3 b0 = new Vector3(Mathf.Sin(a0) * baseRadius, Mathf.Cos(a0) * baseRadius, height);
                Vector3 b1 = new Vector3(Mathf.Sin(a1) * baseRadius, Mathf.Cos(a1) * baseRadius, height);

                float u0 = (float) i      / segments;
                float u1 = (float)(i + 1) / segments;

                // Triangle 1
                int idx = verts.Count;
                verts.Add(t0); uvs.Add(new Vector2(u0, 0f));
                verts.Add(b0); uvs.Add(new Vector2(u0, 1f));
                verts.Add(b1); uvs.Add(new Vector2(u1, 1f));
                tris.Add(idx); tris.Add(idx + 1); tris.Add(idx + 2);

                // Triangle 2
                idx = verts.Count;
                verts.Add(t0); uvs.Add(new Vector2(u0, 0f));
                verts.Add(b1); uvs.Add(new Vector2(u1, 1f));
                verts.Add(t1); uvs.Add(new Vector2(u1, 0f));
                tris.Add(idx); tris.Add(idx + 1); tris.Add(idx + 2);
            }

            // Base disc — most visible in first-person (looking along cone axis).
            // uv.y = 1 at the perimeter edge, 0 at centre, so the rim glows.
            Vector3 discCenter = new Vector3(0f, 0f, height);
            for (int i = 0; i < segments; i++)
            {
                float a0  = 2f * Mathf.PI * i / segments;
                float a1  = 2f * Mathf.PI * (i + 1) / segments;
                int   idx = verts.Count;

                verts.Add(discCenter);
                verts.Add(new Vector3(Mathf.Sin(a0) * baseRadius, Mathf.Cos(a0) * baseRadius, height));
                verts.Add(new Vector3(Mathf.Sin(a1) * baseRadius, Mathf.Cos(a1) * baseRadius, height));

                uvs.Add(new Vector2(0.5f, 0f));
                uvs.Add(new Vector2((float) i       / segments, 1f));
                uvs.Add(new Vector2((float)(i + 1)  / segments, 1f));

                tris.Add(idx); tris.Add(idx + 1); tris.Add(idx + 2);
            }

            var mesh = new Mesh { name = "FlashlightConeMesh" };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        // ── Globals ─────────────────────────────────────────────────────────

        private void BroadcastFlashlightGlobals()
        {
            Shader.SetGlobalFloat(FlashlightEditorReveal, Application.isPlaying ? 0f : 1f);

            if (flashlight == null || !flashlight.enabled)
            {
                Shader.SetGlobalFloat(FlashlightCosHalfAngle, -1f);
                return;
            }

            Shader.SetGlobalVector(FlashlightWorldPos, flashlight.transform.position);
            Shader.SetGlobalVector(FlashlightWorldDir, flashlight.transform.forward.normalized);
            Shader.SetGlobalFloat(FlashlightCosHalfAngle, Mathf.Cos(spotAngle * 0.5f * Mathf.Deg2Rad));
            Shader.SetGlobalFloat(FlashlightRange, Mathf.Max(range, 0.001f));
        }
    }
}
