using UnityEngine;
using Rewired;

namespace TSF
{
    // Attach to the Camera child of the Player GameObject.
    // Player root handles yaw (Y rotation); this component handles pitch (X rotation).
    public class FPSCameraController : MonoBehaviour
    {
        [Header("Look")]
        [SerializeField] private float mouseSensitivity = 300f;
        [SerializeField] private float pitchMin = -80f;
        [SerializeField] private float pitchMax = 80f;

        private Player _player;
        private bool _initialized;
        private Transform _playerRoot;
        private float _pitch;

        void Awake()
        {
            _playerRoot = transform.parent;
            if (_playerRoot == null)
                Debug.LogError("[FPSCameraController] Camera must be a child of the Player root.", this);
        }

        void Update()
        {
            if (!ReInput.isReady) return;
            if (!_initialized) Initialize();

            if (Input.GetKeyDown(KeyCode.Escape))
                SetCursorLocked(!_cursorLocked);

            if (_cursorLocked)
                Look();
        }

        private bool _cursorLocked = true;

        private void SetCursorLocked(bool locked)
        {
            _cursorLocked = locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        private void Initialize()
        {
            _player = ReInput.players.GetPlayer(0);
            _initialized = true;
        }

        private void Look()
        {
            float lookH = _player.GetAxis("Look Horizontal");
            float lookV = _player.GetAxis("Look Vertical");

            // Multiply by Time.deltaTime for frame-rate independent gamepad stick input.
            // For mouse axes configured as Relative in Rewired, lower mouseSensitivity accordingly.
            _playerRoot.Rotate(Vector3.up, lookH * mouseSensitivity * Time.deltaTime, Space.Self);

            _pitch -= lookV * mouseSensitivity * Time.deltaTime;
            _pitch = Mathf.Clamp(_pitch, pitchMin, pitchMax);
            transform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }
    }
}
