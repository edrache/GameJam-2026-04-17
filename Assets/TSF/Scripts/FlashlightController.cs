using UnityEngine;
using Rewired;

namespace TSF
{
    // Attach to the Camera child of the Player GameObject.
    // Requires a child GameObject named "Flashlight" with a Spot Light.
    public class FlashlightController : MonoBehaviour
    {
        [Header("Flashlight")]
        [SerializeField] private Light flashlight;
        [SerializeField] private bool onByDefault = true;

        [Header("Light Settings")]
        [SerializeField] private float intensity = 3f;
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
                flashlight.intensity = intensity;
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
        }

        void Update()
        {
            if (!ReInput.isReady) return;
            if (!_initialized) Initialize();

            if (_player.GetButtonDown("Flashlight"))
                Toggle();
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
    }
}
