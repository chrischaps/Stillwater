using UnityEngine;
using UnityEngine.InputSystem;
using Stillwater.Framework;

namespace Stillwater.Core
{
    /// <summary>
    /// Implementation of IInputService that wraps Unity's Input System.
    /// Reads from StillwaterInput actions and publishes events via EventBus.
    ///
    /// This MonoBehaviour should be placed in a scene that persists (e.g., Boot scene)
    /// or on a DontDestroyOnLoad object.
    /// </summary>
    [ServiceDefault]
    public class InputService : MonoBehaviour, IInputService
    {
        [SerializeField] private InputActionAsset _inputActions;

        private InputActionMap _gameplayMap;
        private InputActionMap _uiMap;

        private InputAction _moveAction;
        private InputAction _castAction;
        private InputAction _reelAction;
        private InputAction _slackAction;
        private InputAction _interactAction;
        private InputAction _cancelAction;

        private Vector2 _moveInput;
        private bool _isReeling;
        private bool _inputEnabled = true;

        public Vector2 MoveInput => _inputEnabled ? _moveInput : Vector2.zero;
        public bool IsReeling => _inputEnabled && _isReeling;

        public bool InputEnabled
        {
            get => _inputEnabled;
            set => _inputEnabled = value;
        }

        private void Awake()
        {
            if (_inputActions == null)
            {
                Debug.LogError("[InputService] Input Actions asset not assigned!");
                return;
            }

            InitializeActions();
        }

        private void InitializeActions()
        {
            _gameplayMap = _inputActions.FindActionMap("Gameplay");
            _uiMap = _inputActions.FindActionMap("UI");

            if (_gameplayMap == null)
            {
                Debug.LogError("[InputService] Gameplay action map not found!");
                return;
            }

            _moveAction = _gameplayMap.FindAction("Move");
            _castAction = _gameplayMap.FindAction("Cast");
            _reelAction = _gameplayMap.FindAction("Reel");
            _slackAction = _gameplayMap.FindAction("Slack");
            _interactAction = _gameplayMap.FindAction("Interact");
            _cancelAction = _gameplayMap.FindAction("Cancel");

            BindCallbacks();
        }

        private void BindCallbacks()
        {
            if (_moveAction != null)
            {
                _moveAction.performed += OnMove;
                _moveAction.canceled += OnMove;
            }

            if (_castAction != null)
            {
                _castAction.performed += OnCast;
            }

            if (_reelAction != null)
            {
                _reelAction.started += OnReelStarted;
                _reelAction.canceled += OnReelCanceled;
            }

            if (_slackAction != null)
            {
                _slackAction.performed += OnSlack;
            }

            if (_interactAction != null)
            {
                _interactAction.performed += OnInteract;
            }

            if (_cancelAction != null)
            {
                _cancelAction.performed += OnCancel;
            }
        }

        private void UnbindCallbacks()
        {
            if (_moveAction != null)
            {
                _moveAction.performed -= OnMove;
                _moveAction.canceled -= OnMove;
            }

            if (_castAction != null)
            {
                _castAction.performed -= OnCast;
            }

            if (_reelAction != null)
            {
                _reelAction.started -= OnReelStarted;
                _reelAction.canceled -= OnReelCanceled;
            }

            if (_slackAction != null)
            {
                _slackAction.performed -= OnSlack;
            }

            if (_interactAction != null)
            {
                _interactAction.performed -= OnInteract;
            }

            if (_cancelAction != null)
            {
                _cancelAction.performed -= OnCancel;
            }
        }

        private void OnEnable()
        {
            EnableGameplayInput();
        }

        private void OnDisable()
        {
            _gameplayMap?.Disable();
            _uiMap?.Disable();
        }

        private void OnDestroy()
        {
            UnbindCallbacks();
        }

        public void EnableGameplayInput()
        {
            _uiMap?.Disable();
            _gameplayMap?.Enable();
        }

        public void EnableUIInput()
        {
            _gameplayMap?.Disable();
            _uiMap?.Enable();
        }

        private void OnMove(InputAction.CallbackContext context)
        {
            _moveInput = context.ReadValue<Vector2>();
        }

        private void OnCast(InputAction.CallbackContext context)
        {
            if (!_inputEnabled) return;
            EventBus.Publish(new CastInputEvent());
        }

        private void OnReelStarted(InputAction.CallbackContext context)
        {
            _isReeling = true;
            if (!_inputEnabled) return;
            EventBus.Publish(new ReelStartedEvent());
        }

        private void OnReelCanceled(InputAction.CallbackContext context)
        {
            _isReeling = false;
            if (!_inputEnabled) return;
            EventBus.Publish(new ReelEndedEvent());
        }

        private void OnSlack(InputAction.CallbackContext context)
        {
            if (!_inputEnabled) return;
            EventBus.Publish(new SlackInputEvent());
        }

        private void OnInteract(InputAction.CallbackContext context)
        {
            if (!_inputEnabled) return;
            EventBus.Publish(new InteractInputEvent());
        }

        private void OnCancel(InputAction.CallbackContext context)
        {
            if (!_inputEnabled) return;
            EventBus.Publish(new CancelInputEvent());
        }
    }
}
