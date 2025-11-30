namespace Stillwater.Fishing.States
{
    /// <summary>
    /// The idle fishing state where the player is not actively fishing.
    /// Waits for cast input to transition to the Casting state.
    /// </summary>
    public class IdleState : IFishingState
    {
        private bool _castRequested;

        /// <summary>
        /// Called when entering the idle state.
        /// Resets cast request flag.
        /// </summary>
        public void Enter(IFishingContext context)
        {
            _castRequested = false;
        }

        /// <summary>
        /// Called every frame while in idle state.
        /// Checks for cast input.
        /// </summary>
        public void Update(IFishingContext context, float deltaTime)
        {
            if (context.CastInputPressed)
            {
                _castRequested = true;
            }
        }

        /// <summary>
        /// Called when exiting the idle state.
        /// </summary>
        public void Exit(IFishingContext context)
        {
            // Nothing to clean up
        }

        /// <summary>
        /// Returns Casting state if cast was requested, null otherwise.
        /// </summary>
        public FishingState? GetNextState(IFishingContext context)
        {
            if (_castRequested)
            {
                return FishingState.Casting;
            }
            return null;
        }
    }
}
