namespace Stillwater.Fishing
{
    /// <summary>
    /// Interface for individual fishing states in the state machine.
    /// Each state implementation handles its own logic for entering, updating,
    /// exiting, and determining when to transition to the next state.
    /// </summary>
    public interface IFishingState
    {
        /// <summary>
        /// Called when the state machine enters this state.
        /// Use for initialization, starting animations, resetting timers, etc.
        /// </summary>
        /// <param name="context">The fishing context providing access to shared data.</param>
        void Enter(IFishingContext context);

        /// <summary>
        /// Called every frame while this state is active.
        /// Handle state-specific logic, input processing, and timer updates here.
        /// </summary>
        /// <param name="context">The fishing context providing access to shared data.</param>
        /// <param name="deltaTime">Time elapsed since the last update.</param>
        void Update(IFishingContext context, float deltaTime);

        /// <summary>
        /// Called when the state machine exits this state.
        /// Use for cleanup, stopping animations, publishing events, etc.
        /// </summary>
        /// <param name="context">The fishing context providing access to shared data.</param>
        void Exit(IFishingContext context);

        /// <summary>
        /// Determines if a state transition should occur and returns the next state.
        /// Called after Update to check for transition conditions.
        /// </summary>
        /// <param name="context">The fishing context providing access to shared data.</param>
        /// <returns>The next state to transition to, or null if no transition should occur.</returns>
        FishingState? GetNextState(IFishingContext context);
    }
}
