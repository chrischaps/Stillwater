using System;
using NUnit.Framework;
using UnityEngine;
using Stillwater.Fishing;

namespace Stillwater.Tests
{
    [TestFixture]
    public class FishingStateTests
    {
        [Test]
        public void FishingState_HasAllExpectedValues()
        {
            // Verify all expected states exist
            Assert.IsTrue(Enum.IsDefined(typeof(FishingState), FishingState.Idle));
            Assert.IsTrue(Enum.IsDefined(typeof(FishingState), FishingState.Casting));
            Assert.IsTrue(Enum.IsDefined(typeof(FishingState), FishingState.LureDrift));
            Assert.IsTrue(Enum.IsDefined(typeof(FishingState), FishingState.Stillness));
            Assert.IsTrue(Enum.IsDefined(typeof(FishingState), FishingState.MicroTwitch));
            Assert.IsTrue(Enum.IsDefined(typeof(FishingState), FishingState.BiteCheck));
            Assert.IsTrue(Enum.IsDefined(typeof(FishingState), FishingState.HookOpportunity));
            Assert.IsTrue(Enum.IsDefined(typeof(FishingState), FishingState.Hooked));
            Assert.IsTrue(Enum.IsDefined(typeof(FishingState), FishingState.Reeling));
            Assert.IsTrue(Enum.IsDefined(typeof(FishingState), FishingState.SlackEvent));
            Assert.IsTrue(Enum.IsDefined(typeof(FishingState), FishingState.Caught));
            Assert.IsTrue(Enum.IsDefined(typeof(FishingState), FishingState.Lost));
        }

        [Test]
        public void FishingState_HasExpectedCount()
        {
            var values = Enum.GetValues(typeof(FishingState));
            Assert.AreEqual(12, values.Length, "FishingState should have exactly 12 states");
        }

        [Test]
        public void FishingState_CanBeUsedInSwitch()
        {
            // Verify enum can be used in switch statement (compile-time validation)
            FishingState state = FishingState.Idle;
            string result = state switch
            {
                FishingState.Idle => "idle",
                FishingState.Casting => "casting",
                FishingState.LureDrift => "drift",
                FishingState.Stillness => "stillness",
                FishingState.MicroTwitch => "twitch",
                FishingState.BiteCheck => "bitecheck",
                FishingState.HookOpportunity => "hook",
                FishingState.Hooked => "hooked",
                FishingState.Reeling => "reeling",
                FishingState.SlackEvent => "slack",
                FishingState.Caught => "caught",
                FishingState.Lost => "lost",
                _ => "unknown"
            };
            Assert.AreEqual("idle", result);
        }

        [Test]
        public void FishingState_CanBeStoredAsNullable()
        {
            // Verify nullable FishingState works (used by IFishingState.GetNextState)
            FishingState? nullableState = null;
            Assert.IsNull(nullableState);

            nullableState = FishingState.Hooked;
            Assert.IsNotNull(nullableState);
            Assert.AreEqual(FishingState.Hooked, nullableState.Value);
        }

        [Test]
        public void FishingState_TerminalStates_AreDistinct()
        {
            // Verify terminal states are correctly distinguishable
            FishingState caught = FishingState.Caught;
            FishingState lost = FishingState.Lost;

            Assert.AreNotEqual(caught, lost);
            Assert.AreEqual(FishingState.Caught, caught);
            Assert.AreEqual(FishingState.Lost, lost);
        }

        [Test]
        public void FishingState_CanBeParsedFromString()
        {
            // Verify enum can be parsed from string names
            Assert.IsTrue(Enum.TryParse<FishingState>("Idle", out var idle));
            Assert.AreEqual(FishingState.Idle, idle);

            Assert.IsTrue(Enum.TryParse<FishingState>("HookOpportunity", out var hook));
            Assert.AreEqual(FishingState.HookOpportunity, hook);
        }

        [Test]
        public void FishingState_ToString_ReturnsExpectedName()
        {
            Assert.AreEqual("Idle", FishingState.Idle.ToString());
            Assert.AreEqual("Casting", FishingState.Casting.ToString());
            Assert.AreEqual("HookOpportunity", FishingState.HookOpportunity.ToString());
        }
    }

    [TestFixture]
    public class FishingInterfaceTests
    {
        // Mock implementation of IFishingState for testing interface contract
        private class MockFishingState : IFishingState
        {
            public int EnterCallCount { get; private set; }
            public int UpdateCallCount { get; private set; }
            public int ExitCallCount { get; private set; }
            public int GetNextStateCallCount { get; private set; }
            public FishingState? NextStateToReturn { get; set; }
            public IFishingContext LastContext { get; private set; }
            public float LastDeltaTime { get; private set; }

            public void Enter(IFishingContext context)
            {
                EnterCallCount++;
                LastContext = context;
            }

            public void Update(IFishingContext context, float deltaTime)
            {
                UpdateCallCount++;
                LastContext = context;
                LastDeltaTime = deltaTime;
            }

            public void Exit(IFishingContext context)
            {
                ExitCallCount++;
                LastContext = context;
            }

            public FishingState? GetNextState(IFishingContext context)
            {
                GetNextStateCallCount++;
                LastContext = context;
                return NextStateToReturn;
            }
        }

        // Mock implementation of IFishingContext for testing interface contract
        private class MockFishingContext : IFishingContext
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

            private float _randomValue = 0.5f;
            public void SetRandomValue(float value) => _randomValue = value;

            public float GetRandomValue() => _randomValue;
            public float GetRandomRange(float min, float max) => min + _randomValue * (max - min);
        }

