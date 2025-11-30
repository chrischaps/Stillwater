using UnityEngine;
using Stillwater.Framework;

namespace Stillwater.Core
{
    public enum MovementMode
    {
        Free,           // No constraints
        DirectionLocked, // Locked to 8 directions but free positioning
        TrackLocked      // Locked to grid lines (rail-based movement)
    }

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
        [SerializeField] private MovementMode _movementMode = MovementMode.TrackLocked;

        [Tooltip("Grid cell size for track-locked movement")]
        [SerializeField] private float _gridSize = 1f;

        [Tooltip("How close to a track to snap to it")]
        [SerializeField] private float _trackSnapDistance = 0.1f;

        [Tooltip("Minimum input magnitude to register movement (prevents drift)")]
        [SerializeField] private float _inputDeadzone = 0.2f;

        [Header("Debug")]
        [SerializeField] private bool _logMovement;
        [SerializeField] private bool _drawTrackGizmos = true;

        private Rigidbody2D _rigidbody;
        private IInputService _inputService;
        private Vector2 _currentVelocity;
        private bool _isInitialized;

        // Current track direction for track-locked mode
        private Vector2 _currentTrackDirection;
        private bool _isOnTrack;

        // 8 grid-aligned directions for isometric movement (2:1 ratio)
        // These are the 4 cardinal directions + 4 isometric tile-edge directions
        private static readonly Vector2[] GridDirections = new Vector2[]
        {
            new Vector2(1, 0),                          // Right (index 0)
            new Vector2(2, 1).normalized,               // Iso North-East (index 1)
            new Vector2(0, 1),                          // Up (index 2)
            new Vector2(-2, 1).normalized,              // Iso North-West (index 3)
            new Vector2(-1, 0),                         // Left (index 4)
            new Vector2(-2, -1).normalized,             // Iso South-West (index 5)
            new Vector2(0, -1),                         // Down (index 6)
            new Vector2(2, -1).normalized,              // Iso South-East (index 7)
        };

        // Track families - pairs of opposite directions that form continuous lines
        // Index 0: Horizontal (Left/Right)
        // Index 1: Iso NE/SW diagonal
        // Index 2: Vertical (Up/Down)
        // Index 3: Iso NW/SE diagonal
        private static readonly int[][] TrackFamilies = new int[][]
        {
            new int[] { 0, 4 },  // Horizontal: Right, Left
            new int[] { 1, 5 },  // Iso diagonal: NE, SW
            new int[] { 2, 6 },  // Vertical: Up, Down
            new int[] { 3, 7 },  // Iso diagonal: NW, SE
        };

        public float MoveSpeed
        {
            get => _moveSpeed;
            set => _moveSpeed = value;
        }

        public bool IsMoving => _currentVelocity.sqrMagnitude > 0.01f;

        public Vector2 CurrentVelocity => _currentVelocity;

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
            if (input.sqrMagnitude < _inputDeadzone * _inputDeadzone)
            {
                return Vector2.zero;
            }

            Vector2 moveDir;

            switch (_movementMode)
            {
                case MovementMode.DirectionLocked:
                    moveDir = SnapToGridDirection(input);
                    break;

                case MovementMode.TrackLocked:
                    moveDir = CalculateTrackLockedDirection(input);
                    break;

                case MovementMode.Free:
                default:
                    moveDir = input.normalized;
                    break;
            }

