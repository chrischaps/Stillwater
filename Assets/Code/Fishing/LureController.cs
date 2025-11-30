using UnityEngine;
using Stillwater.Core;

namespace Stillwater.Fishing
{
    /// <summary>
    /// Controls the lure GameObject during fishing.
    /// Manages spawning, despawning, position, and drift physics.
    /// Works with FishingController to provide lure data for the state machine.
    /// </summary>
    public class LureController : MonoBehaviour
    {
        [Header("Prefab")]
        [SerializeField] private GameObject _lurePrefab;

        [Header("Drift Physics")]
        [SerializeField] private float _driftDrag = 2f;
        [SerializeField] private float _minVelocity = 0.01f;

        [Header("Cast Settings")]
        [SerializeField] private float _initialDriftSpeed = 3f;

        [Header("Debug")]
        [SerializeField] private bool _debugLogging = true;

        // Active lure instance
        private GameObject _activeLure;
        private SpriteRenderer _lureRenderer;

        // Physics state
        private Vector2 _position;
        private Vector2 _velocity;
        private bool _isActive;

        // Reference to the fishing controller for coordination
        private FishingController _fishingController;

        #region Public Properties

        /// <summary>
        /// Whether a lure is currently active in the world.
        /// </summary>
        public bool IsActive => _isActive;

        /// <summary>
        /// Current world position of the lure.
        /// </summary>
        public Vector2 Position => _position;

        /// <summary>
        /// Current velocity of the lure (for drift detection).
        /// </summary>
        public Vector2 Velocity => _velocity;

        /// <summary>
        /// The active lure GameObject (null if no lure is spawned).
        /// </summary>
        public GameObject ActiveLure => _activeLure;

        /// <summary>
        /// Drag coefficient for drift slowdown.
        /// </summary>
        public float DriftDrag
        {
            get => _driftDrag;
            set => _driftDrag = Mathf.Max(0f, value);
        }

        #endregion

        // Track subscription state to prevent double-subscription
        private bool _isSubscribedToEvents;

        #region Unity Lifecycle

        private void Awake()
        {
            // Try to find FishingController on same or parent object
            _fishingController = GetComponent<FishingController>();
            if (_fishingController == null)
            {
                _fishingController = GetComponentInParent<FishingController>();
            }
        }

