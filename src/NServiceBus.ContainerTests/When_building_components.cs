#pragma warning disable CS0618
namespace NServiceBus.ContainerTests
{
    using System;
    using Microsoft.Extensions.DependencyInjection;
    using NServiceBus;
    using NUnit.Framework;

    public class When_building_components
    {
        [Test]
        public void Singleton_components_should_yield_the_same_instance()
        {
            var serviceCollection = new ServiceCollection();
            InitializeServices(serviceCollection);
            var serviceProvider = serviceCollection.BuildServiceProvider();

            Assert.AreEqual(serviceProvider.GetService(typeof(SingletonComponent)), serviceProvider.GetService(typeof(SingletonComponent)));
        }

        [Test]
        public void Singlecall_components_should_yield_unique_instances()
        {
            var serviceCollection = new ServiceCollection();
            InitializeServices(serviceCollection);
            var serviceProvider = serviceCollection.BuildServiceProvider();

            Assert.AreNotEqual(serviceProvider.GetService<SinglecallComponent>(), serviceProvider.GetService<SinglecallComponent>());
        }

        [Test]
        public void UoW_components_should_yield_the_same_instance()
        {
            var serviceCollection = new ServiceCollection();
            InitializeServices(serviceCollection);
            var serviceProvider = serviceCollection.BuildServiceProvider();

            var instance1 = serviceProvider.GetService(typeof(InstancePerUoWComponent));
            var instance2 = serviceProvider.GetService(typeof(InstancePerUoWComponent));

            Assert.AreSame(instance1, instance2);
        }

        [Test]
        public void Lambda_uow_components_should_yield_the_same_instance()
        {
            var serviceCollection = new ServiceCollection();
            InitializeServices(serviceCollection);
            var serviceProvider = serviceCollection.BuildServiceProvider();

            var instance1 = serviceProvider.GetService(typeof(LambdaComponentUoW));
            var instance2 = serviceProvider.GetService(typeof(LambdaComponentUoW));

            Assert.AreSame(instance1, instance2);
        }

        [Test]
        public void Lambda_singlecall_components_should_yield_unique_instances()
        {
            var serviceCollection = new ServiceCollection();
            InitializeServices(serviceCollection);
            var serviceProvider = serviceCollection.BuildServiceProvider();

            Assert.AreNotEqual(serviceProvider.GetService(typeof(SingleCallLambdaComponent)), serviceProvider.GetService(typeof(SingleCallLambdaComponent)));
        }

        [Test]
        public void Lambda_singleton_components_should_yield_the_same_instance()
        {
            var serviceCollection = new ServiceCollection();
            InitializeServices(serviceCollection);
            var serviceProvider = serviceCollection.BuildServiceProvider();

            Assert.AreEqual(serviceProvider.GetService(typeof(SingletonLambdaComponent)), serviceProvider.GetService(typeof(SingletonLambdaComponent)));
        }

        [Test]
        public void Resolving_all_components_of_unregistered_types_should_give_empty_list()
        {
            var serviceCollection = new ServiceCollection();
            InitializeServices(serviceCollection);
            var serviceProvider = serviceCollection.BuildServiceProvider();

            Assert.IsEmpty(serviceProvider.GetServices(typeof(UnregisteredComponent)));
        }

        [Test]
        public void Resolving_recursive_types_does_not_stack_overflow()
        {
            try
            {
                var serviceCollection = new ServiceCollection();
                InitializeServices(serviceCollection);
                var serviceProvider = serviceCollection.BuildServiceProvider();
                serviceProvider.GetService(typeof(RecursiveComponent));
            }
            catch (Exception)
            {
                // this can't be a StackOverflowException as they can't be caught
            }
        }

        void InitializeServices(IServiceCollection serviceCollection)
        {
            serviceCollection.ConfigureComponent(typeof(SingletonComponent), DependencyLifecycle.SingleInstance);
            serviceCollection.ConfigureComponent(typeof(SinglecallComponent), DependencyLifecycle.InstancePerCall);
            serviceCollection.ConfigureComponent(typeof(InstancePerUoWComponent), DependencyLifecycle.InstancePerUnitOfWork);
            serviceCollection.ConfigureComponent(() => new SingletonLambdaComponent(), DependencyLifecycle.SingleInstance);
            serviceCollection.ConfigureComponent(() => new SingleCallLambdaComponent(), DependencyLifecycle.InstancePerCall);
            serviceCollection.ConfigureComponent(() => new LambdaComponentUoW(), DependencyLifecycle.InstancePerUnitOfWork);
            serviceCollection.ConfigureComponent(() => new RecursiveComponent(), DependencyLifecycle.SingleInstance);
        }

        public class RecursiveComponent
        {
            public RecursiveComponent Instance { get; set; }
        }

        public class SingletonComponent
        {
        }

        public interface ISingletonComponentWithPropertyDependency
        {
        }

        public class SingletonComponentWithPropertyDependency : ISingletonComponentWithPropertyDependency
        {
            public SingletonComponent Dependency { get; set; }
        }

        public class SinglecallComponent
        {
        }

        public class UnregisteredComponent
        {
            public SingletonComponent SingletonComponent { get; set; }
        }

        public class SingletonLambdaComponent
        {
        }

        public class LambdaComponentUoW
        {
        }

        public class SingleCallLambdaComponent
        {
        }
    }

    public class StaticFactory
    {
        public ComponentCreatedByFactory Create()
        {
            return new ComponentCreatedByFactory();
        }
    }

    public class ComponentCreatedByFactory
    {
    }

    public class ComponentWithBothConstructorAndSetterInjection
    {
        public ComponentWithBothConstructorAndSetterInjection(ConstructorDependency constructorDependency)
        {
            ConstructorDependency = constructorDependency;
        }

        public ConstructorDependency ConstructorDependency { get; }

        public SetterDependency SetterDependency { get; set; }
    }

    public class ConstructorDependency
    {
    }

    public class SetterDependency
    {
    }
}
#pragma warning restore CS0618