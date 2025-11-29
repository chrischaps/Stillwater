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
    /// or on a DontDestroyOnLoad object. It self-registers with ServiceLocator on Awake.
    /// </summary>
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

        [Header("Debug")]
        [SerializeField] private bool _logInput;

        private void Awake()
        {
            // Self-register with ServiceLocator (MonoBehaviours can't use [ServiceDefault])
            if (ServiceLocator.IsRegistered<IInputService>())
            {
                Debug.LogWarning("[InputService] IInputService already registered, destroying duplicate.");
                Destroy(this);
                return;
            }
            ServiceLocator.Register<IInputService>(this);

            if (_inputActions == null)
            {
                Debug.LogError("[InputService] Input Actions asset not assigned!");
                return;
            }

            // Clone the input actions so we have an independent copy
            // This prevents other components from interfering with our action states
            _inputActions = Instantiate(_inputActions);

            InitializeActions();
            Debug.Log("[InputService] Initialized successfully (using cloned InputActions)");
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
            // Intentionally empty - don't disable maps during scene transitions
            // This service persists across scenes via DontDestroyOnLoad
        }

        private void OnDestroy()
        {
            // Disable maps on actual destruction
            _gameplayMap?.Disable();
            _uiMap?.Disable();

            UnbindCallbacks();

            // Unregister from ServiceLocator
            if (ServiceLocator.TryGet<IInputService>(out var registered) && registered == this)
            {
                ServiceLocator.Unregister<IInputService>();
            }
        }

        public void EnableGameplayInput()
        {
            _uiMap?.Disable();
            _gameplayMap?.Enable();
            Debug.Log("[InputService] Switched to Gameplay input map");
        }

        public void EnableUIInput()
        {
            _gameplayMap?.Disable();
            _uiMap?.Enable();
            Debug.Log("[InputService] Switched to UI input map");
        }

        private void Update()
        {
            // Poll input directly (more reliable than callbacks for continuous input)
            if (_moveAction != null)
            {
                _moveInput = _moveAction.ReadValue<Vector2>();
            }

            if (_logInput && _moveInput.sqrMagnitude > 0.01f)
            {
                Debug.Log($"[InputService] Move: {_moveInput}");
            }
        }

        private void OnMove(InputAction.CallbackContext context)
        {
            _moveInput = context.ReadValue<Vector2>();
            if (_logInput && _moveInput.sqrMagnitude > 0.01f)
            {
                Debug.Log($"[InputService] Move input (callback): {_moveInput}");
            }
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
