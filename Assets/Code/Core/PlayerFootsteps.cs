using UnityEngine;

namespace Stillwater.Core
{
    /// <summary>
    /// Plays footstep sounds synchronized with the player's walk animation.
    /// Triggers sounds on specific animation frames (contact frames when feet hit ground).
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class PlayerFootsteps : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerAnimator _playerAnimator;

        [Header("Footstep Sounds")]
        [Tooltip("Array of footstep sound variants for randomization")]
        [SerializeField] private AudioClip[] _footstepClips;

        [Header("Timing")]
        [Tooltip("Animation frames that trigger footstep sounds (e.g., 1 and 4 for 6-frame walk)")]
        [SerializeField] private int[] _footstepFrames = { 1, 4 };

        [Header("Audio Settings")]
        [SerializeField, Range(0f, 1f)] private float _volume = 0.5f;
        [SerializeField, Range(0f, 0.3f)] private float _volumeVariation = 0.1f;
        [SerializeField, Range(0.8f, 1.2f)] private float _pitchMin = 0.9f;
        [SerializeField, Range(0.8f, 1.2f)] private float _pitchMax = 1.1f;

        [Header("Cooldown")]
        [Tooltip("Minimum time between footstep sounds to prevent rapid repeats")]
        [SerializeField] private float _minTimeBetweenSteps = 0.1f;

        private AudioSource _audioSource;
        private int _lastFrame = -1;
        private bool _wasMoving;
        private float _lastStepTime;
        private int _lastClipIndex = -1;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.loop = false;

            if (_playerAnimator == null)
            {
                _playerAnimator = GetComponent<PlayerAnimator>();
            }
        }

        private void Update()
        {
            if (_playerAnimator == null) return;
            if (_footstepClips == null || _footstepClips.Length == 0) return;

            bool isMoving = _playerAnimator.IsMoving;
            int currentFrame = _playerAnimator.CurrentFrame;

            // Reset frame tracking when starting to move
            if (isMoving && !_wasMoving)
            {
                _lastFrame = -1;
            }

            // Check for footstep trigger
            if (isMoving && currentFrame != _lastFrame)
            {
                if (IsFootstepFrame(currentFrame) && Time.time - _lastStepTime >= _minTimeBetweenSteps)
                {
                    PlayFootstep();
                    _lastStepTime = Time.time;
                }
            }

            _lastFrame = currentFrame;
            _wasMoving = isMoving;
        }

        private bool IsFootstepFrame(int frame)
        {
            foreach (int footstepFrame in _footstepFrames)
            {
                if (frame == footstepFrame)
                    return true;
            }
            return false;
        }

        private void PlayFootstep()
        {
            // Select a random clip, avoiding immediate repeats
            int clipIndex = GetRandomClipIndex();
            AudioClip clip = _footstepClips[clipIndex];

            // Apply volume and pitch variation
            float volumeVariation = Random.Range(-_volumeVariation, _volumeVariation);
            float pitch = Random.Range(_pitchMin, _pitchMax);

            _audioSource.pitch = pitch;
            _audioSource.PlayOneShot(clip, _volume + volumeVariation);

            _lastClipIndex = clipIndex;
        }

        private int GetRandomClipIndex()
        {
            if (_footstepClips.Length == 1)
                return 0;

            // Avoid playing the same clip twice in a row
            int index;
            int attempts = 0;
            do
            {
                index = Random.Range(0, _footstepClips.Length);
                attempts++;
            }
            while (index == _lastClipIndex && attempts < 3);

            return index;
        }

        /// <summary>
        /// Manually trigger a footstep sound (useful for special events).
        /// </summary>
        public void PlayFootstepManual()
        {
            if (_footstepClips != null && _footstepClips.Length > 0)
            {
                PlayFootstep();
            }
        }

        /// <summary>
        /// Set the footstep volume at runtime.
        /// </summary>
        public void SetVolume(float volume)
        {
            _volume = Mathf.Clamp01(volume);
        }
    }
}
