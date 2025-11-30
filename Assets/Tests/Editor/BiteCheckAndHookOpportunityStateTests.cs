using NUnit.Framework;
using UnityEngine;
using Stillwater.Fishing;
using Stillwater.Fishing.States;

namespace Stillwater.Tests
{
    [TestFixture]
    public class BiteCheckStateTests
    {
        private class MockContext : IFishingContext
        {
            public FishingState CurrentState { get; set; }
            public float TimeInState { get; set; }
            public Vector2 LurePosition { get; set; }
            public Vector2 LureVelocity { get; set; }
            public float LineLength { get; set; }
            public float LineTension { get; set; }
            public bool CastInputPressed { get; set; }
            public bool ReelInputHeld { get; set; }
            public bool SlackInputPressed { get; set; }
            public bool CancelInputPressed { get; set; }
            public bool HasFishInterest { get; set; }
            public bool HasHookedFish { get; set; }
            public string HookedFishId { get; set; }
            public float FishStruggleIntensity { get; set; }
            public string CurrentZoneId { get; set; }
            public float BiteProbabilityModifier { get; set; }

            private float _randomValue = 0.5f;
            public void SetRandomValue(float value) => _randomValue = value;
            public float GetRandomValue() => _randomValue;
            public float GetRandomRange(float min, float max) => min + _randomValue * (max - min);
        }

        private BiteCheckState _state;
        private MockContext _context;

        [SetUp]
        public void SetUp()
        {
            // baseBiteProbability=0.5, checkDuration=0.3s, noBiteReturnChance=0.5, timeout=2s
            _state = new BiteCheckState(0.5f, 0.3f, 0.5f, 2f);
            _context = new MockContext
            {
                CurrentState = FishingState.BiteCheck,
                BiteProbabilityModifier = 0f
            };
        }

        [Test]
        public void Constructor_DefaultValues_CreatesValidState()
        {
            var state = new BiteCheckState();
            Assert.IsNotNull(state);
        }

        [Test]
        public void Constructor_CustomValues_CreatesValidState()
        {
            var state = new BiteCheckState(0.4f, 0.5f, 0.7f, 3f);
            Assert.IsNotNull(state);
        }

        [Test]
        public void Enter_ResetsState()
        {
            _state.Enter(_context);
            Assert.IsFalse(_state.BiteOccurred);
            Assert.IsFalse(_state.CheckComplete);
            Assert.AreEqual(0f, _state.ElapsedTime, 0.001f);
        }

        [Test]
        public void Update_BeforeCheckDuration_NoCheckPerformed()
        {
            _state.Enter(_context);
            _state.Update(_context, 0.2f); // Before 0.3s check duration

            Assert.IsFalse(_state.CheckComplete);
            Assert.IsNull(_state.GetNextState(_context));
        }

        [Test]
        public void Update_AtCheckDuration_PerformsBiteCheck()
        {
            _state.Enter(_context);
            _state.Update(_context, 0.3f); // At check duration

            Assert.IsTrue(_state.CheckComplete);
        }

        [Test]
        public void Update_LowRoll_BiteOccurs()
        {
            _state.Enter(_context);
            _context.SetRandomValue(0.2f); // Below 0.5 probability

            _state.Update(_context, 0.3f);

            Assert.IsTrue(_state.BiteOccurred);
            Assert.AreEqual(FishingState.HookOpportunity, _state.GetNextState(_context));
        }

        [Test]
        public void Update_HighRoll_NoBite()
        {
            _state.Enter(_context);
            _context.SetRandomValue(0.8f); // Above 0.5 probability

            _state.Update(_context, 0.3f);

            Assert.IsFalse(_state.BiteOccurred);
            // Will transition to Stillness or Idle based on second roll
        }

        [Test]
        public void Update_NoBite_LowSecondRoll_ReturnsToStillness()
        {
            _state.Enter(_context);
            _context.SetRandomValue(0.8f); // No bite (above 0.5)

            _state.Update(_context, 0.3f);

            // Now set low roll for return chance
            _context.SetRandomValue(0.3f); // Below 0.5 return chance

            Assert.AreEqual(FishingState.Stillness, _state.GetNextState(_context));
        }

        [Test]
        public void Update_NoBite_HighSecondRoll_GoesToIdle()
        {
            _state.Enter(_context);
            _context.SetRandomValue(0.8f); // No bite

            _state.Update(_context, 0.3f);

            // Set high roll for return chance
            _context.SetRandomValue(0.7f); // Above 0.5 return chance

            Assert.AreEqual(FishingState.Idle, _state.GetNextState(_context));
        }

