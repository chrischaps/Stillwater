using UnityEngine;

namespace Stillwater.Fishing
{
    /// <summary>
    /// Data for a curated fishing spot with custom properties.
    /// Used by FishingSpotMarker to define special locations with unique fish populations or modifiers.
    /// </summary>
    [System.Serializable]
    public class FishingSpotData
    {
        /// <summary>
        /// Unique identifier for this fishing spot.
        /// </summary>
        [Tooltip("Unique identifier for this fishing spot")]
        public string SpotId;

        /// <summary>
        /// Display name shown to the player.
        /// </summary>
        [Tooltip("Display name shown to the player")]
        public string DisplayName;

        /// <summary>
        /// Custom fish population for this spot. Overrides zone defaults if set.
        /// </summary>
        [Tooltip("Custom fish population for this spot. Leave empty to use zone defaults.")]
        public FishDefinition[] AvailableFish;

        /// <summary>
        /// Modifier for bite probability at this spot.
        /// 1.0 = normal, > 1.0 = more bites, < 1.0 = fewer bites.
        /// </summary>
        [Tooltip("Bite probability modifier. 1.0 = normal")]
        [Range(0.1f, 3f)]
        public float BiteProbabilityModifier = 1.0f;

        /// <summary>
        /// Optional zone ID override. If set, this spot uses a different zone's properties.
        /// </summary>
        [Tooltip("Optional zone ID override. Leave empty to use current zone.")]
        public string ZoneIdOverride;

        /// <summary>
        /// Creates an empty FishingSpotData with default values.
        /// </summary>
        public FishingSpotData()
        {
            SpotId = string.Empty;
            DisplayName = string.Empty;
            AvailableFish = null;
            BiteProbabilityModifier = 1.0f;
            ZoneIdOverride = null;
        }

        /// <summary>
        /// Creates a FishingSpotData with the specified ID and name.
        /// </summary>
        public FishingSpotData(string spotId, string displayName)
        {
            SpotId = spotId;
            DisplayName = displayName;
            AvailableFish = null;
            BiteProbabilityModifier = 1.0f;
            ZoneIdOverride = null;
        }
    }
}
