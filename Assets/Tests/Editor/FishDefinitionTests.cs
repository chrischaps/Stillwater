using NUnit.Framework;
using UnityEngine;
using Stillwater.Fishing;

namespace Stillwater.Tests
{
    [TestFixture]
    public class FishDefinitionTests
    {
        private FishDefinition _fishDefinition;

        [SetUp]
        public void SetUp()
        {
            _fishDefinition = ScriptableObject.CreateInstance<FishDefinition>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_fishDefinition != null)
            {
                Object.DestroyImmediate(_fishDefinition);
            }
        }

        #region Default Value Tests

        [Test]
        public void Id_DefaultsToEmpty()
        {
            Assert.IsTrue(string.IsNullOrEmpty(_fishDefinition.Id));
        }

        [Test]
        public void DisplayName_DefaultsToEmpty()
        {
            Assert.IsTrue(string.IsNullOrEmpty(_fishDefinition.DisplayName));
        }

        [Test]
        public void Icon_DefaultsToNull()
        {
            Assert.IsNull(_fishDefinition.Icon);
        }

        [Test]
        public void BiteWindowCurve_DefaultsToNotNull()
        {
            Assert.IsNotNull(_fishDefinition.BiteWindowCurve);
        }

        [Test]
        public void MinWaitTime_DefaultsToPositive()
        {
            Assert.GreaterOrEqual(_fishDefinition.MinWaitTime, 0f);
        }

        [Test]
        public void MaxWaitTime_DefaultsToGreaterOrEqualMinWaitTime()
        {
            Assert.GreaterOrEqual(_fishDefinition.MaxWaitTime, _fishDefinition.MinWaitTime);
        }

        [Test]
        public void RarityBase_DefaultsToValidRange()
        {
            Assert.GreaterOrEqual(_fishDefinition.RarityBase, 0f);
            Assert.LessOrEqual(_fishDefinition.RarityBase, 1f);
        }

        [Test]
        public void FlavorTextId_DefaultsToEmpty()
        {
            Assert.IsTrue(string.IsNullOrEmpty(_fishDefinition.FlavorTextId));
        }

        #endregion

        #region EvaluateBiteWindow Tests

        [Test]
        public void EvaluateBiteWindow_AtZero_ReturnsValue()
        {
            float result = _fishDefinition.EvaluateBiteWindow(0f);
            // Just verify it returns a valid number
            Assert.IsFalse(float.IsNaN(result));
        }

        [Test]
        public void EvaluateBiteWindow_AtOne_ReturnsValue()
        {
            float result = _fishDefinition.EvaluateBiteWindow(1f);
            Assert.IsFalse(float.IsNaN(result));
        }

        [Test]
        public void EvaluateBiteWindow_AtMidpoint_ReturnsValue()
        {
            float result = _fishDefinition.EvaluateBiteWindow(0.5f);
            Assert.IsFalse(float.IsNaN(result));
        }

        [Test]
        public void EvaluateBiteWindow_ClampsNegativeInput()
        {
            float atZero = _fishDefinition.EvaluateBiteWindow(0f);
            float atNegative = _fishDefinition.EvaluateBiteWindow(-0.5f);
            Assert.AreEqual(atZero, atNegative, 0.001f);
        }

        [Test]
        public void EvaluateBiteWindow_ClampsOverOneInput()
        {
            float atOne = _fishDefinition.EvaluateBiteWindow(1f);
            float atOver = _fishDefinition.EvaluateBiteWindow(1.5f);
            Assert.AreEqual(atOne, atOver, 0.001f);
        }

        #endregion

        #region GetWaitTime Tests

        [Test]
        public void GetWaitTime_AtZero_ReturnsMinWaitTime()
        {
            float result = _fishDefinition.GetWaitTime(0f);
            Assert.AreEqual(_fishDefinition.MinWaitTime, result, 0.001f);
        }

        [Test]
        public void GetWaitTime_AtOne_ReturnsMaxWaitTime()
        {
            float result = _fishDefinition.GetWaitTime(1f);
            Assert.AreEqual(_fishDefinition.MaxWaitTime, result, 0.001f);
        }

        [Test]
        public void GetWaitTime_AtMidpoint_ReturnsMidValue()
        {
            float expected = (_fishDefinition.MinWaitTime + _fishDefinition.MaxWaitTime) / 2f;
            float result = _fishDefinition.GetWaitTime(0.5f);
            Assert.AreEqual(expected, result, 0.001f);
        }