        [Test]
        public void Update_Timeout_GoesToIdle()
        {
            _state.Enter(_context);
            _context.SetRandomValue(0.8f); // No bite

            _state.Update(_context, 2.5f); // Past 2s timeout

            // Timeout should always go to Idle regardless of other rolls
            Assert.AreEqual(FishingState.Idle, _state.GetNextState(_context));
        }

        [Test]
        public void BiteProbabilityModifier_IncreasesChance()
        {
            _state.Enter(_context);
            _context.BiteProbabilityModifier = 0.5f; // +50% modifier
            _context.SetRandomValue(0.6f); // Would normally be no bite at 0.5 base

            _state.Update(_context, 0.3f);

            // 0.5 * (1 + 0.5) = 0.75, roll 0.6 < 0.75
            Assert.IsTrue(_state.BiteOccurred);
            Assert.AreEqual(0.75f, _state.FinalBiteProbability, 0.001f);
        }

        [Test]
        public void BiteProbabilityModifier_DecreasesChance()
        {
            _state.Enter(_context);
            _context.BiteProbabilityModifier = -0.5f; // -50% modifier
            _context.SetRandomValue(0.3f); // Would normally be bite at 0.5 base

            _state.Update(_context, 0.3f);

            // 0.5 * (1 - 0.5) = 0.25, roll 0.3 > 0.25
            Assert.IsFalse(_state.BiteOccurred);
            Assert.AreEqual(0.25f, _state.FinalBiteProbability, 0.001f);
        }

        [Test]
        public void FinalBiteProbability_ClampedToOne()
        {
            _state.Enter(_context);
            _context.BiteProbabilityModifier = 2.0f; // Would give 1.5 probability

            _state.Update(_context, 0.3f);

            Assert.AreEqual(1f, _state.FinalBiteProbability, 0.001f);
        }

        [Test]
        public void Exit_DoesNotThrow()
        {
            _state.Enter(_context);
            _state.Update(_context, 0.3f);
            Assert.DoesNotThrow(() => _state.Exit(_context));
        }

        [Test]
        public void Reenter_ResetsState()
        {
            _state.Enter(_context);
            _context.SetRandomValue(0.2f);
            _state.Update(_context, 0.3f);
            Assert.IsTrue(_state.BiteOccurred);

            // Re-enter should reset
            _state.Enter(_context);
            Assert.IsFalse(_state.BiteOccurred);
            Assert.IsFalse(_state.CheckComplete);
        }
    }

    [TestFixture]
    public class HookOpportunityStateTests
    {
        private class MockContext : IFishingContext
        {
            public FishingState CurrentState { get; set; }
            public float TimeInState { get; set; }
            public Vector2 LurePosition { get; set; }
            public Vector2 LureVelocity { get; set; }
            public float LineLength { get; set; }
            public float LineTension { get; set; }
            public bool CastInputPressed { get; set; }
            public bool ReelInputHeld { get; set; }
            public bool SlackInputPressed { get; set; }
            public bool CancelInputPressed { get; set; }
            public bool HasFishInterest { get; set; }
            public bool HasHookedFish { get; set; }
            public string HookedFishId { get; set; }
            public float FishStruggleIntensity { get; set; }
            public string CurrentZoneId { get; set; }
            public float BiteProbabilityModifier { get; set; }
            public float GetRandomValue() => 0.5f;
            public float GetRandomRange(float min, float max) => (min + max) / 2f;
        }

        private HookOpportunityState _state;
        private MockContext _context;

        [SetUp]
        public void SetUp()
        {
            // windowDuration=0.8s, earlyPenaltyWindow=0.1s
            _state = new HookOpportunityState(0.8f, 0.1f);
            _context = new MockContext { CurrentState = FishingState.HookOpportunity };
        }

        [Test]
        public void Constructor_DefaultValues_CreatesValidState()
        {
            var state = new HookOpportunityState();
            Assert.IsNotNull(state);
        }

        [Test]
        public void Constructor_CustomValues_CreatesValidState()
        {
            var state = new HookOpportunityState(1.0f, 0.2f);
            Assert.IsNotNull(state);
        }

        [Test]
        public void Enter_ResetsState()
        {
            _state.Enter(_context);
            Assert.IsFalse(_state.HookInputReceived);
            Assert.IsFalse(_state.WindowExpired);
            Assert.IsFalse(_state.EarlyInputPenalty);
            Assert.AreEqual(0f, _state.ElapsedTime, 0.001f);
        }

