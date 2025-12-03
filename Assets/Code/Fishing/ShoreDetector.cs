using System;
using UnityEngine;
using UnityEngine.Tilemaps;
using Stillwater.Core;

namespace Stillwater.Fishing
{
    /// <summary>
    /// Detects when the player is on a shore tile and facing water, enabling fishing interactions.
    /// Attach to the Player GameObject.
    /// </summary>
    public class ShoreDetector : MonoBehaviour, IShoreDetector
    {
        [Header("Tilemap References")]
        [Tooltip("The water tilemap to check for water tiles")]
        [SerializeField] private Tilemap _waterTilemap;

        [Tooltip("Optional: Ground tilemap. If not set, any non-water position counts as potential shore.")]
        [SerializeField] private Tilemap _groundTilemap;

        [Header("Detection Settings")]
        [Tooltip("How often to update detection (0 = every frame)")]
        [SerializeField] private float _updateInterval = 0f;

        [Header("Debug")]
        [SerializeField] private bool _showDebugGizmos = true;
        [SerializeField] private bool _logStateChanges = false;

        // Cached state
        private bool _canFish;
        private FacingDirection _currentFacing = FacingDirection.South;
        private Vector2 _fishingDirection;
        private Vector2 _targetWaterPosition;
        private FishingSpotData _activeSpotData;
        private Vector3Int _lastPlayerCell;
        private float _lastUpdateTime;

        // Dependencies
        private Rigidbody2D _rigidbody;

        /// <inheritdoc/>
        public bool CanFish => _canFish;

        /// <inheritdoc/>
        public Vector2 FishingDirection => _fishingDirection;

        /// <inheritdoc/>
        public Vector2 TargetWaterPosition => _targetWaterPosition;

        /// <inheritdoc/>
        public FishingSpotData ActiveSpotData => _activeSpotData;

        /// <inheritdoc/>
        public event Action<bool> OnCanFishChanged;

        /// <summary>
        /// Gets or sets the current facing direction.
        /// This should be updated by the PlayerController when movement direction changes.
        /// </summary>
        public FacingDirection CurrentFacing
        {
            get => _currentFacing;
            set
            {
                if (_currentFacing != value)
                {
                    _currentFacing = value;
                    UpdateDetection(true); // Force update when facing changes
                }
            }
        }

        /// <summary>
        /// Sets the water tilemap reference. Useful for testing or runtime configuration.
        /// </summary>
        public void SetWaterTilemap(Tilemap tilemap)
        {
            _waterTilemap = tilemap;
            UpdateDetection(true);
        }

        /// <summary>
        /// Sets the ground tilemap reference. Useful for testing or runtime configuration.
        /// </summary>
        public void SetGroundTilemap(Tilemap tilemap)
        {
            _groundTilemap = tilemap;
            UpdateDetection(true);
        }

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            if (_rigidbody == null)
            {
                Debug.LogWarning($"[ShoreDetector] No Rigidbody2D found on {gameObject.name}. Using Transform position instead.");
            }
        }

        private void Start()
        {
            if (_waterTilemap == null)
            {
                Debug.LogWarning("[ShoreDetector] Water tilemap not assigned! Shore detection will not work.");
            }

            UpdateDetection(true);
        }

        private void Update()
        {
            if (_updateInterval > 0 && Time.time - _lastUpdateTime < _updateInterval)
            {
                return;
            }

            UpdateDetection(false);
            _lastUpdateTime = Time.time;
        }

        /// <summary>
        /// Forces an immediate detection update.
        /// </summary>
        public void ForceUpdate()
        {
            UpdateDetection(true);
        }

        private void UpdateDetection(bool forceUpdate)
        {
            if (_waterTilemap == null)
            {
                SetCanFish(false);
                return;
            }

            Vector2 playerPos = _rigidbody != null ? _rigidbody.position : (Vector2)transform.position;
            Vector3Int playerCell = _waterTilemap.WorldToCell(playerPos);

            // Skip if player hasn't moved cells and we're not forcing update
            if (!forceUpdate && playerCell == _lastPlayerCell)
            {
                return;
            }

            _lastPlayerCell = playerCell;

            // Check if player is on a shore tile (not in water, but adjacent to water)
            bool isOnShore = IsShoreCell(playerCell);

            if (!isOnShore)
            {
                SetCanFish(false);
                return;
            }

            // Check if facing direction points to water
            Vector3Int facingCell = playerCell + _currentFacing.ToCellOffset();
            bool facingWater = IsWaterTile(facingCell);

            if (facingWater)
            {
                _fishingDirection = _currentFacing.ToVector2();
                _targetWaterPosition = _waterTilemap.GetCellCenterWorld(facingCell);

                // Check for special fishing spot markers (TODO: implement FishingSpotMarker detection)
                _activeSpotData = null;

                SetCanFish(true);
            }
            else
            {
                SetCanFish(false);
            }
        }

