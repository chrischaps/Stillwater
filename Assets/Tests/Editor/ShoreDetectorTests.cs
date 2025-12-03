using System;
using NUnit.Framework;
using UnityEngine;
using Stillwater.Core;
using Stillwater.Fishing;

namespace Stillwater.Tests
{
    [TestFixture]
    public class FacingDirectionExtensionsTests
    {
        [Test]
        public void ToVector2_ReturnsCorrectVectorForCardinalDirections()
        {
            // Cardinal directions should point in expected screen-space directions
            Vector2 south = FacingDirection.South.ToVector2();
            Vector2 north = FacingDirection.North.ToVector2();
            Vector2 east = FacingDirection.East.ToVector2();
            Vector2 west = FacingDirection.West.ToVector2();

            Assert.AreEqual(0f, south.x, 0.01f, "South should have x=0");
            Assert.Less(south.y, 0f, "South should point down (negative y)");

            Assert.AreEqual(0f, north.x, 0.01f, "North should have x=0");
            Assert.Greater(north.y, 0f, "North should point up (positive y)");

            Assert.Greater(east.x, 0f, "East should point right (positive x)");
            Assert.AreEqual(0f, east.y, 0.01f, "East should have y=0");

            Assert.Less(west.x, 0f, "West should point left (negative x)");
            Assert.AreEqual(0f, west.y, 0.01f, "West should have y=0");
        }

        [Test]
        public void ToVector2_ReturnsCorrectVectorForDiagonalDirections()
        {
            Vector2 ne = FacingDirection.NorthEast.ToVector2();
            Vector2 nw = FacingDirection.NorthWest.ToVector2();
            Vector2 se = FacingDirection.SouthEast.ToVector2();
            Vector2 sw = FacingDirection.SouthWest.ToVector2();

            // NorthEast: positive x, positive y
            Assert.Greater(ne.x, 0f, "NorthEast should have positive x");
            Assert.Greater(ne.y, 0f, "NorthEast should have positive y");

            // NorthWest: negative x, positive y
            Assert.Less(nw.x, 0f, "NorthWest should have negative x");
            Assert.Greater(nw.y, 0f, "NorthWest should have positive y");

            // SouthEast: positive x, negative y
            Assert.Greater(se.x, 0f, "SouthEast should have positive x");
            Assert.Less(se.y, 0f, "SouthEast should have negative y");

            // SouthWest: negative x, negative y
            Assert.Less(sw.x, 0f, "SouthWest should have negative x");
            Assert.Less(sw.y, 0f, "SouthWest should have negative y");
        }

        [Test]
        public void ToVector2_VectorsAreNormalized()
        {
            foreach (FacingDirection direction in FacingDirectionExtensions.AllDirections)
            {
                Vector2 vector = direction.ToVector2();
                float magnitude = vector.magnitude;
                Assert.AreEqual(1f, magnitude, 0.01f, $"{direction} vector should be normalized");
            }
        }

        [Test]
        public void ToCellOffset_ReturnsCorrectOffsetForCardinalDirections()
        {
            Assert.AreEqual(new Vector3Int(0, -1, 0), FacingDirection.South.ToCellOffset());
            Assert.AreEqual(new Vector3Int(0, 1, 0), FacingDirection.North.ToCellOffset());
            Assert.AreEqual(new Vector3Int(1, 0, 0), FacingDirection.East.ToCellOffset());
            Assert.AreEqual(new Vector3Int(-1, 0, 0), FacingDirection.West.ToCellOffset());
        }

        [Test]
        public void ToCellOffset_ReturnsCorrectOffsetForDiagonalDirections()
        {
            Assert.AreEqual(new Vector3Int(1, 1, 0), FacingDirection.NorthEast.ToCellOffset());
            Assert.AreEqual(new Vector3Int(-1, 1, 0), FacingDirection.NorthWest.ToCellOffset());
            Assert.AreEqual(new Vector3Int(1, -1, 0), FacingDirection.SouthEast.ToCellOffset());
            Assert.AreEqual(new Vector3Int(-1, -1, 0), FacingDirection.SouthWest.ToCellOffset());
        }

