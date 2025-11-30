namespace Stillwater.Fishing
{
    /// <summary>
    /// Represents all possible states in the fishing state machine.
    /// States flow roughly: Idle -> Casting -> LureDrift -> Stillness/MicroTwitch -> BiteCheck ->
    /// HookOpportunity -> Hooked -> Reeling/SlackEvent -> Caught/Lost
    /// </summary>
    public enum FishingState
    {
        /// <summary>Player is not fishing. Default resting state.</summary>
        Idle,

        /// <summary>Player is in the casting animation, line being thrown.</summary>
        Casting,

        /// <summary>Lure is drifting on the water after cast.</summary>
        LureDrift,

        /// <summary>Lure has settled, building stillness for bite probability.</summary>
        Stillness,

        /// <summary>Optional player-initiated small movement to attract fish.</summary>
        MicroTwitch,

        /// <summary>System is evaluating whether a fish will bite.</summary>
        BiteCheck,

        /// <summary>A fish is nibbling - player has a window to hook.</summary>
        HookOpportunity,

        /// <summary>Fish is on the line, fight begins.</summary>
        Hooked,

        /// <summary>Player is actively reeling in the fish.</summary>
        Reeling,

        /// <summary>Player gives slack to prevent line break during fish struggle.</summary>
        SlackEvent,

        /// <summary>Fish successfully landed. Terminal success state.</summary>
        Caught,

        /// <summary>Fish escaped. Terminal failure state.</summary>
        Lost
    }
}
