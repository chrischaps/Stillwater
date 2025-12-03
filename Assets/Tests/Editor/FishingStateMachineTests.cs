using System;
using NUnit.Framework;
using UnityEngine;
using Stillwater.Core;
using Stillwater.Fishing;

namespace Stillwater.Tests
{
    [TestFixture]
    public class FishingStateMachineTests
    {
        // Mock state implementation for testing
        private class MockState : IFishingState
        {
            public int EnterCallCount { get; private set; }
            public int UpdateCallCount { get; private set; }
            public int ExitCallCount { get; private set; }
            public int GetNextStateCallCount { get; private set; }
            public float LastDeltaTime { get; private set; }
            public FishingState? NextStateToReturn { get; set; }

            public void Enter(IFishingContext context) => EnterCallCount++;
            public void Update(IFishingContext context, float deltaTime)
            {
                UpdateCallCount++;
                LastDeltaTime = deltaTime;
            }
            public void Exit(IFishingContext context) => ExitCallCount++;
            public FishingState? GetNextState(IFishingContext context)
            {
                GetNextStateCallCount++;
                return NextStateToReturn;
            }

            public void Reset()
            {
                EnterCallCount = 0;
                UpdateCallCount = 0;
                ExitCallCount = 0;
                GetNextStateCallCount = 0;
                LastDeltaTime = 0;
                NextStateToReturn = null;
            }
        }

        // Mock context for testing
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
            public FishDefinition SelectedFish { get; set; }
            public FishDefinition[] AvailableFish { get; set; }
            public string CurrentZoneId { get; set; }
            public float BiteProbabilityModifier { get; set; }
            public float GetRandomValue() => 0.5f;
            public float GetRandomRange(float min, float max) => (min + max) / 2f;
        }

