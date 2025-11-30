using NUnit.Framework;
using UnityEngine;
using Stillwater.Fishing;
using Stillwater.Core;

namespace Stillwater.Tests
{
    [TestFixture]
    public class LureControllerTests
    {
        private GameObject _gameObject;
        private LureController _lureController;

        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();

            _gameObject = new GameObject("TestLureController");
            _lureController = _gameObject.AddComponent<LureController>();

            // Ensure event subscriptions are active (OnEnable may not be called in EditMode tests)
            _lureController.SubscribeToEvents();
        }

        [TearDown]
        public void TearDown()
        {
            if (_gameObject != null)
            {
                Object.DestroyImmediate(_gameObject);
            }
            EventBus.Clear();
        }

        #region Initial State Tests

        [Test]
        public void IsActive_Initially_ReturnsFalse()
        {
            Assert.IsFalse(_lureController.IsActive);
        }

        [Test]
        public void Position_Initially_ReturnsZero()
        {
            Assert.AreEqual(Vector2.zero, _lureController.Position);
        }

        [Test]
        public void Velocity_Initially_ReturnsZero()
        {
            Assert.AreEqual(Vector2.zero, _lureController.Velocity);
        }

        [Test]
        public void ActiveLure_Initially_ReturnsNull()
        {
            Assert.IsNull(_lureController.ActiveLure);
        }

        #endregion

        #region SpawnLure Tests

        [Test]
        public void SpawnLure_SetsIsActiveTrue()
        {
            _lureController.SpawnLure(new Vector2(5, 5));

            Assert.IsTrue(_lureController.IsActive);
        }

        [Test]
        public void SpawnLure_SetsPosition()
        {
            Vector2 position = new Vector2(3, 4);

            _lureController.SpawnLure(position);

            Assert.AreEqual(position, _lureController.Position);
        }

        [Test]
        public void SpawnLure_CreatesActiveLure()
        {
            _lureController.SpawnLure(new Vector2(1, 1));

            Assert.IsNotNull(_lureController.ActiveLure);
        }

        [Test]
        public void SpawnLure_WithDirection_SetsVelocity()
        {
            Vector2 position = new Vector2(5, 5);
            Vector2 direction = new Vector2(1, 0);

            _lureController.SpawnLure(position, direction);

            // Should have some velocity in the direction
            Assert.Greater(_lureController.Velocity.x, 0);
        }

        [Test]
        public void SpawnLure_WithoutDirection_HasZeroVelocity()
        {
            _lureController.SpawnLure(new Vector2(5, 5));

            Assert.AreEqual(Vector2.zero, _lureController.Velocity);
        }

        [Test]
        public void SpawnLure_WhenAlreadyActive_DespawnsPreviousFirst()
        {
            _lureController.SpawnLure(new Vector2(1, 1));
            var firstLure = _lureController.ActiveLure;

            _lureController.SpawnLure(new Vector2(5, 5));

            // First lure should be destroyed
            Assert.IsTrue(firstLure == null);
            Assert.IsNotNull(_lureController.ActiveLure);
        }

        #endregion

        #region DespawnLure Tests

        [Test]
        public void DespawnLure_SetsIsActiveFalse()
        {
            _lureController.SpawnLure(new Vector2(5, 5));

            _lureController.DespawnLure();

            Assert.IsFalse(_lureController.IsActive);
        }

        [Test]
        public void DespawnLure_SetsActiveLureNull()
        {
            _lureController.SpawnLure(new Vector2(5, 5));

            _lureController.DespawnLure();

            Assert.IsNull(_lureController.ActiveLure);
        }

        [Test]
        public void DespawnLure_ResetsVelocity()
        {
            _lureController.SpawnLure(new Vector2(5, 5), new Vector2(1, 0));

            _lureController.DespawnLure();

            Assert.AreEqual(Vector2.zero, _lureController.Velocity);
        }

