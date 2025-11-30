using UnityEngine;

namespace Stillwater.Fishing.States
{
    /// <summary>
    /// The bite check state where fish interest is evaluated.
    /// Determines if a fish bites based on probability modified by stillness, mood, and zone.
    /// Transitions to HookOpportunity on bite, Stillness on no bite, or Idle on timeout.
    /// </summary>
    public class BiteCheckState : IFishingState
    {
        private readonly float _baseBiteProbability;
        private readonly float _checkDuration;
        private readonly float _noBiteReturnToStillnessChance;
        private readonly float _timeoutDuration;

        private float _elapsedTime;
        private bool _biteOccurred;
        private bool _checkComplete;
        private bool _timedOut;
        private float _finalBiteProbability;

        /// <summary>
        /// Whether a bite occurred during this check.
        /// </summary>
        public bool BiteOccurred => _biteOccurred;

        /// <summary>
        /// Whether the bite check is complete.
        /// </summary>
        public bool CheckComplete => _checkComplete;

        /// <summary>
        /// The final calculated bite probability (for debugging/UI).
        /// </summary>
        public float FinalBiteProbability => _finalBiteProbability;

        /// <summary>
        /// Elapsed time in this state.
        /// </summary>
        public float ElapsedTime => _elapsedTime;

        /// <summary>
        /// Creates a new BiteCheckState with default settings.
        /// </summary>
        public BiteCheckState() : this(0.3f, 0.5f, 0.6f, 5f)
        {
        }

        /// <summary>
        /// Creates a new BiteCheckState with custom settings.
        /// </summary>
        /// <param name="baseBiteProbability">Base chance of a bite occurring (0-1).</param>
        /// <param name="checkDuration">Time before bite check is evaluated.</param>
        /// <param name="noBiteReturnToStillnessChance">Chance to return to Stillness instead of Idle on no bite (0-1).</param>
        /// <param name="timeoutDuration">Maximum time before forcing transition to Idle.</param>
        public BiteCheckState(float baseBiteProbability, float checkDuration, float noBiteReturnToStillnessChance, float timeoutDuration)
        {
            _baseBiteProbability = Mathf.Clamp01(baseBiteProbability);
            _checkDuration = Mathf.Max(0.1f, checkDuration);
            _noBiteReturnToStillnessChance = Mathf.Clamp01(noBiteReturnToStillnessChance);
            _timeoutDuration = Mathf.Max(_checkDuration, timeoutDuration);
        }

        /// <summary>
        /// Called when entering the bite check state.
        /// Resets state and prepares for bite evaluation.
        /// </summary>
        public void Enter(IFishingContext context)
        {
            _elapsedTime = 0f;
            _biteOccurred = false;
            _checkComplete = false;
            _timedOut = false;
            _finalBiteProbability = 0f;
        }

        /// <summary>
        /// Called every frame while in bite check.
        /// Evaluates bite probability after check duration.
        /// </summary>
        public void Update(IFishingContext context, float deltaTime)
        {
            _elapsedTime += deltaTime;

            // Check for timeout first
            if (_elapsedTime >= _timeoutDuration)
            {
                _timedOut = true;
                _checkComplete = true;
                return;
            }

            // Perform bite check after duration
            if (!_checkComplete && _elapsedTime >= _checkDuration)
            {
                PerformBiteCheck(context);
            }
        }

        /// <summary>
        /// Evaluates whether a bite occurs based on probability.
        /// </summary>
        private void PerformBiteCheck(IFishingContext context)
        {
            // Calculate final bite probability with modifiers
            _finalBiteProbability = _baseBiteProbability * (1f + context.BiteProbabilityModifier);
            _finalBiteProbability = Mathf.Clamp01(_finalBiteProbability);

            // Roll for bite
            float roll = context.GetRandomValue();
            _biteOccurred = roll < _finalBiteProbability;
            _checkComplete = true;
        }

        /// <summary>
        /// Called when exiting the bite check state.
        /// </summary>
        public void Exit(IFishingContext context)
        {
            // Bite check complete
        }

        /// <summary>
        /// Returns the next state based on bite check results:
        /// - HookOpportunity if bite occurred
        /// - Idle if timed out
        /// - Stillness or Idle if no bite (based on chance)
        /// - null if check not complete
        /// </summary>
        public FishingState? GetNextState(IFishingContext context)
        {
            if (!_checkComplete)
            {
                return null;
            }

            // Timeout always goes to Idle
            if (_timedOut)
            {
                return FishingState.Idle;
            }

            // Bite occurred - go to hook opportunity
            if (_biteOccurred)
            {
                return FishingState.HookOpportunity;
            }

            // No bite - chance to return to Stillness or go to Idle
            float roll = context.GetRandomValue();
            if (roll < _noBiteReturnToStillnessChance)
            {
                return FishingState.Stillness;
            }

            return FishingState.Idle;
        }
    }
}
