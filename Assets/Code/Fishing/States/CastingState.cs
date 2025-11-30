using UnityEngine;

namespace Stillwater.Fishing.States
{
    /// <summary>
    /// The casting state where the player is throwing their line.
    /// Plays cast animation timing and calculates the lure landing position.
    /// Transitions to LureDrift when the cast animation completes.
    /// </summary>
    public class CastingState : IFishingState
    {
        private readonly float _castDuration;
        private readonly float _minCastDistance;
        private readonly float _maxCastDistance;

        private float _elapsedTime;
        private bool _castComplete;
        private Vector2 _calculatedLandingPosition;

        /// <summary>
        /// The calculated landing position for the lure after the cast.
        /// Valid after Enter() is called.
        /// </summary>
        public Vector2 LandingPosition => _calculatedLandingPosition;

        /// <summary>
        /// Progress of the cast animation (0-1).
        /// </summary>
        public float CastProgress => _castDuration > 0 ? Mathf.Clamp01(_elapsedTime / _castDuration) : 1f;

        /// <summary>
        /// Creates a new CastingState with default timing.
        /// </summary>
        public CastingState() : this(0.5f, 2f, 8f)
        {
        }

        /// <summary>
        /// Creates a new CastingState with custom timing and distance.
        /// </summary>
        /// <param name="castDuration">Duration of the cast animation in seconds.</param>
        /// <param name="minCastDistance">Minimum distance the lure can travel.</param>
        /// <param name="maxCastDistance">Maximum distance the lure can travel.</param>
        public CastingState(float castDuration, float minCastDistance, float maxCastDistance)
        {
            _castDuration = Mathf.Max(0.1f, castDuration);
            _minCastDistance = Mathf.Max(0f, minCastDistance);
            _maxCastDistance = Mathf.Max(_minCastDistance, maxCastDistance);
        }

        /// <summary>
        /// Called when entering the casting state.
        /// Calculates the landing position based on a random distance and direction.
        /// </summary>
        public void Enter(IFishingContext context)
        {
            _elapsedTime = 0f;
            _castComplete = false;

            // Calculate landing position
            // Use random distance within range and random direction
            // In a full implementation, this would use player facing direction and input power
            float distance = context.GetRandomRange(_minCastDistance, _maxCastDistance);
            float angle = context.GetRandomRange(0f, 360f) * Mathf.Deg2Rad;

            // Calculate offset from current lure position (which starts at player)
            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
            _calculatedLandingPosition = context.LurePosition + offset;
        }

        /// <summary>
        /// Called every frame while in casting state.
        /// Advances the cast animation timer.
        /// </summary>
        public void Update(IFishingContext context, float deltaTime)
        {
            _elapsedTime += deltaTime;

            if (_elapsedTime >= _castDuration)
            {
                _castComplete = true;
            }
        }

        /// <summary>
        /// Called when exiting the casting state.
        /// </summary>
        public void Exit(IFishingContext context)
        {
            // Cast animation cleanup would happen here
            // Landing position is now available for the LureDrift state
        }

        /// <summary>
        /// Returns LureDrift state when cast animation is complete, null otherwise.
        /// </summary>
        public FishingState? GetNextState(IFishingContext context)
        {
            if (_castComplete)
            {
                return FishingState.LureDrift;
            }
            return null;
        }
    }
}
