using NUnit.Framework;
using UnityEngine;
using Stillwater.Fishing;
using Stillwater.Fishing.States;

namespace Stillwater.Tests
{
    [TestFixture]
    public class LureDriftStateTests
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

        private LureDriftState _state;
        private MockContext _context;

        [SetUp]
        public void SetUp()
        {
            _state = new LureDriftState(0.1f, 0.5f); // velocity threshold 0.1, min drift 0.5s
            _context = new MockContext
            {
                CurrentState = FishingState.LureDrift,
                LureVelocity = new Vector2(1f, 0f) // Moving initially
            };
        }

        [Test]
        public void Constructor_DefaultValues_CreatesValidState()
        {
            var state = new LureDriftState();
            Assert.IsNotNull(state);
        }

        [Test]
        public void Constructor_CustomValues_CreatesValidState()
        {
            var state = new LureDriftState(0.2f, 1.0f);
            Assert.IsNotNull(state);
        }

        [Test]
        public void Enter_ResetsDriftTime()
        {
            _state.Enter(_context);
            Assert.AreEqual(0f, _state.DriftTime, 0.001f);
        }

        [Test]
        public void Update_AdvancesDriftTime()
        {
            _state.Enter(_context);
            _state.Update(_context, 0.1f);
            Assert.AreEqual(0.1f, _state.DriftTime, 0.001f);
        }

        [Test]
        public void Update_HighVelocity_NoTransition()
        {
            _state.Enter(_context);
            _context.LureVelocity = new Vector2(1f, 0f); // Above threshold

            _state.Update(_context, 1.0f); // Well past min drift time

            var nextState = _state.GetNextState(_context);
            Assert.IsNull(nextState, "Should not transition while velocity is high");
        }

        [Test]
        public void Update_LowVelocityBeforeMinTime_NoTransition()
        {
            _state.Enter(_context);
            _context.LureVelocity = Vector2.zero; // Below threshold

            _state.Update(_context, 0.3f); // Before min drift time (0.5s)

            var nextState = _state.GetNextState(_context);
            Assert.IsNull(nextState, "Should not transition before min drift time");
        }

        [Test]
        public void Update_LowVelocityAfterMinTime_TransitionsToStillness()
        {
            _state.Enter(_context);
            _context.LureVelocity = Vector2.zero; // Below threshold

            _state.Update(_context, 0.6f); // After min drift time

            var nextState = _state.GetNextState(_context);
            Assert.AreEqual(FishingState.Stillness, nextState);
        }

        [Test]
        public void Update_VelocityExactlyAtThreshold_TransitionsToStillness()
        {
            _state.Enter(_context);
            _context.LureVelocity = new Vector2(0.1f, 0f); // Exactly at threshold

            _state.Update(_context, 0.6f);

            var nextState = _state.GetNextState(_context);
            Assert.AreEqual(FishingState.Stillness, nextState);
        }

        [Test]
        public void Update_VelocityJustAboveThreshold_NoTransition()
        {
            _state.Enter(_context);
            _context.LureVelocity = new Vector2(0.11f, 0f); // Just above threshold

            _state.Update(_context, 0.6f);

            var nextState = _state.GetNextState(_context);
            Assert.IsNull(nextState);
        }

        [Test]
        public void Update_GradualSlowdown_EventuallyTransitions()
        {
            _state.Enter(_context);

            // Simulate gradual slowdown
            _context.LureVelocity = new Vector2(0.5f, 0f);
            _state.Update(_context, 0.2f);
            Assert.IsNull(_state.GetNextState(_context));

            _context.LureVelocity = new Vector2(0.3f, 0f);
            _state.Update(_context, 0.2f);
            Assert.IsNull(_state.GetNextState(_context));

            _context.LureVelocity = new Vector2(0.05f, 0f);
            _state.Update(_context, 0.2f); // Now past min time with low velocity

            Assert.AreEqual(FishingState.Stillness, _state.GetNextState(_context));
        }

        [Test]
        public void Exit_DoesNotThrow()
        {
            _state.Enter(_context);
            Assert.DoesNotThrow(() => _state.Exit(_context));
        }

