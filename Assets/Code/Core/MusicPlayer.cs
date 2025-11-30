using System.Collections;
using UnityEngine;

namespace Stillwater.Core
{
    /// <summary>
    /// Manages background music playback with crossfade support.
    /// Attach to a persistent GameObject or use DontDestroyOnLoad for scene persistence.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class MusicPlayer : MonoBehaviour
    {
        [Header("Music Tracks")]
        [Tooltip("Default track to play on start (optional)")]
        [SerializeField] private AudioClip _defaultTrack;

        [Tooltip("Play default track automatically on start")]
        [SerializeField] private bool _playOnStart = true;

        [Header("Audio Settings")]
        [SerializeField, Range(0f, 1f)] private float _volume = 0.5f;
        [SerializeField, Range(0f, 5f)] private float _fadeInDuration = 2f;
        [SerializeField, Range(0f, 5f)] private float _fadeOutDuration = 1.5f;
        [SerializeField, Range(0f, 5f)] private float _crossfadeDuration = 2f;

        [Header("Persistence")]
        [Tooltip("Keep music playing across scene loads")]
        [SerializeField] private bool _persistAcrossScenes = true;

        private AudioSource _primarySource;
        private AudioSource _secondarySource;
        private Coroutine _fadeCoroutine;
        private Coroutine _crossfadeCoroutine;
        private float _targetVolume;

        private static MusicPlayer _instance;
        public static MusicPlayer Instance => _instance;

        public float Volume
        {
            get => _volume;
            set
            {
                _volume = Mathf.Clamp01(value);
                _targetVolume = _volume;
                if (_primarySource != null && _fadeCoroutine == null)
                {
                    _primarySource.volume = _volume;
                }
            }
        }

        public bool IsPlaying => _primarySource != null && _primarySource.isPlaying;
        public AudioClip CurrentTrack => _primarySource?.clip;

        private void Awake()
        {
            // Singleton pattern for persistence
            if (_persistAcrossScenes)
            {
                if (_instance != null && _instance != this)
                {
                    Destroy(gameObject);
                    return;
                }
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }

            SetupAudioSources();
            _targetVolume = _volume;
        }

        private void SetupAudioSources()
        {
            _primarySource = GetComponent<AudioSource>();
            _primarySource.playOnAwake = false;
            _primarySource.loop = true;
            _primarySource.volume = 0f;

            // Create secondary source for crossfading
            _secondarySource = gameObject.AddComponent<AudioSource>();
            _secondarySource.playOnAwake = false;
            _secondarySource.loop = true;
            _secondarySource.volume = 0f;
        }

        private void Start()
        {
            if (_playOnStart && _defaultTrack != null)
            {
                Play(_defaultTrack);
            }
        }

        /// <summary>
        /// Play a music track with fade in.
        /// </summary>
        public void Play(AudioClip track)
        {
            if (track == null) return;

            StopAllMusicCoroutines();

            _primarySource.clip = track;
            _primarySource.Play();
            _fadeCoroutine = StartCoroutine(FadeIn(_primarySource, _fadeInDuration));
        }

        /// <summary>
        /// Play a music track immediately without fade.
        /// </summary>
        public void PlayImmediate(AudioClip track)
        {
            if (track == null) return;

            StopAllMusicCoroutines();

            _primarySource.clip = track;
            _primarySource.volume = _volume;
            _primarySource.Play();
        }

        /// <summary>
        /// Crossfade to a new track.
        /// </summary>
        public void CrossfadeTo(AudioClip newTrack)
        {
            if (newTrack == null) return;
            if (newTrack == _primarySource.clip && _primarySource.isPlaying) return;

            StopAllMusicCoroutines();
            _crossfadeCoroutine = StartCoroutine(CrossfadeCoroutine(newTrack));
        }

        /// <summary>
        /// Stop music with fade out.
        /// </summary>
        public void Stop()
        {
            StopAllMusicCoroutines();
            _fadeCoroutine = StartCoroutine(FadeOut(_primarySource, _fadeOutDuration));
        }

        /// <summary>
        /// Stop music immediately without fade.
        /// </summary>
        public void StopImmediate()
        {
            StopAllMusicCoroutines();
            _primarySource.Stop();
            _primarySource.volume = 0f;
            _secondarySource.Stop();
            _secondarySource.volume = 0f;
        }

        /// <summary>
        /// Pause the current track.
        /// </summary>
        public void Pause()
        {
            _primarySource.Pause();
        }

        /// <summary>
        /// Resume the paused track.
        /// </summary>
        public void Resume()
        {
            _primarySource.UnPause();
        }

        /// <summary>
        /// Fade volume to a new level.
        /// </summary>
        public void FadeToVolume(float targetVolume, float duration)
        {
            _targetVolume = Mathf.Clamp01(targetVolume);

            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
            }
            _fadeCoroutine = StartCoroutine(FadeVolumeCoroutine(_primarySource, _targetVolume, duration));
        }

        private void StopAllMusicCoroutines()
        {
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
                _fadeCoroutine = null;
            }
            if (_crossfadeCoroutine != null)
            {
                StopCoroutine(_crossfadeCoroutine);
                _crossfadeCoroutine = null;
            }
        }

        private IEnumerator FadeIn(AudioSource source, float duration)
        {
            source.volume = 0f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                source.volume = Mathf.Lerp(0f, _targetVolume, elapsed / duration);
                yield return null;
            }

            source.volume = _targetVolume;
            _fadeCoroutine = null;
        }

        private IEnumerator FadeOut(AudioSource source, float duration)
        {
            float startVolume = source.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                source.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
                yield return null;
            }

            source.volume = 0f;
            source.Stop();
            _fadeCoroutine = null;
        }

        private IEnumerator FadeVolumeCoroutine(AudioSource source, float targetVol, float duration)
        {
            float startVolume = source.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                source.volume = Mathf.Lerp(startVolume, targetVol, elapsed / duration);
                yield return null;
            }

            source.volume = targetVol;
            _volume = targetVol;
            _fadeCoroutine = null;
        }

        private IEnumerator CrossfadeCoroutine(AudioClip newTrack)
        {
            // Swap sources so secondary becomes the new primary
            var tempSource = _primarySource;
            _primarySource = _secondarySource;
            _secondarySource = tempSource;

            // Start new track on primary
            _primarySource.clip = newTrack;
            _primarySource.volume = 0f;
            _primarySource.Play();

            // Crossfade
            float elapsed = 0f;
            float startVolume = _secondarySource.volume;

            while (elapsed < _crossfadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / _crossfadeDuration;

                _primarySource.volume = Mathf.Lerp(0f, _targetVolume, t);
                _secondarySource.volume = Mathf.Lerp(startVolume, 0f, t);

                yield return null;
            }

            _primarySource.volume = _targetVolume;
            _secondarySource.volume = 0f;
            _secondarySource.Stop();

            _crossfadeCoroutine = null;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}