        /// <summary>
        /// Checks if the given cell is a shore cell (not water, but adjacent to water).
        /// </summary>
        private bool IsShoreCell(Vector3Int cell)
        {
            // If player is standing in water, not a shore
            if (IsWaterTile(cell))
            {
                return false;
            }

            // If ground tilemap is set, check that player is on ground
            if (_groundTilemap != null && _groundTilemap.GetTile(cell) == null)
            {
                return false;
            }

            // Check if any adjacent cell is water
            foreach (var direction in FacingDirectionExtensions.AllDirections)
            {
                Vector3Int neighborCell = cell + direction.ToCellOffset();
                if (IsWaterTile(neighborCell))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if the given cell contains a water tile.
        /// </summary>
        private bool IsWaterTile(Vector3Int cell)
        {
            return _waterTilemap != null && _waterTilemap.GetTile(cell) != null;
        }

        private void SetCanFish(bool value)
        {
            if (_canFish != value)
            {
                _canFish = value;

                if (_logStateChanges)
                {
                    Debug.Log($"[ShoreDetector] CanFish changed to: {value}");
                }

                if (!value)
                {
                    _fishingDirection = Vector2.zero;
                    _targetWaterPosition = Vector2.zero;
                    _activeSpotData = null;
                }

                OnCanFishChanged?.Invoke(value);
            }
        }

        /// <summary>
        /// Gets all valid fishing directions from the current position.
        /// Returns directions where the player could fish (facing water from current shore tile).
        /// </summary>
        public FacingDirection[] GetValidFishingDirections()
        {
            if (_waterTilemap == null)
            {
                return Array.Empty<FacingDirection>();
            }

            Vector2 playerPos = _rigidbody != null ? _rigidbody.position : (Vector2)transform.position;
            Vector3Int playerCell = _waterTilemap.WorldToCell(playerPos);

            if (!IsShoreCell(playerCell))
            {
                return Array.Empty<FacingDirection>();
            }

            var validDirections = new System.Collections.Generic.List<FacingDirection>();

            foreach (var direction in FacingDirectionExtensions.AllDirections)
            {
                Vector3Int neighborCell = playerCell + direction.ToCellOffset();
                if (IsWaterTile(neighborCell))
                {
                    validDirections.Add(direction);
                }
            }

            return validDirections.ToArray();
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!_showDebugGizmos) return;

            Vector3 playerPos = _rigidbody != null ? (Vector3)_rigidbody.position : transform.position;

            // Draw current facing direction
            Gizmos.color = _canFish ? Color.green : Color.yellow;
            Vector3 facingVector = (Vector3)_currentFacing.ToVector2();
            Gizmos.DrawRay(playerPos, facingVector * 1.5f);

            if (_canFish)
            {
                // Draw target water position
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(_targetWaterPosition, 0.3f);

                // Draw line to target
                Gizmos.DrawLine(playerPos, _targetWaterPosition);
            }

            // Draw shore status
            if (_waterTilemap != null)
            {
                Vector3Int playerCell = _waterTilemap.WorldToCell(playerPos);
                bool isOnShore = IsShoreCell(playerCell);

                Gizmos.color = isOnShore ? new Color(0.5f, 1f, 0.5f, 0.3f) : new Color(1f, 0.5f, 0.5f, 0.3f);
                Vector3 cellCenter = _waterTilemap.GetCellCenterWorld(playerCell);
                Gizmos.DrawCube(cellCenter, _waterTilemap.cellSize * 0.8f);

                // Draw valid fishing directions
                var validDirs = GetValidFishingDirections();
                Gizmos.color = new Color(0f, 0.8f, 1f, 0.5f);
                foreach (var dir in validDirs)
                {
                    Vector3Int neighborCell = playerCell + dir.ToCellOffset();
                    Vector3 neighborCenter = _waterTilemap.GetCellCenterWorld(neighborCell);
                    Gizmos.DrawCube(neighborCenter, _waterTilemap.cellSize * 0.6f);
                }
            }
        }
#endif
    }
}
