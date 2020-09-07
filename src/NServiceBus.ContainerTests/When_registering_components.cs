namespace NServiceBus.ContainerTests
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Extensions.DependencyInjection;
    using NServiceBus;
    using NUnit.Framework;


    public class When_registering_components : ContainerTest
    {
        [Test]
        public void Multiple_registrations_of_the_same_component_should_be_allowed()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.ConfigureComponent(typeof(DuplicateClass), DependencyLifecycle.InstancePerCall);
            serviceCollection.ConfigureComponent(typeof(DuplicateClass), DependencyLifecycle.InstancePerCall);

            var builder = BuildContainer(serviceCollection);
            Assert.AreEqual(1, builder.GetServices(typeof(DuplicateClass)).Count());
        }

        [Test]
        public void Should_support_lambdas_that_uses_other_components_registered_later()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.ConfigureComponent(s => ((StaticFactory)s.GetService(typeof(StaticFactory))).Create(), DependencyLifecycle.InstancePerCall);
            serviceCollection.ConfigureComponent(() => new StaticFactory(), DependencyLifecycle.SingleInstance);

            var builder = BuildContainer(serviceCollection);
            Assert.NotNull(builder.GetService(typeof(ComponentCreatedByFactory)));
        }

        [Test]
        public void A_registration_should_be_allowed_to_be_updated()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(typeof(ISingletonComponent), new SingletonComponent());
            serviceCollection.AddSingleton(typeof(ISingletonComponent), new AnotherSingletonComponent());

            var builder = BuildContainer(serviceCollection);
            Assert.IsInstanceOf<AnotherSingletonComponent>(builder.GetService(typeof(ISingletonComponent)));
        }

        [Test]
        public void Register_singleton_should_be_supported()
        {
            var singleton = new SingletonComponent();
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(typeof(ISingletonComponent), singleton);
            serviceCollection.AddSingleton(typeof(SingletonComponent), singleton);

            var builder = BuildContainer(serviceCollection);
            Assert.AreEqual(builder.GetService(typeof(SingletonComponent)), singleton);
            Assert.AreEqual(builder.GetService(typeof(ISingletonComponent)), singleton);
        }

        [Test]
        public void Registering_the_same_singleton_for_different_interfaces_should_be_supported()
        {
            var serviceCollection = new ServiceCollection();
            var singleton = new SingletonThatImplementsToInterfaces();
            serviceCollection.AddSingleton(typeof(ISingleton1), singleton);
            serviceCollection.AddSingleton(typeof(ISingleton2), singleton);
            serviceCollection.ConfigureComponent(typeof(ComponentThatDependsOnMultiSingletons), DependencyLifecycle.InstancePerCall);

            var builder = BuildContainer(serviceCollection);
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
            serviceCollection.ConfigureComponent(typeof(SingletonComponent), DependencyLifecycle.SingleInstance);

            var builder = BuildContainer(serviceCollection);
            Assert.AreSame(builder.GetService(typeof(SingletonComponent)), builder.GetService(typeof(ISingletonComponent)));
        }

        [Test]
        public void All_implemented_interfaces_should_be_registered()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.ConfigureComponent(typeof(ComponentWithMultipleInterfaces),
                DependencyLifecycle.InstancePerCall);
            Assert.True(serviceCollection.HasComponent(typeof(ISomeInterface)));
            Assert.True(serviceCollection.HasComponent(typeof(ISomeOtherInterface)));
            Assert.True(serviceCollection.HasComponent(typeof(IYetAnotherInterface)));

            var builder = BuildContainer(serviceCollection);
            Assert.AreEqual(1, builder.GetServices(typeof(IYetAnotherInterface)).Count());
        }

        [Test]
        public void All_implemented_interfaces_should_be_registered_for_func()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.ConfigureComponent(() => new ComponentWithMultipleInterfaces(), DependencyLifecycle.InstancePerCall);
            Assert.True(serviceCollection.HasComponent(typeof(ISomeInterface)));
            Assert.True(serviceCollection.HasComponent(typeof(ISomeOtherInterface)));
            Assert.True(serviceCollection.HasComponent(typeof(IYetAnotherInterface)));

            var builder = BuildContainer(serviceCollection);

            Assert.AreEqual(1, builder.GetServices(typeof(IYetAnotherInterface)).Count());
        }

        [Test]
        public void Multiple_implementations_should_be_supported()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.ConfigureComponent(typeof(SomeClass), DependencyLifecycle.InstancePerUnitOfWork);
            serviceCollection.ConfigureComponent(typeof(SomeOtherClass), DependencyLifecycle.InstancePerUnitOfWork);

            var builder = BuildContainer(serviceCollection);
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
            var expected = new InheritedFromSomeClass();
            serviceCollection.AddSingleton(typeof(SomeClass), expected);

            var builder = BuildContainer(serviceCollection);
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
            serviceCollection.ConfigureComponent(typeof(ComponentWithGenericInterface),
                DependencyLifecycle.InstancePerCall);

            Assert.True(serviceCollection.HasComponent(typeof(ISomeGenericInterface<string>)));
        }

        [Test, Ignore("Not sure that we should enforce this")]
        public void System_interfaces_should_not_be_auto_registered()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.ConfigureComponent(typeof(ComponentWithSystemInterface),
                DependencyLifecycle.InstancePerCall);

            Assert.False(serviceCollection.HasComponent(typeof(IGrouping<string, string>)));
            Assert.False(serviceCollection.HasComponent(typeof(IDisposable)));
        }

        public When_registering_components(Func<IServiceCollection, IServiceProvider> buildContainer) : base(buildContainer)
        {
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