using Stillwater.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Stillwater.UI
{
    /// <summary>
    /// Controller for the main menu UI.
    /// Handles start game and exit button interactions.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private string gameSceneName = "Main Base";

        private Button _startButton;
        private Button _exitButton;

        private void OnEnable()
        {
            if (uiDocument == null)
            {
                uiDocument = GetComponent<UIDocument>();
            }

            var root = uiDocument.rootVisualElement;

            _startButton = root.Q<Button>("start-button");
            _exitButton = root.Q<Button>("exit-button");

            _startButton?.RegisterCallback<ClickEvent>(OnStartClicked);
            _exitButton?.RegisterCallback<ClickEvent>(OnExitClicked);
        }

        private void OnDisable()
        {
            _startButton?.UnregisterCallback<ClickEvent>(OnStartClicked);
            _exitButton?.UnregisterCallback<ClickEvent>(OnExitClicked);
        }

        private void OnStartClicked(ClickEvent evt)
        {
            Debug.Log("Starting game...");
            GameRoot.Instance.StartGame();
        }

        private void OnExitClicked(ClickEvent evt)
        {
            Debug.Log("Exiting game...");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
