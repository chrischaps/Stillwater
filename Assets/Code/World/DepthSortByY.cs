using UnityEngine;

namespace Stillwater.World
{
    /// <summary>
    /// Updates a SpriteRenderer's sortingOrder based on the object's Y position.
    /// Attach to dynamic sprites (player, NPCs) for proper depth sorting in isometric view.
    ///
    /// Lower Y positions (closer to camera) result in higher sortingOrder (rendered in front).
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class DepthSortByY : MonoBehaviour
    {
        [Tooltip("Multiplier for Y position to sortingOrder conversion. Higher values provide finer granularity.")]
        [SerializeField] private int sortingPrecision = 100;

        [Tooltip("Offset added to the calculated sorting order.")]
        [SerializeField] private int sortingOffset = 0;

        [Tooltip("If true, updates sorting order every frame. Disable for static objects after positioning.")]
        [SerializeField] private bool updateEveryFrame = true;

        private SpriteRenderer _spriteRenderer;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Start()
        {
            UpdateSortingOrder();
        }

        private void LateUpdate()
        {
            if (updateEveryFrame)
            {
                UpdateSortingOrder();
            }
        }

        /// <summary>
        /// Manually update the sorting order. Call this after repositioning static objects.
        /// </summary>
        public void UpdateSortingOrder()
        {
            // Negate Y so lower positions (closer to camera bottom) have higher sorting order
            int order = Mathf.RoundToInt(-transform.position.y * sortingPrecision) + sortingOffset;
            _spriteRenderer.sortingOrder = order;
        }
    }
}
