using UnityEngine;

namespace Stillwater.Fishing.States
{
    /// <summary>
    /// The hook opportunity state where the player has a brief window to set the hook.
    /// Correct timing leads to Hooked state, missed or late input leads to Lost state.
    /// </summary>
    public class HookOpportunityState : IFishingState
    {
        private readonly float _windowDuration;
        private readonly float _earlyInputPenaltyWindow;

        private float _elapsedTime;
        private bool _hookInputReceived;
        private bool _windowExpired;
        private bool _earlyInputPenalty;

        /// <summary>
        /// Whether the hook input was received during the window.
        /// </summary>
        public bool HookInputReceived => _hookInputReceived;

        /// <summary>
        /// Whether the hook window has expired.
        /// </summary>
        public bool WindowExpired => _windowExpired;

        /// <summary>
        /// Whether the player input too early (before the window opened).
        /// </summary>
        public bool EarlyInputPenalty => _earlyInputPenalty;

        /// <summary>
        /// Progress through the hook window (0-1).
        /// </summary>
        public float WindowProgress => _windowDuration > 0
            ? Mathf.Clamp01(_elapsedTime / _windowDuration)
            : 1f;

        /// <summary>
        /// Elapsed time in this state.
        /// </summary>
        public float ElapsedTime => _elapsedTime;

        /// <summary>
        /// Creates a new HookOpportunityState with default settings.
        /// </summary>
        public HookOpportunityState() : this(0.8f, 0.1f)
        {
        }

        /// <summary>
        /// Creates a new HookOpportunityState with custom settings.
        /// </summary>
        /// <param name="windowDuration">Duration of the hook window in seconds.</param>
        /// <param name="earlyInputPenaltyWindow">Time at start where input is considered "too early".</param>
        public HookOpportunityState(float windowDuration, float earlyInputPenaltyWindow = 0.1f)
        {
            _windowDuration = Mathf.Max(0.1f, windowDuration);
            _earlyInputPenaltyWindow = Mathf.Clamp(earlyInputPenaltyWindow, 0f, _windowDuration * 0.5f);
        }

        /// <summary>
        /// Called when entering the hook opportunity state.
        /// Resets window timer and input flags.
        /// </summary>
        public void Enter(IFishingContext context)
        {
            _elapsedTime = 0f;
            _hookInputReceived = false;
            _windowExpired = false;
            _earlyInputPenalty = false;
        }

        /// <summary>
        /// Called every frame while in hook opportunity.
        /// Monitors for hook input and window expiration.
        /// </summary>
        public void Update(IFishingContext context, float deltaTime)
        {
            // Don't process if already resolved
            if (_hookInputReceived || _windowExpired)
            {
                return;
            }

            _elapsedTime += deltaTime;

            // Check for hook input (using cast input as hook action)
            if (context.CastInputPressed)
            {
                _hookInputReceived = true;

                // Check if input was too early
                if (_elapsedTime < _earlyInputPenaltyWindow)
                {
                    _earlyInputPenalty = true;
                }
            }
            // Check for window expiration
            else if (_elapsedTime >= _windowDuration)
            {
                _windowExpired = true;
            }
        }

        /// <summary>
        /// Called when exiting the hook opportunity state.
        /// </summary>
        public void Exit(IFishingContext context)
        {
            // Hook opportunity resolved
        }

        /// <summary>
        /// Returns the next state based on hook attempt:
        /// - Hooked if input received in valid window (not too early)
        /// - Lost if window expired or input was too early
        /// - null if still waiting for input
        /// </summary>
        public FishingState? GetNextState(IFishingContext context)
        {
            // Window expired - fish got away
            if (_windowExpired)
            {
                return FishingState.Lost;
            }

            // Hook input received
            if (_hookInputReceived)
            {
                // Early input is penalized - fish escapes
                if (_earlyInputPenalty)
                {
                    return FishingState.Lost;
                }

                // Successful hook!
                return FishingState.Hooked;
            }

            // Still waiting
            return null;
        }
    }
}
