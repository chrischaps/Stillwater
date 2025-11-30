using NUnit.Framework;
using UnityEngine;
using Stillwater.Fishing;
using Stillwater.Fishing.States;

namespace Stillwater.Tests
{
    [TestFixture]
    public class ReelingStateTests
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

        private ReelingState _state;
        private MockContext _context;

        [SetUp]
        public void SetUp()
        {
            // tensionIncrease=0.5, tensionDecrease=0.3, maxTension=1.0, progress=0.2/s, slackChance=0.15, slackInterval=2s, escapeThreshold=0.1
            _state = new ReelingState(0.5f, 0.3f, 1.0f, 0.2f, 0.15f, 2.0f, 0.1f);
            _context = new MockContext
            {
                CurrentState = FishingState.Reeling,
                FishStruggleIntensity = 0f
            };
        }

        [Test]
        public void Constructor_DefaultValues_CreatesValidState()
        {
            var state = new ReelingState();
            Assert.IsNotNull(state);
        }

        [Test]
        public void Constructor_CustomValues_CreatesValidState()
        {
            var state = new ReelingState(0.4f, 0.2f, 1.5f, 0.3f, 0.2f, 1.5f, 0.15f);
            Assert.IsNotNull(state);
        }

        [Test]
        public void Enter_InitializesTensionAndProgress()
        {
            _state.Enter(_context);

            Assert.AreEqual(0.3f, _state.CurrentTension, 0.001f); // 30% of max tension
            Assert.AreEqual(0f, _state.ReelProgress, 0.001f);
            Assert.IsFalse(_state.SlackEventTriggered);
            Assert.IsFalse(_state.LineSnapped);
            Assert.IsFalse(_state.FishEscaped);
            Assert.IsFalse(_state.FishCaught);
        }

        [Test]
        public void Update_ReelingIncreaseTension()
        {
            _state.Enter(_context);
            float initialTension = _state.CurrentTension;
            _context.ReelInputHeld = true;

            _state.Update(_context, 0.5f);

            Assert.Greater(_state.CurrentTension, initialTension);
        }

        [Test]
        public void Update_NotReelingDecreasesTension()
        {
            _state.Enter(_context);
            float initialTension = _state.CurrentTension;
            _context.ReelInputHeld = false;

            _state.Update(_context, 0.5f);

            Assert.Less(_state.CurrentTension, initialTension);
        }

        [Test]
        public void Update_ReelingIncreasesProgress()
        {
            _state.Enter(_context);
            _context.ReelInputHeld = true;

            _state.Update(_context, 1.0f);

            Assert.Greater(_state.ReelProgress, 0f);
        }

        [Test]
        public void Update_NotReelingNoProgress()
        {
            _state.Enter(_context);
            _context.ReelInputHeld = false;

            _state.Update(_context, 1.0f);

            Assert.AreEqual(0f, _state.ReelProgress, 0.001f);
        }

        [Test]
        public void Update_ExcessiveTension_LineSnaps()
        {
            _state.Enter(_context);
            _context.ReelInputHeld = true;

            // Reel until line snaps
            for (int i = 0; i < 20; i++)
            {
                _state.Update(_context, 0.2f);
                if (_state.LineSnapped) break;
            }

            Assert.IsTrue(_state.LineSnapped);
            Assert.AreEqual(FishingState.Lost, _state.GetNextState(_context));
        }

        [Test]
        public void Update_CompletingProgress_FishCaught()
        {
            // Use lower tension increase to avoid line snap
            var state = new ReelingState(0.2f, 0.3f, 1.0f, 0.5f, 0f, 10f, 0.1f);
            state.Enter(_context);
            _context.ReelInputHeld = true;

            // Reel until fish caught (alternate reeling and releasing to manage tension)
            for (int i = 0; i < 50; i++)
            {
                state.Update(_context, 0.1f);

                // Release if tension getting too high
                if (state.CurrentTension > 0.8f)
                {
                    _context.ReelInputHeld = false;
                    state.Update(_context, 0.2f);
                    _context.ReelInputHeld = true;
                }

                if (state.FishCaught) break;
            }

            Assert.IsTrue(state.FishCaught);
            Assert.AreEqual(FishingState.Caught, state.GetNextState(_context));
        }

        [Test]
        public void Update_VeryLowTension_FishEscapes()
        {
            _state.Enter(_context);
            _context.ReelInputHeld = false;
            _context.SetRandomValue(0.001f); // Very low roll for escape chance

            // Let tension drop to very low
            for (int i = 0; i < 20; i++)
            {
                _state.Update(_context, 0.3f);
                if (_state.FishEscaped) break;
            }

            Assert.IsTrue(_state.FishEscaped);
            Assert.AreEqual(FishingState.Lost, _state.GetNextState(_context));
        }