        private void OnEnable()
        {
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        /// <summary>
        /// Subscribes to required events. Called automatically by OnEnable().
        /// Can be called manually for testing purposes when OnEnable() isn't triggered.
        /// Safe to call multiple times.
        /// </summary>
        public void SubscribeToEvents()
        {
            if (_isSubscribedToEvents)
            {
                return;
            }

            EventBus.Subscribe<FishingStateChangedEvent>(OnFishingStateChanged);
            _isSubscribedToEvents = true;
        }

        /// <summary>
        /// Unsubscribes from events. Called automatically by OnDisable().
        /// </summary>
        public void UnsubscribeFromEvents()
        {
            if (!_isSubscribedToEvents)
            {
                return;
            }

            EventBus.Unsubscribe<FishingStateChangedEvent>(OnFishingStateChanged);
            _isSubscribedToEvents = false;
        }

        private void Update()
        {
            if (!_isActive)
            {
                return;
            }

            UpdateDrift(Time.deltaTime);
            UpdateLureTransform();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Spawns the lure at the specified position with initial velocity.
        /// </summary>
        /// <param name="position">World position to spawn the lure.</param>
        /// <param name="castDirection">Direction the line was cast (normalized).</param>
        public void SpawnLure(Vector2 position, Vector2 castDirection)
        {
            if (_isActive)
            {
                DespawnLure();
            }

            _position = position;
            // Initial velocity in cast direction
            _velocity = castDirection.normalized * _initialDriftSpeed;
            _isActive = true;

            // Instantiate lure prefab
            if (_lurePrefab != null)
            {
                _activeLure = Instantiate(_lurePrefab, new Vector3(_position.x, _position.y, 0f), Quaternion.identity);
                _activeLure.transform.SetParent(transform);
                _lureRenderer = _activeLure.GetComponent<SpriteRenderer>();
            }
            else
            {
                // Create a simple placeholder if no prefab assigned
                _activeLure = CreatePlaceholderLure();
            }

            if (_debugLogging)
            {
                Debug.Log($"[LureController] Spawned lure at {_position} with velocity {_velocity}");
            }
        }

        /// <summary>
        /// Spawns the lure at the specified position with no initial velocity.
        /// </summary>
        /// <param name="position">World position to spawn the lure.</param>
        public void SpawnLure(Vector2 position)
        {
            SpawnLure(position, Vector2.zero);
        }

        /// <summary>
        /// Despawns the active lure.
        /// </summary>
        public void DespawnLure()
        {
            if (!_isActive)
            {
                return;
            }

            if (_activeLure != null)
            {
                // Use DestroyImmediate in edit mode (for tests), Destroy in play mode
                if (Application.isPlaying)
                {
                    Destroy(_activeLure);
                }
                else
                {
                    DestroyImmediate(_activeLure);
                }
                _activeLure = null;
                _lureRenderer = null;
            }

            _velocity = Vector2.zero;
            _isActive = false;

            if (_debugLogging)
            {
                Debug.Log("[LureController] Despawned lure");
            }
        }

        /// <summary>
        /// Sets the lure's velocity directly (for external physics influence).
        /// </summary>
        /// <param name="velocity">New velocity vector.</param>
        public void SetVelocity(Vector2 velocity)
        {
            _velocity = velocity;
        }

        /// <summary>
        /// Adds velocity to the lure (for impulses like fish tugging).
        /// </summary>
        /// <param name="impulse">Velocity to add.</param>
        public void AddImpulse(Vector2 impulse)
        {
            _velocity += impulse;
        }

        /// <summary>
        /// Sets the lure position directly (teleport).
        /// </summary>
        /// <param name="position">New world position.</param>
        public void SetPosition(Vector2 position)
        {
            _position = position;
            UpdateLureTransform();
        }

        /// <summary>
        /// Sets the FishingController reference for coordination.
        /// </summary>
        /// <param name="controller">The FishingController to coordinate with.</param>
        public void SetFishingController(FishingController controller)
        {
            _fishingController = controller;
        }

        #endregion

        #region Private Methods

        private void UpdateDrift(float deltaTime)
        {
            if (_velocity.sqrMagnitude < _minVelocity * _minVelocity)
            {
                _velocity = Vector2.zero;
                return;
            }

            // Apply drag to slow drift
            float dragFactor = 1f - (_driftDrag * deltaTime);
            dragFactor = Mathf.Max(0f, dragFactor);
            _velocity *= dragFactor;

            // Update position
            _position += _velocity * deltaTime;
        }

        private void UpdateLureTransform()
        {
            if (_activeLure != null)
            {
                _activeLure.transform.position = new Vector3(_position.x, _position.y, 0f);
            }
        }

        private GameObject CreatePlaceholderLure()
        {
            var lure = new GameObject("Lure");
            lure.transform.SetParent(transform);
            lure.transform.position = new Vector3(_position.x, _position.y, 0f);

            // Add a simple sprite renderer with a placeholder
            var spriteRenderer = lure.AddComponent<SpriteRenderer>();
            spriteRenderer.color = Color.red;

            // Create a simple circle texture for placeholder
            var texture = new Texture2D(32, 32);
            var pixels = new Color[32 * 32];
            Vector2 center = new Vector2(16, 16);
            float radius = 12f;

            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    pixels[y * 32 + x] = dist <= radius ? Color.white : Color.clear;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            texture.filterMode = FilterMode.Point;

            spriteRenderer.sprite = Sprite.Create(
                texture,
                new Rect(0, 0, 32, 32),
                new Vector2(0.5f, 0.5f),
                32f
            );

            _lureRenderer = spriteRenderer;

            if (_debugLogging)
            {
                Debug.Log("[LureController] Created placeholder lure (no prefab assigned)");
            }

            return lure;
        }

        private void OnFishingStateChanged(FishingStateChangedEvent evt)
        {
            // Handle state transitions that affect the lure
            switch (evt.NewState)
            {
                case "LureDrift":
                    // Lure should be spawned when entering LureDrift
                    // FishingController handles the actual spawn call with correct position
                    break;

                case "Caught":
                case "Lost":
                case "Idle":
                    // Despawn lure when fish is caught, lost, or returning to idle
                    if (_isActive && evt.PreviousState != "Idle")
                    {
                        DespawnLure();
                    }
                    break;
            }
        }

        #endregion

        #region Editor Helpers

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying || !_isActive)
            {
                return;
            }

            // Draw lure position
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(new Vector3(_position.x, _position.y, 0f), 0.2f);

            // Draw velocity vector
            if (_velocity.sqrMagnitude > 0.001f)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawRay(
                    new Vector3(_position.x, _position.y, 0f),
                    new Vector3(_velocity.x, _velocity.y, 0f)
                );
            }
        }
#endif

        #endregion
    }
}
