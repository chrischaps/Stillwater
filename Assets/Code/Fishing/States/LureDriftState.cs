using UnityEngine;

namespace Stillwater.Fishing.States
{
    /// <summary>
    /// The lure drift state where the lure is moving on the water after casting.
    /// Lure velocity is influenced by wind and echo system.
    /// Transitions to Stillness when velocity drops below threshold.
    /// </summary>
    public class LureDriftState : IFishingState
    {
        private readonly float _velocityThreshold;
        private readonly float _minDriftTime;

        private float _elapsedTime;
        private bool _readyToTransition;

        /// <summary>
        /// Current drift time in seconds.
        /// </summary>
        public float DriftTime => _elapsedTime;

        /// <summary>
        /// Creates a new LureDriftState with default settings.
        /// </summary>
        public LureDriftState() : this(0.1f, 0.5f)
        {
        }

        /// <summary>
        /// Creates a new LureDriftState with custom settings.
        /// </summary>
        /// <param name="velocityThreshold">Velocity magnitude below which lure is considered still.</param>
        /// <param name="minDriftTime">Minimum time to drift before allowing transition to Stillness.</param>
        public LureDriftState(float velocityThreshold, float minDriftTime)
        {
            _velocityThreshold = Mathf.Max(0.01f, velocityThreshold);
            _minDriftTime = Mathf.Max(0f, minDriftTime);
        }

        /// <summary>
        /// Called when entering the drift state.
        /// Resets drift timer.
        /// </summary>
        public void Enter(IFishingContext context)
        {
            _elapsedTime = 0f;
            _readyToTransition = false;
        }

        /// <summary>
        /// Called every frame while drifting.
        /// Monitors lure velocity to determine when to transition.
        /// </summary>
        public void Update(IFishingContext context, float deltaTime)
        {
            _elapsedTime += deltaTime;

            // Check if minimum drift time has passed and velocity is low enough
            if (_elapsedTime >= _minDriftTime)
            {
                float velocityMagnitude = context.LureVelocity.magnitude;
                if (velocityMagnitude <= _velocityThreshold)
                {
                    _readyToTransition = true;
                }
            }
        }

        /// <summary>
        /// Called when exiting the drift state.
        /// </summary>
        public void Exit(IFishingContext context)
        {
            // Drift complete, lure is now stationary
        }

        /// <summary>
        /// Returns Stillness state when lure velocity is low enough, null otherwise.
        /// </summary>
        public FishingState? GetNextState(IFishingContext context)
        {
            if (_readyToTransition)
            {
                return FishingState.Stillness;
            }
            return null;
        }
    }
}