        [Test]
        public void Update_FishStruggle_IncreasesTensionFaster()
        {
            _state.Enter(_context);
            _context.ReelInputHeld = true;
            _context.FishStruggleIntensity = 0f;
            _state.Update(_context, 0.5f);
            float tensionNoStruggle = _state.CurrentTension;

            _state.Enter(_context);
            _context.FishStruggleIntensity = 1.0f;
            _state.Update(_context, 0.5f);
            float tensionWithStruggle = _state.CurrentTension;

            Assert.Greater(tensionWithStruggle, tensionNoStruggle);
        }

        [Test]
        public void Update_SlackEventTriggered_RequiresRelease()
        {
            // High slack chance to trigger it
            var state = new ReelingState(0.3f, 0.3f, 1.0f, 0.2f, 1.0f, 0.1f, 0.1f);
            state.Enter(_context);
            _context.ReelInputHeld = true;
            _context.SetRandomValue(0.01f); // Ensure slack event triggers

            // Update until slack event triggers
            for (int i = 0; i < 10; i++)
            {
                state.Update(_context, 0.15f);
                if (state.SlackEventTriggered) break;
            }

            Assert.IsTrue(state.SlackEventTriggered);
        }

        [Test]
        public void Update_SlackEvent_ReleasingClears()
        {
            var state = new ReelingState(0.3f, 0.3f, 1.0f, 0.2f, 1.0f, 0.1f, 0.1f);
            state.Enter(_context);
            _context.ReelInputHeld = true;
            _context.SetRandomValue(0.01f);

            // Trigger slack event
            for (int i = 0; i < 10; i++)
            {
                state.Update(_context, 0.15f);
                if (state.SlackEventTriggered) break;
            }

            Assert.IsTrue(state.SlackEventTriggered);

            // Release to clear
            _context.ReelInputHeld = false;
            state.Update(_context, 0.1f);

            Assert.IsFalse(state.SlackEventTriggered);
        }

        [Test]
        public void Update_SlackEvent_ContinuingToReelSnapsLine()
        {
            var state = new ReelingState(0.3f, 0.3f, 1.0f, 0.2f, 1.0f, 0.1f, 0.1f);
            state.Enter(_context);
            _context.ReelInputHeld = true;
            _context.SetRandomValue(0.01f);

            // Trigger slack event and continue reeling
            for (int i = 0; i < 50; i++)
            {
                state.Update(_context, 0.15f);
                if (state.LineSnapped) break;
            }

            Assert.IsTrue(state.LineSnapped);
        }

        [Test]
        public void Update_AlreadyResolved_NoFurtherChanges()
        {
            _state.Enter(_context);
            _context.ReelInputHeld = true;

            // Force line snap
            for (int i = 0; i < 20; i++)
            {
                _state.Update(_context, 0.2f);
                if (_state.LineSnapped) break;
            }

            Assert.IsTrue(_state.LineSnapped);
            float tensionAfterSnap = _state.CurrentTension;

            // Further updates should not change state
            _state.Update(_context, 1.0f);
            Assert.AreEqual(tensionAfterSnap, _state.CurrentTension, 0.001f);
        }

        [Test]
        public void Exit_DoesNotThrow()
        {
            _state.Enter(_context);
            _state.Update(_context, 0.5f);
            Assert.DoesNotThrow(() => _state.Exit(_context));
        }

