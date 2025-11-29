using UnityEngine;
using Stillwater.Framework;

namespace Stillwater.Core
{
    /// <summary>
    /// Service interface for reading player input state.
    /// Provides access to movement vectors and button states from the Input System.
    /// </summary>
    [Service]
    public interface IInputService
    {
        /// <summary>
        /// The current movement input as a Vector2 (x: horizontal, y: vertical).
        /// Values range from -1 to 1 for each axis.
        /// </summary>
        Vector2 MoveInput { get; }

        /// <summary>
        /// Whether the Reel button is currently held down.
        /// </summary>
        bool IsReeling { get; }

        /// <summary>
        /// Enables or disables all gameplay input.
        /// When disabled, all input reads return default values.
        /// </summary>
        bool InputEnabled { get; set; }

        /// <summary>
        /// Switches to the Gameplay action map.
        /// </summary>
        void EnableGameplayInput();

        /// <summary>
        /// Switches to the UI action map.
        /// </summary>
        void EnableUIInput();
    }
}