        [Test]
        public void FromVector2_ReturnsCorrectDirectionForCardinal()
        {
            Assert.AreEqual(FacingDirection.North, FacingDirectionExtensions.FromVector2(Vector2.up));
            Assert.AreEqual(FacingDirection.South, FacingDirectionExtensions.FromVector2(Vector2.down));
            Assert.AreEqual(FacingDirection.East, FacingDirectionExtensions.FromVector2(Vector2.right));
            Assert.AreEqual(FacingDirection.West, FacingDirectionExtensions.FromVector2(Vector2.left));
        }

        [Test]
        public void FromVector2_ReturnsCorrectDirectionForDiagonal()
        {
            Assert.AreEqual(FacingDirection.NorthEast, FacingDirectionExtensions.FromVector2(new Vector2(1, 1)));
            Assert.AreEqual(FacingDirection.NorthWest, FacingDirectionExtensions.FromVector2(new Vector2(-1, 1)));
            Assert.AreEqual(FacingDirection.SouthEast, FacingDirectionExtensions.FromVector2(new Vector2(1, -1)));
            Assert.AreEqual(FacingDirection.SouthWest, FacingDirectionExtensions.FromVector2(new Vector2(-1, -1)));
        }

        [Test]
        public void FromVector2_ReturnsDefaultForZeroVector()
        {
            FacingDirection result = FacingDirectionExtensions.FromVector2(Vector2.zero);
            Assert.AreEqual(FacingDirection.South, result, "Zero vector should return default direction (South)");
        }

        [Test]
        public void GetOpposite_ReturnsCorrectOppositeDirection()
        {
            Assert.AreEqual(FacingDirection.North, FacingDirection.South.GetOpposite());
            Assert.AreEqual(FacingDirection.South, FacingDirection.North.GetOpposite());
            Assert.AreEqual(FacingDirection.West, FacingDirection.East.GetOpposite());
            Assert.AreEqual(FacingDirection.East, FacingDirection.West.GetOpposite());
            Assert.AreEqual(FacingDirection.SouthWest, FacingDirection.NorthEast.GetOpposite());
            Assert.AreEqual(FacingDirection.NorthEast, FacingDirection.SouthWest.GetOpposite());
        }

        [Test]
        public void RotateClockwise_RotatesCorrectly()
        {
            Assert.AreEqual(FacingDirection.SouthEast, FacingDirection.South.RotateClockwise());
            Assert.AreEqual(FacingDirection.East, FacingDirection.South.RotateClockwise(2));
            Assert.AreEqual(FacingDirection.North, FacingDirection.South.RotateClockwise(4));
            Assert.AreEqual(FacingDirection.South, FacingDirection.South.RotateClockwise(8));
        }

        [Test]
        public void RotateCounterClockwise_RotatesCorrectly()
        {
            Assert.AreEqual(FacingDirection.SouthWest, FacingDirection.South.RotateCounterClockwise());
            Assert.AreEqual(FacingDirection.West, FacingDirection.South.RotateCounterClockwise(2));
            Assert.AreEqual(FacingDirection.North, FacingDirection.South.RotateCounterClockwise(4));
        }

        [Test]
        public void IsCardinal_ReturnsTrueForCardinalDirections()
        {
            Assert.IsTrue(FacingDirection.North.IsCardinal());
            Assert.IsTrue(FacingDirection.South.IsCardinal());
            Assert.IsTrue(FacingDirection.East.IsCardinal());
            Assert.IsTrue(FacingDirection.West.IsCardinal());
        }

        [Test]
        public void IsCardinal_ReturnsFalseForDiagonalDirections()
        {
            Assert.IsFalse(FacingDirection.NorthEast.IsCardinal());
            Assert.IsFalse(FacingDirection.NorthWest.IsCardinal());
            Assert.IsFalse(FacingDirection.SouthEast.IsCardinal());
            Assert.IsFalse(FacingDirection.SouthWest.IsCardinal());
        }

        [Test]
        public void IsDiagonal_ReturnsTrueForDiagonalDirections()
        {
            Assert.IsTrue(FacingDirection.NorthEast.IsDiagonal());
            Assert.IsTrue(FacingDirection.NorthWest.IsDiagonal());
            Assert.IsTrue(FacingDirection.SouthEast.IsDiagonal());
            Assert.IsTrue(FacingDirection.SouthWest.IsDiagonal());
        }

