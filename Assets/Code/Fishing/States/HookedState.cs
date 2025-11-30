using UnityEngine;

namespace Stillwater.Fishing.States
{
    /// <summary>
    /// Brief transitional state after successfully setting the hook.
    /// Plays the hook-set animation/feedback and transitions to Reeling.
    /// </summary>
    public class HookedState : IFishingState
    {
        private readonly float _hookSetDuration;

        private float _elapsedTime;
        private bool _hookSetComplete;

        /// <summary>
        /// Progress through the hook-set animation (0-1).
        /// </summary>
        public float HookSetProgress => _hookSetDuration > 0
            ? Mathf.Clamp01(_elapsedTime / _hookSetDuration)
            : 1f;

        /// <summary>
        /// Whether the hook-set animation is complete.
        /// </summary>
        public bool HookSetComplete => _hookSetComplete;

        /// <summary>
        /// Creates a new HookedState with default settings.
        /// </summary>
        public HookedState() : this(0.3f)
        {
        }

        /// <summary>
        /// Creates a new HookedState with custom duration.
        /// </summary>
        /// <param name="hookSetDuration">Duration of the hook-set animation in seconds.</param>
        public HookedState(float hookSetDuration)
        {
            _hookSetDuration = Mathf.Max(0.1f, hookSetDuration);
        }

        /// <summary>
        /// Called when entering the hooked state.
        /// Resets the hook-set timer.
        /// </summary>
        public void Enter(IFishingContext context)
        {
            _elapsedTime = 0f;
            _hookSetComplete = false;
        }

        /// <summary>
        /// Called every frame while in hooked state.
        /// Advances the hook-set animation timer.
        /// </summary>
        public void Update(IFishingContext context, float deltaTime)
        {
            _elapsedTime += deltaTime;

            if (_elapsedTime >= _hookSetDuration)
            {
                _hookSetComplete = true;
            }
        }

        /// <summary>
        /// Called when exiting the hooked state.
        /// </summary>
        public void Exit(IFishingContext context)
        {
            // Hook-set animation complete
        }

        /// <summary>
        /// Returns Reeling state when hook-set is complete, null otherwise.
        /// </summary>
        public FishingState? GetNextState(IFishingContext context)
        {
            if (_hookSetComplete)
            {
                return FishingState.Reeling;
            }
            return null;
        }
    }
}
