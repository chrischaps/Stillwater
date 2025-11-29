using NUnit.Framework;
using Stillwater.Core;
using Stillwater.Framework;

namespace Stillwater.Tests
{
    [TestFixture]
    public class GameRootTests
    {
        [SetUp]
        public void SetUp()
        {
            // Ensure clean state before each test
            GameRoot.ResetForTesting();
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up after each test
            GameRoot.ResetForTesting();
        }

        [Test]
        public void IsInitialized_DefaultsToFalse()
        {
            Assert.IsFalse(GameRoot.IsInitialized);
        }

        [Test]
        public void Instance_DefaultsToNull()
        {
            Assert.IsNull(GameRoot.Instance);
        }

        [Test]
        public void ResetForTesting_ClearsIsInitialized()
        {
            // We can't easily set IsInitialized to true without instantiating GameRoot,
            // but we can verify that ResetForTesting doesn't throw and leaves state clean
            GameRoot.ResetForTesting();

            Assert.IsFalse(GameRoot.IsInitialized);
            Assert.IsNull(GameRoot.Instance);
        }

        [Test]
        public void ResetForTesting_ClearsServiceLocator()
        {
            // Register a service
            ServiceLocator.Register<ITestService>(new TestService());
            Assert.IsTrue(ServiceLocator.IsRegistered<ITestService>());

            // Reset should clear it
            GameRoot.ResetForTesting();

            Assert.IsFalse(ServiceLocator.IsRegistered<ITestService>());
            Assert.AreEqual(0, ServiceLocator.ServiceCount);
        }

        [Test]
        public void ResetForTesting_ClearsEventBus()
        {
            // Subscribe to an event
            EventBus.Subscribe<GameInitializedEvent>(e => { });
            Assert.AreEqual(1, EventBus.GetSubscriberCount<GameInitializedEvent>());

            // Reset should clear it
            GameRoot.ResetForTesting();

            Assert.AreEqual(0, EventBus.GetSubscriberCount<GameInitializedEvent>());
        }

        // Test helpers
        private interface ITestService { }
        private class TestService : ITestService { }
    }
}
