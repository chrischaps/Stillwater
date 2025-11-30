using UnityEngine;

namespace Stillwater.Fishing.States
{
    /// <summary>
    /// The reason why the fish was lost.
    /// </summary>
    public enum LostReason
    {
        /// <summary>Default/unknown reason.</summary>
        Unknown,
        /// <summary>Player missed the hook opportunity window.</summary>
        MissedHook,
        /// <summary>Player input too early during hook opportunity.</summary>
        EarlyHook,
        /// <summary>Line snapped due to excess tension.</summary>
        LineSnapped,
        /// <summary>Fish escaped due to low tension.</summary>
        FishEscaped,
        /// <summary>Slack event caused line to snap.</summary>
        SlackEventFailure
    }

    /// <summary>
    /// The lost state when the fish has escaped or line has broken.
    /// Displays loss feedback and returns to Idle after a delay.
    /// The FishingStateMachine or controller should publish FishLostEvent on entering this state.
    /// </summary>
    public class LostState : IFishingState
    {
        private readonly float _displayDuration;

        private float _elapsedTime;
        private bool _displayComplete;
        private bool _eventReady;
        private LostReason _lostReason;

        /// <summary>
        /// Time elapsed in this state.
        /// </summary>
        public float ElapsedTime => _elapsedTime;

        /// <summary>
        /// Progress through the display duration (0-1).
        /// </summary>
        public float DisplayProgress => _displayDuration > 0
            ? Mathf.Clamp01(_elapsedTime / _displayDuration)
            : 1f;

        /// <summary>
        /// Whether the display is complete and ready to transition.
        /// </summary>
        public bool DisplayComplete => _displayComplete;

        /// <summary>
        /// Whether the FishLostEvent should be fired.
        /// Set to true on Enter, should be cleared by the event handler.
        /// </summary>
        public bool EventReady => _eventReady;

        /// <summary>
        /// The reason the fish was lost.
        /// Can be set before entering or during transition.
        /// </summary>
        public LostReason Reason
        {
            get => _lostReason;
            set => _lostReason = value;
        }

        /// <summary>
        /// Creates a new LostState with default settings.
        /// </summary>
        public LostState() : this(1.5f)
        {
        }

        /// <summary>
        /// Creates a new LostState with custom display duration.
        /// </summary>
        /// <param name="displayDuration">Time in seconds to display loss before returning to Idle.</param>
        public LostState(float displayDuration)
        {
            _displayDuration = Mathf.Max(0.1f, displayDuration);
        }

        /// <summary>
        /// Called when entering the lost state.
        /// Resets timer and signals event is ready.
        /// </summary>
        public void Enter(IFishingContext context)
        {
            _elapsedTime = 0f;
            _displayComplete = false;
            _eventReady = true;
            // Note: Reason should be set by the state machine before entering
            // based on what caused the loss (line snap, escape, missed hook, etc.)
        }

        /// <summary>
        /// Called every frame while displaying loss.
        /// Advances timer toward display completion.
        /// </summary>
        public void Update(IFishingContext context, float deltaTime)
        {
            if (_displayComplete)
            {
                return;
            }

            _elapsedTime += deltaTime;

            if (_elapsedTime >= _displayDuration)
            {
                _displayComplete = true;
            }
        }

        /// <summary>
        /// Called when exiting the lost state.
        /// </summary>
        public void Exit(IFishingContext context)
        {
            _eventReady = false;
            _lostReason = LostReason.Unknown;
        }

        /// <summary>
        /// Clears the event ready flag after the event has been handled.
        /// </summary>
        public void ClearEventReady()
        {
            _eventReady = false;
        }

        /// <summary>
        /// Sets the reason for losing the fish.
        /// Should be called before entering this state.
        /// </summary>
        public void SetReason(LostReason reason)
        {
            _lostReason = reason;
        }

        /// <summary>
        /// Returns Idle when display is complete, null otherwise.
        /// </summary>
        public FishingState? GetNextState(IFishingContext context)
        {
            if (_displayComplete)
            {
                return FishingState.Idle;
            }

            return null;
        }
    }
}
