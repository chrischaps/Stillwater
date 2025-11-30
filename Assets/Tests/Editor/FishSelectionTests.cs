using NUnit.Framework;
using UnityEngine;
using Stillwater.Fishing;
using Stillwater.Core;

namespace Stillwater.Tests
{
    [TestFixture]
    public class FishSelectionTests
    {
        private GameObject _gameObject;
        private FishingController _controller;
        private FishDefinition _commonFish;
        private FishDefinition _rareFish;
        private FishDefinition _veryRareFish;

        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();

            _gameObject = new GameObject("TestFishingController");
            _controller = _gameObject.AddComponent<FishingController>();
            _controller.Initialize();

            // Create test fish definitions
            _commonFish = CreateFishDefinition("common_fish", "Common Fish", 0.8f);
            _rareFish = CreateFishDefinition("rare_fish", "Rare Fish", 0.15f);
            _veryRareFish = CreateFishDefinition("very_rare_fish", "Very Rare Fish", 0.05f);
        }

        [TearDown]
        public void TearDown()
        {
            if (_gameObject != null)
            {
                Object.DestroyImmediate(_gameObject);
            }

            if (_commonFish != null) Object.DestroyImmediate(_commonFish);
            if (_rareFish != null) Object.DestroyImmediate(_rareFish);
            if (_veryRareFish != null) Object.DestroyImmediate(_veryRareFish);

            EventBus.Clear();
        }

        private FishDefinition CreateFishDefinition(string id, string displayName, float rarity)
        {
            var fish = ScriptableObject.CreateInstance<FishDefinition>();
            var serializedObject = new UnityEditor.SerializedObject(fish);
            serializedObject.FindProperty("_id").stringValue = id;
            serializedObject.FindProperty("_displayName").stringValue = displayName;
            serializedObject.FindProperty("_rarityBase").floatValue = rarity;
            serializedObject.FindProperty("_minWaitTime").floatValue = 1f;
            serializedObject.FindProperty("_maxWaitTime").floatValue = 5f;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            return fish;
        }

        #region AvailableFish Tests

        [Test]
        public void AvailableFish_DefaultsToNull()
        {
            Assert.IsNull(_controller.AvailableFish);
        }

        [Test]
        public void SetAvailableFish_SetsArray()
        {
            var fish = new[] { _commonFish, _rareFish };
            _controller.SetAvailableFish(fish);

            Assert.AreEqual(2, _controller.AvailableFish.Length);
        }

        #endregion

        #region SelectedFish Tests

        [Test]
        public void SelectedFish_DefaultsToNull()
        {
            Assert.IsNull(_controller.SelectedFish);
        }

        [Test]
        public void SetSelectedFish_SetsValue()
        {
            _controller.SetSelectedFish(_commonFish);

            Assert.AreEqual(_commonFish, _controller.SelectedFish);
        }

        [Test]
        public void ClearHookedFish_ClearsSelectedFish()
        {
            _controller.SetSelectedFish(_commonFish);
            _controller.ClearHookedFish();

            Assert.IsNull(_controller.SelectedFish);
        }

        #endregion

        #region SelectRandomFish Tests

        [Test]
        public void SelectRandomFish_WithNoFish_ReturnsNull()
        {
            _controller.SetAvailableFish(null);

            var result = _controller.SelectRandomFish();

            Assert.IsNull(result);
        }

        [Test]
        public void SelectRandomFish_WithEmptyArray_ReturnsNull()
        {
            _controller.SetAvailableFish(new FishDefinition[0]);

            var result = _controller.SelectRandomFish();

            Assert.IsNull(result);
        }

        [Test]
        public void SelectRandomFish_WithSingleFish_ReturnsThatFish()
        {
            _controller.SetAvailableFish(new[] { _commonFish });

            var result = _controller.SelectRandomFish();

            Assert.AreEqual(_commonFish, result);
            Assert.AreEqual(_commonFish, _controller.SelectedFish);
        }

        [Test]
        public void SelectRandomFish_WithMultipleFish_ReturnsAFish()
        {
            _controller.SetAvailableFish(new[] { _commonFish, _rareFish, _veryRareFish });

            var result = _controller.SelectRandomFish();

            Assert.IsNotNull(result);
            Assert.Contains(result, new[] { _commonFish, _rareFish, _veryRareFish });
        }

        [Test]
        public void SelectRandomFish_SetsSelectedFish()
        {
            _controller.SetAvailableFish(new[] { _commonFish });

            _controller.SelectRandomFish();

            Assert.AreEqual(_commonFish, _controller.SelectedFish);
        }

        [Test]
        public void SelectRandomFish_WithNullsInArray_SkipsNulls()
        {
            _controller.SetAvailableFish(new FishDefinition[] { null, _commonFish, null });

            var result = _controller.SelectRandomFish();

            Assert.AreEqual(_commonFish, result);
        }