        [Test]
        public void Update_NoInput_NoTransition()
        {
            _state.Enter(_context);
            _state.Update(_context, 0.3f);

            Assert.IsNull(_state.GetNextState(_context));
        }

        [Test]
        public void Update_WindowExpires_TransitionsToLost()
        {
            _state.Enter(_context);
            _state.Update(_context, 0.9f); // Past 0.8s window

            Assert.IsTrue(_state.WindowExpired);
            Assert.AreEqual(FishingState.Lost, _state.GetNextState(_context));
        }

        [Test]
        public void Update_ValidInput_TransitionsToHooked()
        {
            _state.Enter(_context);
            _state.Update(_context, 0.3f); // Within window, past early penalty
            _context.CastInputPressed = true;
            _state.Update(_context, 0.016f);

            Assert.IsTrue(_state.HookInputReceived);
            Assert.IsFalse(_state.EarlyInputPenalty);
            Assert.AreEqual(FishingState.Hooked, _state.GetNextState(_context));
        }

        [Test]
        public void Update_EarlyInput_TransitionsToLost()
        {
            _state.Enter(_context);
            _context.CastInputPressed = true;
            _state.Update(_context, 0.05f); // Within early penalty window (0.1s)

            Assert.IsTrue(_state.HookInputReceived);
            Assert.IsTrue(_state.EarlyInputPenalty);
            Assert.AreEqual(FishingState.Lost, _state.GetNextState(_context));
        }

        [Test]
        public void Update_InputJustAfterEarlyWindow_TransitionsToHooked()
        {
            _state.Enter(_context);
            _state.Update(_context, 0.15f); // Just past early window
            _context.CastInputPressed = true;
            _state.Update(_context, 0.016f);

            Assert.IsTrue(_state.HookInputReceived);
            Assert.IsFalse(_state.EarlyInputPenalty);
            Assert.AreEqual(FishingState.Hooked, _state.GetNextState(_context));
        }

        [Test]
        public void Update_InputAtEndOfWindow_TransitionsToHooked()
        {
            _state.Enter(_context);
            _state.Update(_context, 0.75f); // Near end of 0.8s window
            _context.CastInputPressed = true;
            _state.Update(_context, 0.016f);

            Assert.IsTrue(_state.HookInputReceived);
            Assert.AreEqual(FishingState.Hooked, _state.GetNextState(_context));
        }

        [Test]
        public void WindowProgress_CalculatedCorrectly()
        {
            _state.Enter(_context);
            Assert.AreEqual(0f, _state.WindowProgress, 0.001f);

            _state.Update(_context, 0.4f); // Half of 0.8s
            Assert.AreEqual(0.5f, _state.WindowProgress, 0.001f);

            _state.Update(_context, 0.4f); // Full window
            Assert.AreEqual(1f, _state.WindowProgress, 0.001f);
        }

        [Test]
        public void WindowProgress_ClampedToOne()
        {
            _state.Enter(_context);
            _state.Update(_context, 1.0f); // Past window

            Assert.AreEqual(1f, _state.WindowProgress, 0.001f);
        }

        [Test]
        public void Update_InputReceivedThenExpires_StaysHooked()
        {
            _state.Enter(_context);
            _state.Update(_context, 0.3f);
            _context.CastInputPressed = true;
            _state.Update(_context, 0.016f);

            Assert.IsTrue(_state.HookInputReceived);

            // Continue updating past window
            _context.CastInputPressed = false;
            _state.Update(_context, 0.6f);

            // Should still be Hooked, not Lost
            Assert.AreEqual(FishingState.Hooked, _state.GetNextState(_context));
        }

        [Test]
        public void Exit_DoesNotThrow()
        {
            _state.Enter(_context);
            _context.CastInputPressed = true;
            _state.Update(_context, 0.3f);
            Assert.DoesNotThrow(() => _state.Exit(_context));
        }

        [Test]
        public void Reenter_ResetsState()
        {
            _state.Enter(_context);
            _state.Update(_context, 0.9f); // Window expired

            Assert.IsTrue(_state.WindowExpired);

            // Re-enter should reset
            _state.Enter(_context);
            Assert.IsFalse(_state.WindowExpired);
            Assert.IsFalse(_state.HookInputReceived);
        }