        [Test]
        public void GetNextState_StillReeling_ReturnsNull()
        {
            _state.Enter(_context);
            _context.ReelInputHeld = true;
            _state.Update(_context, 0.2f);

            Assert.IsNull(_state.GetNextState(_context));
        }
    }

    [TestFixture]
    public class CaughtStateTests
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

        private CaughtState _state;
        private MockContext _context;

        [SetUp]
        public void SetUp()
        {
            _state = new CaughtState(2.0f);
            _context = new MockContext { CurrentState = FishingState.Caught };
        }

        [Test]
        public void Constructor_DefaultValues_CreatesValidState()
        {
            var state = new CaughtState();
            Assert.IsNotNull(state);
        }

        [Test]
        public void Constructor_CustomValues_CreatesValidState()
        {
            var state = new CaughtState(3.0f);
            Assert.IsNotNull(state);
        }

        [Test]
        public void Enter_InitializesState()
        {
            _state.Enter(_context);

            Assert.AreEqual(0f, _state.ElapsedTime, 0.001f);
            Assert.IsFalse(_state.DisplayComplete);
            Assert.IsTrue(_state.EventReady);
        }

        [Test]
        public void Update_AdvancesTime()
        {
            _state.Enter(_context);
            _state.Update(_context, 0.5f);

            Assert.AreEqual(0.5f, _state.ElapsedTime, 0.001f);
        }

        [Test]
        public void DisplayProgress_CalculatedCorrectly()
        {
            _state.Enter(_context);

            _state.Update(_context, 1.0f); // 1/2 = 0.5
            Assert.AreEqual(0.5f, _state.DisplayProgress, 0.001f);

            _state.Update(_context, 1.0f); // 2/2 = 1.0
            Assert.AreEqual(1.0f, _state.DisplayProgress, 0.001f);
        }

        [Test]
        public void Update_AfterDuration_DisplayComplete()
        {
            _state.Enter(_context);
            _state.Update(_context, 2.5f);

            Assert.IsTrue(_state.DisplayComplete);
        }

        [Test]
        public void GetNextState_BeforeComplete_ReturnsNull()
        {
            _state.Enter(_context);
            _state.Update(_context, 1.0f);

            Assert.IsNull(_state.GetNextState(_context));
        }

        [Test]
        public void GetNextState_AfterComplete_ReturnsIdle()
        {
            _state.Enter(_context);
            _state.Update(_context, 2.5f);

            Assert.AreEqual(FishingState.Idle, _state.GetNextState(_context));
        }

        [Test]
        public void ClearEventReady_ClearsFlag()
        {
            _state.Enter(_context);
            Assert.IsTrue(_state.EventReady);

            _state.ClearEventReady();
            Assert.IsFalse(_state.EventReady);
        }

        [Test]
        public void Exit_ClearsEventReady()
        {
            _state.Enter(_context);
            Assert.IsTrue(_state.EventReady);

            _state.Exit(_context);
            Assert.IsFalse(_state.EventReady);
        }

        [Test]
        public void Update_AfterComplete_NoFurtherChanges()
        {
            _state.Enter(_context);
            _state.Update(_context, 2.5f);
            Assert.IsTrue(_state.DisplayComplete);

            float timeAfterComplete = _state.ElapsedTime;
            _state.Update(_context, 1.0f);
            Assert.AreEqual(timeAfterComplete, _state.ElapsedTime, 0.001f);
        }
    }

    [TestFixture]
    public class LostStateTests
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

        private LostState _state;
        private MockContext _context;

        [SetUp]
        public void SetUp()
        {
            _state = new LostState(1.5f);
            _context = new MockContext { CurrentState = FishingState.Lost };
        }

        [Test]
        public void Constructor_DefaultValues_CreatesValidState()
        {
            var state = new LostState();
            Assert.IsNotNull(state);
        }

        [Test]
        public void Constructor_CustomValues_CreatesValidState()
        {
            var state = new LostState(2.5f);
            Assert.IsNotNull(state);
        }

        [Test]
        public void Enter_InitializesState()
        {
            _state.Enter(_context);

            Assert.AreEqual(0f, _state.ElapsedTime, 0.001f);
            Assert.IsFalse(_state.DisplayComplete);
            Assert.IsTrue(_state.EventReady);
        }

        [Test]
        public void Update_AdvancesTime()
        {
            _state.Enter(_context);
            _state.Update(_context, 0.5f);

            Assert.AreEqual(0.5f, _state.ElapsedTime, 0.001f);
        }

        [Test]
        public void DisplayProgress_CalculatedCorrectly()
        {
            _state.Enter(_context);

            _state.Update(_context, 0.75f); // 0.75/1.5 = 0.5
            Assert.AreEqual(0.5f, _state.DisplayProgress, 0.001f);
        }

        [Test]
        public void Update_AfterDuration_DisplayComplete()
        {
            _state.Enter(_context);
            _state.Update(_context, 2.0f);

            Assert.IsTrue(_state.DisplayComplete);
        }

        [Test]
        public void GetNextState_BeforeComplete_ReturnsNull()
        {
            _state.Enter(_context);
            _state.Update(_context, 0.5f);

            Assert.IsNull(_state.GetNextState(_context));
        }

        [Test]
        public void GetNextState_AfterComplete_ReturnsIdle()
        {
            _state.Enter(_context);
            _state.Update(_context, 2.0f);

            Assert.AreEqual(FishingState.Idle, _state.GetNextState(_context));
        }

        [Test]
        public void SetReason_SetsLostReason()
        {
            _state.SetReason(LostReason.LineSnapped);
            Assert.AreEqual(LostReason.LineSnapped, _state.Reason);

            _state.SetReason(LostReason.FishEscaped);
            Assert.AreEqual(LostReason.FishEscaped, _state.Reason);
        }

        [Test]
        public void Reason_Property_CanBeSetDirectly()
        {
            _state.Reason = LostReason.MissedHook;
            Assert.AreEqual(LostReason.MissedHook, _state.Reason);
        }

        [Test]
        public void Exit_ResetsReason()
        {
            _state.SetReason(LostReason.LineSnapped);
            _state.Enter(_context);
            _state.Exit(_context);

            Assert.AreEqual(LostReason.Unknown, _state.Reason);
        }

        [Test]
        public void ClearEventReady_ClearsFlag()
        {
            _state.Enter(_context);
            Assert.IsTrue(_state.EventReady);

            _state.ClearEventReady();
            Assert.IsFalse(_state.EventReady);
        }

        [Test]
        public void LostReason_AllValues_Valid()
        {
            Assert.AreEqual(LostReason.Unknown, (LostReason)0);
            Assert.AreEqual(LostReason.MissedHook, (LostReason)1);
            Assert.AreEqual(LostReason.EarlyHook, (LostReason)2);
            Assert.AreEqual(LostReason.LineSnapped, (LostReason)3);
            Assert.AreEqual(LostReason.FishEscaped, (LostReason)4);
            Assert.AreEqual(LostReason.SlackEventFailure, (LostReason)5);
        }
    }

    [TestFixture]
    public class ReelingToResultIntegrationTests
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
        public void FullFlow_HookedToReelingToCaught()
        {
            var context = new MockContext();
            var reelingState = new ReelingState(0.2f, 0.3f, 1.0f, 0.5f, 0f, 10f, 0.05f);
            var caughtState = new CaughtState(1.0f);

            // Start in Reeling (simulating transition from Hooked)
            context.CurrentState = FishingState.Reeling;
            context.SetRandomValue(0.99f); // High roll to avoid escape
            reelingState.Enter(context);

            // Reel carefully to avoid line snap
            for (int i = 0; i < 50; i++)
            {
                context.ReelInputHeld = true;
                reelingState.Update(context, 0.1f);

                // Release if tension getting high
                if (reelingState.CurrentTension > 0.7f)
                {
                    context.ReelInputHeld = false;
                    reelingState.Update(context, 0.2f);
                }

                if (reelingState.FishCaught) break;
            }

            Assert.IsTrue(reelingState.FishCaught);
            Assert.AreEqual(FishingState.Caught, reelingState.GetNextState(context));

            // Transition to Caught
            reelingState.Exit(context);
            context.CurrentState = FishingState.Caught;
            caughtState.Enter(context);
            Assert.IsTrue(caughtState.EventReady);

            // Wait for display
            caughtState.Update(context, 1.5f);
            Assert.AreEqual(FishingState.Idle, caughtState.GetNextState(context));
        }

        [Test]
        public void FullFlow_ReelingToLineSnapToLost()
        {
            var context = new MockContext();
            var reelingState = new ReelingState(0.5f, 0.3f, 1.0f, 0.2f, 0f, 10f, 0.1f);
            var lostState = new LostState(1.0f);

            context.CurrentState = FishingState.Reeling;
            reelingState.Enter(context);

            // Reel aggressively until line snaps
            context.ReelInputHeld = true;
            for (int i = 0; i < 20; i++)
            {
                reelingState.Update(context, 0.2f);
                if (reelingState.LineSnapped) break;
            }

            Assert.IsTrue(reelingState.LineSnapped);
            Assert.AreEqual(FishingState.Lost, reelingState.GetNextState(context));

            // Transition to Lost
            reelingState.Exit(context);
            context.CurrentState = FishingState.Lost;
            lostState.SetReason(LostReason.LineSnapped);
            lostState.Enter(context);

            Assert.IsTrue(lostState.EventReady);
            Assert.AreEqual(LostReason.LineSnapped, lostState.Reason);

            // Wait for display
            lostState.Update(context, 1.5f);
            Assert.AreEqual(FishingState.Idle, lostState.GetNextState(context));
        }

        [Test]
        public void FullFlow_ReelingToFishEscapeToLost()
        {
            var context = new MockContext();
            var reelingState = new ReelingState(0.3f, 0.5f, 1.0f, 0.2f, 0f, 10f, 0.3f);
            var lostState = new LostState(1.0f);

            context.CurrentState = FishingState.Reeling;
            context.SetRandomValue(0.001f); // Very low roll for escape
            reelingState.Enter(context);

            // Let tension drop and fish escape
            context.ReelInputHeld = false;
            for (int i = 0; i < 30; i++)
            {
                reelingState.Update(context, 0.2f);
                if (reelingState.FishEscaped) break;
            }

            Assert.IsTrue(reelingState.FishEscaped);
            Assert.AreEqual(FishingState.Lost, reelingState.GetNextState(context));

            // Transition to Lost
            reelingState.Exit(context);
            context.CurrentState = FishingState.Lost;
            lostState.SetReason(LostReason.FishEscaped);
            lostState.Enter(context);

            Assert.AreEqual(LostReason.FishEscaped, lostState.Reason);
        }
    }
}
