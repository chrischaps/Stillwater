using UnityEngine;

namespace Stillwater.Fishing.States
{
    /// <summary>
    /// The caught state when the fish has been successfully landed.
    /// Displays catch results and returns to Idle after a delay.
    /// The FishingStateMachine or controller should publish FishCaughtEvent on entering this state.
    /// </summary>
    public class CaughtState : IFishingState
    {
        private readonly float _displayDuration;

        private float _elapsedTime;
        private bool _displayComplete;
        private bool _eventReady;

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
        /// Whether the FishCaughtEvent should be fired.
        /// Set to true on Enter, should be cleared by the event handler.
        /// </summary>
        public bool EventReady => _eventReady;

        /// <summary>
        /// Creates a new CaughtState with default settings.
        /// </summary>
        public CaughtState() : this(2.0f)
        {
        }

        /// <summary>
        /// Creates a new CaughtState with custom display duration.
        /// </summary>
        /// <param name="displayDuration">Time in seconds to display catch before returning to Idle.</param>
        public CaughtState(float displayDuration)
        {
            _displayDuration = Mathf.Max(0.1f, displayDuration);
        }

        /// <summary>
        /// Called when entering the caught state.
        /// Resets timer and signals event is ready.
        /// </summary>
        public void Enter(IFishingContext context)
        {
            _elapsedTime = 0f;
            _displayComplete = false;
            _eventReady = true;
        }

        /// <summary>
        /// Called every frame while displaying catch.
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
        /// Called when exiting the caught state.
        /// </summary>
        public void Exit(IFishingContext context)
        {
            _eventReady = false;
        }

        /// <summary>
        /// Clears the event ready flag after the event has been handled.
        /// </summary>
        public void ClearEventReady()
        {
            _eventReady = false;
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
