using UnityEngine;

namespace Stillwater.Fishing
{
    /// <summary>
    /// Marks a special curated fishing spot with custom data.
    /// Place on an empty GameObject at the fishing location.
    /// ShoreDetector will detect these markers and apply their custom properties.
    /// </summary>
    public class FishingSpotMarker : MonoBehaviour
    {
        [Header("Spot Configuration")]
        [Tooltip("Configuration data for this fishing spot")]
        [SerializeField] private FishingSpotData _spotData = new FishingSpotData();

        [Header("Detection Settings")]
        [Tooltip("Radius within which the player can interact with this spot")]
        [SerializeField] private float _interactionRadius = 1.5f;

        [Header("Gizmo Settings")]
        [SerializeField] private Color _gizmoColor = new Color(0.2f, 0.6f, 1f, 0.5f);
        [SerializeField] private bool _showGizmoAlways = true;

        /// <summary>
        /// Gets the fishing spot data associated with this marker.
        /// </summary>
        public FishingSpotData SpotData => _spotData;

        /// <summary>
        /// Gets the interaction radius for this fishing spot.
        /// </summary>
        public float InteractionRadius => _interactionRadius;

        /// <summary>
        /// Gets the world position of this fishing spot.
        /// </summary>
        public Vector2 Position => transform.position;

        /// <summary>
        /// Checks if the given position is within interaction range of this spot.
        /// </summary>
        /// <param name="position">The position to check.</param>
        /// <returns>True if the position is within interaction radius.</returns>
        public bool IsWithinRange(Vector2 position)
        {
            return Vector2.Distance(position, Position) <= _interactionRadius;
        }

        private void OnValidate()
        {
            // Auto-generate spot ID from GameObject name if empty
            if (string.IsNullOrEmpty(_spotData.SpotId))
            {
                _spotData.SpotId = gameObject.name.ToLowerInvariant().Replace(" ", "_");
            }

            // Auto-set display name from GameObject name if empty
            if (string.IsNullOrEmpty(_spotData.DisplayName))
            {
                _spotData.DisplayName = gameObject.name;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!_showGizmoAlways) return;
            DrawGizmo(0.5f);
        }

        private void OnDrawGizmosSelected()
        {
            DrawGizmo(1f);
        }

        private void DrawGizmo(float alphaMultiplier)
        {
            Vector3 pos = transform.position;

            // Draw interaction radius
            Color radiusColor = _gizmoColor;
            radiusColor.a *= alphaMultiplier * 0.3f;
            Gizmos.color = radiusColor;
            Gizmos.DrawWireSphere(pos, _interactionRadius);

            // Draw filled circle at center
            Color centerColor = _gizmoColor;
            centerColor.a *= alphaMultiplier;
            Gizmos.color = centerColor;
            Gizmos.DrawSphere(pos, 0.2f);

            // Draw fishing icon (simple rod shape)
            Gizmos.color = Color.white * alphaMultiplier;
            Vector3 rodStart = pos + Vector3.up * 0.3f;
            Vector3 rodEnd = rodStart + new Vector3(0.3f, 0.4f, 0f);
            Gizmos.DrawLine(rodStart, rodEnd);
            Gizmos.DrawLine(rodEnd, rodEnd + Vector3.down * 0.3f);

            // Draw label with spot name
            if (!string.IsNullOrEmpty(_spotData.DisplayName))
            {
                UnityEditor.Handles.Label(pos + Vector3.up * (_interactionRadius + 0.3f), _spotData.DisplayName);
            }
        }
#endif
    }
}