        [Test]
        public void DespawnLure_WhenNotActive_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _lureController.DespawnLure());
        }

        #endregion

        #region SetVelocity Tests

        [Test]
        public void SetVelocity_UpdatesVelocity()
        {
            _lureController.SpawnLure(new Vector2(0, 0));
            Vector2 newVelocity = new Vector2(2, 3);

            _lureController.SetVelocity(newVelocity);

            Assert.AreEqual(newVelocity, _lureController.Velocity);
        }

        #endregion

        #region AddImpulse Tests

        [Test]
        public void AddImpulse_AddsToVelocity()
        {
            _lureController.SpawnLure(new Vector2(0, 0));
            _lureController.SetVelocity(new Vector2(1, 0));
            Vector2 impulse = new Vector2(0, 2);

            _lureController.AddImpulse(impulse);

            Assert.AreEqual(new Vector2(1, 2), _lureController.Velocity);
        }

        #endregion

        #region SetPosition Tests

        [Test]
        public void SetPosition_UpdatesPosition()
        {
            _lureController.SpawnLure(new Vector2(0, 0));
            Vector2 newPosition = new Vector2(10, 15);

            _lureController.SetPosition(newPosition);

            Assert.AreEqual(newPosition, _lureController.Position);
        }

        #endregion

        #region DriftDrag Property Tests

        [Test]
        public void DriftDrag_CanBeSet()
        {
            _lureController.DriftDrag = 5f;

            Assert.AreEqual(5f, _lureController.DriftDrag, 0.001f);
        }

        [Test]
        public void DriftDrag_ClampsNegativeToZero()
        {
            _lureController.DriftDrag = -1f;

            Assert.AreEqual(0f, _lureController.DriftDrag, 0.001f);
        }

        #endregion

        #region Event Integration Tests

        [Test]
        public void FishingStateChanged_ToCaught_DespawnsLure()
        {
            _lureController.SpawnLure(new Vector2(5, 5));

            EventBus.Publish(new FishingStateChangedEvent
            {
                PreviousState = "Reeling",
                NewState = "Caught"
            });

            Assert.IsFalse(_lureController.IsActive);
        }

        [Test]
        public void FishingStateChanged_ToLost_DespawnsLure()
        {
            _lureController.SpawnLure(new Vector2(5, 5));

            EventBus.Publish(new FishingStateChangedEvent
            {
                PreviousState = "Reeling",
                NewState = "Lost"
            });

            Assert.IsFalse(_lureController.IsActive);
        }

        [Test]
        public void FishingStateChanged_ToIdle_FromNonIdle_DespawnsLure()
        {
            _lureController.SpawnLure(new Vector2(5, 5));

            EventBus.Publish(new FishingStateChangedEvent
            {
                PreviousState = "LureDrift",
                NewState = "Idle"
            });

            Assert.IsFalse(_lureController.IsActive);
        }

        [Test]
        public void FishingStateChanged_ToIdle_FromIdle_DoesNotDespawn()
        {
            // This covers the initial state case where previous is also Idle
            _lureController.SpawnLure(new Vector2(5, 5));

            EventBus.Publish(new FishingStateChangedEvent
            {
                PreviousState = "Idle",
                NewState = "Idle"
            });

            // Should still be active because this is a self-transition
            Assert.IsTrue(_lureController.IsActive);
        }

        [Test]
        public void FishingStateChanged_ToLureDrift_DoesNotAutoDespawn()
        {
            _lureController.SpawnLure(new Vector2(5, 5));

            EventBus.Publish(new FishingStateChangedEvent
            {
                PreviousState = "Casting",
                NewState = "LureDrift"
            });

            // Should remain active - spawning is handled by FishingController
            Assert.IsTrue(_lureController.IsActive);
        }

        #endregion

        #region Integration Tests

        [Test]
        public void SetFishingController_SetsReference()
        {
            var controllerGO = new GameObject("Controller");
            var fishingController = controllerGO.AddComponent<FishingController>();
            fishingController.Initialize();

            try
            {
                _lureController.SetFishingController(fishingController);
                // Should not throw
                Assert.Pass();
            }
            finally
            {
                Object.DestroyImmediate(controllerGO);
            }
        }

        #endregion
    }
}
