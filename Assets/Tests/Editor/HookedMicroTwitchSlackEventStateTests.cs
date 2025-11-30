using NUnit.Framework;
using UnityEngine;
using Stillwater.Fishing;
using Stillwater.Fishing.States;

namespace Stillwater.Tests
{
    [TestFixture]
    public class HookedStateTests
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

        private HookedState _state;
        private MockContext _context;

        [SetUp]
        public void SetUp()
        {
            _state = new HookedState();
            _context = new MockContext { CurrentState = FishingState.Hooked };
        }

        [Test]
        public void Constructor_Default_SetsDefaultDuration()
        {
            var state = new HookedState();
            state.Enter(_context);
            Assert.AreEqual(0f, state.HookSetProgress, 0.001f, "Progress should start at 0");
        }

        [Test]
        public void Constructor_CustomDuration_UsesCustomValue()
        {
            var state = new HookedState(0.5f);
            state.Enter(_context);

            state.Update(_context, 0.25f);

            Assert.AreEqual(0.5f, state.HookSetProgress, 0.001f, "Progress should be 50% at half duration");
        }

        [Test]
        public void Enter_ResetsState()
        {
            _state.Enter(_context);
            _state.Update(_context, 1f); // Complete the state

            _state.Enter(_context);

            Assert.AreEqual(0f, _state.HookSetProgress, 0.001f, "Progress should reset on Enter");
            Assert.IsFalse(_state.HookSetComplete, "HookSetComplete should be false on Enter");
        }

        [Test]
        public void Update_IncreasesProgress()
        {
            _state.Enter(_context);
            float initialProgress = _state.HookSetProgress;

            _state.Update(_context, 0.1f);

            Assert.Greater(_state.HookSetProgress, initialProgress, "Progress should increase");
        }

        [Test]
        public void Update_CompletesAfterDuration()
        {
            _state.Enter(_context);

            _state.Update(_context, 0.5f); // Default duration is 0.3s, so this should complete

            Assert.IsTrue(_state.HookSetComplete, "Should be complete after full duration");
        }

        [Test]
        public void GetNextState_NotComplete_ReturnsNull()
        {
            _state.Enter(_context);
            _state.Update(_context, 0.1f);

            var nextState = _state.GetNextState(_context);

            Assert.IsNull(nextState, "Should not transition before complete");
        }

        [Test]
        public void GetNextState_Complete_ReturnsReeling()
        {
            _state.Enter(_context);
            _state.Update(_context, 0.5f); // Complete the state

            var nextState = _state.GetNextState(_context);

            Assert.AreEqual(FishingState.Reeling, nextState, "Should transition to Reeling when complete");
        }

