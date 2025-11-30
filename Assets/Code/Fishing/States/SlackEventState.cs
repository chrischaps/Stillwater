using UnityEngine;

namespace Stillwater.Fishing.States
{
    /// <summary>
    /// State where the player must release tension to prevent line break.
    /// Player must stop reeling temporarily during fish struggle surges.
    /// Note: This is typically handled within ReelingState, but this class
    /// provides a standalone implementation for future flexibility.
    /// </summary>
    public class SlackEventState : IFishingState
    {
        private readonly float _maxSlackDuration;
        private readonly float _requiredReleaseDuration;

        private float _elapsedTime;
        private float _releaseTime;
        private bool _slackCleared;
        private bool _lineSnapped;

        /// <summary>
        /// Progress through the slack event (0-1).
        /// </summary>
        public float SlackProgress => _maxSlackDuration > 0
            ? Mathf.Clamp01(_elapsedTime / _maxSlackDuration)
            : 1f;

        /// <summary>
        /// Whether the slack event was successfully cleared.
        /// </summary>
        public bool SlackCleared => _slackCleared;

        /// <summary>
        /// Whether the line snapped during the slack event.
        /// </summary>
        public bool LineSnapped => _lineSnapped;

        /// <summary>
        /// Creates a new SlackEventState with default settings.
        /// </summary>
        public SlackEventState() : this(1.5f, 0.3f)
        {
        }

        /// <summary>
        /// Creates a new SlackEventState with custom settings.
        /// </summary>
        /// <param name="maxSlackDuration">Maximum time before line snaps if still reeling.</param>
        /// <param name="requiredReleaseDuration">Time player must release reel to clear event.</param>
        public SlackEventState(float maxSlackDuration, float requiredReleaseDuration)
        {
            _maxSlackDuration = Mathf.Max(0.5f, maxSlackDuration);
            _requiredReleaseDuration = Mathf.Max(0.1f, requiredReleaseDuration);
        }

        /// <summary>
        /// Called when entering the slack event state.
        /// </summary>
        public void Enter(IFishingContext context)
        {
            _elapsedTime = 0f;
            _releaseTime = 0f;
            _slackCleared = false;
            _lineSnapped = false;
        }

        /// <summary>
        /// Called every frame while in slack event state.
        /// Player must release reel input to clear the event.
        /// </summary>
        public void Update(IFishingContext context, float deltaTime)
        {
            if (_slackCleared || _lineSnapped)
            {
                return;
            }

            _elapsedTime += deltaTime;

            // Check if player is releasing (not reeling)
            if (!context.ReelInputHeld)
            {
                _releaseTime += deltaTime;

                if (_releaseTime >= _requiredReleaseDuration)
                {
                    _slackCleared = true;
                }
            }
            else
            {
                // Reset release time if player starts reeling again
                _releaseTime = 0f;

                // Check for line snap if still reeling past max duration
                if (_elapsedTime >= _maxSlackDuration)
                {
                    _lineSnapped = true;
                }
            }
        }

        /// <summary>
        /// Called when exiting the slack event state.
        /// </summary>
        public void Exit(IFishingContext context)
        {
            // Slack event resolved
        }

        /// <summary>
        /// Returns the next state based on slack event outcome:
        /// - Reeling if slack was successfully cleared
        /// - Lost if line snapped
        /// - null if still in progress
        /// </summary>
        public FishingState? GetNextState(IFishingContext context)
        {
            if (_slackCleared)
            {
                return FishingState.Reeling;
            }

            if (_lineSnapped)
            {
                return FishingState.Lost;
            }

            return null;
        }
    }
}
