using System;
using NUnit.Framework;
using Stillwater.Framework;

namespace Stillwater.Tests
{
    [TestFixture]
    public class ServiceLocatorTests
    {
        // Test interface and implementation
        private interface ITestService
        {
            string Name { get; }
        }

        private class TestService : ITestService
        {
            public string Name { get; }
            public TestService(string name = "TestService") => Name = name;
        }

        private interface IAnotherService { }
        private class AnotherService : IAnotherService { }

        [SetUp]
        public void SetUp()
        {
            // Clear all services before each test
            ServiceLocator.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up after each test
            ServiceLocator.Clear();
        }

        [Test]
        public void Register_And_Get_ReturnsRegisteredService()
        {
            var service = new TestService("MyService");

            ServiceLocator.Register<ITestService>(service);
            var retrieved = ServiceLocator.Get<ITestService>();

            Assert.AreSame(service, retrieved);
            Assert.AreEqual("MyService", retrieved.Name);
        }

        [Test]
        public void Get_WhenNotRegistered_ThrowsInvalidOperationException()
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
            {
                ServiceLocator.Get<ITestService>();
            });

            Assert.That(ex.Message, Does.Contain("ITestService"));
            Assert.That(ex.Message, Does.Contain("not registered"));
        }

        [Test]
        public void Register_Null_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                ServiceLocator.Register<ITestService>(null);
            });
        }

        [Test]
        public void Register_Duplicate_ThrowsInvalidOperationException()
        {
            ServiceLocator.Register<ITestService>(new TestService());

            var ex = Assert.Throws<InvalidOperationException>(() =>
            {
                ServiceLocator.Register<ITestService>(new TestService());
            });

            Assert.That(ex.Message, Does.Contain("already registered"));
        }

        [Test]
        public void RegisterOrReplace_ReplacesPreviousService()
        {
            var first = new TestService("First");
            var second = new TestService("Second");

            ServiceLocator.Register<ITestService>(first);
            ServiceLocator.RegisterOrReplace<ITestService>(second);

            var retrieved = ServiceLocator.Get<ITestService>();

            Assert.AreSame(second, retrieved);
            Assert.AreEqual("Second", retrieved.Name);
        }

        [Test]
        public void RegisterOrReplace_WhenNotExists_RegistersService()
        {
            var service = new TestService();

            ServiceLocator.RegisterOrReplace<ITestService>(service);
            var retrieved = ServiceLocator.Get<ITestService>();

            Assert.AreSame(service, retrieved);
        }

        [Test]
        public void Unregister_RemovesService()
        {
            ServiceLocator.Register<ITestService>(new TestService());

            bool result = ServiceLocator.Unregister<ITestService>();

            Assert.IsTrue(result);
            Assert.IsFalse(ServiceLocator.IsRegistered<ITestService>());
        }

        [Test]
        public void Unregister_WhenNotRegistered_ReturnsFalse()
        {
            bool result = ServiceLocator.Unregister<ITestService>();

            Assert.IsFalse(result);
        }

        [Test]
        public void TryGet_WhenRegistered_ReturnsTrueAndService()
        {
            var service = new TestService();
            ServiceLocator.Register<ITestService>(service);

            bool result = ServiceLocator.TryGet<ITestService>(out var retrieved);

            Assert.IsTrue(result);
            Assert.AreSame(service, retrieved);
        }

        [Test]
        public void TryGet_WhenNotRegistered_ReturnsFalseAndNull()
        {
            bool result = ServiceLocator.TryGet<ITestService>(out var retrieved);

            Assert.IsFalse(result);
            Assert.IsNull(retrieved);
        }

        [Test]
        public void IsRegistered_WhenRegistered_ReturnsTrue()
        {
            ServiceLocator.Register<ITestService>(new TestService());

            Assert.IsTrue(ServiceLocator.IsRegistered<ITestService>());
        }

        [Test]
        public void IsRegistered_WhenNotRegistered_ReturnsFalse()
        {
            Assert.IsFalse(ServiceLocator.IsRegistered<ITestService>());
        }

        [Test]
        public void Clear_RemovesAllServices()
        {
            ServiceLocator.Register<ITestService>(new TestService());
            ServiceLocator.Register<IAnotherService>(new AnotherService());

            ServiceLocator.Clear();

            Assert.AreEqual(0, ServiceLocator.ServiceCount);
            Assert.IsFalse(ServiceLocator.IsRegistered<ITestService>());
            Assert.IsFalse(ServiceLocator.IsRegistered<IAnotherService>());
        }

        [Test]
        public void ServiceCount_ReturnsCorrectCount()
        {
            Assert.AreEqual(0, ServiceLocator.ServiceCount);

            ServiceLocator.Register<ITestService>(new TestService());
            Assert.AreEqual(1, ServiceLocator.ServiceCount);

            ServiceLocator.Register<IAnotherService>(new AnotherService());
            Assert.AreEqual(2, ServiceLocator.ServiceCount);

            ServiceLocator.Unregister<ITestService>();
            Assert.AreEqual(1, ServiceLocator.ServiceCount);
        }

        [Test]
        public void MultipleServices_CanBeRegisteredAndRetrieved()
        {
            var testService = new TestService();
            var anotherService = new AnotherService();

            ServiceLocator.Register<ITestService>(testService);
            ServiceLocator.Register<IAnotherService>(anotherService);

            Assert.AreSame(testService, ServiceLocator.Get<ITestService>());
            Assert.AreSame(anotherService, ServiceLocator.Get<IAnotherService>());
        }
    }
}
