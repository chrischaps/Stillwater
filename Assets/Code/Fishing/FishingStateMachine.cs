using System;
using System.Collections.Generic;
using Stillwater.Core;

namespace Stillwater.Fishing
{
    /// <summary>
    /// Core state machine that manages fishing state transitions.
    /// Holds the current state, processes updates, and publishes state change events.
    /// </summary>
    public class FishingStateMachine
    {
        private readonly Dictionary<FishingState, IFishingState> _states = new();
        private readonly IFishingContext _context;

        private IFishingState _currentStateImpl;
        private FishingState _currentState;
        private bool _isInitialized;

        /// <summary>
        /// The current state of the fishing state machine.
        /// </summary>
        public FishingState CurrentState => _currentState;

        /// <summary>
        /// Whether the state machine has been initialized with a starting state.
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// Creates a new FishingStateMachine with the given context.
        /// </summary>
        /// <param name="context">The fishing context providing shared data to states.</param>
        /// <exception cref="ArgumentNullException">Thrown if context is null.</exception>
        public FishingStateMachine(IFishingContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Registers a state implementation for a given state type.
        /// </summary>
        /// <param name="stateType">The state enum value to register.</param>
        /// <param name="stateImpl">The state implementation.</param>
        /// <exception cref="ArgumentNullException">Thrown if stateImpl is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if state is already registered.</exception>
        public void RegisterState(FishingState stateType, IFishingState stateImpl)
        {
            if (stateImpl == null)
                throw new ArgumentNullException(nameof(stateImpl));

            if (_states.ContainsKey(stateType))
                throw new InvalidOperationException($"State {stateType} is already registered.");

            _states[stateType] = stateImpl;
        }

        /// <summary>
        /// Checks if a state implementation is registered for the given state type.
        /// </summary>
        /// <param name="stateType">The state to check.</param>
        /// <returns>True if the state is registered, false otherwise.</returns>
        public bool HasState(FishingState stateType)
        {
            return _states.ContainsKey(stateType);
        }

        /// <summary>
        /// Gets the state implementation for a given state type.
        /// </summary>
        /// <param name="stateType">The state to retrieve.</param>
        /// <returns>The state implementation, or null if not registered.</returns>
        public IFishingState GetState(FishingState stateType)
        {
            return _states.TryGetValue(stateType, out var state) ? state : null;
        }

        /// <summary>
        /// Initializes the state machine with a starting state.
        /// Must be called before Update().
        /// </summary>
        /// <param name="initialState">The state to start in.</param>
        /// <exception cref="InvalidOperationException">Thrown if state is not registered or already initialized.</exception>
        public void Initialize(FishingState initialState)
        {
            if (_isInitialized)
                throw new InvalidOperationException("State machine is already initialized.");

            if (!_states.TryGetValue(initialState, out var stateImpl))
                throw new InvalidOperationException($"State {initialState} is not registered.");

            _currentState = initialState;
            _currentStateImpl = stateImpl;
            _isInitialized = true;

            _currentStateImpl.Enter(_context);

            EventBus.Publish(new FishingStateChangedEvent
            {
                PreviousState = null,
                NewState = _currentState.ToString()
            });
        }

        /// <summary>
        /// Updates the current state and handles transitions.
        /// Call this every frame from Update() or FixedUpdate().
        /// </summary>
        /// <param name="deltaTime">Time elapsed since last update.</param>
        /// <exception cref="InvalidOperationException">Thrown if not initialized.</exception>
        public void Update(float deltaTime)
        {
            if (!_isInitialized)
                throw new InvalidOperationException("State machine must be initialized before calling Update().");

            if (_currentStateImpl == null)
                return;

            // Update the current state
            _currentStateImpl.Update(_context, deltaTime);

            // Check for state transition
            var nextState = _currentStateImpl.GetNextState(_context);
            if (nextState.HasValue && nextState.Value != _currentState)
            {
                TransitionTo(nextState.Value);
            }
        }

        /// <summary>
        /// Forces a transition to a new state.
        /// Useful for external triggers like player input or game events.
        /// </summary>
        /// <param name="newState">The state to transition to.</param>
        /// <exception cref="InvalidOperationException">Thrown if state is not registered.</exception>
        public void TransitionTo(FishingState newState)
        {
            if (!_states.TryGetValue(newState, out var newStateImpl))
                throw new InvalidOperationException($"State {newState} is not registered.");

            var previousState = _currentState;

            // Exit current state
            _currentStateImpl?.Exit(_context);

            // Enter new state
            _currentState = newState;
            _currentStateImpl = newStateImpl;

            if (_isInitialized)
            {
                _currentStateImpl.Enter(_context);
            }

            // Publish state change event
            EventBus.Publish(new FishingStateChangedEvent
            {
                PreviousState = previousState.ToString(),
                NewState = newState.ToString()
            });
        }

        /// <summary>
        /// Resets the state machine to uninitialized state.
        /// Useful for testing or restarting the fishing session.
        /// </summary>
        public void Reset()
        {
            if (_isInitialized && _currentStateImpl != null)
            {
                _currentStateImpl.Exit(_context);
            }

            _currentStateImpl = null;
            _isInitialized = false;
        }

        /// <summary>
        /// Gets the number of registered states. Useful for testing.
        /// </summary>
        public int RegisteredStateCount => _states.Count;
    }
}