            return moveDir * _moveSpeed;
        }

        /// <summary>
        /// Calculate movement direction constrained to grid tracks.
        /// Player can only move along grid lines and change direction at intersections.
        /// </summary>
        private Vector2 CalculateTrackLockedDirection(Vector2 input)
        {
            Vector2 pos = _rigidbody.position;
            Vector2 desiredDir = SnapToGridDirection(input);

            // Find which tracks we're currently on (or near)
            var availableTracks = GetAvailableTracksAtPosition(pos);

            if (availableTracks.Count == 0)
            {
                // Not on any track - snap to nearest track
                SnapToNearestTrack(ref pos, desiredDir);
                _rigidbody.position = pos;
                availableTracks = GetAvailableTracksAtPosition(pos);
            }

            // Check if desired direction is available
            int desiredFamily = GetTrackFamilyForDirection(desiredDir);
            if (availableTracks.Contains(desiredFamily))
            {
                _currentTrackDirection = desiredDir;
                _isOnTrack = true;
                return desiredDir;
            }

            // If we're at an intersection and want to change direction, allow it
            if (availableTracks.Count > 1)
            {
                // Find the track family closest to desired direction
                float bestDot = -2f;
                int bestFamily = -1;
                Vector2 bestDir = Vector2.zero;

                foreach (int family in availableTracks)
                {
                    foreach (int dirIndex in TrackFamilies[family])
                    {
                        float dot = Vector2.Dot(input.normalized, GridDirections[dirIndex]);
                        if (dot > bestDot)
                        {
                            bestDot = dot;
                            bestFamily = family;
                            bestDir = GridDirections[dirIndex];
                        }
                    }
                }

                if (bestFamily >= 0 && bestDot > 0.3f)
                {
                    _currentTrackDirection = bestDir;
                    return bestDir;
                }
            }

            // Continue on current track if we have one
            if (_isOnTrack && _currentTrackDirection != Vector2.zero)
            {
                // Check if input is roughly aligned with current track (forward or backward)
                float dot = Vector2.Dot(input.normalized, _currentTrackDirection);
                if (Mathf.Abs(dot) > 0.3f)
                {
                    return dot > 0 ? _currentTrackDirection : -_currentTrackDirection;
                }
            }

            // Default: use first available track in input direction
            if (availableTracks.Count > 0)
            {
                int family = availableTracks[0];
                foreach (int dirIndex in TrackFamilies[family])
                {
                    if (Vector2.Dot(input.normalized, GridDirections[dirIndex]) > 0)
                    {
                        _currentTrackDirection = GridDirections[dirIndex];
                        return GridDirections[dirIndex];
                    }
                }
            }

            return Vector2.zero;
        }

        /// <summary>
        /// Get list of track families (0-3) that pass through or near the given position.
        /// </summary>
        private System.Collections.Generic.List<int> GetAvailableTracksAtPosition(Vector2 pos)
        {
            var tracks = new System.Collections.Generic.List<int>();

            // Check horizontal track (y = n * gridSize)
            float yRemainder = Mathf.Abs(pos.y % _gridSize);
            if (yRemainder < _trackSnapDistance || yRemainder > _gridSize - _trackSnapDistance)
            {
                tracks.Add(0); // Horizontal family
            }

            // Check vertical track (x = n * gridSize)
            float xRemainder = Mathf.Abs(pos.x % _gridSize);
            if (xRemainder < _trackSnapDistance || xRemainder > _gridSize - _trackSnapDistance)
            {
                tracks.Add(2); // Vertical family
            }

            // Check iso NE/SW diagonal (y = 0.5x + c, so y - 0.5x = c)
            // Track exists where (y - 0.5x) is a multiple of gridSize
            float isoNE = pos.y - 0.5f * pos.x;
            float neRemainder = Mathf.Abs(isoNE % _gridSize);
            if (neRemainder < _trackSnapDistance || neRemainder > _gridSize - _trackSnapDistance)
            {
                tracks.Add(1); // Iso NE/SW family
            }

            // Check iso NW/SE diagonal (y = -0.5x + c, so y + 0.5x = c)
            float isoNW = pos.y + 0.5f * pos.x;
            float nwRemainder = Mathf.Abs(isoNW % _gridSize);
            if (nwRemainder < _trackSnapDistance || nwRemainder > _gridSize - _trackSnapDistance)
            {
                tracks.Add(3); // Iso NW/SE family
            }

            return tracks;
        }

        /// <summary>
        /// Get which track family (0-3) a direction belongs to.
        /// </summary>
        private int GetTrackFamilyForDirection(Vector2 dir)
        {
            float bestDot = -2f;
            int bestFamily = 0;

            for (int family = 0; family < TrackFamilies.Length; family++)
            {
                foreach (int dirIndex in TrackFamilies[family])
                {
                    float dot = Vector2.Dot(dir, GridDirections[dirIndex]);
                    if (dot > bestDot)
                    {
                        bestDot = dot;
                        bestFamily = family;
                    }
                }
            }

            return bestFamily;
        }

        /// <summary>
        /// Snap position to the nearest track in the desired direction family.
        /// </summary>
        private void SnapToNearestTrack(ref Vector2 pos, Vector2 desiredDir)
        {
            int family = GetTrackFamilyForDirection(desiredDir);

            switch (family)
            {
                case 0: // Horizontal - snap y to grid
                    pos.y = Mathf.Round(pos.y / _gridSize) * _gridSize;
                    break;
                case 2: // Vertical - snap x to grid
                    pos.x = Mathf.Round(pos.x / _gridSize) * _gridSize;
                    break;
                case 1: // Iso NE/SW - snap (y - 0.5x) to grid
                    float isoNE = pos.y - 0.5f * pos.x;
                    float snappedNE = Mathf.Round(isoNE / _gridSize) * _gridSize;
                    pos.y = snappedNE + 0.5f * pos.x;
                    break;
                case 3: // Iso NW/SE - snap (y + 0.5x) to grid
                    float isoNW = pos.y + 0.5f * pos.x;
                    float snappedNW = Mathf.Round(isoNW / _gridSize) * _gridSize;
                    pos.y = snappedNW - 0.5f * pos.x;
                    break;
            }
        }

        /// <summary>
        /// Snaps input direction to the nearest of 8 grid-aligned directions.
        /// These align with isometric tile edges and cardinal screen directions.
        /// </summary>
        private Vector2 SnapToGridDirection(Vector2 input)
        {
            Vector2 normalizedInput = input.normalized;
            Vector2 bestDirection = GridDirections[0];
            float bestDot = -2f;

            for (int i = 0; i < GridDirections.Length; i++)
            {
                float dot = Vector2.Dot(normalizedInput, GridDirections[i]);
                if (dot > bestDot)
                {
                    bestDot = dot;
                    bestDirection = GridDirections[i];
                }
            }

            return bestDirection;
        }

        private void ApplyMovement(Vector2 targetVelocity)
        {
            float rate = targetVelocity.sqrMagnitude > 0.01f ? _acceleration : _deceleration;
            _currentVelocity = Vector2.MoveTowards(_currentVelocity, targetVelocity, rate * Time.deltaTime);

            // In track-locked mode, continuously correct position to stay on track
            if (_movementMode == MovementMode.TrackLocked && _isOnTrack && _currentVelocity.sqrMagnitude > 0.01f)
            {
                CorrectPositionToTrack();
            }
        }

        /// <summary>
        /// Snaps player to the nearest grid intersection point.
        /// </summary>
        private void SnapToNearestIntersection()
        {
            Vector2 pos = _rigidbody.position;

            // Snap to nearest intersection of horizontal/vertical grid
            pos.x = Mathf.Round(pos.x / _gridSize) * _gridSize;
            pos.y = Mathf.Round(pos.y / _gridSize) * _gridSize;

            _rigidbody.position = pos;
            _isOnTrack = false;
            _currentTrackDirection = Vector2.zero;

            if (_logMovement)
            {
                Debug.Log($"[PlayerController] Snapped to intersection: {pos}");
            }
        }

        /// <summary>
        /// Corrects the player's position to stay exactly on the current track.
        /// </summary>
        private void CorrectPositionToTrack()
        {
            if (_currentTrackDirection == Vector2.zero) return;

            Vector2 pos = _rigidbody.position;
            int family = GetTrackFamilyForDirection(_currentTrackDirection);

            switch (family)
            {
                case 0: // Horizontal - lock y to nearest grid line
                    pos.y = Mathf.Round(pos.y / _gridSize) * _gridSize;
                    break;
                case 2: // Vertical - lock x to nearest grid line
                    pos.x = Mathf.Round(pos.x / _gridSize) * _gridSize;
                    break;
                case 1: // Iso NE/SW - lock to diagonal line
                    float isoNE = pos.y - 0.5f * pos.x;
                    float snappedNE = Mathf.Round(isoNE / _gridSize) * _gridSize;
                    pos.y = snappedNE + 0.5f * pos.x;
                    break;
                case 3: // Iso NW/SE - lock to diagonal line
                    float isoNW = pos.y + 0.5f * pos.x;
                    float snappedNW = Mathf.Round(isoNW / _gridSize) * _gridSize;
                    pos.y = snappedNW - 0.5f * pos.x;
                    break;
            }

            _rigidbody.position = pos;
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
            if (Application.isPlaying)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawRay(transform.position, _currentVelocity * 0.5f);
            }

            // Draw track grid for track-locked mode
            if (_drawTrackGizmos && _movementMode == MovementMode.TrackLocked)
            {
                DrawTrackGrid();
            }
        }

        private void OnDrawGizmos()
        {
            // Always draw track grid when selected in track-locked mode
            if (_drawTrackGizmos && _movementMode == MovementMode.TrackLocked)
            {
                DrawTrackGrid();
            }
        }

        private void DrawTrackGrid()
        {
            Vector3 center = transform.position;
            float extent = 10f;

            // Horizontal tracks (red)
            Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.5f);
            for (float y = Mathf.Floor((center.y - extent) / _gridSize) * _gridSize; y <= center.y + extent; y += _gridSize)
            {
                Gizmos.DrawLine(
                    new Vector3(center.x - extent, y, 0),
                    new Vector3(center.x + extent, y, 0));
            }

            // Vertical tracks (blue)
            Gizmos.color = new Color(0.3f, 0.3f, 1f, 0.5f);
            for (float x = Mathf.Floor((center.x - extent) / _gridSize) * _gridSize; x <= center.x + extent; x += _gridSize)
            {
                Gizmos.DrawLine(
                    new Vector3(x, center.y - extent, 0),
                    new Vector3(x, center.y + extent, 0));
            }

            // Iso NE/SW diagonals (yellow) - slope 0.5
            Gizmos.color = new Color(1f, 1f, 0.3f, 0.5f);
            float isoBase = center.y - 0.5f * center.x;
            for (float c = Mathf.Floor((isoBase - extent) / _gridSize) * _gridSize; c <= isoBase + extent * 2; c += _gridSize)
            {
                // y = 0.5x + c
                float x1 = center.x - extent;
                float y1 = 0.5f * x1 + c;
                float x2 = center.x + extent;
                float y2 = 0.5f * x2 + c;
                Gizmos.DrawLine(new Vector3(x1, y1, 0), new Vector3(x2, y2, 0));
            }

            // Iso NW/SE diagonals (cyan) - slope -0.5
            Gizmos.color = new Color(0.3f, 1f, 1f, 0.5f);
            float isoBase2 = center.y + 0.5f * center.x;
            for (float c = Mathf.Floor((isoBase2 - extent) / _gridSize) * _gridSize; c <= isoBase2 + extent * 2; c += _gridSize)
            {
                // y = -0.5x + c
                float x1 = center.x - extent;
                float y1 = -0.5f * x1 + c;
                float x2 = center.x + extent;
                float y2 = -0.5f * x2 + c;
                Gizmos.DrawLine(new Vector3(x1, y1, 0), new Vector3(x2, y2, 0));
            }

            // Highlight current position's available tracks
            if (Application.isPlaying && _rigidbody != null)
            {
                var tracks = GetAvailableTracksAtPosition(_rigidbody.position);
                Gizmos.color = Color.white;
                UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f,
                    $"Tracks: {string.Join(", ", tracks)} Dir: {_currentTrackDirection}");
                Gizmos.DrawLine(transform.position + Vector3.up * 0.2f,transform.position + Vector3.up * -0.2f);
                Gizmos.DrawLine(transform.position + Vector3.right * 0.2f,transform.position + Vector3.right * -0.2f);
            }
        }
#endif
    }
}
