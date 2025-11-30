using NUnit.Framework;
using UnityEngine;
using Stillwater.Fishing;
using Stillwater.Fishing.States;

namespace Stillwater.Tests
{
    [TestFixture]
    public class IdleStateTests
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

        private IdleState _state;
        private MockContext _context;

        [SetUp]
        public void SetUp()
        {
            _state = new IdleState();
            _context = new MockContext { CurrentState = FishingState.Idle };
        }

        [Test]
        public void Enter_ResetsCastRequestFlag()
        {
            // Simulate a cast request, then re-enter
            _context.CastInputPressed = true;
            _state.Enter(_context);
            _state.Update(_context, 0.016f);

            // Re-enter should reset
            _state.Enter(_context);
            _context.CastInputPressed = false;

            var nextState = _state.GetNextState(_context);
            Assert.IsNull(nextState, "Cast request should be reset on Enter");
        }

        [Test]
        public void Update_NoCastInput_NoTransition()
        {
            _state.Enter(_context);
            _context.CastInputPressed = false;

            _state.Update(_context, 0.016f);

            var nextState = _state.GetNextState(_context);
            Assert.IsNull(nextState);
        }

        [Test]
        public void Update_CastInputPressed_RequestsCast()
        {
            _state.Enter(_context);
            _context.CastInputPressed = true;

            _state.Update(_context, 0.016f);

            var nextState = _state.GetNextState(_context);
            Assert.AreEqual(FishingState.Casting, nextState);
        }

        [Test]
        public void Update_CastInputPressedThenReleased_StillTransitions()
        {
            _state.Enter(_context);

            // Press cast
            _context.CastInputPressed = true;
            _state.Update(_context, 0.016f);

            // Release cast (next frame)
            _context.CastInputPressed = false;
            _state.Update(_context, 0.016f);

            // Should still transition because cast was pressed
            var nextState = _state.GetNextState(_context);
            Assert.AreEqual(FishingState.Casting, nextState);
        }

        [Test]
        public void GetNextState_BeforeUpdate_ReturnsNull()
        {
            _state.Enter(_context);

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
        public void MultipleUpdates_NoCastInput_StaysIdle()
        {
            _state.Enter(_context);
            _context.CastInputPressed = false;

            for (int i = 0; i < 100; i++)
            {
                _state.Update(_context, 0.016f);
            }

            var nextState = _state.GetNextState(_context);
            Assert.IsNull(nextState);
        }
    }

    [TestFixture]
    public class CastingStateTests
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

        private CastingState _state;
        private MockContext _context;

        [SetUp]
        public void SetUp()
        {
            _state = new CastingState(0.5f, 2f, 8f);
            _context = new MockContext
            {
                CurrentState = FishingState.Casting,
                LurePosition = Vector2.zero
            };
        }

        [Test]
        public void Constructor_DefaultValues_CreatesValidState()
        {
            var state = new CastingState();
            Assert.IsNotNull(state);
        }

        [Test]
        public void Constructor_CustomValues_CreatesValidState()
        {
            var state = new CastingState(1.0f, 3f, 10f);
            Assert.IsNotNull(state);
        }

        [Test]
        public void Enter_ResetsElapsedTime()
        {
            _state.Enter(_context);
            Assert.AreEqual(0f, _state.CastProgress, 0.001f);
        }

        [Test]
        public void Enter_CalculatesLandingPosition()
        {
            _context.LurePosition = new Vector2(5f, 5f);
            _context.SetRandomValue(0.5f);

            _state.Enter(_context);

            // Landing position should be offset from lure position
            var landingPos = _state.LandingPosition;
            var distance = Vector2.Distance(_context.LurePosition, landingPos);

            // With random value 0.5, distance should be midpoint: 2 + 0.5 * (8 - 2) = 5
            Assert.AreEqual(5f, distance, 0.1f);
        }

        [Test]
        public void Enter_LandingPositionWithinDistanceRange()
        {
            _context.LurePosition = Vector2.zero;

            // Test with different random values
            for (float r = 0f; r <= 1f; r += 0.1f)
            {
                _context.SetRandomValue(r);
                _state.Enter(_context);

                var distance = Vector2.Distance(_context.LurePosition, _state.LandingPosition);
                Assert.GreaterOrEqual(distance, 2f, $"Distance should be >= minCastDistance (random={r})");
                Assert.LessOrEqual(distance, 8f, $"Distance should be <= maxCastDistance (random={r})");
            }
        }

