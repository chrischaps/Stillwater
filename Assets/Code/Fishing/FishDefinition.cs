using UnityEngine;

namespace Stillwater.Fishing
{
    /// <summary>
    /// ScriptableObject that defines a fish type's properties and behavior.
    /// Used by the fishing system to determine bite timing, rarity, and display info.
    /// </summary>
    [CreateAssetMenu(fileName = "NewFish", menuName = "Stillwater/Fish Definition", order = 1)]
    public class FishDefinition : ScriptableObject
    {
        [Header("Identification")]
        [Tooltip("Unique identifier for this fish type")]
        [SerializeField] private string _id;

        [Tooltip("Display name shown to the player")]
        [SerializeField] private string _displayName;

        [Tooltip("Icon sprite for UI display")]
        [SerializeField] private Sprite _icon;

        [Header("Bite Behavior")]
        [Tooltip("Curve defining bite window intensity over time (0-1). Higher values = more likely to bite.")]
        [SerializeField] private AnimationCurve _biteWindowCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Tooltip("Minimum wait time before fish shows interest (seconds)")]
        [SerializeField] private float _minWaitTime = 2f;

        [Tooltip("Maximum wait time before fish shows interest (seconds)")]
        [SerializeField] private float _maxWaitTime = 8f;

        [Header("Rarity")]
        [Tooltip("Base rarity value (0-1). Lower = more rare.")]
        [Range(0f, 1f)]
        [SerializeField] private float _rarityBase = 0.5f;

        [Header("Flavor")]
        [Tooltip("Reference ID for flavor text lookup")]
        [SerializeField] private string _flavorTextId;

        #region Public Properties

        /// <summary>
        /// Unique identifier for this fish type.
        /// </summary>
        public string Id => _id;

        /// <summary>
        /// Display name shown to the player.
        /// </summary>
        public string DisplayName => _displayName;

        /// <summary>
        /// Icon sprite for UI display.
        /// </summary>
        public Sprite Icon => _icon;

        /// <summary>
        /// Curve defining bite window intensity over time.
        /// X-axis: normalized time (0-1), Y-axis: bite probability multiplier.
        /// </summary>
        public AnimationCurve BiteWindowCurve => _biteWindowCurve;

        /// <summary>
        /// Minimum wait time before fish shows interest (seconds).
        /// </summary>
        public float MinWaitTime => _minWaitTime;

        /// <summary>
        /// Maximum wait time before fish shows interest (seconds).
        /// </summary>
        public float MaxWaitTime => _maxWaitTime;

        /// <summary>
        /// Base rarity value (0-1). Lower values indicate rarer fish.
        /// </summary>
        public float RarityBase => _rarityBase;

        /// <summary>
        /// Reference ID for flavor text lookup in the journal system.
        /// </summary>
        public string FlavorTextId => _flavorTextId;

        #endregion

        #region Public Methods

        /// <summary>
        /// Evaluates the bite window curve at the given normalized time.
        /// </summary>
        /// <param name="normalizedTime">Time value between 0 and 1.</param>
        /// <returns>Bite probability multiplier at that time.</returns>
        public float EvaluateBiteWindow(float normalizedTime)
        {
            return _biteWindowCurve.Evaluate(Mathf.Clamp01(normalizedTime));
        }

        /// <summary>
        /// Gets a random wait time between min and max wait times.
        /// </summary>
        /// <returns>Random wait time in seconds.</returns>
        public float GetRandomWaitTime()
        {
            return Random.Range(_minWaitTime, _maxWaitTime);
        }

        /// <summary>
        /// Gets a random wait time using the provided random value (0-1).
        /// Useful for deterministic testing.
        /// </summary>
        /// <param name="randomValue">Random value between 0 and 1.</param>
        /// <returns>Wait time in seconds.</returns>
        public float GetWaitTime(float randomValue)
        {
            return Mathf.Lerp(_minWaitTime, _maxWaitTime, Mathf.Clamp01(randomValue));
        }

        /// <summary>
        /// Validates the fish definition has required data.
        /// </summary>
        /// <returns>True if the definition is valid.</returns>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(_id) &&
                   !string.IsNullOrEmpty(_displayName) &&
                   _minWaitTime >= 0f &&
                   _maxWaitTime >= _minWaitTime;
        }

        #endregion

        #region Editor

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Ensure min is not greater than max
            if (_minWaitTime > _maxWaitTime)
            {
                _maxWaitTime = _minWaitTime;
            }

            // Auto-generate ID from name if empty
            if (string.IsNullOrEmpty(_id) && !string.IsNullOrEmpty(name))
            {
                _id = name.ToLowerInvariant().Replace(" ", "_");
            }
        }
#endif

        #endregion
    }
}
