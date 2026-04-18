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
        private Object _forcedLookOwner;
        private Transform _forcedLookTarget;

        void Awake()
        {
            _playerRoot = transform.parent;
            if (_playerRoot == null)
                Debug.LogError("[FPSCameraController] Camera must be a child of the Player root.", this);
            SetCursorLocked(true);
        }

        void Update()
        {
            if (!ReInput.isReady) return;
            if (!_initialized) Initialize();

            if (Input.GetKeyDown(KeyCode.Escape))
                SetCursorLocked(!_cursorLocked);

            if (_forcedLookTarget != null)
                ForceLookAtTarget();
            else if (_cursorLocked)
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

        public void SetForcedLookAt(Object owner, Transform target)
        {
            if (owner == null || target == null)
                return;

            _forcedLookOwner = owner;
            _forcedLookTarget = target;
            ForceLookAtTarget();
        }

        public void ClearForcedLookAt(Object owner)
        {
            if (_forcedLookOwner != owner)
                return;

            _forcedLookOwner = null;
            _forcedLookTarget = null;
        }

        private void ForceLookAtTarget()
        {
            if (_playerRoot == null || _forcedLookTarget == null)
                return;

            Vector3 direction = _forcedLookTarget.position - transform.position;
            if (direction.sqrMagnitude <= 0.0001f)
                return;

            Vector3 planarDirection = direction;
            planarDirection.y = 0f;
            if (planarDirection.sqrMagnitude > 0.0001f)
                _playerRoot.rotation = Quaternion.LookRotation(planarDirection.normalized, Vector3.up);

            Vector3 localDirection = _playerRoot.InverseTransformDirection(direction.normalized);
            float targetPitch = -Mathf.Atan2(localDirection.y, new Vector2(localDirection.x, localDirection.z).magnitude) * Mathf.Rad2Deg;
            _pitch = Mathf.Clamp(targetPitch, pitchMin, pitchMax);
            transform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }
    }
}
