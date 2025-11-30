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
        // Note: LureController reference will be added when that component is implemented
        // [SerializeField] private LureController _lureController;

        [Header("Zone Settings")]
        [SerializeField] private string _currentZoneId = "starting_lake";
        [SerializeField] private float _biteProbabilityModifier = 1f;

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

        // Fish data (stub values until fish system exists)
        private bool _hasFishInterest;
        private bool _hasHookedFish;
        private string _hookedFishId;
        private float _fishStruggleIntensity;

        // Random number generator
        private System.Random _random;

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

            // Subscribe to input events
            EventBus.Subscribe<CastInputEvent>(OnCastInput);
            EventBus.Subscribe<SlackInputEvent>(OnSlackInput);
            EventBus.Subscribe<CancelInputEvent>(OnCancelInput);

            // Subscribe to state change events for debug logging
            EventBus.Subscribe<FishingStateChangedEvent>(OnFishingStateChanged);
        }

        private void OnDisable()
        {
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
            if (_lureTransform != null)
            {
                Vector2 newPosition = _lureTransform.position;
                _lureVelocity = (newPosition - _lurePosition) / Time.deltaTime;
                _lurePosition = newPosition;

                // Calculate line length from player to lure
                _lineLength = Vector2.Distance(transform.position, _lurePosition);
            }
            else
            {
                // Default to player position if no lure transform
                _lurePosition = transform.position;
                _lureVelocity = Vector2.zero;
                _lineLength = 0f;
            }

            // LineTension would come from LureController/ReelingState
            // For now, keep it at current value (modified by states)
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
