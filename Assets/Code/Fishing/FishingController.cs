using UnityEngine;
using Stillwater.Core;
using Stillwater.Fishing.States;
using Stillwater.Framework;

namespace Stillwater.Fishing
{
    /// <summary>
    /// Main controller for the fishing system.
    /// Implements IFishingContext to provide shared state to fishing states,
    /// owns the FishingStateMachine, and bridges input to the state machine.
    /// Attach this to the Player GameObject to enable fishing.
    /// </summary>
    public class FishingController : MonoBehaviour, IFishingContext
    {
        [Header("Debug")]
        [SerializeField] private bool _debugLogging = true;

        [Header("References")]
        [SerializeField] private Transform _lureTransform;
        [SerializeField] private LureController _lureController;

        [Header("Zone Settings")]
        [SerializeField] private string _currentZoneId = "starting_lake";
        [SerializeField] private float _biteProbabilityModifier = 1f;

        [Header("Fish Selection")]
        [SerializeField] private FishDefinition[] _availableFish;

        // State machine
        private FishingStateMachine _stateMachine;
        private float _timeInState;

        // Input state (tracked per-frame)
        private IInputService _inputService;
        private bool _castInputPressed;
        private bool _slackInputPressed;
        private bool _cancelInputPressed;

        // Lure data (stub values until LureController exists)
        private Vector2 _lurePosition;
        private Vector2 _lureVelocity;
        private float _lineLength;
        private float _lineTension;

        // Fish data
        private bool _hasFishInterest;
        private bool _hasHookedFish;
        private string _hookedFishId;
        private float _fishStruggleIntensity;
        private FishDefinition _selectedFish;

        // Random number generator
        private System.Random _random;

        // Event subscription tracking
        private bool _isSubscribed;

        #region IFishingContext Implementation

        public FishingState CurrentState => _stateMachine?.CurrentState ?? FishingState.Idle;
        public float TimeInState => _timeInState;

        public Vector2 LurePosition => _lurePosition;
        public Vector2 LureVelocity => _lureVelocity;
        public float LineLength => _lineLength;
        public float LineTension => _lineTension;

        public bool CastInputPressed => _castInputPressed;
        public bool ReelInputHeld => _inputService?.IsReeling ?? false;
        public bool SlackInputPressed => _slackInputPressed;
        public bool CancelInputPressed => _cancelInputPressed;

        public bool HasFishInterest => _hasFishInterest;
        public bool HasHookedFish => _hasHookedFish;
        public string HookedFishId => _hookedFishId;
        public float FishStruggleIntensity => _fishStruggleIntensity;
        public FishDefinition SelectedFish => _selectedFish;
        public FishDefinition[] AvailableFish => _availableFish;

        public string CurrentZoneId => _currentZoneId;
        public float BiteProbabilityModifier => _biteProbabilityModifier;

        public float GetRandomValue()
        {
            return (float)_random.NextDouble();
        }

