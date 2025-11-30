using UnityEngine;

namespace Stillwater.Core
{
    /// <summary>
    /// Smoothly follows a target (typically the player) with configurable damping and offset.
    /// Designed for 2D isometric games with optional bounds clamping.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform _target;
        [Tooltip("Automatically find player by tag on start")]
        [SerializeField] private bool _findPlayerOnStart = true;
        [SerializeField] private string _playerTag = "Player";

        [Header("Follow Settings")]
        [Tooltip("Offset from target position (useful for isometric framing)")]
        [SerializeField] private Vector3 _offset = new Vector3(0f, 0f, -10f);

        [Tooltip("How quickly the camera catches up to the target (lower = smoother)")]
        [SerializeField, Range(0.01f, 1f)] private float _smoothSpeed = 0.125f;

        [Tooltip("Use unscaled time (camera moves during pause)")]
        [SerializeField] private bool _useUnscaledTime = false;

        [Header("Look Ahead")]
        [Tooltip("Camera looks ahead in the direction of movement")]
        [SerializeField] private bool _useLookAhead = false;
        [SerializeField] private float _lookAheadDistance = 1f;
        [SerializeField] private float _lookAheadSmoothing = 0.5f;

        [Header("Bounds (Optional)")]
        [SerializeField] private bool _useBounds = false;
        [SerializeField] private Vector2 _boundsMin = new Vector2(-10f, -10f);
        [SerializeField] private Vector2 _boundsMax = new Vector2(10f, 10f);

        [Header("Dead Zone (Optional)")]
        [Tooltip("Target must move this far before camera follows")]
        [SerializeField] private bool _useDeadZone = false;
        [SerializeField] private Vector2 _deadZoneSize = new Vector2(1f, 1f);

        private Vector3 _currentVelocity;
        private Vector3 _lookAheadOffset;
        private Vector3 _targetLookAhead;
        private PlayerController _playerController;

        public Transform Target
        {
            get => _target;
            set
            {
                _target = value;
                CachePlayerController();
            }
        }

        public Vector3 Offset
        {
            get => _offset;
            set => _offset = value;
        }

        private void Start()
        {
            if (_findPlayerOnStart && _target == null)
            {
                FindPlayer();
            }

            if (_target != null)
            {
                CachePlayerController();
                // Snap to target on start
                SnapToTarget();
            }
        }

        private void FindPlayer()
        {
            GameObject player = GameObject.FindGameObjectWithTag(_playerTag);
            if (player != null)
            {
                _target = player.transform;
            }
            else
            {
                Debug.LogWarning($"CameraFollow: No GameObject found with tag '{_playerTag}'");
            }
        }

        private void CachePlayerController()
        {
            if (_target != null)
            {
                _playerController = _target.GetComponent<PlayerController>();
            }
        }

        private void LateUpdate()
        {
            if (_target == null) return;

            Vector3 targetPosition = CalculateTargetPosition();

            // Apply dead zone
            if (_useDeadZone)
            {
                targetPosition = ApplyDeadZone(targetPosition);
            }

            // Apply bounds
            if (_useBounds)
            {
                targetPosition = ClampToBounds(targetPosition);
            }

            // Smooth follow
            float deltaTime = _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            Vector3 smoothedPosition = Vector3.SmoothDamp(
                transform.position,
                targetPosition,
                ref _currentVelocity,
                _smoothSpeed,
                Mathf.Infinity,
                deltaTime
            );

            transform.position = smoothedPosition;
        }

        private Vector3 CalculateTargetPosition()
        {
            Vector3 basePosition = _target.position + _offset;

            // Add look ahead based on player velocity
            if (_useLookAhead && _playerController != null)
            {
                Vector2 velocity = _playerController.CurrentVelocity;
                if (velocity.sqrMagnitude > 0.01f)
                {
                    _targetLookAhead = new Vector3(velocity.x, velocity.y, 0f).normalized * _lookAheadDistance;
                }
                else
                {
                    _targetLookAhead = Vector3.zero;
                }

                _lookAheadOffset = Vector3.Lerp(
                    _lookAheadOffset,
                    _targetLookAhead,
                    Time.deltaTime / _lookAheadSmoothing
                );

                basePosition += _lookAheadOffset;
            }

            return basePosition;
        }

        private Vector3 ApplyDeadZone(Vector3 targetPosition)
        {
            Vector3 currentPos = transform.position;
            Vector3 delta = targetPosition - currentPos;

            // Only move if outside dead zone
            if (Mathf.Abs(delta.x) < _deadZoneSize.x * 0.5f)
            {
                targetPosition.x = currentPos.x;
            }
            if (Mathf.Abs(delta.y) < _deadZoneSize.y * 0.5f)
            {
                targetPosition.y = currentPos.y;
            }

            return targetPosition;
        }

        private Vector3 ClampToBounds(Vector3 position)
        {
            position.x = Mathf.Clamp(position.x, _boundsMin.x, _boundsMax.x);
            position.y = Mathf.Clamp(position.y, _boundsMin.y, _boundsMax.y);
            return position;
        }

        /// <summary>
        /// Instantly move camera to target position (no smoothing).
        /// </summary>
        public void SnapToTarget()
        {
            if (_target == null) return;

            Vector3 targetPosition = _target.position + _offset;

            if (_useBounds)
            {
                targetPosition = ClampToBounds(targetPosition);
            }

            transform.position = targetPosition;
            _currentVelocity = Vector3.zero;
            _lookAheadOffset = Vector3.zero;
        }

        /// <summary>
        /// Set new bounds for camera movement.
        /// </summary>
        public void SetBounds(Vector2 min, Vector2 max)
        {
            _boundsMin = min;
            _boundsMax = max;
            _useBounds = true;
        }

        /// <summary>
        /// Disable bounds clamping.
        /// </summary>
        public void ClearBounds()
        {
            _useBounds = false;
        }

        /// <summary>
        /// Temporarily override the target (useful for cutscenes).
        /// </summary>
        public void SetTemporaryTarget(Transform newTarget)
        {
            _target = newTarget;
            _playerController = null;
        }

        /// <summary>
        /// Return to following the player.
        /// </summary>
        public void ReturnToPlayer()
        {
            FindPlayer();
            CachePlayerController();
        }

        private void OnDrawGizmosSelected()
        {
            // Draw bounds
            if (_useBounds)
            {
                Gizmos.color = Color.yellow;
                Vector3 center = new Vector3(
                    (_boundsMin.x + _boundsMax.x) * 0.5f,
                    (_boundsMin.y + _boundsMax.y) * 0.5f,
                    0f
                );
                Vector3 size = new Vector3(
                    _boundsMax.x - _boundsMin.x,
                    _boundsMax.y - _boundsMin.y,
                    0.1f
                );
                Gizmos.DrawWireCube(center, size);
            }

            // Draw dead zone
            if (_useDeadZone)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireCube(transform.position, new Vector3(_deadZoneSize.x, _deadZoneSize.y, 0.1f));
            }

            // Draw target connection
            if (_target != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, _target.position);
                Gizmos.DrawWireSphere(_target.position + _offset, 0.2f);
            }
        }
    }
}
