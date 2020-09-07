#pragma warning disable 0618
namespace NServiceBus.ContainerTests
{
    using System;
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;

    public class When_using_nested_containers : ContainerTest
    {
        [Test]
        public void Instance_per_uow__components_should_be_disposed_when_the_child_container_is_disposed()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.ConfigureComponent(typeof(InstancePerUoWComponent), DependencyLifecycle.InstancePerUnitOfWork);

            var builder = BuildContainer(serviceCollection);
            using (var scope = builder.CreateScope())
            {
                scope.ServiceProvider.GetService(typeof(InstancePerUoWComponent));
            }
            Assert.True(InstancePerUoWComponent.DisposeCalled);
        }

        [Test]
        public void Instance_per_uow_components_should_yield_different_instances_between_parent_and_child_containers()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.ConfigureComponent(typeof(InstancePerUoWComponent), DependencyLifecycle.InstancePerUnitOfWork);

            var builder = BuildContainer(serviceCollection);
            var parentInstance = builder.GetService(typeof(InstancePerUoWComponent));
            using (var scope = builder.CreateScope())
            {
                var childInstance = scope.ServiceProvider.GetService(typeof(InstancePerUoWComponent));

                Assert.AreNotSame(parentInstance, childInstance);
            }
        }

        [Test]
        public void Instance_per_uow_components_should_yield_different_instances_between_different_instances_of_child_containers()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.ConfigureComponent(typeof(InstancePerUoWComponent), DependencyLifecycle.InstancePerUnitOfWork);

            var builder = BuildContainer(serviceCollection);
            object instance1;
            using (var scope = builder.CreateScope())
            {
                instance1 = scope.ServiceProvider.GetService(typeof(InstancePerUoWComponent));
            }

            object instance2;
            using (var scope = builder.CreateScope())
            {
                instance2 = scope.ServiceProvider.GetService(typeof(InstancePerUoWComponent));
            }

            Assert.AreNotSame(instance1, instance2);
        }

        [Test]
        public void Instance_per_call_components_should_not_be_shared_across_child_containers()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.ConfigureComponent(typeof(InstancePerCallComponent), DependencyLifecycle.InstancePerCall);

            var builder = BuildContainer(serviceCollection);
            object instance1;
            using (var scope = builder.CreateScope())
            {
                instance1 = scope.ServiceProvider.GetService(typeof(InstancePerCallComponent));
            }

            object instance2;
            using (var scope = builder.CreateScope())
            {
                instance2 = scope.ServiceProvider.GetService(typeof(InstancePerCallComponent));
            }

            Assert.AreNotSame(instance1, instance2);
        }

        [Test]
        public void UoW_components_in_the_parent_container_should_be_singletons_in_the_same_child_container()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.ConfigureComponent(typeof(InstancePerUoWComponent), DependencyLifecycle.InstancePerUnitOfWork);

            var builder = BuildContainer(serviceCollection);
            using (var scope = builder.CreateScope())
            {
                var instance1 = scope.ServiceProvider.GetService(typeof(InstancePerUoWComponent));
                var instance2 = scope.ServiceProvider.GetService(typeof(InstancePerUoWComponent));

                Assert.AreSame(instance1, instance2, "UoW's should be singleton in child container");
            }
        }

        [Test]
        public void UoW_components_built_on_root_container_should_be_singletons_even_with_child_builder_present()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.ConfigureComponent(typeof(InstancePerUoWComponent), DependencyLifecycle.InstancePerUnitOfWork);

            var builder = BuildContainer(serviceCollection);

            using (builder.CreateScope())
            {
            }
            var instance1 = builder.GetService(typeof(InstancePerUoWComponent));
            var instance2 = builder.GetService(typeof(InstancePerUoWComponent));
            Assert.AreSame(instance1, instance2, "UoW's should be singletons in the root container");
        }

        [Test]
        public void Should_not_dispose_singletons_when_container_goes_out_of_scope()
        {
            var serviceCollection = new ServiceCollection();
            var singletonInMainContainer = new SingletonComponent();
            serviceCollection.AddSingleton(typeof(ISingletonComponent), singletonInMainContainer);
            serviceCollection.ConfigureComponent(typeof(ComponentThatDependsOfSingleton), DependencyLifecycle.InstancePerUnitOfWork);

            var builder = BuildContainer(serviceCollection);
            using (var scope = builder.CreateScope())
            {
                scope.ServiceProvider.GetService(typeof(ComponentThatDependsOfSingleton));
            }
            Assert.False(SingletonComponent.DisposeCalled);
        }

        [Test]
        public void Should_dispose_all_non_percall_IDisposable_components_in_child_container()
        {
            var serviceCollection = new ServiceCollection();
            DisposableComponent.DisposeCalled = false;
            AnotherDisposableComponent.DisposeCalled = false;
            serviceCollection.AddSingleton(typeof(AnotherDisposableComponent), new AnotherDisposableComponent());
            serviceCollection.ConfigureComponent(typeof(DisposableComponent), DependencyLifecycle.InstancePerUnitOfWork);


            var builder = BuildContainer(serviceCollection);
            using (var scope = builder.CreateScope())
            {
                scope.ServiceProvider.GetService(typeof(DisposableComponent));
            }
            Assert.False(AnotherDisposableComponent.DisposeCalled, "Dispose should not be called on AnotherSingletonComponent because it belongs to main container");
            Assert.True(DisposableComponent.DisposeCalled, "Dispose should be called on DisposableComponent");
        }

        public interface IInstanceToReplaceInNested
        {
        }

        public class InstanceToReplaceInNested_Parent : IInstanceToReplaceInNested
        {
        }

        public class InstanceToReplaceInNested_Child : IInstanceToReplaceInNested
        {
        }

        class SingletonComponent : ISingletonComponent, IDisposable
        {
            public void Dispose()
            {
                DisposeCalled = true;
            }

            public static bool DisposeCalled;
        }

        class ComponentThatDependsOfSingleton
        {
        }

        public When_using_nested_containers(Func<IServiceCollection, IServiceProvider> buildContainer) : base(buildContainer)
        {
        }
    }

    public class InstancePerCallComponent : IDisposable
    {
        public void Dispose()
        {
        }
    }

    public class InstancePerUoWComponent : IDisposable
    {
        public void Dispose()
        {
            DisposeCalled = true;
        }

        public static bool DisposeCalled;
    }

    public class SingletonComponent : ISingletonComponent
    {
    }

    public class AnotherSingletonComponent : ISingletonComponent
    {
    }

    public interface ISingletonComponent
    {
    }

    public class DisposableComponent : IDisposable
    {
        public static bool DisposeCalled;

        public void Dispose()
        {
            DisposeCalled = true;
        }
    }

    public class AnotherDisposableComponent : IDisposable
    {
        public static bool DisposeCalled;

        public void Dispose()
        {
            DisposeCalled = true;
        }
    }
}
#pragma warning restore 0618