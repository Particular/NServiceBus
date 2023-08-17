namespace NServiceBus.ContainerTests
{
    using System;
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;

    public class When_using_nested_containers
    {
        [Test]
        public void Scoped__components_should_be_disposed_when_the_child_container_is_disposed()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddScoped(typeof(ScopedComponent));

            var serviceProvider = serviceCollection.BuildServiceProvider();
            using (var scope = serviceProvider.CreateScope())
            {
                scope.ServiceProvider.GetService(typeof(ScopedComponent));
            }
            Assert.True(ScopedComponent.DisposeCalled);
        }

        [Test]
        public void Scoped_components_should_yield_different_instances_between_parent_and_child_containers()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddScoped(typeof(ScopedComponent));

            var serviceProvider = serviceCollection.BuildServiceProvider();
            var parentInstance = serviceProvider.GetService(typeof(ScopedComponent));
            using (var scope = serviceProvider.CreateScope())
            {
                var childInstance = scope.ServiceProvider.GetService(typeof(ScopedComponent));

                Assert.AreNotSame(parentInstance, childInstance);
            }
        }

        [Test]
        public void Scoped_components_should_yield_different_instances_between_different_instances_of_child_containers()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddScoped(typeof(ScopedComponent));

            var serviceProvider = serviceCollection.BuildServiceProvider();
            object instance1;
            using (var scope = serviceProvider.CreateScope())
            {
                instance1 = scope.ServiceProvider.GetService(typeof(ScopedComponent));
            }

            object instance2;
            using (var scope = serviceProvider.CreateScope())
            {
                instance2 = scope.ServiceProvider.GetService(typeof(ScopedComponent));
            }

            Assert.AreNotSame(instance1, instance2);
        }

        [Test]
        public void Transient_components_should_not_be_shared_across_child_containers()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddTransient(typeof(TransientComponent));

            var serviceProvider = serviceCollection.BuildServiceProvider();
            object instance1;
            using (var scope = serviceProvider.CreateScope())
            {
                instance1 = scope.ServiceProvider.GetService(typeof(TransientComponent));
            }

            object instance2;
            using (var scope = serviceProvider.CreateScope())
            {
                instance2 = scope.ServiceProvider.GetService(typeof(TransientComponent));
            }

            Assert.AreNotSame(instance1, instance2);
        }

        [Test]
        public void Scoped_components_in_the_parent_container_should_be_singletons_in_the_same_child_container()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddScoped(typeof(ScopedComponent));

            var serviceProvider = serviceCollection.BuildServiceProvider();
            using (var scope = serviceProvider.CreateScope())
            {
                var instance1 = scope.ServiceProvider.GetService(typeof(ScopedComponent));
                var instance2 = scope.ServiceProvider.GetService(typeof(ScopedComponent));

                Assert.AreSame(instance1, instance2, "UoW's should be singleton in child container");
            }
        }

        [Test]
        public void Scoped_components_built_on_root_container_should_be_singletons_even_with_child_builder_present()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddScoped(typeof(ScopedComponent));

            var serviceProvider = serviceCollection.BuildServiceProvider();

            using (serviceProvider.CreateScope())
            {
            }
            var instance1 = serviceProvider.GetService(typeof(ScopedComponent));
            var instance2 = serviceProvider.GetService(typeof(ScopedComponent));
            Assert.AreSame(instance1, instance2, "UoW's should be singletons in the root container");
        }

        [Test]
        public void Should_not_dispose_singletons_when_container_goes_out_of_scope()
        {
            var serviceCollection = new ServiceCollection();
            var singletonInMainContainer = new SingletonComponent();
            serviceCollection.AddSingleton(typeof(ISingletonComponent), singletonInMainContainer);
            serviceCollection.AddScoped(typeof(ComponentThatDependsOfSingleton));

            var serviceProvider = serviceCollection.BuildServiceProvider();
            using (var scope = serviceProvider.CreateScope())
            {
                scope.ServiceProvider.GetService(typeof(ComponentThatDependsOfSingleton));
            }
            Assert.False(SingletonComponent.DisposeCalled);
        }

        [Test]
        public void Should_dispose_all_non_singleton_IDisposable_components_in_child_container()
        {
            var serviceCollection = new ServiceCollection();
            DisposableComponent.DisposeCalled = false;
            AnotherDisposableComponent.DisposeCalled = false;
            serviceCollection.AddSingleton(typeof(AnotherDisposableComponent), new AnotherDisposableComponent());
            serviceCollection.AddScoped(typeof(DisposableComponent));


            var serviceProvider = serviceCollection.BuildServiceProvider();
            using (var scope = serviceProvider.CreateScope())
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
    }

    public class TransientComponent : IDisposable
    {
        public void Dispose()
        {
        }
    }

    public class ScopedComponent : IDisposable
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