using UnityEngine;

namespace Stillwater.Core
{
    /// <summary>
    /// Extension methods and utilities for FacingDirection enum.
    /// Provides conversion between FacingDirection and Vector2/Vector3Int for isometric grids.
    /// </summary>
    public static class FacingDirectionExtensions
    {
        // Isometric direction vectors (2:1 ratio for horizontal:vertical)
        // These match the isometric grid cell offsets
        private static readonly Vector2[] DirectionVectors = new Vector2[]
        {
            new Vector2(0, -1),           // South (down on screen)
            new Vector2(1, -0.5f).normalized,   // SouthEast (iso diagonal)
            new Vector2(1, 0),            // East (right on screen)
            new Vector2(1, 0.5f).normalized,    // NorthEast (iso diagonal)
            new Vector2(0, 1),            // North (up on screen)
            new Vector2(-1, 0.5f).normalized,   // NorthWest (iso diagonal)
            new Vector2(-1, 0),           // West (left on screen)
            new Vector2(-1, -0.5f).normalized,  // SouthWest (iso diagonal)
        };

        // Cell offsets for each direction (for tilemap queries)
        private static readonly Vector3Int[] CellOffsets = new Vector3Int[]
        {
            new Vector3Int(0, -1, 0),     // South
            new Vector3Int(1, -1, 0),     // SouthEast
            new Vector3Int(1, 0, 0),      // East
            new Vector3Int(1, 1, 0),      // NorthEast
            new Vector3Int(0, 1, 0),      // North
            new Vector3Int(-1, 1, 0),     // NorthWest
            new Vector3Int(-1, 0, 0),     // West
            new Vector3Int(-1, -1, 0),    // SouthWest
        };

        /// <summary>
        /// Gets the normalized world-space direction vector for this facing direction.
        /// </summary>
        public static Vector2 ToVector2(this FacingDirection direction)
        {
            return DirectionVectors[(int)direction];
        }

        /// <summary>
        /// Gets the cell offset for tilemap queries in this direction.
        /// </summary>
        public static Vector3Int ToCellOffset(this FacingDirection direction)
        {
            return CellOffsets[(int)direction];
        }

        /// <summary>
        /// Gets the FacingDirection from a movement vector.
        /// Returns the closest of the 8 directions.
        /// </summary>
        public static FacingDirection FromVector2(Vector2 direction)
        {
            if (direction.sqrMagnitude < 0.001f)
            {
                return FacingDirection.South; // Default facing
            }

            Vector2 normalized = direction.normalized;
            float bestDot = float.MinValue;
            FacingDirection bestDirection = FacingDirection.South;

            for (int i = 0; i < DirectionVectors.Length; i++)
            {
                float dot = Vector2.Dot(normalized, DirectionVectors[i]);
                if (dot > bestDot)
                {
                    bestDot = dot;
                    bestDirection = (FacingDirection)i;
                }
            }

            return bestDirection;
        }

        /// <summary>
        /// Gets the opposite facing direction.
        /// </summary>
        public static FacingDirection GetOpposite(this FacingDirection direction)
        {
            return (FacingDirection)(((int)direction + 4) % 8);
        }

        /// <summary>
        /// Rotates the facing direction clockwise by the specified number of steps (each step is 45 degrees).
        /// </summary>
        public static FacingDirection RotateClockwise(this FacingDirection direction, int steps = 1)
        {
            return (FacingDirection)(((int)direction + steps) % 8);
        }

        /// <summary>
        /// Rotates the facing direction counter-clockwise by the specified number of steps.
        /// </summary>
        public static FacingDirection RotateCounterClockwise(this FacingDirection direction, int steps = 1)
        {
            return (FacingDirection)(((int)direction - steps + 8) % 8);
        }

        /// <summary>
        /// Returns true if this direction is a cardinal direction (N, S, E, W).
        /// </summary>
        public static bool IsCardinal(this FacingDirection direction)
        {
            return direction == FacingDirection.North ||
                   direction == FacingDirection.South ||
                   direction == FacingDirection.East ||
                   direction == FacingDirection.West;
        }

        /// <summary>
        /// Returns true if this direction is a diagonal direction (NE, NW, SE, SW).
        /// </summary>
        public static bool IsDiagonal(this FacingDirection direction)
        {
            return !direction.IsCardinal();
        }

        /// <summary>
        /// Gets all 8 facing directions.
        /// </summary>
        public static FacingDirection[] AllDirections => new FacingDirection[]
        {
            FacingDirection.South,
            FacingDirection.SouthEast,
            FacingDirection.East,
            FacingDirection.NorthEast,
            FacingDirection.North,
            FacingDirection.NorthWest,
            FacingDirection.West,
            FacingDirection.SouthWest
        };
    }
}
