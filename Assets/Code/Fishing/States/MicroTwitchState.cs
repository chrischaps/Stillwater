using UnityEngine;

namespace Stillwater.Fishing.States
{
    /// <summary>
    /// State where the player performs a small twitch movement to attract fish.
    /// Briefly disturbs the water before returning to Stillness.
    /// May increase or decrease bite probability based on timing and fish behavior.
    /// </summary>
    public class MicroTwitchState : IFishingState
    {
        private readonly float _twitchDuration;

        private float _elapsedTime;
        private bool _twitchComplete;

        /// <summary>
        /// Progress through the twitch animation (0-1).
        /// </summary>
        public float TwitchProgress => _twitchDuration > 0
            ? Mathf.Clamp01(_elapsedTime / _twitchDuration)
            : 1f;

        /// <summary>
        /// Whether the twitch animation is complete.
        /// </summary>
        public bool TwitchComplete => _twitchComplete;

        /// <summary>
        /// Creates a new MicroTwitchState with default settings.
        /// </summary>
        public MicroTwitchState() : this(0.2f)
        {
        }

        /// <summary>
        /// Creates a new MicroTwitchState with custom duration.
        /// </summary>
        /// <param name="twitchDuration">Duration of the twitch animation in seconds.</param>
        public MicroTwitchState(float twitchDuration)
        {
            _twitchDuration = Mathf.Max(0.05f, twitchDuration);
        }

        /// <summary>
        /// Called when entering the micro-twitch state.
        /// Resets the twitch timer.
        /// </summary>
        public void Enter(IFishingContext context)
        {
            _elapsedTime = 0f;
            _twitchComplete = false;
        }

        /// <summary>
        /// Called every frame while in micro-twitch state.
        /// Advances the twitch animation timer.
        /// </summary>
        public void Update(IFishingContext context, float deltaTime)
        {
            _elapsedTime += deltaTime;

            if (_elapsedTime >= _twitchDuration)
            {
                _twitchComplete = true;
            }
        }

        /// <summary>
        /// Called when exiting the micro-twitch state.
        /// </summary>
        public void Exit(IFishingContext context)
        {
            // Twitch animation complete
        }

        /// <summary>
        /// Returns Stillness state when twitch is complete, null otherwise.
        /// </summary>
        public FishingState? GetNextState(IFishingContext context)
        {
            if (_twitchComplete)
            {
                return FishingState.Stillness;
            }
            return null;
        }
    }
}