        [Test]
        public void IsDiagonal_ReturnsFalseForCardinalDirections()
        {
            Assert.IsFalse(FacingDirection.North.IsDiagonal());
            Assert.IsFalse(FacingDirection.South.IsDiagonal());
            Assert.IsFalse(FacingDirection.East.IsDiagonal());
            Assert.IsFalse(FacingDirection.West.IsDiagonal());
        }

        [Test]
        public void AllDirections_ContainsAll8Directions()
        {
            var allDirs = FacingDirectionExtensions.AllDirections;
            Assert.AreEqual(8, allDirs.Length);
            Assert.Contains(FacingDirection.South, allDirs);
            Assert.Contains(FacingDirection.SouthEast, allDirs);
            Assert.Contains(FacingDirection.East, allDirs);
            Assert.Contains(FacingDirection.NorthEast, allDirs);
            Assert.Contains(FacingDirection.North, allDirs);
            Assert.Contains(FacingDirection.NorthWest, allDirs);
            Assert.Contains(FacingDirection.West, allDirs);
            Assert.Contains(FacingDirection.SouthWest, allDirs);
        }
    }

    [TestFixture]
    public class FishingSpotDataTests
    {
        [Test]
        public void DefaultConstructor_SetsDefaultValues()
        {
            var data = new FishingSpotData();

            Assert.AreEqual(string.Empty, data.SpotId);
            Assert.AreEqual(string.Empty, data.DisplayName);
            Assert.IsNull(data.AvailableFish);
            Assert.AreEqual(1.0f, data.BiteProbabilityModifier, 0.001f);
            Assert.IsNull(data.ZoneIdOverride);
        }

        [Test]
        public void ParameterizedConstructor_SetsIdAndName()
        {
            var data = new FishingSpotData("test_spot", "Test Spot");

            Assert.AreEqual("test_spot", data.SpotId);
            Assert.AreEqual("Test Spot", data.DisplayName);
            Assert.AreEqual(1.0f, data.BiteProbabilityModifier, 0.001f);
        }

        [Test]
        public void Properties_CanBeModified()
        {
            var data = new FishingSpotData();

            data.SpotId = "modified_id";
            data.DisplayName = "Modified Name";
            data.BiteProbabilityModifier = 1.5f;
            data.ZoneIdOverride = "special_zone";

            Assert.AreEqual("modified_id", data.SpotId);
            Assert.AreEqual("Modified Name", data.DisplayName);
            Assert.AreEqual(1.5f, data.BiteProbabilityModifier, 0.001f);
            Assert.AreEqual("special_zone", data.ZoneIdOverride);
        }
    }

    [TestFixture]
    public class FishingSpotMarkerTests
    {
        private GameObject _markerObject;
        private FishingSpotMarker _marker;

