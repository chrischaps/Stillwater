namespace Stillwater.Core
{
    /// <summary>
    /// Fired when the fishing state machine transitions to a new state.
    /// </summary>
    public struct FishingStateChangedEvent
    {
        /// <summary>The previous fishing state.</summary>
        public string PreviousState;

        /// <summary>The new fishing state.</summary>
        public string NewState;
    }

    /// <summary>
    /// Fired when a fish is successfully caught.
    /// </summary>
    public struct FishCaughtEvent
    {
        /// <summary>Identifier for the type of fish caught.</summary>
        public string FishId;

        /// <summary>The zone where the fish was caught.</summary>
        public string ZoneId;

        /// <summary>Size or weight of the caught fish.</summary>
        public float Size;

        /// <summary>Whether this was a rare catch.</summary>
        public bool IsRare;
    }

    /// <summary>
    /// Fired when a hooked fish escapes.
    /// </summary>
    public struct FishLostEvent
    {
        /// <summary>Identifier for the type of fish that escaped (if known).</summary>
        public string FishId;

        /// <summary>The zone where the fish was lost.</summary>
        public string ZoneId;

        /// <summary>Reason the fish escaped (e.g., "line_break", "timeout", "missed_input").</summary>
        public string Reason;
    }

    /// <summary>
    /// Fired when the Lake Watcher updates mood scores.
    /// </summary>
    public struct MoodUpdatedEvent
    {
        /// <summary>Current stillness mood score (0-1).</summary>
        public float Stillness;

        /// <summary>Current curiosity mood score (0-1).</summary>
        public float Curiosity;

        /// <summary>Current loss mood score (0-1).</summary>
        public float Loss;

        /// <summary>Current disruption mood score (0-1).</summary>
        public float Disruption;
    }

    /// <summary>
    /// Fired when GameRoot has completed initialization and all core services are ready.
    /// </summary>
    public struct GameInitializedEvent { }

    /// <summary>
    /// Fired when a scene begins loading.
    /// </summary>
    public struct SceneLoadStartedEvent
    {
        /// <summary>The name of the scene being loaded.</summary>
        public string SceneName;
    }

    /// <summary>
    /// Fired when a scene has finished loading.
    /// </summary>
    public struct SceneLoadCompletedEvent
    {
        /// <summary>The name of the scene that was loaded.</summary>
        public string SceneName;
    }

    /// <summary>
    /// Fired when the main game has started and all main scenes are loaded.
    /// </summary>
    public struct GameStartedEvent { }

    /// <summary>
    /// Fired when the Cast action is performed (button press).
    /// </summary>
    public struct CastInputEvent { }

    /// <summary>
    /// Fired when the Reel action starts (hold begins).
    /// </summary>
    public struct ReelStartedEvent { }

    /// <summary>
    /// Fired when the Reel action ends (hold released).
    /// </summary>
    public struct ReelEndedEvent { }

    /// <summary>
    /// Fired when the Slack action is performed.
    /// </summary>
    public struct SlackInputEvent { }

    /// <summary>
    /// Fired when the Interact action is performed.
    /// </summary>
    public struct InteractInputEvent { }

    /// <summary>
    /// Fired when the Cancel action is performed.
    /// </summary>
    public struct CancelInputEvent { }
}
