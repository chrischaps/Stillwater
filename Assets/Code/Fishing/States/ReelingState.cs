using UnityEngine;

namespace Stillwater.Fishing.States
{
    /// <summary>
    /// The reeling state where the player fights to land the hooked fish.
    /// Manages line tension through input timing - too much tension breaks the line,
    /// too little lets the fish escape. May trigger SlackEvent requiring input release.
    /// </summary>
    public class ReelingState : IFishingState
    {
        private readonly float _tensionIncreaseRate;
        private readonly float _tensionDecreaseRate;
        private readonly float _maxTension;
        private readonly float _progressPerSecond;
        private readonly float _slackEventChance;
        private readonly float _slackEventCheckInterval;
        private readonly float _fishEscapeThreshold;

        private float _currentTension;
        private float _reelProgress;
        private float _timeSinceSlackCheck;
        private bool _slackEventTriggered;
        private bool _lineSnapped;
        private bool _fishEscaped;
        private bool _fishCaught;

        /// <summary>
        /// Current line tension (0-1, where 1 is max/snapping point).
        /// </summary>
        public float CurrentTension => _currentTension;

        /// <summary>
        /// Progress toward landing the fish (0-1).
        /// </summary>
        public float ReelProgress => _reelProgress;

        /// <summary>
        /// Whether a slack event has been triggered requiring input release.
        /// </summary>
        public bool SlackEventTriggered => _slackEventTriggered;

        /// <summary>
        /// Whether the line has snapped due to excess tension.
        /// </summary>
        public bool LineSnapped => _lineSnapped;

        /// <summary>
        /// Whether the fish escaped due to low tension.
        /// </summary>
        public bool FishEscaped => _fishEscaped;

        /// <summary>
        /// Whether the fish has been successfully caught.
        /// </summary>
        public bool FishCaught => _fishCaught;

        /// <summary>
        /// Creates a new ReelingState with default settings.
        /// </summary>
        public ReelingState() : this(0.5f, 0.3f, 1.0f, 0.2f, 0.15f, 2.0f, 0.1f)
        {
        }

        /// <summary>
        /// Creates a new ReelingState with custom settings.
        /// </summary>
        /// <param name="tensionIncreaseRate">Rate tension increases while reeling (per second).</param>
        /// <param name="tensionDecreaseRate">Rate tension decreases when not reeling (per second).</param>
        /// <param name="maxTension">Maximum tension before line snaps.</param>
        /// <param name="progressPerSecond">Reel progress gained per second while reeling.</param>
        /// <param name="slackEventChance">Chance per check interval to trigger slack event.</param>
        /// <param name="slackEventCheckInterval">Seconds between slack event checks.</param>
        /// <param name="fishEscapeThreshold">Tension below which fish may escape.</param>
        public ReelingState(
            float tensionIncreaseRate,
            float tensionDecreaseRate,
            float maxTension,
            float progressPerSecond,
            float slackEventChance,
            float slackEventCheckInterval,
            float fishEscapeThreshold)
        {
            _tensionIncreaseRate = Mathf.Max(0.1f, tensionIncreaseRate);
            _tensionDecreaseRate = Mathf.Max(0.1f, tensionDecreaseRate);
            _maxTension = Mathf.Max(0.1f, maxTension);
            _progressPerSecond = Mathf.Max(0.01f, progressPerSecond);
            _slackEventChance = Mathf.Clamp01(slackEventChance);
            _slackEventCheckInterval = Mathf.Max(0.5f, slackEventCheckInterval);
            _fishEscapeThreshold = Mathf.Clamp(fishEscapeThreshold, 0f, _maxTension * 0.5f);
        }

        /// <summary>
        /// Called when entering the reeling state.
        /// Resets tension and progress.
        /// </summary>
        public void Enter(IFishingContext context)
        {
            _currentTension = _maxTension * 0.3f; // Start at 30% tension
            _reelProgress = 0f;
            _timeSinceSlackCheck = 0f;
            _slackEventTriggered = false;
            _lineSnapped = false;
            _fishEscaped = false;
            _fishCaught = false;
        }