        [Test]
        public void DiagonalVelocity_MagnitudeUsedCorrectly()
        {
            _state.Enter(_context);
            // Velocity of (0.07, 0.07) has magnitude ~0.099, just under threshold of 0.1
            _context.LureVelocity = new Vector2(0.07f, 0.07f);

            _state.Update(_context, 0.6f);

            var nextState = _state.GetNextState(_context);
            Assert.AreEqual(FishingState.Stillness, nextState);
        }
    }

    [TestFixture]
    public class StillnessStateTests
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

        private StillnessState _state;
        private MockContext _context;

        [SetUp]
        public void SetUp()
        {
            _state = new StillnessState(3f); // 3 second stillness threshold
            _context = new MockContext { CurrentState = FishingState.Stillness };
        }

        [Test]
        public void Constructor_DefaultValues_CreatesValidState()
        {
            var state = new StillnessState();
            Assert.IsNotNull(state);
        }

        [Test]
        public void Constructor_CustomValues_CreatesValidState()
        {
            var state = new StillnessState(5f, 0.2f);
            Assert.IsNotNull(state);
        }

        [Test]
        public void Enter_ResetsStillnessTime()
        {
            _state.Enter(_context);
            Assert.AreEqual(0f, _state.StillnessTime, 0.001f);
            Assert.AreEqual(0f, _state.StillnessProgress, 0.001f);
            Assert.IsFalse(_state.ThresholdReached);
        }

        [Test]
        public void Update_AccumulatesStillnessTime()
        {
            _state.Enter(_context);

            _state.Update(_context, 1.0f);
            Assert.AreEqual(1.0f, _state.StillnessTime, 0.001f);

            _state.Update(_context, 0.5f);
            Assert.AreEqual(1.5f, _state.StillnessTime, 0.001f);
        }

        [Test]
        public void StillnessProgress_CalculatedCorrectly()
        {
            _state.Enter(_context);

            _state.Update(_context, 1.5f); // 1.5 / 3.0 = 0.5
            Assert.AreEqual(0.5f, _state.StillnessProgress, 0.001f);

            _state.Update(_context, 1.5f); // 3.0 / 3.0 = 1.0
            Assert.AreEqual(1.0f, _state.StillnessProgress, 0.001f);
        }

        [Test]
        public void StillnessProgress_ClampedToOne()
        {
            _state.Enter(_context);

            _state.Update(_context, 5.0f); // Way past threshold

            Assert.AreEqual(1.0f, _state.StillnessProgress, 0.001f);
        }

        [Test]
        public void Update_BeforeThreshold_NoTransition()
        {
            _state.Enter(_context);
            _state.Update(_context, 2.0f); // 2s < 3s threshold

            var nextState = _state.GetNextState(_context);
            Assert.IsNull(nextState);
            Assert.IsFalse(_state.ThresholdReached);
        }

        [Test]
        public void Update_AtThreshold_TransitionsToBiteCheck()
        {
            _state.Enter(_context);
            _state.Update(_context, 3.0f); // Exactly at threshold

            var nextState = _state.GetNextState(_context);
            Assert.AreEqual(FishingState.BiteCheck, nextState);
            Assert.IsTrue(_state.ThresholdReached);
        }

        [Test]
        public void Update_PastThreshold_TransitionsToBiteCheck()
        {
            _state.Enter(_context);
            _state.Update(_context, 5.0f); // Past threshold

            var nextState = _state.GetNextState(_context);
            Assert.AreEqual(FishingState.BiteCheck, nextState);
        }

        [Test]
        public void Update_MicroTwitchInput_TransitionsToMicroTwitch()
        {
            _state.Enter(_context);
            _context.CastInputPressed = true;

            _state.Update(_context, 0.5f);

            var nextState = _state.GetNextState(_context);
            Assert.AreEqual(FishingState.MicroTwitch, nextState);
        }

        [Test]
        public void Update_MicroTwitchBeforeThreshold_TakesPriority()
        {
            _state.Enter(_context);

            // Update to just before threshold
            _state.Update(_context, 2.9f);
            Assert.IsNull(_state.GetNextState(_context));

            // Now trigger micro-twitch AND pass threshold in same frame
            _context.CastInputPressed = true;
            _state.Update(_context, 0.2f); // Now at 3.1s, threshold reached

            // Micro-twitch should take priority
            var nextState = _state.GetNextState(_context);
            Assert.AreEqual(FishingState.MicroTwitch, nextState);
        }

        [Test]
        public void Update_NoInput_StaysInStillness()
        {
            _state.Enter(_context);
            _context.CastInputPressed = false;

            for (int i = 0; i < 10; i++)
            {
                _state.Update(_context, 0.1f); // 1 second total, under threshold
            }

            var nextState = _state.GetNextState(_context);
            Assert.IsNull(nextState);
        }

        [Test]
        public void Exit_DoesNotThrow()
        {
            _state.Enter(_context);
            Assert.DoesNotThrow(() => _state.Exit(_context));
        }

        [Test]
        public void Reenter_ResetsAllState()
        {
            _state.Enter(_context);
            _state.Update(_context, 2.0f);
            Assert.AreEqual(2.0f, _state.StillnessTime, 0.001f);

            // Re-enter should reset
            _state.Enter(_context);
            Assert.AreEqual(0f, _state.StillnessTime, 0.001f);
            Assert.IsFalse(_state.ThresholdReached);
        }
    }

    [TestFixture]
    public class DriftToStillnessIntegrationTests
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

        [Test]
        public void FullFlow_CastingToDriftToStillnessToBiteCheck()
        {
            var context = new MockContext();
            var castingState = new CastingState(0.3f, 2f, 5f);
            var driftState = new LureDriftState(0.1f, 0.5f);
            var stillnessState = new StillnessState(2f);

            // Start in Casting
            context.CurrentState = FishingState.Casting;
            castingState.Enter(context);
            castingState.Update(context, 0.3f);
            Assert.AreEqual(FishingState.LureDrift, castingState.GetNextState(context));

            // Transition to Drift
            castingState.Exit(context);
            context.CurrentState = FishingState.LureDrift;
            context.LureVelocity = new Vector2(0.5f, 0f); // Moving initially
            driftState.Enter(context);

            // Drift with high velocity
            driftState.Update(context, 0.3f);
            Assert.IsNull(driftState.GetNextState(context));

            // Slow down
            context.LureVelocity = new Vector2(0.05f, 0f);
            driftState.Update(context, 0.3f);
            Assert.AreEqual(FishingState.Stillness, driftState.GetNextState(context));

            // Transition to Stillness
            driftState.Exit(context);
            context.CurrentState = FishingState.Stillness;
            stillnessState.Enter(context);

            // Accumulate stillness
            stillnessState.Update(context, 1.0f);
            Assert.IsNull(stillnessState.GetNextState(context));
            Assert.AreEqual(0.5f, stillnessState.StillnessProgress, 0.001f);

            // Reach threshold
            stillnessState.Update(context, 1.0f); // 2s total
            Assert.AreEqual(FishingState.BiteCheck, stillnessState.GetNextState(context));
        }

        [Test]
        public void StillnessInterrupted_MicroTwitchResetsFlow()
        {
            var context = new MockContext();
            var stillnessState = new StillnessState(3f);

            context.CurrentState = FishingState.Stillness;
            stillnessState.Enter(context);

            // Build up some stillness
            stillnessState.Update(context, 2.0f);
            Assert.AreEqual(2.0f, stillnessState.StillnessTime, 0.001f);

            // Trigger micro-twitch
            context.CastInputPressed = true;
            stillnessState.Update(context, 0.1f);
            Assert.AreEqual(FishingState.MicroTwitch, stillnessState.GetNextState(context));

            // After micro-twitch, re-entering stillness should reset
            stillnessState.Exit(context);
            stillnessState.Enter(context);
            Assert.AreEqual(0f, stillnessState.StillnessTime, 0.001f);
        }
    }
}