        private FishingStateMachine _stateMachine;
        private MockContext _context;
        private MockState _idleState;
        private MockState _castingState;
        private MockState _driftState;

        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();
            _context = new MockContext();
            _stateMachine = new FishingStateMachine(_context);
            _idleState = new MockState();
            _castingState = new MockState();
            _driftState = new MockState();
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.Clear();
        }

        // ==================== Constructor Tests ====================

        [Test]
        public void Constructor_NullContext_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new FishingStateMachine(null));
        }

        [Test]
        public void Constructor_ValidContext_CreatesStateMachine()
        {
            var sm = new FishingStateMachine(_context);
            Assert.IsNotNull(sm);
            Assert.IsFalse(sm.IsInitialized);
            Assert.AreEqual(0, sm.RegisteredStateCount);
        }

        // ==================== RegisterState Tests ====================

        [Test]
        public void RegisterState_ValidState_RegistersSuccessfully()
        {
            _stateMachine.RegisterState(FishingState.Idle, _idleState);

            Assert.IsTrue(_stateMachine.HasState(FishingState.Idle));
            Assert.AreEqual(1, _stateMachine.RegisteredStateCount);
        }

        [Test]
        public void RegisterState_NullImpl_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _stateMachine.RegisterState(FishingState.Idle, null));
        }

        [Test]
        public void RegisterState_Duplicate_ThrowsInvalidOperationException()
        {
            _stateMachine.RegisterState(FishingState.Idle, _idleState);

            Assert.Throws<InvalidOperationException>(() =>
                _stateMachine.RegisterState(FishingState.Idle, new MockState()));
        }

        [Test]
        public void RegisterState_MultipleStates_AllRegistered()
        {
            _stateMachine.RegisterState(FishingState.Idle, _idleState);
            _stateMachine.RegisterState(FishingState.Casting, _castingState);
            _stateMachine.RegisterState(FishingState.LureDrift, _driftState);

            Assert.AreEqual(3, _stateMachine.RegisteredStateCount);
            Assert.IsTrue(_stateMachine.HasState(FishingState.Idle));
            Assert.IsTrue(_stateMachine.HasState(FishingState.Casting));
            Assert.IsTrue(_stateMachine.HasState(FishingState.LureDrift));
        }

        // ==================== HasState Tests ====================

        [Test]
        public void HasState_NotRegistered_ReturnsFalse()
        {
            Assert.IsFalse(_stateMachine.HasState(FishingState.Idle));
        }

        [Test]
        public void HasState_Registered_ReturnsTrue()
        {
            _stateMachine.RegisterState(FishingState.Idle, _idleState);
            Assert.IsTrue(_stateMachine.HasState(FishingState.Idle));
        }

        // ==================== Initialize Tests ====================

        [Test]
        public void Initialize_ValidState_InitializesAndEntersState()
        {
            _stateMachine.RegisterState(FishingState.Idle, _idleState);

            _stateMachine.Initialize(FishingState.Idle);

            Assert.IsTrue(_stateMachine.IsInitialized);
            Assert.AreEqual(FishingState.Idle, _stateMachine.CurrentState);
            Assert.AreEqual(1, _idleState.EnterCallCount);
        }

        [Test]
        public void Initialize_UnregisteredState_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() =>
                _stateMachine.Initialize(FishingState.Idle));
        }

        [Test]
        public void Initialize_AlreadyInitialized_ThrowsInvalidOperationException()
        {
            _stateMachine.RegisterState(FishingState.Idle, _idleState);
            _stateMachine.Initialize(FishingState.Idle);

            Assert.Throws<InvalidOperationException>(() =>
                _stateMachine.Initialize(FishingState.Idle));
        }

        [Test]
        public void Initialize_PublishesFishingStateChangedEvent()
        {
            _stateMachine.RegisterState(FishingState.Idle, _idleState);
            FishingStateChangedEvent? receivedEvent = null;
            EventBus.Subscribe<FishingStateChangedEvent>(e => receivedEvent = e);

            _stateMachine.Initialize(FishingState.Idle);

            Assert.IsNotNull(receivedEvent);
            Assert.IsNull(receivedEvent.Value.PreviousState);
            Assert.AreEqual("Idle", receivedEvent.Value.NewState);
        }

        // ==================== Update Tests ====================

        [Test]
        public void Update_NotInitialized_ThrowsInvalidOperationException()
        {
            _stateMachine.RegisterState(FishingState.Idle, _idleState);

            Assert.Throws<InvalidOperationException>(() =>
                _stateMachine.Update(0.016f));
        }

        [Test]
        public void Update_CallsCurrentStateUpdate()
        {
            _stateMachine.RegisterState(FishingState.Idle, _idleState);
            _stateMachine.Initialize(FishingState.Idle);

            _stateMachine.Update(0.016f);

            Assert.AreEqual(1, _idleState.UpdateCallCount);
            Assert.AreEqual(0.016f, _idleState.LastDeltaTime, 0.0001f);
        }

        [Test]
        public void Update_ChecksForTransition()
        {
            _stateMachine.RegisterState(FishingState.Idle, _idleState);
            _stateMachine.Initialize(FishingState.Idle);

            _stateMachine.Update(0.016f);

            Assert.AreEqual(1, _idleState.GetNextStateCallCount);
        }

        [Test]
        public void Update_NoTransition_StaysInCurrentState()
        {
            _stateMachine.RegisterState(FishingState.Idle, _idleState);
            _idleState.NextStateToReturn = null;
            _stateMachine.Initialize(FishingState.Idle);

            _stateMachine.Update(0.016f);
            _stateMachine.Update(0.016f);
            _stateMachine.Update(0.016f);

            Assert.AreEqual(FishingState.Idle, _stateMachine.CurrentState);
            Assert.AreEqual(3, _idleState.UpdateCallCount);
            Assert.AreEqual(0, _idleState.ExitCallCount); // Never exited
        }

        [Test]
        public void Update_SameStateReturned_NoTransition()
        {
            _stateMachine.RegisterState(FishingState.Idle, _idleState);
            _idleState.NextStateToReturn = FishingState.Idle; // Same state
            _stateMachine.Initialize(FishingState.Idle);

            _stateMachine.Update(0.016f);

            Assert.AreEqual(FishingState.Idle, _stateMachine.CurrentState);
            Assert.AreEqual(0, _idleState.ExitCallCount); // Should not exit/re-enter same state
        }

        [Test]
        public void Update_TransitionTriggered_TransitionsToNewState()
        {
            _stateMachine.RegisterState(FishingState.Idle, _idleState);
            _stateMachine.RegisterState(FishingState.Casting, _castingState);
            _idleState.NextStateToReturn = FishingState.Casting;
            _stateMachine.Initialize(FishingState.Idle);

            _stateMachine.Update(0.016f);

            Assert.AreEqual(FishingState.Casting, _stateMachine.CurrentState);
            Assert.AreEqual(1, _idleState.ExitCallCount);
            Assert.AreEqual(1, _castingState.EnterCallCount);
        }

        [Test]
        public void Update_TransitionTriggered_PublishesEvent()
        {
            _stateMachine.RegisterState(FishingState.Idle, _idleState);
            _stateMachine.RegisterState(FishingState.Casting, _castingState);
            _stateMachine.Initialize(FishingState.Idle);

            FishingStateChangedEvent? lastEvent = null;
            EventBus.Subscribe<FishingStateChangedEvent>(e => lastEvent = e);

            _idleState.NextStateToReturn = FishingState.Casting;
            _stateMachine.Update(0.016f);

            Assert.IsNotNull(lastEvent);
            Assert.AreEqual("Idle", lastEvent.Value.PreviousState);
            Assert.AreEqual("Casting", lastEvent.Value.NewState);
        }

        // ==================== TransitionTo Tests ====================

        [Test]
        public void TransitionTo_UnregisteredState_ThrowsInvalidOperationException()
        {
            _stateMachine.RegisterState(FishingState.Idle, _idleState);
            _stateMachine.Initialize(FishingState.Idle);

            Assert.Throws<InvalidOperationException>(() =>
                _stateMachine.TransitionTo(FishingState.Casting));
        }

        [Test]
        public void TransitionTo_ValidState_TransitionsCorrectly()
        {
            _stateMachine.RegisterState(FishingState.Idle, _idleState);
            _stateMachine.RegisterState(FishingState.Casting, _castingState);
            _stateMachine.Initialize(FishingState.Idle);

            _stateMachine.TransitionTo(FishingState.Casting);

            Assert.AreEqual(FishingState.Casting, _stateMachine.CurrentState);
            Assert.AreEqual(1, _idleState.ExitCallCount);
            Assert.AreEqual(1, _castingState.EnterCallCount);
        }

        [Test]
        public void TransitionTo_PublishesEvent()
        {
            _stateMachine.RegisterState(FishingState.Idle, _idleState);
            _stateMachine.RegisterState(FishingState.Casting, _castingState);
            _stateMachine.Initialize(FishingState.Idle);

            FishingStateChangedEvent? lastEvent = null;
            EventBus.Subscribe<FishingStateChangedEvent>(e => lastEvent = e);

            _stateMachine.TransitionTo(FishingState.Casting);

            Assert.IsNotNull(lastEvent);
            Assert.AreEqual("Idle", lastEvent.Value.PreviousState);
            Assert.AreEqual("Casting", lastEvent.Value.NewState);
        }

        [Test]
        public void TransitionTo_MultipleTransitions_WorksCorrectly()
        {
            _stateMachine.RegisterState(FishingState.Idle, _idleState);
            _stateMachine.RegisterState(FishingState.Casting, _castingState);
            _stateMachine.RegisterState(FishingState.LureDrift, _driftState);
            _stateMachine.Initialize(FishingState.Idle);

            _stateMachine.TransitionTo(FishingState.Casting);
            _stateMachine.TransitionTo(FishingState.LureDrift);

            Assert.AreEqual(FishingState.LureDrift, _stateMachine.CurrentState);
            Assert.AreEqual(1, _idleState.ExitCallCount);
            Assert.AreEqual(1, _castingState.EnterCallCount);
            Assert.AreEqual(1, _castingState.ExitCallCount);
            Assert.AreEqual(1, _driftState.EnterCallCount);
        }

        // ==================== Reset Tests ====================

        [Test]
        public void Reset_AfterInitialize_ResetsStateMachine()
        {
            _stateMachine.RegisterState(FishingState.Idle, _idleState);
            _stateMachine.Initialize(FishingState.Idle);

            _stateMachine.Reset();

            Assert.IsFalse(_stateMachine.IsInitialized);
            Assert.AreEqual(1, _idleState.ExitCallCount);
        }

        [Test]
        public void Reset_WhenNotInitialized_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _stateMachine.Reset());
        }

        [Test]
        public void Reset_AllowsReinitialization()
        {
            _stateMachine.RegisterState(FishingState.Idle, _idleState);
            _stateMachine.RegisterState(FishingState.Casting, _castingState);
            _stateMachine.Initialize(FishingState.Idle);

            _stateMachine.Reset();
            _idleState.Reset();
            _stateMachine.Initialize(FishingState.Casting);

            Assert.IsTrue(_stateMachine.IsInitialized);
            Assert.AreEqual(FishingState.Casting, _stateMachine.CurrentState);
            Assert.AreEqual(1, _castingState.EnterCallCount);
        }

        // ==================== Integration Tests ====================

        [Test]
        public void FullCycle_IdleToCastingToDrift_WorksCorrectly()
        {
            _stateMachine.RegisterState(FishingState.Idle, _idleState);
            _stateMachine.RegisterState(FishingState.Casting, _castingState);
            _stateMachine.RegisterState(FishingState.LureDrift, _driftState);

            var stateChanges = new System.Collections.Generic.List<FishingStateChangedEvent>();
            EventBus.Subscribe<FishingStateChangedEvent>(e => stateChanges.Add(e));

            // Initialize to Idle
            _stateMachine.Initialize(FishingState.Idle);
            Assert.AreEqual(1, stateChanges.Count);

            // Update with no transition
            _idleState.NextStateToReturn = null;
            _stateMachine.Update(0.016f);
            Assert.AreEqual(FishingState.Idle, _stateMachine.CurrentState);
            Assert.AreEqual(1, stateChanges.Count);

            // Trigger transition to Casting
            _idleState.NextStateToReturn = FishingState.Casting;
            _stateMachine.Update(0.016f);
            Assert.AreEqual(FishingState.Casting, _stateMachine.CurrentState);
            Assert.AreEqual(2, stateChanges.Count);
            Assert.AreEqual("Casting", stateChanges[1].NewState);

            // Trigger transition to LureDrift
            _castingState.NextStateToReturn = FishingState.LureDrift;
            _stateMachine.Update(0.016f);
            Assert.AreEqual(FishingState.LureDrift, _stateMachine.CurrentState);
            Assert.AreEqual(3, stateChanges.Count);
            Assert.AreEqual("LureDrift", stateChanges[2].NewState);
        }

        [Test]
        public void EdgeCase_TransitionBeforeUpdate_WorksCorrectly()
        {
            _stateMachine.RegisterState(FishingState.Idle, _idleState);
            _stateMachine.RegisterState(FishingState.Casting, _castingState);
            _stateMachine.Initialize(FishingState.Idle);

            // Force transition without update
            _stateMachine.TransitionTo(FishingState.Casting);

            Assert.AreEqual(FishingState.Casting, _stateMachine.CurrentState);
            Assert.AreEqual(0, _idleState.UpdateCallCount); // Never updated
        }
    }
}
