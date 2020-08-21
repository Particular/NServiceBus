namespace NServiceBus.ContainerTests
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Extensions.DependencyInjection;
    using MicrosoftExtensionsDependencyInjection;
    using NServiceBus;
    using NUnit.Framework;

    [TestFixture]
    public class When_registering_components
    {
        [Test]
        public void Multiple_registrations_of_the_same_component_should_be_allowed()
        {
            var serviceCollection = new ServiceCollection();
            var configureComponents = new CommonObjectBuilder(serviceCollection);
            configureComponents.ConfigureComponent(typeof(DuplicateClass), DependencyLifecycle.InstancePerCall);
            configureComponents.ConfigureComponent(typeof(DuplicateClass), DependencyLifecycle.InstancePerCall);

            var builder = TestContainerBuilder.CreateServiceProvider(serviceCollection);
            Assert.AreEqual(1, builder.GetServices(typeof(DuplicateClass)).Count());
        }

        [Test]
        public void Should_support_lambdas_that_uses_other_components_registered_later()
        {
            var serviceCollection = new ServiceCollection();
            var configureComponents = new CommonObjectBuilder(serviceCollection);
            configureComponents.ConfigureComponent(s => ((StaticFactory)s.GetService(typeof(StaticFactory))).Create(), DependencyLifecycle.InstancePerCall);
            configureComponents.ConfigureComponent(() => new StaticFactory(), DependencyLifecycle.SingleInstance);

            var builder = TestContainerBuilder.CreateServiceProvider(serviceCollection);
            Assert.NotNull(builder.GetService(typeof(ComponentCreatedByFactory)));
        }

        [Test]
        public void A_registration_should_be_allowed_to_be_updated()
        {
            var serviceCollection = new ServiceCollection();
            var configureComponents = new CommonObjectBuilder(serviceCollection);
            configureComponents.RegisterSingleton(typeof(ISingletonComponent), new SingletonComponent());
            configureComponents.RegisterSingleton(typeof(ISingletonComponent), new AnotherSingletonComponent());

            var builder = TestContainerBuilder.CreateServiceProvider(serviceCollection);
            Assert.IsInstanceOf<AnotherSingletonComponent>(builder.GetService(typeof(ISingletonComponent)));
        }

        [Test]
        public void Register_singleton_should_be_supported()
        {
            var singleton = new SingletonComponent();
            var serviceCollection = new ServiceCollection();
            var configureComponents = new CommonObjectBuilder(serviceCollection);
            configureComponents.RegisterSingleton(typeof(ISingletonComponent), singleton);
            configureComponents.RegisterSingleton(typeof(SingletonComponent), singleton);

            var builder = TestContainerBuilder.CreateServiceProvider(serviceCollection);
            Assert.AreEqual(builder.GetService(typeof(SingletonComponent)), singleton);
            Assert.AreEqual(builder.GetService(typeof(ISingletonComponent)), singleton);
        }

        [Test]
        public void Registering_the_same_singleton_for_different_interfaces_should_be_supported()
        {
            var serviceCollection = new ServiceCollection();
            var configureComponents = new CommonObjectBuilder(serviceCollection);
            var singleton = new SingletonThatImplementsToInterfaces();
            configureComponents.RegisterSingleton(typeof(ISingleton1), singleton);
            configureComponents.RegisterSingleton(typeof(ISingleton2), singleton);
            configureComponents.ConfigureComponent(typeof(ComponentThatDependsOnMultiSingletons), DependencyLifecycle.InstancePerCall);
            
            var builder = TestContainerBuilder.CreateServiceProvider(serviceCollection);
            var dependency = (ComponentThatDependsOnMultiSingletons)builder.GetService(typeof(ComponentThatDependsOnMultiSingletons));

            Assert.NotNull(dependency.Singleton1);
            Assert.NotNull(dependency.Singleton2);

            Assert.AreEqual(builder.GetService(typeof(ISingleton1)), singleton);
            Assert.AreEqual(builder.GetService(typeof(ISingleton2)), singleton);
        }

        [Test]
        public void Concrete_classes_should_get_the_same_lifecycle_as_their_interfaces()
        {
            var serviceCollection = new ServiceCollection();
            var configureComponents = new CommonObjectBuilder(serviceCollection);
            configureComponents.ConfigureComponent(typeof(SingletonComponent), DependencyLifecycle.SingleInstance);

            var builder = TestContainerBuilder.CreateServiceProvider(serviceCollection);
            Assert.AreSame(builder.GetService(typeof(SingletonComponent)), builder.GetService(typeof(ISingletonComponent)));
        }

        [Test]
        public void All_implemented_interfaces_should_be_registered()
        {
            var serviceCollection = new ServiceCollection();
            var configureComponents = new CommonObjectBuilder(serviceCollection);
            configureComponents.ConfigureComponent(typeof(ComponentWithMultipleInterfaces),
                DependencyLifecycle.InstancePerCall);
            Assert.True(configureComponents.HasComponent(typeof(ISomeInterface)));
            Assert.True(configureComponents.HasComponent(typeof(ISomeOtherInterface)));
            Assert.True(configureComponents.HasComponent(typeof(IYetAnotherInterface)));

            var builder = TestContainerBuilder.CreateServiceProvider(serviceCollection);
            Assert.AreEqual(1, builder.GetServices(typeof(IYetAnotherInterface)).Count());
        }

        [Test]
        public void All_implemented_interfaces_should_be_registered_for_func()
        {
            var serviceCollection = new ServiceCollection();
            var configureComponents = new CommonObjectBuilder(serviceCollection);
            configureComponents.ConfigureComponent(() => new ComponentWithMultipleInterfaces(), DependencyLifecycle.InstancePerCall);
            Assert.True(configureComponents.HasComponent(typeof(ISomeInterface)));
            Assert.True(configureComponents.HasComponent(typeof(ISomeOtherInterface)));
            Assert.True(configureComponents.HasComponent(typeof(IYetAnotherInterface)));

            var builder = TestContainerBuilder.CreateServiceProvider(serviceCollection);

            Assert.AreEqual(1, builder.GetServices(typeof(IYetAnotherInterface)).Count());
        }

        [Test]
        public void Multiple_implementations_should_be_supported()
        {
            var serviceCollection = new ServiceCollection();
            var configureComponents = new CommonObjectBuilder(serviceCollection);
            configureComponents.ConfigureComponent(typeof(SomeClass), DependencyLifecycle.InstancePerUnitOfWork);
            configureComponents.ConfigureComponent(typeof(SomeOtherClass), DependencyLifecycle.InstancePerUnitOfWork);

            var builder = TestContainerBuilder.CreateServiceProvider(serviceCollection);
            Assert.NotNull(builder.GetService(typeof(SomeClass)));
            Assert.AreEqual(2, builder.GetServices(typeof(ISomeInterface)).Count());

            using (var scope = builder.CreateScope())
            {
                Assert.NotNull(scope.ServiceProvider.GetService(typeof(SomeClass)));
                Assert.AreEqual(2, scope.ServiceProvider.GetServices(typeof(ISomeInterface)).Count());
            }
        }

        [Test]
        public void Given_lookupType_should_be_used_as_service_in_the_registration_when_RegisterSingleton()
        {
            var serviceCollection = new ServiceCollection();
            var configureComponents = new CommonObjectBuilder(serviceCollection);
            var expected = new InheritedFromSomeClass();
            configureComponents.RegisterSingleton(typeof(SomeClass), expected);

            var builder = TestContainerBuilder.CreateServiceProvider(serviceCollection);
            Assert.AreEqual(expected, builder.GetService(typeof(SomeClass)));

            using (var scope = builder.CreateScope())
            {
                Assert.AreEqual(expected, scope.ServiceProvider.GetService(typeof(SomeClass)));
            }
        }

        [Test]
        public void Generic_interfaces_should_be_registered()
        {
            var serviceCollection = new ServiceCollection();
            var configureComponents = new CommonObjectBuilder(serviceCollection);
            configureComponents.ConfigureComponent(typeof(ComponentWithGenericInterface),
                DependencyLifecycle.InstancePerCall);

            Assert.True(configureComponents.HasComponent(typeof(ISomeGenericInterface<string>)));
        }

        [Test, Ignore("Not sure that we should enforce this")]
        public void System_interfaces_should_not_be_auto_registered()
        {
            var serviceCollection = new ServiceCollection();
            var configureComponents = new CommonObjectBuilder(serviceCollection);
            configureComponents.ConfigureComponent(typeof(ComponentWithSystemInterface),
                DependencyLifecycle.InstancePerCall);

            Assert.False(configureComponents.HasComponent(typeof(IGrouping<string, string>)));
            Assert.False(configureComponents.HasComponent(typeof(IDisposable)));
        }
    }

    public class ComponentThatDependsOnMultiSingletons
    {
        public ComponentThatDependsOnMultiSingletons(ISingleton1 singleton1, ISingleton2 singleton2)
        {
            Singleton1 = singleton1;
            Singleton2 = singleton2;
        }

        public ISingleton1 Singleton1 { get; private set; }
        public ISingleton2 Singleton2 { get; private set; }
    }

    public class SingletonThatImplementsToInterfaces : ISingleton2
    {
    }

    public interface ISingleton2 : ISingleton1
    {
    }

    public interface ISingleton1
    {
    }

    public class ComponentWithMultipleInterfaces : ISomeInterface, ISomeOtherInterface
    {
    }

    public class ComponentWithGenericInterface : ISomeGenericInterface<string>
    {
    }

    public class ComponentWithSystemInterface : IGrouping<string, string>, IDisposable
    {
        public IEnumerator<string> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public string Key
        {
            get { throw new NotImplementedException(); }
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }

    public interface ISomeGenericInterface<T>
    {
    }

    public interface ISomeOtherInterface : IYetAnotherInterface
    {
    }

    public interface IYetAnotherInterface
    {
    }

    public class DuplicateClass
    {
        public bool SomeProperty { get; set; }
        public bool AnotherProperty { get; set; }
    }

    public interface IWithSetterDependencies
    {
    }

    public class SomeClass : ISomeInterface
    {
    }

    public class InheritedFromSomeClass : SomeClass
    {
    }

    public class SomeOtherClass : ISomeInterface
    {
    }

    public interface ISomeInterface
    {
    }

    public enum SomeEnum
    {
        X
    }
}