        /// <summary>
        /// Called every frame while reeling.
        /// Manages tension based on input and fish struggle.
        /// </summary>
        public void Update(IFishingContext context, float deltaTime)
        {
            // Don't update if already resolved
            if (_lineSnapped || _fishEscaped || _fishCaught)
            {
                return;
            }

            // Handle slack event
            if (_slackEventTriggered)
            {
                HandleSlackEvent(context, deltaTime);
                return;
            }

            // Update tension based on input
            UpdateTension(context, deltaTime);

            // Check for line snap
            if (_currentTension >= _maxTension)
            {
                _lineSnapped = true;
                return;
            }

            // Check for fish escape (tension too low for too long)
            if (_currentTension <= _fishEscapeThreshold && !context.ReelInputHeld)
            {
                // Give chance for escape based on how low tension is
                float escapeChance = 1f - (_currentTension / _fishEscapeThreshold);
                if (context.GetRandomValue() < escapeChance * deltaTime)
                {
                    _fishEscaped = true;
                    return;
                }
            }

            // Update progress if reeling
            if (context.ReelInputHeld && _currentTension < _maxTension * 0.9f)
            {
                _reelProgress += _progressPerSecond * deltaTime;
                _reelProgress = Mathf.Clamp01(_reelProgress);

                if (_reelProgress >= 1f)
                {
                    _fishCaught = true;
                    return;
                }
            }

            // Check for slack event trigger
            CheckSlackEvent(context, deltaTime);
        }

        private void UpdateTension(IFishingContext context, float deltaTime)
        {
            if (context.ReelInputHeld)
            {
                // Reeling increases tension
                float increase = _tensionIncreaseRate * deltaTime;

                // Fish struggle adds extra tension
                increase += context.FishStruggleIntensity * _tensionIncreaseRate * deltaTime * 0.5f;

                _currentTension += increase;
            }
            else
            {
                // Not reeling decreases tension
                _currentTension -= _tensionDecreaseRate * deltaTime;
            }

            _currentTension = Mathf.Clamp(_currentTension, 0f, _maxTension);
        }

        private void CheckSlackEvent(IFishingContext context, float deltaTime)
        {
            _timeSinceSlackCheck += deltaTime;

            if (_timeSinceSlackCheck >= _slackEventCheckInterval)
            {
                _timeSinceSlackCheck = 0f;

                // Higher tension increases slack event chance
                float tensionModifier = _currentTension / _maxTension;
                float adjustedChance = _slackEventChance * (0.5f + tensionModifier);

                if (context.GetRandomValue() < adjustedChance)
                {
                    _slackEventTriggered = true;
                }
            }
        }

        private void HandleSlackEvent(IFishingContext context, float deltaTime)
        {
            // Player must release reel input to clear slack event
            if (!context.ReelInputHeld)
            {
                _slackEventTriggered = false;
                // Bonus: successfully handling slack reduces tension
                _currentTension = Mathf.Max(0f, _currentTension - _maxTension * 0.1f);
            }
            else
            {
                // Continuing to reel during slack event increases tension rapidly
                _currentTension += _tensionIncreaseRate * 2f * deltaTime;
                if (_currentTension >= _maxTension)
                {
                    _lineSnapped = true;
                }
            }
        }

        /// <summary>
        /// Called when exiting the reeling state.
        /// </summary>
        public void Exit(IFishingContext context)
        {
            // Reeling complete
        }

        /// <summary>
        /// Returns the next state based on reeling outcome:
        /// - Caught if fish successfully landed
        /// - Lost if line snapped or fish escaped
        /// - SlackEvent if slack event triggered (handled internally, transitions to Lost on snap)
        /// - null if still reeling
        /// </summary>
        public FishingState? GetNextState(IFishingContext context)
        {
            if (_fishCaught)
            {
                return FishingState.Caught;
            }

            if (_lineSnapped || _fishEscaped)
            {
                return FishingState.Lost;
            }

            return null;
        }
    }
}