        [Test]
        public void IFishingState_Enter_ReceivesContext()
        {
            var state = new MockFishingState();
            var context = new MockFishingContext { CurrentState = FishingState.Idle };

            state.Enter(context);

            Assert.AreEqual(1, state.EnterCallCount);
            Assert.AreSame(context, state.LastContext);
        }

        [Test]
        public void IFishingState_Update_ReceivesContextAndDeltaTime()
        {
            var state = new MockFishingState();
            var context = new MockFishingContext();

            state.Update(context, 0.016f);

            Assert.AreEqual(1, state.UpdateCallCount);
            Assert.AreSame(context, state.LastContext);
            Assert.AreEqual(0.016f, state.LastDeltaTime, 0.0001f);
        }

        [Test]
        public void IFishingState_Exit_ReceivesContext()
        {
            var state = new MockFishingState();
            var context = new MockFishingContext();

            state.Exit(context);

            Assert.AreEqual(1, state.ExitCallCount);
            Assert.AreSame(context, state.LastContext);
        }

        [Test]
        public void IFishingState_GetNextState_CanReturnNull()
        {
            var state = new MockFishingState { NextStateToReturn = null };
            var context = new MockFishingContext();

            var result = state.GetNextState(context);

            Assert.IsNull(result);
        }

        [Test]
        public void IFishingState_GetNextState_CanReturnState()
        {
            var state = new MockFishingState { NextStateToReturn = FishingState.Casting };
            var context = new MockFishingContext();

            var result = state.GetNextState(context);

            Assert.IsNotNull(result);
            Assert.AreEqual(FishingState.Casting, result.Value);
        }

        [Test]
        public void IFishingContext_StateInfo_IsAccessible()
        {
            var context = new MockFishingContext
            {
                CurrentState = FishingState.Reeling,
                TimeInState = 2.5f
            };

            Assert.AreEqual(FishingState.Reeling, context.CurrentState);
            Assert.AreEqual(2.5f, context.TimeInState, 0.001f);
        }

        [Test]
        public void IFishingContext_LureData_IsAccessible()
        {
            var context = new MockFishingContext
            {
                LurePosition = new Vector2(10f, 5f),
                LureVelocity = new Vector2(1f, 0.5f),
                LineLength = 15f,
                LineTension = 0.75f
            };

            Assert.AreEqual(new Vector2(10f, 5f), context.LurePosition);
            Assert.AreEqual(new Vector2(1f, 0.5f), context.LureVelocity);
            Assert.AreEqual(15f, context.LineLength, 0.001f);
            Assert.AreEqual(0.75f, context.LineTension, 0.001f);
        }

        [Test]
        public void IFishingContext_InputState_IsAccessible()
        {
            var context = new MockFishingContext
            {
                CastInputPressed = true,
                ReelInputHeld = true,
                SlackInputPressed = false,
                CancelInputPressed = false
            };

            Assert.IsTrue(context.CastInputPressed);
            Assert.IsTrue(context.ReelInputHeld);
            Assert.IsFalse(context.SlackInputPressed);
            Assert.IsFalse(context.CancelInputPressed);
        }

        [Test]
        public void IFishingContext_FishData_IsAccessible()
        {
            var context = new MockFishingContext
            {
                HasFishInterest = true,
                HasHookedFish = true,
                HookedFishId = "bass_01",
                FishStruggleIntensity = 0.8f
            };

            Assert.IsTrue(context.HasFishInterest);
            Assert.IsTrue(context.HasHookedFish);
            Assert.AreEqual("bass_01", context.HookedFishId);
            Assert.AreEqual(0.8f, context.FishStruggleIntensity, 0.001f);
        }

        [Test]
        public void IFishingContext_ZoneData_IsAccessible()
        {
            var context = new MockFishingContext
            {
                CurrentZoneId = "lake_01",
                BiteProbabilityModifier = 1.2f
            };

            Assert.AreEqual("lake_01", context.CurrentZoneId);
            Assert.AreEqual(1.2f, context.BiteProbabilityModifier, 0.001f);
        }

        [Test]
        public void IFishingContext_GetRandomValue_ReturnsValueInRange()
        {
            var context = new MockFishingContext();
            context.SetRandomValue(0.75f);

            float value = context.GetRandomValue();

            Assert.AreEqual(0.75f, value, 0.001f);
        }

        [Test]
        public void IFishingContext_GetRandomRange_ReturnsValueInRange()
        {
            var context = new MockFishingContext();
            context.SetRandomValue(0.5f); // 50% between min and max

            float value = context.GetRandomRange(10f, 20f);

            Assert.AreEqual(15f, value, 0.001f);
        }

        [Test]
        public void StateTransition_SimulatedFlow_Works()
        {
            // Simulate a basic state transition flow
            var context = new MockFishingContext { CurrentState = FishingState.Idle };
            var idleState = new MockFishingState { NextStateToReturn = FishingState.Casting };
            var castingState = new MockFishingState { NextStateToReturn = FishingState.LureDrift };

            // Enter Idle
            idleState.Enter(context);
            Assert.AreEqual(1, idleState.EnterCallCount);

            // Update Idle and check for transition
            idleState.Update(context, 0.016f);
            var nextState = idleState.GetNextState(context);
            Assert.AreEqual(FishingState.Casting, nextState);

            // Exit Idle, Enter Casting
            idleState.Exit(context);
            Assert.AreEqual(1, idleState.ExitCallCount);

            context.CurrentState = FishingState.Casting;
            castingState.Enter(context);
            Assert.AreEqual(1, castingState.EnterCallCount);
        }
    }
}
