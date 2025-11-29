namespace Stillwater.World
{
    /// <summary>
    /// Documentation for the Isometric Grid configuration used in Stillwater.
    ///
    /// ## Grid Settings (configured in SampleScene.unity)
    ///
    /// The IsometricGrid GameObject uses Unity's Grid component with these settings:
    ///
    /// - **Cell Layout:** IsometricZAsY (value: 3)
    ///   Uses Z position for depth sorting, enabling proper isometric occlusion.
    ///   Sprites closer to the camera (higher Y in world space) render on top.
    ///
    /// - **Cell Size:** (1, 0.5, 1)
    ///   Standard 2:1 isometric ratio. With 32 PPU:
    ///   - 1 unit = 32 pixels horizontally
    ///   - 0.5 units = 16 pixels vertically (half-height for isometric diamond)
    ///
    /// - **Cell Gap:** (0, 0, 0)
    ///   No gap between tiles for seamless tiling.
    ///
    /// - **Cell Swizzle:** XYZ (value: 0)
    ///   Standard coordinate mapping, no axis swapping.
    ///
    /// ## Tile Art Guidelines
    ///
    /// When creating isometric tiles for this grid:
    /// - Base sprite size: 32x32 pixels (1x1 units)
    /// - Isometric tile footprint: 32x16 pixels (1x0.5 units)
    /// - Tile sprites should be set to 32 PPU
    /// - Pivot point at bottom-center for proper positioning
    /// - Use Transparency Sort Mode: Custom Axis (0, 1, 0) for depth sorting
    ///
    /// ## Tilemap Layer Structure
    ///
    /// Create Tilemap children under the IsometricGrid:
    /// - Tilemap_Ground (sorting layer: Ground)
    /// - Tilemap_Water (sorting layer: Water)
    /// - Tilemap_Props (sorting layer: Props_Back or Props_Front)
    /// - Tilemap_Interactables (sorting layer: Characters)
    /// - Tilemap_FX (sorting layer: FX)
    ///
    /// ## Coordinate Conversion
    ///
    /// Use Grid.CellToWorld() and Grid.WorldToCell() for position conversion:
    /// <code>
    /// Grid grid = FindObjectOfType&lt;Grid&gt;();
    /// Vector3Int cellPos = grid.WorldToCell(worldPosition);
    /// Vector3 worldPos = grid.CellToWorld(cellPosition);
    /// </code>
    /// </summary>
    public static class IsometricGridSettings
    {
        public const float CellWidth = 1f;
        public const float CellHeight = 0.5f;
        public const int PixelsPerUnit = 32;

        public const int BaseSpriteSize = 32;
        public const int TilePixelWidth = 32;
        public const int TilePixelHeight = 16;
    }
}
