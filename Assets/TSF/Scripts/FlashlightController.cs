using UnityEngine;
using Rewired;

namespace TSF
{
    // Attach to the Camera child of the Player GameObject.
    // Requires a child GameObject named "Flashlight" with a Spot Light.
    [ExecuteAlways]
    public class FlashlightController : MonoBehaviour
    {
        private static readonly int FlashlightWorldPos = Shader.PropertyToID("_FlashlightWorldPos");
        private static readonly int FlashlightWorldDir = Shader.PropertyToID("_FlashlightWorldDir");
        private static readonly int FlashlightCosHalfAngle = Shader.PropertyToID("_FlashlightCosHalfAngle");
        private static readonly int FlashlightEditorReveal = Shader.PropertyToID("_FlashlightEditorReveal");

        [Header("Flashlight")]
        [SerializeField] private Light flashlight;
        [SerializeField] private bool onByDefault = true;

        [Header("Light Settings")]
        [SerializeField] private float range = 15f;
        [SerializeField] [Range(1f, 179f)] private float spotAngle = 45f;
        [SerializeField] private float innerSpotAngle = 20f;
        [SerializeField] private Color lightColor = Color.white;

        private Player _player;
        private bool _initialized;

        void Awake()
        {
            if (flashlight == null)
                flashlight = GetComponentInChildren<Light>();

            if (flashlight != null)
            {
                flashlight.type = LightType.Spot;
                flashlight.intensity = 0f;
                flashlight.range = range;
                flashlight.spotAngle = spotAngle;
                flashlight.innerSpotAngle = innerSpotAngle;
                flashlight.color = lightColor;
                flashlight.shadows = LightShadows.Soft;
                flashlight.enabled = onByDefault;
            }
            else
            {
                Debug.LogWarning("[FlashlightController] No Light found. Add a child GameObject with a Light component.", this);
            }

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
        }

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
        }
    }
}