        [Test]
        public void ZeroEarlyPenaltyWindow_AllInputsValid()
        {
            var state = new HookOpportunityState(0.8f, 0f);
            state.Enter(_context);
            _context.CastInputPressed = true;
            state.Update(_context, 0.001f); // Very early

            Assert.IsTrue(state.HookInputReceived);
            Assert.IsFalse(state.EarlyInputPenalty);
            Assert.AreEqual(FishingState.Hooked, state.GetNextState(_context));
        }
    }

    [TestFixture]
    public class BiteCheckToHookIntegrationTests
    {
        private class MockContext : IFishingContext
        {
            public FishingState CurrentState { get; set; }
            public float TimeInState { get; set; }
            public Vector2 LurePosition { get; set; }
            public Vector2 LureVelocity { get; set; }
            public float LineLength { get; set; }
            public float LineTension { get; set; }
            public bool CastInputPressed { get; set; }
            public bool ReelInputHeld { get; set; }
            public bool SlackInputPressed { get; set; }
            public bool CancelInputPressed { get; set; }
            public bool HasFishInterest { get; set; }
            public bool HasHookedFish { get; set; }
            public string HookedFishId { get; set; }
            public float FishStruggleIntensity { get; set; }
            public string CurrentZoneId { get; set; }
            public float BiteProbabilityModifier { get; set; }

            private float _randomValue = 0.5f;
            public void SetRandomValue(float value) => _randomValue = value;
            public float GetRandomValue() => _randomValue;
            public float GetRandomRange(float min, float max) => min + _randomValue * (max - min);
        }

        [Test]
        public void FullFlow_StillnessToBiteCheckToHookToHooked()
        {
            var context = new MockContext();
            var stillnessState = new StillnessState(2f);
            var biteCheckState = new BiteCheckState(0.8f, 0.3f, 0.5f, 5f);
            var hookOpportunityState = new HookOpportunityState(0.8f, 0.1f);

            // Start in Stillness
            context.CurrentState = FishingState.Stillness;
            stillnessState.Enter(context);
            stillnessState.Update(context, 2.0f);
            Assert.AreEqual(FishingState.BiteCheck, stillnessState.GetNextState(context));

            // Transition to BiteCheck
            stillnessState.Exit(context);
            context.CurrentState = FishingState.BiteCheck;
            context.SetRandomValue(0.2f); // Low roll = bite occurs
            biteCheckState.Enter(context);
            biteCheckState.Update(context, 0.3f);
            Assert.AreEqual(FishingState.HookOpportunity, biteCheckState.GetNextState(context));

            // Transition to HookOpportunity
            biteCheckState.Exit(context);
            context.CurrentState = FishingState.HookOpportunity;
            hookOpportunityState.Enter(context);

            // Wait, then input
            hookOpportunityState.Update(context, 0.3f);
            context.CastInputPressed = true;
            hookOpportunityState.Update(context, 0.016f);

            Assert.AreEqual(FishingState.Hooked, hookOpportunityState.GetNextState(context));
        }

        [Test]
        public void FullFlow_BiteCheckNoBite_ReturnsToStillness()
        {
            var context = new MockContext();
            var biteCheckState = new BiteCheckState(0.5f, 0.3f, 1.0f, 5f); // 100% return to stillness

            context.CurrentState = FishingState.BiteCheck;
            context.SetRandomValue(0.8f); // High roll = no bite
            biteCheckState.Enter(context);
            biteCheckState.Update(context, 0.3f);

            Assert.IsFalse(biteCheckState.BiteOccurred);
            Assert.AreEqual(FishingState.Stillness, biteCheckState.GetNextState(context));
        }

        [Test]
        public void FullFlow_HookOpportunityMissed_GoesToLost()
        {
            var context = new MockContext();
            var hookOpportunityState = new HookOpportunityState(0.5f, 0.1f);

            context.CurrentState = FishingState.HookOpportunity;
            hookOpportunityState.Enter(context);

            // Let window expire
            hookOpportunityState.Update(context, 0.6f);

            Assert.AreEqual(FishingState.Lost, hookOpportunityState.GetNextState(context));
        }

        [Test]
        public void FullFlow_HookOpportunityTooEarly_GoesToLost()
        {
            var context = new MockContext();
            var hookOpportunityState = new HookOpportunityState(0.8f, 0.2f);

            context.CurrentState = FishingState.HookOpportunity;
            hookOpportunityState.Enter(context);

            // Input too early
            context.CastInputPressed = true;
            hookOpportunityState.Update(context, 0.05f);

            Assert.AreEqual(FishingState.Lost, hookOpportunityState.GetNextState(context));
        }
    }
}
