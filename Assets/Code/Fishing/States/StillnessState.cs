using UnityEngine;

namespace Stillwater.Fishing.States
{
    /// <summary>
    /// The stillness state where the lure is stationary on the water.
    /// Accumulates stillness time which increases bite probability.
    /// Can transition to MicroTwitch on small input, or to BiteCheck after threshold.
    /// </summary>
    public class StillnessState : IFishingState
    {
        private readonly float _stillnessThreshold;
        private readonly float _microTwitchInputThreshold;

        private float _stillnessTime;
        private bool _microTwitchRequested;
        private bool _thresholdReached;

        /// <summary>
        /// Accumulated stillness time in seconds.
        /// </summary>
        public float StillnessTime => _stillnessTime;

        /// <summary>
        /// Progress toward bite check (0-1).
        /// </summary>
        public float StillnessProgress => _stillnessThreshold > 0
            ? Mathf.Clamp01(_stillnessTime / _stillnessThreshold)
            : 1f;

        /// <summary>
        /// Whether the stillness threshold has been reached.
        /// </summary>
        public bool ThresholdReached => _thresholdReached;

        /// <summary>
        /// Creates a new StillnessState with default settings.
        /// </summary>
        public StillnessState() : this(3f)
        {
        }

        /// <summary>
        /// Creates a new StillnessState with custom stillness threshold.
        /// </summary>
        /// <param name="stillnessThreshold">Time in seconds before transitioning to BiteCheck.</param>
        /// <param name="microTwitchInputThreshold">Not currently used - reserved for analog input sensitivity.</param>
        public StillnessState(float stillnessThreshold, float microTwitchInputThreshold = 0.1f)
        {
            _stillnessThreshold = Mathf.Max(0.1f, stillnessThreshold);
            _microTwitchInputThreshold = Mathf.Clamp01(microTwitchInputThreshold);
        }

        /// <summary>
        /// Called when entering the stillness state.
        /// Resets stillness timer.
        /// </summary>
        public void Enter(IFishingContext context)
        {
            _stillnessTime = 0f;
            _microTwitchRequested = false;
            _thresholdReached = false;
        }

        /// <summary>
        /// Called every frame while in stillness.
        /// Accumulates stillness time and checks for micro-twitch input.
        /// </summary>
        public void Update(IFishingContext context, float deltaTime)
        {
            _stillnessTime += deltaTime;

            // Check for micro-twitch input (using cast input as a stand-in for small movement)
            // In a full implementation, this might use analog stick magnitude
            if (context.CastInputPressed)
            {
                _microTwitchRequested = true;
            }

            // Check if stillness threshold reached
            if (_stillnessTime >= _stillnessThreshold)
            {
                _thresholdReached = true;
            }
        }

        /// <summary>
        /// Called when exiting the stillness state.
        /// </summary>
        public void Exit(IFishingContext context)
        {
            // Stillness period complete
        }

        /// <summary>
        /// Returns the next state based on conditions:
        /// - MicroTwitch if small input detected
        /// - BiteCheck if stillness threshold reached
        /// - null otherwise
        /// </summary>
        public FishingState? GetNextState(IFishingContext context)
        {
            // Micro-twitch takes priority (player intentionally moved)
            if (_microTwitchRequested)
            {
                return FishingState.MicroTwitch;
            }

            // Threshold reached, time for bite check
            if (_thresholdReached)
            {
                return FishingState.BiteCheck;
            }

            return null;
        }
    }
}
