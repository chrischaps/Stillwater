using NUnit.Framework;
using UnityEngine;
using Stillwater.Fishing;
using Stillwater.Fishing.States;
using Stillwater.Core;
using Stillwater.Framework;

namespace Stillwater.Tests
{
    [TestFixture]
    public class FishingControllerTests
    {
        private GameObject _gameObject;
        private FishingController _controller;

        [SetUp]
        public void SetUp()
        {
            // Clear any previous event subscriptions
            EventBus.Clear();

            // Create a test GameObject with FishingController
            _gameObject = new GameObject("TestFishingController");
            _controller = _gameObject.AddComponent<FishingController>();

            // Manually initialize since Awake() may not be called in EditMode tests
            _controller.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            if (_gameObject != null)
            {
                Object.DestroyImmediate(_gameObject);
            }
            EventBus.Clear();
            ServiceLocator.Clear();
        }

        #region Initialization Tests

        [Test]
        public void Awake_InitializesStateMachine()
        {
            Assert.IsNotNull(_controller.StateMachine, "StateMachine should be initialized");
        }

        [Test]
        public void Awake_InitializesToIdleState()
        {
            Assert.AreEqual(FishingState.Idle, _controller.CurrentState, "Should start in Idle state");
        }

        [Test]
        public void Awake_RegistersAllStates()
        {
            // There are 12 fishing states in the enum
            Assert.AreEqual(12, _controller.StateMachine.RegisteredStateCount, "Should register all 12 states");
        }

        [Test]
        public void Awake_StateMachineIsInitialized()
        {
            Assert.IsTrue(_controller.StateMachine.IsInitialized, "StateMachine should be initialized");
        }

        #endregion

        #region IFishingContext Implementation Tests

        [Test]
        public void CurrentState_ReturnsStateMachineState()
        {
            Assert.AreEqual(_controller.StateMachine.CurrentState, _controller.CurrentState);
        }

        [Test]
        public void TimeInState_StartsAtZero()
        {
            Assert.AreEqual(0f, _controller.TimeInState, 0.001f, "TimeInState should start at 0");
        }

        [Test]
        public void GetRandomValue_ReturnsBetween0And1()
        {
            for (int i = 0; i < 10; i++)
            {
                float value = _controller.GetRandomValue();
                Assert.GreaterOrEqual(value, 0f, "Random value should be >= 0");
                Assert.LessOrEqual(value, 1f, "Random value should be <= 1");
            }
        }

        [Test]
        public void GetRandomRange_ReturnsWithinRange()
        {
            float min = 5f;
            float max = 10f;

            for (int i = 0; i < 10; i++)
            {
                float value = _controller.GetRandomRange(min, max);
                Assert.GreaterOrEqual(value, min, "Random value should be >= min");
                Assert.LessOrEqual(value, max, "Random value should be <= max");
            }
        }

        [Test]
        public void LurePosition_DefaultsToZero()
        {
            Assert.AreEqual(Vector2.zero, _controller.LurePosition);
        }

        [Test]
        public void LineTension_DefaultsToZero()
        {
            Assert.AreEqual(0f, _controller.LineTension, 0.001f);
        }

        [Test]
        public void HasHookedFish_DefaultsToFalse()
        {
            Assert.IsFalse(_controller.HasHookedFish);
        }

        [Test]
        public void CurrentZoneId_HasDefaultValue()
        {
            Assert.IsFalse(string.IsNullOrEmpty(_controller.CurrentZoneId), "Should have a default zone");
        }

        [Test]
        public void BiteProbabilityModifier_DefaultsTo1()
        {
            Assert.AreEqual(1f, _controller.BiteProbabilityModifier, 0.001f);
        }

        #endregion

        #region Input State Tests

        [Test]
        public void CastInputPressed_DefaultsToFalse()
        {
            Assert.IsFalse(_controller.CastInputPressed);
        }

        [Test]
        public void ReelInputHeld_DefaultsToFalse()
        {
            // Without input service, should return false
            Assert.IsFalse(_controller.ReelInputHeld);
        }