        [Test]
        public void GetWaitTime_ClampsNegativeInput()
        {
            float result = _fishDefinition.GetWaitTime(-0.5f);
            Assert.AreEqual(_fishDefinition.MinWaitTime, result, 0.001f);
        }

        [Test]
        public void GetWaitTime_ClampsOverOneInput()
        {
            float result = _fishDefinition.GetWaitTime(1.5f);
            Assert.AreEqual(_fishDefinition.MaxWaitTime, result, 0.001f);
        }

        #endregion

        #region GetRandomWaitTime Tests

        [Test]
        public void GetRandomWaitTime_ReturnsWithinRange()
        {
            // Run multiple times to account for randomness
            for (int i = 0; i < 10; i++)
            {
                float result = _fishDefinition.GetRandomWaitTime();
                Assert.GreaterOrEqual(result, _fishDefinition.MinWaitTime);
                Assert.LessOrEqual(result, _fishDefinition.MaxWaitTime);
            }
        }

        #endregion

        #region IsValid Tests

        [Test]
        public void IsValid_WithDefaults_ReturnsFalse()
        {
            // Default has empty id and displayName
            Assert.IsFalse(_fishDefinition.IsValid());
        }

        [Test]
        public void IsValid_WithValidData_ReturnsTrue()
        {
            // Use SerializedObject to set private fields
            var serializedObject = new UnityEditor.SerializedObject(_fishDefinition);
            serializedObject.FindProperty("_id").stringValue = "test_fish";
            serializedObject.FindProperty("_displayName").stringValue = "Test Fish";
            serializedObject.FindProperty("_minWaitTime").floatValue = 1f;
            serializedObject.FindProperty("_maxWaitTime").floatValue = 5f;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            Assert.IsTrue(_fishDefinition.IsValid());
        }

        [Test]
        public void IsValid_WithEmptyId_ReturnsFalse()
        {
            var serializedObject = new UnityEditor.SerializedObject(_fishDefinition);
            serializedObject.FindProperty("_id").stringValue = "";
            serializedObject.FindProperty("_displayName").stringValue = "Test Fish";
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            Assert.IsFalse(_fishDefinition.IsValid());
        }

        [Test]
        public void IsValid_WithEmptyDisplayName_ReturnsFalse()
        {
            var serializedObject = new UnityEditor.SerializedObject(_fishDefinition);
            serializedObject.FindProperty("_id").stringValue = "test_fish";
            serializedObject.FindProperty("_displayName").stringValue = "";
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            Assert.IsFalse(_fishDefinition.IsValid());
        }

        [Test]
        public void IsValid_WithNegativeMinWaitTime_ReturnsFalse()
        {
            var serializedObject = new UnityEditor.SerializedObject(_fishDefinition);
            serializedObject.FindProperty("_id").stringValue = "test_fish";
            serializedObject.FindProperty("_displayName").stringValue = "Test Fish";
            serializedObject.FindProperty("_minWaitTime").floatValue = -1f;
            serializedObject.FindProperty("_maxWaitTime").floatValue = 5f;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            Assert.IsFalse(_fishDefinition.IsValid());
        }

        [Test]
        public void IsValid_WithMinGreaterThanMax_ReturnsFalse()
        {
            var serializedObject = new UnityEditor.SerializedObject(_fishDefinition);
            serializedObject.FindProperty("_id").stringValue = "test_fish";
            serializedObject.FindProperty("_displayName").stringValue = "Test Fish";
            serializedObject.FindProperty("_minWaitTime").floatValue = 10f;
            serializedObject.FindProperty("_maxWaitTime").floatValue = 5f;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            Assert.IsFalse(_fishDefinition.IsValid());
        }

        #endregion

        #region Property Access Tests

        [Test]
        public void Properties_CanBeReadAfterSerialization()
        {
            var serializedObject = new UnityEditor.SerializedObject(_fishDefinition);
            serializedObject.FindProperty("_id").stringValue = "test_id";
            serializedObject.FindProperty("_displayName").stringValue = "Test Display Name";
            serializedObject.FindProperty("_rarityBase").floatValue = 0.75f;
            serializedObject.FindProperty("_flavorTextId").stringValue = "flavor_test";
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            Assert.AreEqual("test_id", _fishDefinition.Id);
            Assert.AreEqual("Test Display Name", _fishDefinition.DisplayName);
            Assert.AreEqual(0.75f, _fishDefinition.RarityBase, 0.001f);
            Assert.AreEqual("flavor_test", _fishDefinition.FlavorTextId);
        }

        #endregion
    }
}