        [Test]
        public void Update_AdvancesElapsedTime()
        {
            _state.Enter(_context);

            _state.Update(_context, 0.1f);

            // 0.1 / 0.5 = 0.2 progress
            Assert.AreEqual(0.2f, _state.CastProgress, 0.001f);
        }

        [Test]
        public void Update_BeforeDurationComplete_NoTransition()
        {
            _state.Enter(_context);

            _state.Update(_context, 0.3f); // 0.3 < 0.5 duration

            var nextState = _state.GetNextState(_context);
            Assert.IsNull(nextState);
        }

        [Test]
        public void Update_AtDurationComplete_TransitionsToLureDrift()
        {
            _state.Enter(_context);

            _state.Update(_context, 0.5f); // Exactly at duration

            var nextState = _state.GetNextState(_context);
            Assert.AreEqual(FishingState.LureDrift, nextState);
        }

        [Test]
        public void Update_AfterDurationComplete_TransitionsToLureDrift()
        {
            _state.Enter(_context);

            _state.Update(_context, 1.0f); // Well past duration

            var nextState = _state.GetNextState(_context);
            Assert.AreEqual(FishingState.LureDrift, nextState);
        }

        [Test]
        public void Update_MultipleSmallSteps_EventuallyCompletes()
        {
            _state.Enter(_context);

            // 50 frames at 0.016s = 0.8s total, should complete 0.5s cast
            for (int i = 0; i < 50; i++)
            {
                _state.Update(_context, 0.016f);
            }

            var nextState = _state.GetNextState(_context);
            Assert.AreEqual(FishingState.LureDrift, nextState);
        }

        [Test]
        public void CastProgress_ClampedToZeroOne()
        {
            _state.Enter(_context);
            Assert.AreEqual(0f, _state.CastProgress, 0.001f);

            _state.Update(_context, 0.25f);
            Assert.AreEqual(0.5f, _state.CastProgress, 0.001f);

            _state.Update(_context, 0.5f); // Total 0.75s, past 0.5s duration
            Assert.AreEqual(1f, _state.CastProgress, 0.001f); // Clamped to 1
        }

        [Test]
        public void Exit_DoesNotThrow()
        {
            _state.Enter(_context);
            _state.Update(_context, 0.5f);
            Assert.DoesNotThrow(() => _state.Exit(_context));
        }

        [Test]
        public void GetNextState_BeforeEnter_ReturnsNull()
        {
            // Don't call Enter
            var nextState = _state.GetNextState(_context);
            Assert.IsNull(nextState);
        }

        [Test]
        public void ZeroDuration_ImmediatelyCompletes()
        {
            // Constructor clamps to minimum 0.1f
            var state = new CastingState(0f, 2f, 8f);
            state.Enter(_context);

            state.Update(_context, 0.1f);

            var nextState = state.GetNextState(_context);
            Assert.AreEqual(FishingState.LureDrift, nextState);
        }

        [Test]
        public void LandingPosition_DifferentForDifferentRandomValues()
        {
            var state1 = new CastingState();
            var state2 = new CastingState();

            _context.SetRandomValue(0.0f);
            state1.Enter(_context);
            var pos1 = state1.LandingPosition;

            _context.SetRandomValue(1.0f);
            state2.Enter(_context);
            var pos2 = state2.LandingPosition;

            Assert.AreNotEqual(pos1, pos2);
        }
    }

    [TestFixture]
    public class IdleToCastingIntegrationTests
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
        public void FullFlow_IdleToCastingToLureDrift()
        {
            var context = new MockContext { CurrentState = FishingState.Idle };
            var idleState = new IdleState();
            var castingState = new CastingState(0.5f, 2f, 8f);

            // Start in Idle
            idleState.Enter(context);
            idleState.Update(context, 0.016f);
            Assert.IsNull(idleState.GetNextState(context), "Should stay in Idle without cast input");

            // Press cast
            context.CastInputPressed = true;
            idleState.Update(context, 0.016f);
            Assert.AreEqual(FishingState.Casting, idleState.GetNextState(context));

            // Transition to Casting
            idleState.Exit(context);
            context.CurrentState = FishingState.Casting;
            castingState.Enter(context);

            // Update casting (not complete yet)
            castingState.Update(context, 0.2f);
            Assert.IsNull(castingState.GetNextState(context), "Should stay in Casting before duration complete");

            // Complete cast
            castingState.Update(context, 0.4f); // Total 0.6s > 0.5s duration
            Assert.AreEqual(FishingState.LureDrift, castingState.GetNextState(context));

            // Verify landing position was calculated
            var landingPos = castingState.LandingPosition;
            Assert.AreNotEqual(Vector2.zero, landingPos);
        }
    }
}