        [Test]
        public void SelectRandomFish_WeightsByRarity_CommonMoreLikely()
        {
            // Run many selections and verify common fish is selected more often
            _controller.SetAvailableFish(new[] { _commonFish, _veryRareFish });

            int commonCount = 0;
            int rareCount = 0;

            for (int i = 0; i < 100; i++)
            {
                var result = _controller.SelectRandomFish();
                if (result == _commonFish) commonCount++;
                else if (result == _veryRareFish) rareCount++;
            }

            // Common fish (0.8 rarity) should be selected much more often than very rare (0.05 rarity)
            // Expected ratio is roughly 16:1
            Assert.Greater(commonCount, rareCount, "Common fish should be selected more often");
            Assert.Greater(commonCount, 50, "Common fish should be selected majority of the time");
        }

        #endregion

        #region IFishingContext Integration Tests

        [Test]
        public void IFishingContext_SelectedFish_ReturnsCorrectValue()
        {
            _controller.SetSelectedFish(_rareFish);
            IFishingContext context = _controller;

            Assert.AreEqual(_rareFish, context.SelectedFish);
        }

        [Test]
        public void IFishingContext_AvailableFish_ReturnsCorrectValue()
        {
            var fish = new[] { _commonFish, _rareFish };
            _controller.SetAvailableFish(fish);
            IFishingContext context = _controller;

            Assert.AreEqual(fish, context.AvailableFish);
        }

        #endregion

        #region FishCaughtEvent Tests

        [Test]
        public void FishCaughtEvent_PublishedOnCaughtState()
        {
            _controller.SetAvailableFish(new[] { _commonFish });
            _controller.SetSelectedFish(_commonFish);

            FishCaughtEvent? capturedEvent = null;
            EventBus.Subscribe<FishCaughtEvent>(e => capturedEvent = e);

            // Simulate entering Caught state
            EventBus.Publish(new FishingStateChangedEvent
            {
                PreviousState = "Reeling",
                NewState = "Caught"
            });

            Assert.IsNotNull(capturedEvent, "FishCaughtEvent should be published");
            Assert.AreEqual("common_fish", capturedEvent.Value.FishId);
            Assert.AreEqual(_commonFish, capturedEvent.Value.FishDefinition);
        }

        [Test]
        public void FishCaughtEvent_ContainsZoneId()
        {
            _controller.SetAvailableFish(new[] { _commonFish });
            _controller.SetSelectedFish(_commonFish);
            _controller.SetZone("test_zone");

            FishCaughtEvent? capturedEvent = null;
            EventBus.Subscribe<FishCaughtEvent>(e => capturedEvent = e);

            EventBus.Publish(new FishingStateChangedEvent
            {
                PreviousState = "Reeling",
                NewState = "Caught"
            });

            Assert.AreEqual("test_zone", capturedEvent.Value.ZoneId);
        }

        [Test]
        public void FishCaughtEvent_IsRare_TrueForRareFish()
        {
            _controller.SetSelectedFish(_veryRareFish); // 0.05 rarity < 0.3 threshold

            FishCaughtEvent? capturedEvent = null;
            EventBus.Subscribe<FishCaughtEvent>(e => capturedEvent = e);

            EventBus.Publish(new FishingStateChangedEvent
            {
                PreviousState = "Reeling",
                NewState = "Caught"
            });

            Assert.IsTrue(capturedEvent.Value.IsRare);
        }

        [Test]
        public void FishCaughtEvent_IsRare_FalseForCommonFish()
        {
            _controller.SetSelectedFish(_commonFish); // 0.8 rarity > 0.3 threshold

            FishCaughtEvent? capturedEvent = null;
            EventBus.Subscribe<FishCaughtEvent>(e => capturedEvent = e);

            EventBus.Publish(new FishingStateChangedEvent
            {
                PreviousState = "Reeling",
                NewState = "Caught"
            });

            Assert.IsFalse(capturedEvent.Value.IsRare);
        }

        #endregion

        #region State Transition Tests

        [Test]
        public void HookOpportunityState_TriggersFishSelection()
        {
            _controller.SetAvailableFish(new[] { _commonFish });

            // Simulate entering HookOpportunity state
            EventBus.Publish(new FishingStateChangedEvent
            {
                PreviousState = "BiteCheck",
                NewState = "HookOpportunity"
            });

            Assert.IsNotNull(_controller.SelectedFish, "Fish should be selected on HookOpportunity");
        }

        [Test]
        public void IdleState_ClearsSelectedFish()
        {
            _controller.SetSelectedFish(_commonFish);

            // Simulate returning to Idle
            EventBus.Publish(new FishingStateChangedEvent
            {
                PreviousState = "Lost",
                NewState = "Idle"
            });

            Assert.IsNull(_controller.SelectedFish, "Selected fish should be cleared on Idle");
        }

        [Test]
        public void IdleToIdle_DoesNotClearFish()
        {
            _controller.SetSelectedFish(_commonFish);

            // Simulate Idle to Idle transition (edge case)
            EventBus.Publish(new FishingStateChangedEvent
            {
                PreviousState = "Idle",
                NewState = "Idle"
            });

            // Fish should not be cleared on self-transition
            Assert.AreEqual(_commonFish, _controller.SelectedFish);
        }

        #endregion
    }
}
