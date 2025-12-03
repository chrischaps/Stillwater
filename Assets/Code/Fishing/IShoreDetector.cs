using System;
using UnityEngine;

namespace Stillwater.Fishing
{
    /// <summary>
    /// Interface for detecting valid fishing positions based on shore and water tiles.
    /// The player can fish when standing on a shore tile and facing water.
    /// </summary>
    public interface IShoreDetector
    {
        /// <summary>
        /// True when the player is on a shore tile and facing a water tile.
        /// </summary>
        bool CanFish { get; }

        /// <summary>
        /// The direction the player is facing toward water (normalized vector).
        /// Only valid when CanFish is true.
        /// </summary>
        Vector2 FishingDirection { get; }

        /// <summary>
        /// The world position of the target water tile.
        /// Only valid when CanFish is true.
        /// </summary>
        Vector2 TargetWaterPosition { get; }

        /// <summary>
        /// Data for the active fishing spot, or null for generic shore.
        /// </summary>
        FishingSpotData ActiveSpotData { get; }

        /// <summary>
        /// Fired when the CanFish state changes.
        /// Parameter is the new CanFish value.
        /// </summary>
        event Action<bool> OnCanFishChanged;
    }
}
