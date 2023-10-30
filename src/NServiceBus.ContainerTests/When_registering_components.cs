namespace NServiceBus.ContainerTests
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;


    public class When_registering_components
    {
        [Test]
        public void Multiple_registrations_of_the_same_component_should_be_allowed()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddTransient(typeof(DuplicateClass));
            serviceCollection.AddTransient(typeof(DuplicateClass));

            var serviceProvider = serviceCollection.BuildServiceProvider();
            Assert.AreEqual(2, serviceProvider.GetServices(typeof(DuplicateClass)).Count());
        }

        [Test]
        public void Should_support_lambdas_that_uses_other_components_registered_later()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddTransient(s => ((StaticFactory)s.GetService(typeof(StaticFactory))).Create());
            serviceCollection.AddSingleton(_ => new StaticFactory());

            var serviceProvider = serviceCollection.BuildServiceProvider();
            Assert.NotNull(serviceProvider.GetService(typeof(ComponentCreatedByFactory)));
        }

        [Test]
        public void A_registration_should_be_allowed_to_be_updated()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(typeof(ISingletonComponent), new SingletonComponent());
            serviceCollection.AddSingleton(typeof(ISingletonComponent), new AnotherSingletonComponent());

            var serviceProvider = serviceCollection.BuildServiceProvider();
            Assert.IsInstanceOf<AnotherSingletonComponent>(serviceProvider.GetService(typeof(ISingletonComponent)));
        }

        [Test]
        public void Register_singleton_should_be_supported()
        {
            var singleton = new SingletonComponent();
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(typeof(ISingletonComponent), singleton);
            serviceCollection.AddSingleton(typeof(SingletonComponent), singleton);

            var serviceProvider = serviceCollection.BuildServiceProvider();
            Assert.AreEqual(serviceProvider.GetService(typeof(SingletonComponent)), singleton);
            Assert.AreEqual(serviceProvider.GetService(typeof(ISingletonComponent)), singleton);
        }

        [Test]
        public void Registering_the_same_singleton_for_different_interfaces_should_be_supported()
        {
            var serviceCollection = new ServiceCollection();
            var singleton = new SingletonThatImplementsToInterfaces();
            serviceCollection.AddSingleton(typeof(ISingleton1), singleton);
            serviceCollection.AddSingleton(typeof(ISingleton2), singleton);
            serviceCollection.AddTransient(typeof(ComponentThatDependsOnMultiSingletons));

            var serviceProvider = serviceCollection.BuildServiceProvider();
            var dependency = (ComponentThatDependsOnMultiSingletons)serviceProvider.GetService(typeof(ComponentThatDependsOnMultiSingletons));

            Assert.NotNull(dependency.Singleton1);
            Assert.NotNull(dependency.Singleton2);

            Assert.AreEqual(serviceProvider.GetService(typeof(ISingleton1)), singleton);
            Assert.AreEqual(serviceProvider.GetService(typeof(ISingleton2)), singleton);
        }

        [Test]
        public void Given_lookupType_should_be_used_as_service_in_the_registration_when_RegisterSingleton()
        {
            var serviceCollection = new ServiceCollection();
            var expected = new InheritedFromSomeClass();
            serviceCollection.AddSingleton(typeof(SomeClass), expected);

            var serviceProvider = serviceCollection.BuildServiceProvider();
            Assert.AreEqual(expected, serviceProvider.GetService(typeof(SomeClass)));

            using (var scope = serviceProvider.CreateScope())
            {
                Assert.AreEqual(expected, scope.ServiceProvider.GetService(typeof(SomeClass)));
            }
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

        public string Key => throw new NotImplementedException();

        public void Dispose()
        {
            GC.SuppressFinalize(this);
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

#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
    public enum SomeEnum
#pragma warning restore CA1711 // Identifiers should not have incorrect suffix
    {
        X
    }
}