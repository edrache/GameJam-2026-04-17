using UnityEngine;
using Rewired;

namespace TSF
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float walkSpeed = 5f;
        [SerializeField] private float sprintSpeed = 10f;
        [SerializeField] private float jumpHeight = 1.5f;
        [SerializeField] private float gravity = -20f;

        private CharacterController _cc;
        private Player _player;
        private bool _initialized;

        private Vector3 _velocity;
        private float _moveH;
        private float _moveV;
        private bool _jumpPressed;
        private bool _sprinting;

        void Awake()
        {
            _cc = GetComponent<CharacterController>();
        }

        void Update()
        {
            if (!ReInput.isReady) return;
            if (!_initialized) Initialize();

            GetInput();
            Move();
        }

        private void Initialize()
        {
            _player = ReInput.players.GetPlayer(0);
            _initialized = true;
        }

        private void GetInput()
        {
            _moveH = _player.GetAxis("Move Horizontal");
            _moveV = _player.GetAxis("Move Vertical");
            _jumpPressed = _player.GetButtonDown("Jump");
            _sprinting = _player.GetButton("Sprint");
        }

        private void Move()
        {
            // Horizontal movement relative to player facing direction
            Vector3 move = transform.right * _moveH + transform.forward * _moveV;
            float speed = _sprinting ? sprintSpeed : walkSpeed;
            _cc.Move(move * speed * Time.deltaTime);

            // Gravity first, then ground clamp, then jump — order matters
            _velocity.y += gravity * Time.deltaTime;

            if (_cc.isGrounded && _velocity.y < 0f)
                _velocity.y = -2f; // small negative keeps isGrounded reliable

            if (_jumpPressed && _cc.isGrounded)
                _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

            _cc.Move(_velocity * Time.deltaTime);
        }
    }
}