        [Test]
        public void HookSetProgress_ClampsTo1()
        {
            _state.Enter(_context);
            _state.Update(_context, 10f); // Way past duration

            Assert.AreEqual(1f, _state.HookSetProgress, 0.001f, "Progress should clamp to 1");
        }
    }

    [TestFixture]
    public class MicroTwitchStateTests
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

        private MicroTwitchState _state;
        private MockContext _context;

        [SetUp]
        public void SetUp()
        {
            _state = new MicroTwitchState();
            _context = new MockContext { CurrentState = FishingState.MicroTwitch };
        }

        [Test]
        public void Constructor_Default_SetsDefaultDuration()
        {
            var state = new MicroTwitchState();
            state.Enter(_context);
            Assert.AreEqual(0f, state.TwitchProgress, 0.001f, "Progress should start at 0");
        }

        [Test]
        public void Constructor_CustomDuration_UsesCustomValue()
        {
            var state = new MicroTwitchState(0.4f);
            state.Enter(_context);

            state.Update(_context, 0.2f);

            Assert.AreEqual(0.5f, state.TwitchProgress, 0.001f, "Progress should be 50% at half duration");
        }

        [Test]
        public void Enter_ResetsState()
        {
            _state.Enter(_context);
            _state.Update(_context, 1f); // Complete the state

            _state.Enter(_context);

            Assert.AreEqual(0f, _state.TwitchProgress, 0.001f, "Progress should reset on Enter");
            Assert.IsFalse(_state.TwitchComplete, "TwitchComplete should be false on Enter");
        }

        [Test]
        public void Update_IncreasesProgress()
        {
            _state.Enter(_context);
            float initialProgress = _state.TwitchProgress;

            _state.Update(_context, 0.05f);

            Assert.Greater(_state.TwitchProgress, initialProgress, "Progress should increase");
        }

        [Test]
        public void Update_CompletesAfterDuration()
        {
            _state.Enter(_context);

            _state.Update(_context, 0.3f); // Default duration is 0.2s, so this should complete

            Assert.IsTrue(_state.TwitchComplete, "Should be complete after full duration");
        }

        [Test]
        public void GetNextState_NotComplete_ReturnsNull()
        {
            _state.Enter(_context);
            _state.Update(_context, 0.05f);

            var nextState = _state.GetNextState(_context);

            Assert.IsNull(nextState, "Should not transition before complete");
        }

        [Test]
        public void GetNextState_Complete_ReturnsStillness()
        {
            _state.Enter(_context);
            _state.Update(_context, 0.3f); // Complete the state

            var nextState = _state.GetNextState(_context);

            Assert.AreEqual(FishingState.Stillness, nextState, "Should transition to Stillness when complete");
        }

        [Test]
        public void TwitchProgress_ClampsTo1()
        {
            _state.Enter(_context);
            _state.Update(_context, 10f); // Way past duration

            Assert.AreEqual(1f, _state.TwitchProgress, 0.001f, "Progress should clamp to 1");
        }

        [Test]
        public void Constructor_VeryShortDuration_ClampsToMinimum()
        {
            var state = new MicroTwitchState(0.01f); // Below minimum
            state.Enter(_context);

            // Should use minimum duration of 0.05s
            state.Update(_context, 0.03f);
            Assert.IsFalse(state.TwitchComplete, "Should not complete with very short update if minimum enforced");
        }
    }

    [TestFixture]
    public class SlackEventStateTests
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

        private SlackEventState _state;
        private MockContext _context;

        [SetUp]
        public void SetUp()
        {
            _state = new SlackEventState();
            _context = new MockContext { CurrentState = FishingState.SlackEvent };
        }

        [Test]
        public void Constructor_Default_SetsDefaultValues()
        {
            var state = new SlackEventState();
            state.Enter(_context);
            Assert.AreEqual(0f, state.SlackProgress, 0.001f, "Progress should start at 0");
            Assert.IsFalse(state.SlackCleared, "SlackCleared should be false initially");
            Assert.IsFalse(state.LineSnapped, "LineSnapped should be false initially");
        }

        [Test]
        public void Enter_ResetsState()
        {
            _state.Enter(_context);
            _context.ReelInputHeld = false;
            _state.Update(_context, 0.5f); // Clear the slack

            _state.Enter(_context);

            Assert.AreEqual(0f, _state.SlackProgress, 0.001f, "Progress should reset on Enter");
            Assert.IsFalse(_state.SlackCleared, "SlackCleared should be false on Enter");
            Assert.IsFalse(_state.LineSnapped, "LineSnapped should be false on Enter");
        }

        [Test]
        public void Update_NotReeling_AccumulatesReleaseTime()
        {
            _state.Enter(_context);
            _context.ReelInputHeld = false;

            _state.Update(_context, 0.2f);

            // Not asserting internal state, but should not snap
            Assert.IsFalse(_state.LineSnapped, "Should not snap when not reeling");
        }

        [Test]
        public void Update_ReleaseForRequiredDuration_ClearsSlack()
        {
            _state.Enter(_context);
            _context.ReelInputHeld = false;

            // Default required release duration is 0.3s
            _state.Update(_context, 0.4f);

            Assert.IsTrue(_state.SlackCleared, "Slack should be cleared after releasing for required duration");
        }

        [Test]
        public void Update_ReelWhileSlack_IncreasesTension()
        {
            _state.Enter(_context);
            _context.ReelInputHeld = true;

            _state.Update(_context, 0.5f);

            Assert.IsFalse(_state.SlackCleared, "Slack should not clear while reeling");
        }

        [Test]
        public void Update_ReelPastMaxDuration_SnapsLine()
        {
            _state.Enter(_context);
            _context.ReelInputHeld = true;

            // Default max duration is 1.5s
            _state.Update(_context, 2f);

            Assert.IsTrue(_state.LineSnapped, "Line should snap when reeling past max duration");
        }

        [Test]
        public void Update_ReleaseResumed_ResetsReleaseTime()
        {
            _state.Enter(_context);

            // Start releasing
            _context.ReelInputHeld = false;
            _state.Update(_context, 0.2f);

            // Start reeling again
            _context.ReelInputHeld = true;
            _state.Update(_context, 0.1f);

            // Release again - should need full required duration again
            _context.ReelInputHeld = false;
            _state.Update(_context, 0.2f);

            Assert.IsFalse(_state.SlackCleared, "Release time should reset when reeling resumes");
        }

        [Test]
        public void GetNextState_SlackCleared_ReturnsReeling()
        {
            _state.Enter(_context);
            _context.ReelInputHeld = false;
            _state.Update(_context, 0.4f); // Clear slack

            var nextState = _state.GetNextState(_context);

            Assert.AreEqual(FishingState.Reeling, nextState, "Should return to Reeling when slack cleared");
        }

        [Test]
        public void GetNextState_LineSnapped_ReturnsLost()
        {
            _state.Enter(_context);
            _context.ReelInputHeld = true;
            _state.Update(_context, 2f); // Snap line

            var nextState = _state.GetNextState(_context);

            Assert.AreEqual(FishingState.Lost, nextState, "Should transition to Lost when line snaps");
        }

        [Test]
        public void GetNextState_InProgress_ReturnsNull()
        {
            _state.Enter(_context);
            _context.ReelInputHeld = false;
            _state.Update(_context, 0.1f);

            var nextState = _state.GetNextState(_context);

            Assert.IsNull(nextState, "Should return null while in progress");
        }

        [Test]
        public void SlackProgress_IncreasesOverTime()
        {
            _state.Enter(_context);
            float initialProgress = _state.SlackProgress;

            _state.Update(_context, 0.5f);

            Assert.Greater(_state.SlackProgress, initialProgress, "Progress should increase over time");
        }

        [Test]
        public void SlackProgress_ClampsTo1()
        {
            _state.Enter(_context);
            _state.Update(_context, 10f);

            Assert.AreEqual(1f, _state.SlackProgress, 0.001f, "Progress should clamp to 1");
        }

        [Test]
        public void Update_AlreadyCleared_DoesNotProcess()
        {
            _state.Enter(_context);
            _context.ReelInputHeld = false;
            _state.Update(_context, 0.4f); // Clear slack
            Assert.IsTrue(_state.SlackCleared);

            // Try to snap line after already cleared
            _context.ReelInputHeld = true;
            _state.Update(_context, 5f);

            Assert.IsFalse(_state.LineSnapped, "Should not snap if already cleared");
        }

        [Test]
        public void Update_AlreadySnapped_DoesNotProcess()
        {
            _state.Enter(_context);
            _context.ReelInputHeld = true;
            _state.Update(_context, 2f); // Snap line
            Assert.IsTrue(_state.LineSnapped);

            // Try to clear after already snapped
            _context.ReelInputHeld = false;
            _state.Update(_context, 1f);

            Assert.IsFalse(_state.SlackCleared, "Should not clear if already snapped");
        }
    }
}