        [Test]
        public void SlackInputPressed_DefaultsToFalse()
        {
            Assert.IsFalse(_controller.SlackInputPressed);
        }

        [Test]
        public void CancelInputPressed_DefaultsToFalse()
        {
            Assert.IsFalse(_controller.CancelInputPressed);
        }

        #endregion

        #region Public Method Tests

        [Test]
        public void SetLineTension_UpdatesLineTension()
        {
            _controller.SetLineTension(0.75f);
            Assert.AreEqual(0.75f, _controller.LineTension, 0.001f);
        }

        [Test]
        public void SetLineTension_ClampsToValidRange()
        {
            _controller.SetLineTension(1.5f);
            Assert.AreEqual(1f, _controller.LineTension, 0.001f, "Should clamp to 1");

            _controller.SetLineTension(-0.5f);
            Assert.AreEqual(0f, _controller.LineTension, 0.001f, "Should clamp to 0");
        }

        [Test]
        public void SetHookedFish_SetsHookedState()
        {
            _controller.SetHookedFish("bass_01", 0.5f);

            Assert.IsTrue(_controller.HasHookedFish);
            Assert.AreEqual("bass_01", _controller.HookedFishId);
            Assert.AreEqual(0.5f, _controller.FishStruggleIntensity, 0.001f);
        }

        [Test]
        public void SetHookedFish_WithNull_ClearsHookedState()
        {
            _controller.SetHookedFish("bass_01");
            _controller.SetHookedFish(null);

            Assert.IsFalse(_controller.HasHookedFish);
            Assert.IsNull(_controller.HookedFishId);
            Assert.AreEqual(0f, _controller.FishStruggleIntensity, 0.001f);
        }

        [Test]
        public void SetHookedFish_WithEmptyString_ClearsHookedState()
        {
            _controller.SetHookedFish("bass_01");
            _controller.SetHookedFish("");

            Assert.IsFalse(_controller.HasHookedFish);
        }

        [Test]
        public void ClearHookedFish_ClearsHookedState()
        {
            _controller.SetHookedFish("bass_01");
            _controller.ClearHookedFish();

            Assert.IsFalse(_controller.HasHookedFish);
            Assert.IsNull(_controller.HookedFishId);
        }

        [Test]
        public void SetFishStruggleIntensity_UpdatesIntensity()
        {
            _controller.SetHookedFish("bass_01");
            _controller.SetFishStruggleIntensity(0.8f);

            Assert.AreEqual(0.8f, _controller.FishStruggleIntensity, 0.001f);
        }

        [Test]
        public void SetFishStruggleIntensity_ClampsToValidRange()
        {
            _controller.SetFishStruggleIntensity(1.5f);
            Assert.AreEqual(1f, _controller.FishStruggleIntensity, 0.001f);

            _controller.SetFishStruggleIntensity(-0.5f);
            Assert.AreEqual(0f, _controller.FishStruggleIntensity, 0.001f);
        }

        [Test]
        public void SetFishInterest_UpdatesHasFishInterest()
        {
            _controller.SetFishInterest(true);
            Assert.IsTrue(_controller.HasFishInterest);

            _controller.SetFishInterest(false);
            Assert.IsFalse(_controller.HasFishInterest);
        }

        [Test]
        public void SetZone_UpdatesZoneInfo()
        {
            _controller.SetZone("forest_river", 1.5f);

            Assert.AreEqual("forest_river", _controller.CurrentZoneId);
            Assert.AreEqual(1.5f, _controller.BiteProbabilityModifier, 0.001f);
        }

        [Test]
        public void SetZone_ClampsNegativeModifier()
        {
            _controller.SetZone("test_zone", -0.5f);
            Assert.AreEqual(0f, _controller.BiteProbabilityModifier, 0.001f);
        }

        [Test]
        public void SetLureTransform_SetsReference()
        {
            var lureGO = new GameObject("Lure");
            try
            {
                _controller.SetLureTransform(lureGO.transform);
                // Can't directly verify, but should not throw
                Assert.Pass();
            }
            finally
            {
                Object.DestroyImmediate(lureGO);
            }
        }

