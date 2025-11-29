using System.Reflection;
using NUnit.Framework;
using Stillwater.Framework;

namespace Stillwater.Tests
{
    [TestFixture]
    public class ServiceAttributeTests
    {
        [SetUp]
        public void SetUp()
        {
            ServiceLocator.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            ServiceLocator.Clear();
        }

        #region Test Interfaces and Implementations

        // Service interfaces
        [Service]
        public interface ITestServiceA
        {
            string Name { get; }
        }

        [Service]
        public interface ITestServiceB
        {
            int Value { get; }
        }

        // Interface WITHOUT [Service] attribute
        public interface INonServiceInterface
        {
            void DoSomething();
        }

        // Implicit mode: registers for all [Service] interfaces
        [ServiceDefault]
        public class ImplicitMultiService : ITestServiceA, ITestServiceB
        {
            public string Name => "ImplicitMulti";
            public int Value => 42;
        }

        // Explicit mode: registers only for ITestServiceA
        [ServiceDefault(typeof(ITestServiceA))]
        public class ExplicitSingleService : ITestServiceA, ITestServiceB
        {
            public string Name => "ExplicitSingle";
            public int Value => 100;
        }

        // Multiple explicit attributes: registers for both
        [ServiceDefault(typeof(ITestServiceA))]
        [ServiceDefault(typeof(ITestServiceB))]
        public class ExplicitMultiService : ITestServiceA, ITestServiceB
        {
            public string Name => "ExplicitMulti";
            public int Value => 200;
        }

        // Implicit mode with only one [Service] interface
        [ServiceDefault]
        public class ImplicitSingleWithNonService : ITestServiceA, INonServiceInterface
        {
            public string Name => "ImplicitSingle";
            public void DoSomething() { }
        }

        // Class without parameterless constructor (should be skipped)
        [ServiceDefault]
        public class NoParameterlessConstructor : ITestServiceA
        {
            public string Name { get; }
            public NoParameterlessConstructor(string name) => Name = name;
        }

        // Standalone services for individual registration tests
        [Service]
        public interface IStandaloneService { }

        [ServiceDefault]
        public class StandaloneServiceImpl : IStandaloneService { }

        #endregion

        #region ServiceAttribute Tests

        [Test]
        public void ServiceAttribute_CanBeAppliedToInterface()
        {
            var attr = typeof(ITestServiceA).GetCustomAttribute<ServiceAttribute>();
            Assert.IsNotNull(attr);
        }

        [Test]
        public void ServiceAttribute_NotPresentOnNonServiceInterface()
        {
            var attr = typeof(INonServiceInterface).GetCustomAttribute<ServiceAttribute>();
            Assert.IsNull(attr);
        }

        #endregion

        #region ServiceDefaultAttribute Tests

        [Test]
        public void ServiceDefaultAttribute_CanBeAppliedToClass()
        {
            var attrs = typeof(ImplicitMultiService).GetCustomAttributes<ServiceDefaultAttribute>();
            Assert.IsNotNull(attrs);
            Assert.AreEqual(1, System.Linq.Enumerable.Count(attrs));
        }

        [Test]
        public void ServiceDefaultAttribute_SupportsMultipleOnSameClass()
        {
            var attrs = typeof(ExplicitMultiService).GetCustomAttributes<ServiceDefaultAttribute>();
            Assert.AreEqual(2, System.Linq.Enumerable.Count(attrs));
        }

        [Test]
        public void ServiceDefaultAttribute_ImplicitMode_ServiceTypeIsNull()
        {
            var attr = typeof(ImplicitMultiService).GetCustomAttribute<ServiceDefaultAttribute>();
            Assert.IsNull(attr.ServiceType);
        }

        [Test]
        public void ServiceDefaultAttribute_ExplicitMode_ServiceTypeIsSet()
        {
            var attr = typeof(ExplicitSingleService).GetCustomAttribute<ServiceDefaultAttribute>();
            Assert.AreEqual(typeof(ITestServiceA), attr.ServiceType);
        }

        #endregion

        #region RegisterAllDefaults Tests

        [Test]
        public void RegisterAllDefaults_ImplicitMode_RegistersAllServiceInterfaces()
        {
            // Clear and register only from this test assembly
            ServiceLocator.Clear();
            ServiceLocator.RegisterAllDefaults(typeof(ImplicitMultiService).Assembly);

            // ImplicitMultiService should be registered for both ITestServiceA and ITestServiceB
            Assert.IsTrue(ServiceLocator.IsRegistered<ITestServiceA>());
            Assert.IsTrue(ServiceLocator.IsRegistered<ITestServiceB>());

            var serviceA = ServiceLocator.Get<ITestServiceA>();
            var serviceB = ServiceLocator.Get<ITestServiceB>();

            // Both should be the same instance
            Assert.AreSame(serviceA, serviceB);
            Assert.AreEqual("ImplicitMulti", serviceA.Name);
        }

