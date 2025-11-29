using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Stillwater.Framework;

namespace Stillwater.Core
{
    /// <summary>
    /// The root bootstrap object that initializes the game.
    /// Lives in the Boot scene and persists across scene loads.
    ///
    /// Responsibilities:
    /// - Initialize and register core services with ServiceLocator
    /// - Handle scene loading flow (Boot → Title → Main)
    /// - Persist across scene transitions (DontDestroyOnLoad)
    ///
    /// Usage:
    /// <code>
    /// // Check if game is ready
    /// if (GameRoot.IsInitialized)
    /// {
    ///     // Safe to access services
    /// }
    ///
    /// // Load a scene
    /// GameRoot.Instance.LoadScene("Main");
    /// </code>
    /// </summary>
    public class GameRoot : MonoBehaviour
    {
        private static GameRoot _instance;

        /// <summary>
        /// Singleton instance of GameRoot.
        /// </summary>
        public static GameRoot Instance => _instance;

        /// <summary>
        /// Whether the game has been fully initialized and services are ready.
        /// </summary>
        public static bool IsInitialized { get; private set; }

        [Header("Scene Names")]
        [SerializeField] private string _titleSceneName = "Title";
        [SerializeField] private string _mainSceneName = "Main";

        [Header("Debug")]
        [SerializeField] private bool _skipToMain;
        [SerializeField] private bool _logInitialization = true;

        private void Awake()
        {
            // Singleton pattern with enforcement
            if (_instance != null && _instance != this)
            {
                Log("Duplicate GameRoot detected, destroying...");
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            Initialize();
        }

        private void Initialize()
        {
            Log("Initializing GameRoot...");

            // Clear any stale state from previous sessions (useful for domain reloads in editor)
            ServiceLocator.Clear();
            EventBus.Clear();

            // Register core services
            RegisterServices();

            // Publish initialization event
            EventBus.Publish(new GameInitializedEvent());

            IsInitialized = true;
            Log("GameRoot initialization complete.");

            // Start the game flow
            if (_skipToMain)
            {
                LoadScene(_mainSceneName);
            }
            else
            {
                LoadScene(_titleSceneName);
            }
        }

        private void RegisterServices()
        {
            // EventBus is static, no registration needed

            // Auto-discover and register all services marked with [ServiceDefault]
            int count = ServiceLocator.RegisterAllDefaults();
            Log($"Auto-registered {count} service(s).");

            // Manual registrations can still be added here if needed:
            // ServiceLocator.Register<ISpecialService>(new SpecialService(customConfig));

            Log("Core services registered.");
        }

        /// <summary>
        /// Load a scene by name, replacing the current scene.
        /// </summary>
        /// <param name="sceneName">The name of the scene to load.</param>
        public void LoadScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogWarning("[GameRoot] Cannot load scene: scene name is null or empty.");
                return;
            }

            Log($"Loading scene: {sceneName}");
            StartCoroutine(LoadSceneAsync(sceneName, LoadSceneMode.Single));
        }

        /// <summary>
        /// Load a scene additively (on top of current scenes).
        /// </summary>
        /// <param name="sceneName">The name of the scene to load.</param>
        public void LoadSceneAdditive(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogWarning("[GameRoot] Cannot load scene: scene name is null or empty.");
                return;
            }

            Log($"Loading scene additively: {sceneName}");
            StartCoroutine(LoadSceneAsync(sceneName, LoadSceneMode.Additive));
        }

        /// <summary>
        /// Unload a scene by name.
        /// </summary>
        /// <param name="sceneName">The name of the scene to unload.</param>
        public void UnloadScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogWarning("[GameRoot] Cannot unload scene: scene name is null or empty.");
                return;
            }

            Log($"Unloading scene: {sceneName}");
            StartCoroutine(UnloadSceneAsync(sceneName));
        }

        private IEnumerator LoadSceneAsync(string sceneName, LoadSceneMode mode)
        {
            EventBus.Publish(new SceneLoadStartedEvent { SceneName = sceneName });

            var operation = SceneManager.LoadSceneAsync(sceneName, mode);
            if (operation == null)
            {
                Debug.LogError($"[GameRoot] Failed to start loading scene: {sceneName}");
                yield break;
            }

            while (!operation.isDone)
            {
                yield return null;
            }

            Log($"Scene loaded: {sceneName}");
            EventBus.Publish(new SceneLoadCompletedEvent { SceneName = sceneName });
        }

        private IEnumerator UnloadSceneAsync(string sceneName)
        {
            var operation = SceneManager.UnloadSceneAsync(sceneName);
            if (operation == null)
            {
                Debug.LogWarning($"[GameRoot] Failed to start unloading scene: {sceneName}");
                yield break;
            }

            while (!operation.isDone)
            {
                yield return null;
            }

            Log($"Scene unloaded: {sceneName}");
        }

        /// <summary>
        /// Transition to the main game scene.
        /// </summary>
        public void StartGame()
        {
            LoadScene(_mainSceneName);
        }

        /// <summary>
        /// Return to the title screen.
        /// </summary>
        public void ReturnToTitle()
        {
            LoadScene(_titleSceneName);
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                IsInitialized = false;
                _instance = null;

                // Clean up services
                ServiceLocator.Clear();
                EventBus.Clear();

                Log("GameRoot destroyed, services cleared.");
            }
        }

        private void Log(string message)
        {
            if (_logInitialization)
            {
                Debug.Log($"[GameRoot] {message}");
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor-only method to reset static state (useful for testing).
        /// </summary>
        public static void ResetForTesting()
        {
            IsInitialized = false;
            _instance = null;
            ServiceLocator.Clear();
            EventBus.Clear();
        }
#endif
    }
}