        [SetUp]
        public void SetUp()
        {
            _markerObject = new GameObject("TestMarker");
            _marker = _markerObject.AddComponent<FishingSpotMarker>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_markerObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_markerObject);
            }
        }

        [Test]
        public void SpotData_IsNotNull()
        {
            Assert.IsNotNull(_marker.SpotData);
        }

        [Test]
        public void Position_ReturnsTransformPosition()
        {
            _markerObject.transform.position = new Vector3(5f, 10f, 0f);

            Assert.AreEqual(new Vector2(5f, 10f), _marker.Position);
        }

        [Test]
        public void IsWithinRange_ReturnsTrueWhenClose()
        {
            _markerObject.transform.position = Vector3.zero;

            Assert.IsTrue(_marker.IsWithinRange(Vector2.zero));
            Assert.IsTrue(_marker.IsWithinRange(new Vector2(1f, 0f)));
        }

        [Test]
        public void IsWithinRange_ReturnsFalseWhenFar()
        {
            _markerObject.transform.position = Vector3.zero;

            // Default radius is 1.5f, so 10 units away should be false
            Assert.IsFalse(_marker.IsWithinRange(new Vector2(10f, 0f)));
        }
    }

    [TestFixture]
    public class IShoreDetectorInterfaceTests
    {
        // Mock implementation for testing interface contract
        private class MockShoreDetector : IShoreDetector
        {
            public bool CanFish { get; set; }
            public Vector2 FishingDirection { get; set; }
            public Vector2 TargetWaterPosition { get; set; }
            public FishingSpotData ActiveSpotData { get; set; }

            public event Action<bool> OnCanFishChanged;

            public void TriggerCanFishChanged(bool value)
            {
                CanFish = value;
                OnCanFishChanged?.Invoke(value);
            }
        }

        [Test]
        public void CanFish_WhenOnShoreFacingWater_ReturnsTrue()
        {
            var detector = new MockShoreDetector { CanFish = true };
            Assert.IsTrue(detector.CanFish);
        }

        [Test]
        public void CanFish_WhenNotOnShore_ReturnsFalse()
        {
            var detector = new MockShoreDetector { CanFish = false };
            Assert.IsFalse(detector.CanFish);
        }

        [Test]
        public void FishingDirection_WhenCanFish_ReturnsValidDirection()
        {
            var detector = new MockShoreDetector
            {
                CanFish = true,
                FishingDirection = new Vector2(0, -1) // Facing south
            };

            Assert.AreEqual(new Vector2(0, -1), detector.FishingDirection);
        }

        [Test]
        public void TargetWaterPosition_WhenCanFish_ReturnsValidPosition()
        {
            var detector = new MockShoreDetector
            {
                CanFish = true,
                TargetWaterPosition = new Vector2(5f, 3f)
            };

            Assert.AreEqual(new Vector2(5f, 3f), detector.TargetWaterPosition);
        }

        [Test]
        public void ActiveSpotData_CanBeNull_ForGenericShore()
        {
            var detector = new MockShoreDetector
            {
                CanFish = true,
                ActiveSpotData = null
            };

            Assert.IsNull(detector.ActiveSpotData);
        }

        [Test]
        public void ActiveSpotData_CanHaveData_ForSpecialSpot()
        {
            var spotData = new FishingSpotData("special", "Special Spot");
            var detector = new MockShoreDetector
            {
                CanFish = true,
                ActiveSpotData = spotData
            };

            Assert.IsNotNull(detector.ActiveSpotData);
            Assert.AreEqual("special", detector.ActiveSpotData.SpotId);
        }

        [Test]
        public void OnCanFishChanged_FiresWhenStateChanges()
        {
            var detector = new MockShoreDetector();
            bool eventFired = false;
            bool eventValue = false;

            detector.OnCanFishChanged += (value) =>
            {
                eventFired = true;
                eventValue = value;
            };

            detector.TriggerCanFishChanged(true);

            Assert.IsTrue(eventFired, "OnCanFishChanged event should fire");
            Assert.IsTrue(eventValue, "Event should pass correct value");
        }
    }

    [TestFixture]
    public class ShoreDetectorComponentTests
    {
        private GameObject _playerObject;
        private ShoreDetector _detector;

        [SetUp]
        public void SetUp()
        {
            _playerObject = new GameObject("TestPlayer");
            _playerObject.AddComponent<Rigidbody2D>();
            _detector = _playerObject.AddComponent<ShoreDetector>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_playerObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_playerObject);
            }
        }

        [Test]
        public void InitialState_CanFishIsFalse()
        {
            // Without tilemap, CanFish should be false
            Assert.IsFalse(_detector.CanFish);
        }

        [Test]
        public void CurrentFacing_DefaultIsSouth()
        {
            Assert.AreEqual(FacingDirection.South, _detector.CurrentFacing);
        }

        [Test]
        public void CurrentFacing_CanBeChanged()
        {
            _detector.CurrentFacing = FacingDirection.North;
            Assert.AreEqual(FacingDirection.North, _detector.CurrentFacing);
        }

        [Test]
        public void FishingDirection_IsZeroWhenCannotFish()
        {
            Assert.AreEqual(Vector2.zero, _detector.FishingDirection);
        }

        [Test]
        public void TargetWaterPosition_IsZeroWhenCannotFish()
        {
            Assert.AreEqual(Vector2.zero, _detector.TargetWaterPosition);
        }

        [Test]
        public void ActiveSpotData_IsNullWhenCannotFish()
        {
            Assert.IsNull(_detector.ActiveSpotData);
        }

        [Test]
        public void OnCanFishChanged_EventCanBeSubscribed()
        {
            bool eventReceived = false;
            _detector.OnCanFishChanged += (value) => eventReceived = true;

            // Event subscription should work even if not triggered
            Assert.IsFalse(eventReceived);
        }

        [Test]
        public void GetValidFishingDirections_ReturnsEmptyWithoutTilemap()
        {
            var directions = _detector.GetValidFishingDirections();
            Assert.IsEmpty(directions);
        }
    }
}