        public float GetRandomRange(float min, float max)
        {
            return min + (float)_random.NextDouble() * (max - min);
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// The underlying state machine. Useful for testing and external transitions.
        /// </summary>
        public FishingStateMachine StateMachine => _stateMachine;

        /// <summary>
        /// Whether debug logging is enabled.
        /// </summary>
        public bool DebugLogging
        {
            get => _debugLogging;
            set => _debugLogging = value;
        }

        /// <summary>
        /// The LureController managing the active lure.
        /// </summary>
        public LureController LureController
        {
            get => _lureController;
            set => _lureController = value;
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            Initialize();
        }

        /// <summary>
        /// Initializes the controller. Called automatically by Awake().
        /// Can be called manually for testing purposes.
        /// Safe to call multiple times - subsequent calls are ignored.
        /// </summary>
        public void Initialize()
        {
            // Prevent re-initialization
            if (_random != null && _stateMachine != null)
            {
                return;
            }

            _random = new System.Random();
            
            // Subscribe to events (ensures tests work even if OnEnable isn't called)
            SubscribeToEvents();
            InitializeStateMachine();
        }

        private void OnEnable()
        {
            // Get input service
            if (!ServiceLocator.TryGet(out _inputService))
            {
                if (_debugLogging)
                {
                    Debug.LogWarning("[FishingController] IInputService not found. Input will not work.");
                }
            }

            SubscribeToEvents();
        }

        /// <summary>
        /// Subscribes to event bus events. Safe to call multiple times.
        /// Called from both Initialize() and OnEnable() to ensure tests work.
        /// </summary>
        private void SubscribeToEvents()
        {
            if (_isSubscribed) return;
            _isSubscribed = true;

            // Subscribe to input events
            EventBus.Subscribe<CastInputEvent>(OnCastInput);
            EventBus.Subscribe<SlackInputEvent>(OnSlackInput);
            EventBus.Subscribe<CancelInputEvent>(OnCancelInput);

            // Subscribe to state change events
            EventBus.Subscribe<FishingStateChangedEvent>(OnFishingStateChanged);
        }

        private void OnDisable()
        {
            // Reset subscription tracking so we can re-subscribe on enable
            _isSubscribed = false;

            // Unsubscribe from events
            EventBus.Unsubscribe<CastInputEvent>(OnCastInput);
            EventBus.Unsubscribe<SlackInputEvent>(OnSlackInput);
            EventBus.Unsubscribe<CancelInputEvent>(OnCancelInput);
            EventBus.Unsubscribe<FishingStateChangedEvent>(OnFishingStateChanged);
        }

        private void Update()
        {
            if (_stateMachine == null || !_stateMachine.IsInitialized)
            {
                return;
            }

            // Update lure data from transform if available
            UpdateLureData();

            // Update state machine
            float deltaTime = Time.deltaTime;
            _stateMachine.Update(deltaTime);
            _timeInState += deltaTime;

            // Clear per-frame input flags at end of frame
            ClearPerFrameInputFlags();
        }

        #endregion

        #region State Machine Setup

        private void InitializeStateMachine()
        {
            _stateMachine = new FishingStateMachine(this);

            // Register all states
            RegisterAllStates();

            // Initialize to Idle state
            _stateMachine.Initialize(FishingState.Idle);

            if (_debugLogging)
            {
                Debug.Log($"[FishingController] State machine initialized with {_stateMachine.RegisteredStateCount} states");
            }
        }

        private void RegisterAllStates()
        {
            // Core states
            _stateMachine.RegisterState(FishingState.Idle, new IdleState());
            _stateMachine.RegisterState(FishingState.Casting, new CastingState());
            _stateMachine.RegisterState(FishingState.LureDrift, new LureDriftState());
            _stateMachine.RegisterState(FishingState.Stillness, new StillnessState());
            _stateMachine.RegisterState(FishingState.MicroTwitch, new MicroTwitchState());

            // Bite and hook states
            _stateMachine.RegisterState(FishingState.BiteCheck, new BiteCheckState());
            _stateMachine.RegisterState(FishingState.HookOpportunity, new HookOpportunityState());
            _stateMachine.RegisterState(FishingState.Hooked, new HookedState());

            // Reeling states
            _stateMachine.RegisterState(FishingState.Reeling, new ReelingState());
            _stateMachine.RegisterState(FishingState.SlackEvent, new SlackEventState());

            // Result states
            _stateMachine.RegisterState(FishingState.Caught, new CaughtState());
            _stateMachine.RegisterState(FishingState.Lost, new LostState());
        }

        #endregion

        #region Input Handling

        private void OnCastInput(CastInputEvent evt)
        {
            _castInputPressed = true;
        }

        private void OnSlackInput(SlackInputEvent evt)
        {
            _slackInputPressed = true;
        }

        private void OnCancelInput(CancelInputEvent evt)
        {
            _cancelInputPressed = true;
        }

        private void ClearPerFrameInputFlags()
        {
            _castInputPressed = false;
            _slackInputPressed = false;
            _cancelInputPressed = false;
        }

        #endregion

        #region Lure Data

        private void UpdateLureData()
        {
            // Prefer LureController if available
            if (_lureController != null && _lureController.IsActive)
            {
                _lurePosition = _lureController.Position;
                _lureVelocity = _lureController.Velocity;
                _lineLength = Vector2.Distance(transform.position, _lurePosition);

                // Update lure transform reference for backward compatibility
                if (_lureController.ActiveLure != null)
                {
                    _lureTransform = _lureController.ActiveLure.transform;
                }
            }
            else if (_lureTransform != null)
            {
                // Fallback to direct transform reference
                Vector2 newPosition = _lureTransform.position;
                if (Time.deltaTime > 0)
                {
                    _lureVelocity = (newPosition - _lurePosition) / Time.deltaTime;
                }
                _lurePosition = newPosition;
                _lineLength = Vector2.Distance(transform.position, _lurePosition);
            }
            else
            {
                // Default to player position if no lure
                _lurePosition = transform.position;
                _lureVelocity = Vector2.zero;
                _lineLength = 0f;
            }
        }

        /// <summary>
        /// Sets the line tension. Called by states during reeling.
        /// </summary>
        /// <param name="tension">Tension value (0-1).</param>
        public void SetLineTension(float tension)
        {
            _lineTension = Mathf.Clamp01(tension);
        }

        /// <summary>
        /// Sets the lure transform reference.
        /// </summary>
        /// <param name="lureTransform">The lure's transform.</param>
        public void SetLureTransform(Transform lureTransform)
        {
            _lureTransform = lureTransform;
        }

        #endregion

        #region Fish Data

        /// <summary>
        /// Sets whether a fish is currently showing interest in the lure.
        /// </summary>
        /// <param name="hasInterest">True if fish is interested.</param>
        public void SetFishInterest(bool hasInterest)
        {
            _hasFishInterest = hasInterest;
        }

        /// <summary>
        /// Sets the hooked fish data.
        /// </summary>
        /// <param name="fishId">ID of the hooked fish, or null if none.</param>
        /// <param name="struggleIntensity">Initial struggle intensity (0-1).</param>
        public void SetHookedFish(string fishId, float struggleIntensity = 0.5f)
        {
            if (string.IsNullOrEmpty(fishId))
            {
                _hasHookedFish = false;
                _hookedFishId = null;
                _fishStruggleIntensity = 0f;
            }
            else
            {
                _hasHookedFish = true;
                _hookedFishId = fishId;
                _fishStruggleIntensity = Mathf.Clamp01(struggleIntensity);
            }
        }

        /// <summary>
        /// Updates the fish struggle intensity during reeling.
        /// </summary>
        /// <param name="intensity">New struggle intensity (0-1).</param>
        public void SetFishStruggleIntensity(float intensity)
        {
            _fishStruggleIntensity = Mathf.Clamp01(intensity);
        }

        /// <summary>
        /// Clears the hooked fish (after catch or loss).
        /// </summary>
        public void ClearHookedFish()
        {
            SetHookedFish(null);
            _selectedFish = null;
        }

        /// <summary>
        /// Selects a random fish from available definitions weighted by rarity.
        /// Higher rarity values mean more likely to be selected.
        /// </summary>
        /// <returns>The selected fish definition, or null if none available.</returns>
        public FishDefinition SelectRandomFish()
        {
            if (_availableFish == null || _availableFish.Length == 0)
            {
                _selectedFish = null;
                return null;
            }

            // Calculate total weight (rarity values)
            float totalWeight = 0f;
            foreach (var fish in _availableFish)
            {
                if (fish != null)
                {
                    totalWeight += fish.RarityBase;
                }
            }

            if (totalWeight <= 0f)
            {
                // Fallback to first valid fish if no weights
                foreach (var fish in _availableFish)
                {
                    if (fish != null)
                    {
                        _selectedFish = fish;
                        return _selectedFish;
                    }
                }
                return null;
            }

            // Roll for fish selection
            float roll = GetRandomValue() * totalWeight;
            float cumulative = 0f;

            foreach (var fish in _availableFish)
            {
                if (fish != null)
                {
                    cumulative += fish.RarityBase;
                    if (roll <= cumulative)
                    {
                        _selectedFish = fish;

                        if (_debugLogging)
                        {
                            Debug.Log($"[FishingController] Selected fish: {fish.DisplayName} (rarity: {fish.RarityBase})");
                        }

                        return _selectedFish;
                    }
                }
            }

            // Fallback to last valid fish
            for (int i = _availableFish.Length - 1; i >= 0; i--)
            {
                if (_availableFish[i] != null)
                {
                    _selectedFish = _availableFish[i];
                    return _selectedFish;
                }
            }

            return null;
        }

        /// <summary>
        /// Sets the available fish definitions for selection.
        /// </summary>
        /// <param name="fish">Array of fish definitions.</param>
        public void SetAvailableFish(FishDefinition[] fish)
        {
            _availableFish = fish;
        }

        /// <summary>
        /// Sets the selected fish directly (for testing or external selection).
        /// </summary>
        /// <param name="fish">The fish to select.</param>
        public void SetSelectedFish(FishDefinition fish)
        {
            _selectedFish = fish;
        }

        #endregion

        #region Zone Settings

        /// <summary>
        /// Sets the current fishing zone.
        /// </summary>
        /// <param name="zoneId">The zone identifier.</param>
        /// <param name="biteProbabilityModifier">Bite probability modifier for this zone.</param>
        public void SetZone(string zoneId, float biteProbabilityModifier = 1f)
        {
            _currentZoneId = zoneId;
            _biteProbabilityModifier = Mathf.Max(0f, biteProbabilityModifier);
        }

        #endregion

        #region Event Handlers

        private void OnFishingStateChanged(FishingStateChangedEvent evt)
        {
            // Reset time in state on state change
            _timeInState = 0f;

            if (_debugLogging)
            {
                string previous = string.IsNullOrEmpty(evt.PreviousState) ? "None" : evt.PreviousState;
                Debug.Log($"[FishingController] State: {previous} -> {evt.NewState}");
            }

            // Handle lure spawn/despawn based on state transitions
            HandleLureStateTransition(evt.PreviousState, evt.NewState);

            // Handle fish selection on bite
            if (evt.NewState == "HookOpportunity")
            {
                SelectRandomFish();
            }

            // Publish FishCaughtEvent when entering Caught state
            if (evt.NewState == "Caught")
            {
                PublishFishCaughtEvent();
            }

            // Clear fish data when returning to Idle
            if (evt.NewState == "Idle" && evt.PreviousState != "Idle")
            {
                ClearHookedFish();
            }
        }

        private void PublishFishCaughtEvent()
        {
            var fishEvent = new FishCaughtEvent
            {
                FishId = _selectedFish?.Id ?? _hookedFishId ?? "unknown",
                ZoneId = _currentZoneId,
                Size = GetRandomRange(0.5f, 1.5f), // Random size for now
                IsRare = _selectedFish != null && _selectedFish.RarityBase < 0.3f,
                FishDefinition = _selectedFish
            };

            EventBus.Publish(fishEvent);

            if (_debugLogging)
            {
                string fishName = _selectedFish?.DisplayName ?? fishEvent.FishId;
                Debug.Log($"[FishingController] Fish caught: {fishName} (size: {fishEvent.Size:F2}, rare: {fishEvent.IsRare})");
            }
        }

        private void HandleLureStateTransition(string previousState, string newState)
        {
            if (_lureController == null)
            {
                return;
            }

            // Spawn lure when entering LureDrift from Casting
            if (newState == "LureDrift" && previousState == "Casting")
            {
                SpawnLureAtCastPosition();
            }
        }

        private void SpawnLureAtCastPosition()
        {
            if (_lureController == null || _stateMachine == null)
            {
                return;
            }

            // Get the CastingState to retrieve the calculated landing position
            var castingState = _stateMachine.GetState(FishingState.Casting) as CastingState;
            if (castingState != null)
            {
                Vector2 landingPosition = castingState.LandingPosition;
                Vector2 playerPosition = transform.position;
                Vector2 castDirection = (landingPosition - playerPosition).normalized;

                _lureController.SpawnLure(landingPosition, castDirection);

                if (_debugLogging)
                {
                    Debug.Log($"[FishingController] Spawned lure at {landingPosition}");
                }
            }
            else
            {
                // Fallback: spawn at a default position in front of player
                Vector2 fallbackPosition = (Vector2)transform.position + Vector2.up * 3f;
                _lureController.SpawnLure(fallbackPosition);

                if (_debugLogging)
                {
                    Debug.LogWarning("[FishingController] CastingState not found, using fallback lure position");
                }
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Forces a transition to a specific state. Use with caution.
        /// </summary>
        /// <param name="state">The state to transition to.</param>
        public void ForceTransition(FishingState state)
        {
            if (_stateMachine != null && _stateMachine.IsInitialized)
            {
                _stateMachine.TransitionTo(state);
            }
        }

        /// <summary>
        /// Resets the fishing controller to Idle state.
        /// </summary>
        public void ResetToIdle()
        {
            ClearHookedFish();
            _lineTension = 0f;
            _hasFishInterest = false;

            if (_stateMachine != null && _stateMachine.IsInitialized)
            {
                _stateMachine.TransitionTo(FishingState.Idle);
            }
        }

        #endregion
    }
}
