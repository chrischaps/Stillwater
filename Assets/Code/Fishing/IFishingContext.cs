using UnityEngine;

namespace Stillwater.Fishing
{
    /// <summary>
    /// Provides shared context data to fishing states.
    /// Exposes lure information, input state, and timing data
    /// so states can make decisions without tight coupling to other systems.
    /// </summary>
    public interface IFishingContext
    {
        // ==================== State Info ====================

        /// <summary>The current state of the fishing state machine.</summary>
        FishingState CurrentState { get; }

        /// <summary>Time spent in the current state (seconds).</summary>
        float TimeInState { get; }

        // ==================== Lure Data ====================

        /// <summary>Current world position of the lure.</summary>
        Vector2 LurePosition { get; }

        /// <summary>Current velocity of the lure.</summary>
        Vector2 LureVelocity { get; }

        /// <summary>Distance from the player to the lure.</summary>
        float LineLength { get; }

        /// <summary>Current tension on the line (0-1, where 1 is near breaking).</summary>
        float LineTension { get; }

        // ==================== Input State ====================

        /// <summary>True if the cast input was pressed this frame.</summary>
        bool CastInputPressed { get; }

        /// <summary>True if the reel input is currently held.</summary>
        bool ReelInputHeld { get; }

        /// <summary>True if the slack input was pressed this frame.</summary>
        bool SlackInputPressed { get; }

        /// <summary>True if the cancel input was pressed this frame.</summary>
        bool CancelInputPressed { get; }

        // ==================== Fish Data ====================

        /// <summary>True if a fish is currently targeting/nibbling the lure.</summary>
        bool HasFishInterest { get; }

        /// <summary>True if a fish is currently hooked.</summary>
        bool HasHookedFish { get; }

        /// <summary>Identifier of the currently hooked fish (null if none).</summary>
        string HookedFishId { get; }

        /// <summary>Current struggle intensity of the hooked fish (0-1).</summary>
        float FishStruggleIntensity { get; }

        /// <summary>
        /// The currently selected fish definition (set during bite check).
        /// May be null if no fish has been selected.
        /// </summary>
        FishDefinition SelectedFish { get; }

        /// <summary>
        /// Available fish definitions for the current zone.
        /// </summary>
        FishDefinition[] AvailableFish { get; }

        // ==================== Zone/Environment ====================

        /// <summary>Identifier of the current fishing zone.</summary>
        string CurrentZoneId { get; }

        /// <summary>Base bite probability modifier for the current zone/conditions.</summary>
        float BiteProbabilityModifier { get; }

        // ==================== Randomness ====================

        /// <summary>
        /// Get a random float value between 0 and 1.
        /// Useful for probability checks in states.
        /// </summary>
        float GetRandomValue();

        /// <summary>
        /// Get a random float value within a specified range.
        /// </summary>
        float GetRandomRange(float min, float max);
    }
}
