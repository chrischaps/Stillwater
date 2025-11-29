namespace Stillwater.Core
{
    /// <summary>
    /// Constants for Unity Sorting Layers used in sprite rendering.
    /// Layers are rendered in order from back to front (lowest to highest).
    ///
    /// Layer Order (back to front):
    /// 1. Background  - Sky, distant scenery, parallax backgrounds
    /// 2. Water       - Lake surface, rivers, water effects
    /// 3. Ground      - Terrain, ground tiles, paths
    /// 4. Props_Back  - Objects behind the player (trees, rocks, buildings)
    /// 5. Characters  - Player, NPCs, fish, interactive entities
    /// 6. Props_Front - Objects in front of the player (foreground foliage, posts)
    /// 7. FX          - Particles, weather effects, screen overlays
    /// 8. UI          - HUD elements, menus, tooltips
    ///
    /// Usage:
    /// - Set SpriteRenderer.sortingLayerName = SortingLayers.Characters
    /// - Within a layer, use sortingOrder for fine-grained control
    /// - For isometric depth sorting, use "Z as Y" with Transparency Sort Axis
    /// </summary>
    public static class SortingLayers
    {
        public const string Default = "Default";
        public const string Background = "Background";
        public const string Water = "Water";
        public const string Ground = "Ground";
        public const string PropsBack = "Props_Back";
        public const string Characters = "Characters";
        public const string PropsFront = "Props_Front";
        public const string FX = "FX";
        public const string UI = "UI";
    }
}
