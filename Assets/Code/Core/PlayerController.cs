using UnityEngine;
using Stillwater.Framework;

namespace Stillwater.Core
{
    /// <summary>
    /// Controls player movement with isometric direction conversion.
    /// Reads input from IInputService and applies smooth movement.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _acceleration = 10f;
        [SerializeField] private float _deceleration = 10f;

        [Header("Isometric Settings")]
        [Tooltip("Enable isometric direction conversion for input")]
        [SerializeField] private bool _useIsometricMovement = true;

        [Tooltip("Ratio for isometric Y movement (typically 0.5 for 2:1 isometric)")]
        [SerializeField] private float _isometricYRatio = 0.5f;

        [Header("Debug")]
        [SerializeField] private bool _logMovement;

        private Rigidbody2D _rigidbody;
        private IInputService _inputService;
        private Vector2 _currentVelocity;
        private bool _isInitialized;

        public float MoveSpeed
        {
            get => _moveSpeed;
            set => _moveSpeed = value;
        }

        public bool IsMoving => _currentVelocity.sqrMagnitude > 0.01f;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _rigidbody.gravityScale = 0f;
            _rigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;
            _isKinematic = _rigidbody.bodyType == RigidbodyType2D.Kinematic;
        }

        private bool _isKinematic;

        private void Start()
        {
            TryInitialize();
        }

        private void TryInitialize()
        {
            if (_isInitialized) return;

            if (ServiceLocator.TryGet<IInputService>(out var service))
            {
                _inputService = service;
                _isInitialized = true;
                Debug.Log("[PlayerController] Initialized - connected to IInputService");
            }
            else
            {
                Debug.LogWarning("[PlayerController] IInputService not available yet, will retry...");
            }
        }

        private void Update()
        {
            if (!_isInitialized)
            {
                TryInitialize();
                return;
            }

            Vector2 inputDir = _inputService.MoveInput;
            Vector2 targetVelocity = CalculateTargetVelocity(inputDir);
            ApplyMovement(targetVelocity);

            if (_logMovement && inputDir.sqrMagnitude > 0.01f)
            {
                Debug.Log($"[PlayerController] Input: {inputDir}, Velocity: {_currentVelocity}");
            }
        }

        private Vector2 CalculateTargetVelocity(Vector2 input)
        {
            if (input.sqrMagnitude < 0.01f)
            {
                return Vector2.zero;
            }

            Vector2 moveDir = _useIsometricMovement
                ? ConvertToIsometric(input)
                : input;

            return moveDir.normalized * _moveSpeed;
        }

        /// <summary>
        /// Converts screen-space input to isometric world-space direction.
        /// For a 2:1 isometric projection:
        /// - Right input moves right-down in world
        /// - Up input moves right-up in world
        /// </summary>
        private Vector2 ConvertToIsometric(Vector2 input)
        {
            // Convert cardinal input to isometric directions
            // This creates the classic isometric diamond movement
            float isoX = input.x - input.y;
            float isoY = (input.x + input.y) * _isometricYRatio;

            return new Vector2(isoX, isoY);
        }

        private void ApplyMovement(Vector2 targetVelocity)
        {
            float rate = targetVelocity.sqrMagnitude > 0.01f ? _acceleration : _deceleration;
            _currentVelocity = Vector2.MoveTowards(_currentVelocity, targetVelocity, rate * Time.deltaTime);
        }

        private void FixedUpdate()
        {
            if (_isKinematic)
            {
                // Kinematic bodies need MovePosition instead of velocity
                _rigidbody.MovePosition(_rigidbody.position + _currentVelocity * Time.fixedDeltaTime);
            }
            else
            {
                _rigidbody.linearVelocity = _currentVelocity;
            }
        }

        /// <summary>
        /// Stops all movement immediately.
        /// </summary>
        public void StopMovement()
        {
            _currentVelocity = Vector2.zero;
            _rigidbody.linearVelocity = Vector2.zero;
        }

        /// <summary>
        /// Temporarily disables player movement.
        /// </summary>
        public void DisableMovement()
        {
            enabled = false;
            StopMovement();
        }

        /// <summary>
        /// Re-enables player movement.
        /// </summary>
        public void EnableMovement()
        {
            enabled = true;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Draw movement direction in editor
            if (!Application.isPlaying) return;

            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, _currentVelocity * 0.5f);
        }
#endif
    }
}