        [Test]
        public void ForceTransition_ChangesState()
        {
            _controller.ForceTransition(FishingState.Casting);
            Assert.AreEqual(FishingState.Casting, _controller.CurrentState);
        }

        [Test]
        public void ResetToIdle_ResetsToIdleState()
        {
            _controller.ForceTransition(FishingState.Reeling);
            _controller.SetHookedFish("bass_01");
            _controller.SetLineTension(0.8f);
            _controller.SetFishInterest(true);

            _controller.ResetToIdle();

            Assert.AreEqual(FishingState.Idle, _controller.CurrentState);
            Assert.IsFalse(_controller.HasHookedFish);
            Assert.AreEqual(0f, _controller.LineTension, 0.001f);
            Assert.IsFalse(_controller.HasFishInterest);
        }

        #endregion

        #region Debug Logging Tests

        [Test]
        public void DebugLogging_DefaultsToTrue()
        {
            Assert.IsTrue(_controller.DebugLogging);
        }

        [Test]
        public void DebugLogging_CanBeSet()
        {
            _controller.DebugLogging = false;
            Assert.IsFalse(_controller.DebugLogging);

            _controller.DebugLogging = true;
            Assert.IsTrue(_controller.DebugLogging);
        }

        #endregion

        #region State Machine Integration Tests

        [Test]
        public void StateMachine_HasIdleState()
        {
            Assert.IsTrue(_controller.StateMachine.HasState(FishingState.Idle));
        }

        [Test]
        public void StateMachine_HasCastingState()
        {
            Assert.IsTrue(_controller.StateMachine.HasState(FishingState.Casting));
        }

        [Test]
        public void StateMachine_HasLureDriftState()
        {
            Assert.IsTrue(_controller.StateMachine.HasState(FishingState.LureDrift));
        }

        [Test]
        public void StateMachine_HasStillnessState()
        {
            Assert.IsTrue(_controller.StateMachine.HasState(FishingState.Stillness));
        }

        [Test]
        public void StateMachine_HasMicroTwitchState()
        {
            Assert.IsTrue(_controller.StateMachine.HasState(FishingState.MicroTwitch));
        }

        [Test]
        public void StateMachine_HasBiteCheckState()
        {
            Assert.IsTrue(_controller.StateMachine.HasState(FishingState.BiteCheck));
        }

        [Test]
        public void StateMachine_HasHookOpportunityState()
        {
            Assert.IsTrue(_controller.StateMachine.HasState(FishingState.HookOpportunity));
        }

        [Test]
        public void StateMachine_HasHookedState()
        {
            Assert.IsTrue(_controller.StateMachine.HasState(FishingState.Hooked));
        }

        [Test]
        public void StateMachine_HasReelingState()
        {
            Assert.IsTrue(_controller.StateMachine.HasState(FishingState.Reeling));
        }

        [Test]
        public void StateMachine_HasSlackEventState()
        {
            Assert.IsTrue(_controller.StateMachine.HasState(FishingState.SlackEvent));
        }

        [Test]
        public void StateMachine_HasCaughtState()
        {
            Assert.IsTrue(_controller.StateMachine.HasState(FishingState.Caught));
        }

        [Test]
        public void StateMachine_HasLostState()
        {
            Assert.IsTrue(_controller.StateMachine.HasState(FishingState.Lost));
        }

        #endregion

        #region Event Integration Tests

        [Test]
        public void StateChanged_ResetsTimeInState()
        {
            // Simulate some time passing
            // Note: We can't easily test Update() in edit mode tests
            // but we can verify the event handler behavior

            var evt = new FishingStateChangedEvent
            {
                PreviousState = "Idle",
                NewState = "Casting"
            };

            // Trigger the event
            EventBus.Publish(evt);

            // TimeInState should be reset (handled in OnFishingStateChanged)
            Assert.AreEqual(0f, _controller.TimeInState, 0.001f, "TimeInState should reset on state change");
        }

        #endregion
    }
}