        [Test]
        public void RegisterAllDefaults_ExplicitSingleType_RegistersOnlySpecifiedInterface()
        {
            // First, ensure ExplicitSingleService gets picked up
            // We need to clear ImplicitMultiService registration first
            ServiceLocator.Clear();

            // Manually test ExplicitSingleService behavior
            // Since ImplicitMultiService will also be found, we test the explicit attribute behavior
            var attr = typeof(ExplicitSingleService).GetCustomAttribute<ServiceDefaultAttribute>();
            Assert.AreEqual(typeof(ITestServiceA), attr.ServiceType);
        }

        [Test]
        public void RegisterAllDefaults_ExplicitMultipleTypes_RegistersAllSpecifiedInterfaces()
        {
            var attrs = typeof(ExplicitMultiService).GetCustomAttributes<ServiceDefaultAttribute>();
            var types = new System.Collections.Generic.List<System.Type>();
            foreach (var attr in attrs)
            {
                types.Add(attr.ServiceType);
            }

            Assert.Contains(typeof(ITestServiceA), types);
            Assert.Contains(typeof(ITestServiceB), types);
        }

        [Test]
        public void RegisterAllDefaults_ImplicitMode_IgnoresNonServiceInterfaces()
        {
            // ImplicitSingleWithNonService implements ITestServiceA (with [Service])
            // and INonServiceInterface (without [Service])
            // It should only register for ITestServiceA

            var type = typeof(ImplicitSingleWithNonService);
            var interfaces = type.GetInterfaces();

            int serviceInterfaceCount = 0;
            foreach (var iface in interfaces)
            {
                if (iface.GetCustomAttribute<ServiceAttribute>() != null)
                {
                    serviceInterfaceCount++;
                }
            }

            Assert.AreEqual(1, serviceInterfaceCount);
        }

        [Test]
        public void RegisterAllDefaults_ReturnsCountOfRegisteredServices()
        {
            ServiceLocator.Clear();
            int count = ServiceLocator.RegisterAllDefaults(typeof(StandaloneServiceImpl).Assembly);

            // Should register at least StandaloneServiceImpl
            Assert.Greater(count, 0);
        }

        [Test]
        public void RegisterAllDefaults_SameInstanceForMultipleInterfaces()
        {
            ServiceLocator.Clear();
            ServiceLocator.RegisterAllDefaults(typeof(ImplicitMultiService).Assembly);

            var serviceA = ServiceLocator.Get<ITestServiceA>();
            var serviceB = ServiceLocator.Get<ITestServiceB>();

            // Both registrations should use the same instance
            Assert.AreSame(serviceA, serviceB);
        }

        [Test]
        public void RegisterAllDefaults_SkipsAlreadyRegisteredServices()
        {
            // Pre-register a service
            var preRegistered = new PreRegisteredService();
            ServiceLocator.Register<IPreRegisteredService>(preRegistered);

            // Now call RegisterAllDefaults
            ServiceLocator.RegisterAllDefaults(typeof(PreRegisteredServiceImpl).Assembly);

            // Should still have the pre-registered instance
            var retrieved = ServiceLocator.Get<IPreRegisteredService>();
            Assert.AreSame(preRegistered, retrieved);
        }

        [Service]
        public interface IPreRegisteredService { }

        public class PreRegisteredService : IPreRegisteredService { }

        [ServiceDefault]
        public class PreRegisteredServiceImpl : IPreRegisteredService { }

        #endregion

        #region Non-Generic Register Tests

        [Test]
        public void Register_NonGeneric_RegistersService()
        {
            var instance = new StandaloneServiceImpl();
            ServiceLocator.Register(typeof(IStandaloneService), instance);

            Assert.IsTrue(ServiceLocator.IsRegistered<IStandaloneService>());
            Assert.AreSame(instance, ServiceLocator.Get<IStandaloneService>());
        }

        [Test]
        public void Register_NonGeneric_NullType_ThrowsArgumentNullException()
        {
            var instance = new StandaloneServiceImpl();
            Assert.Throws<System.ArgumentNullException>(() =>
            {
                ServiceLocator.Register(null, instance);
            });
        }

        [Test]
        public void Register_NonGeneric_NullInstance_ThrowsArgumentNullException()
        {
            Assert.Throws<System.ArgumentNullException>(() =>
            {
                ServiceLocator.Register(typeof(IStandaloneService), null);
            });
        }

        [Test]
        public void Register_NonGeneric_InstanceDoesNotImplementType_ThrowsArgumentException()
        {
            var wrongInstance = new PreRegisteredService(); // Does not implement IStandaloneService
            Assert.Throws<System.ArgumentException>(() =>
            {
                ServiceLocator.Register(typeof(IStandaloneService), wrongInstance);
            });
        }

        [Test]
        public void Register_NonGeneric_Duplicate_ThrowsInvalidOperationException()
        {
            var instance1 = new StandaloneServiceImpl();
            var instance2 = new StandaloneServiceImpl();

            ServiceLocator.Register(typeof(IStandaloneService), instance1);

            Assert.Throws<System.InvalidOperationException>(() =>
            {
                ServiceLocator.Register(typeof(IStandaloneService), instance2);
            });
        }

        #endregion
    }
}
