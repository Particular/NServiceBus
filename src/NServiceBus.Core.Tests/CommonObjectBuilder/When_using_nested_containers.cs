namespace NServiceBus.Core.Tests.CommonObjectBuilder
{
    using System;
    using NUnit.Framework;
    using CommonObjectBuilder = NServiceBus.CommonObjectBuilder;

    [TestFixture]
    public class When_using_nested_containers
    {
        [Test]
        public void Instance_per_uow_components_should_be_disposed_when_the_child_container_is_disposed()
        {
            using (var builder = new CommonObjectBuilder(new LightInjectObjectBuilder()))
            {
                builder.ConfigureComponent(typeof(InstancePerUoWComponent), DependencyLifecycle.InstancePerUnitOfWork);

                using (var nestedContainer = builder.CreateChildBuilder())
                {
                    nestedContainer.Build(typeof(InstancePerUoWComponent));
                }
                Assert.True(InstancePerUoWComponent.DisposeCalled);
            }
        }

        [Test]
        public void Instance_per_uow_component_factory_should_be_disposed_when_the_child_container_is_disposed()
        {
            using (var builder = new CommonObjectBuilder(new LightInjectObjectBuilder()))
            {
                builder.ConfigureComponent(() => new InstancePerUoWComponent(), DependencyLifecycle.InstancePerUnitOfWork);

                using (var nestedContainer = builder.CreateChildBuilder())
                {
                    nestedContainer.Build(typeof(InstancePerUoWComponent));
                }
                Assert.True(InstancePerUoWComponent.DisposeCalled);
            }
        }

        [Test]
        public void Instance_per_uow_components_should_yield_different_instances_between_parent_and_child_containers()
        {
            using (var builder = new CommonObjectBuilder(new LightInjectObjectBuilder()))
            {
                builder.ConfigureComponent(typeof(InstancePerUoWComponent), DependencyLifecycle.InstancePerUnitOfWork);

                var parentInstance = builder.Build(typeof(InstancePerUoWComponent));

                using (var childContainer = builder.CreateChildBuilder())
                {
                    var childInstance = childContainer.Build(typeof(InstancePerUoWComponent));

                    Assert.AreNotSame(parentInstance, childInstance);
                }
            }
        }

        [Test]
        public void Instance_per_uow_component_factory_should_yield_different_instances_between_parent_and_child_containers()
        {
            using (var builder = new CommonObjectBuilder(new LightInjectObjectBuilder()))
            {
                builder.ConfigureComponent(() => new InstancePerUoWComponent(), DependencyLifecycle.InstancePerUnitOfWork);

                var parentInstance = builder.Build(typeof(InstancePerUoWComponent));

                using (var childContainer = builder.CreateChildBuilder())
                {
                    var childInstance = childContainer.Build(typeof(InstancePerUoWComponent));

                    Assert.AreNotSame(parentInstance, childInstance);
                }
            }
        }

        [Test]
        public void Instance_per_uow_components_should_yield_different_instances_between_different_instances_of_child_containers()
        {
            using (var builder = new CommonObjectBuilder(new LightInjectObjectBuilder()))
            {
                builder.ConfigureComponent(typeof(InstancePerUoWComponent), DependencyLifecycle.InstancePerUnitOfWork);

                object instance1;
                using (var nestedContainer = builder.CreateChildBuilder())
                {
                    instance1 = nestedContainer.Build(typeof(InstancePerUoWComponent));
                }

                object instance2;
                using (var anotherNestedContainer = builder.CreateChildBuilder())
                {
                    instance2 = anotherNestedContainer.Build(typeof(InstancePerUoWComponent));
                }
                Assert.AreNotSame(instance1, instance2);
            }
        }

        [Test]
        public void Instance_per_uow_component_factory_should_yield_different_instances_between_different_instances_of_child_containers()
        {
            using (var builder = new CommonObjectBuilder(new LightInjectObjectBuilder()))
            {
                builder.ConfigureComponent(() => new InstancePerUoWComponent(), DependencyLifecycle.InstancePerUnitOfWork);

                object instance1;
                using (var nestedContainer = builder.CreateChildBuilder())
                {
                    instance1 = nestedContainer.Build(typeof(InstancePerUoWComponent));
                }

                object instance2;
                using (var anotherNestedContainer = builder.CreateChildBuilder())
                {
                    instance2 = anotherNestedContainer.Build(typeof(InstancePerUoWComponent));
                }
                Assert.AreNotSame(instance1, instance2);
            }
        }

        [Test]
        public void Instance_per_call_components_should_not_be_shared_across_child_containers()
        {
            using (var builder = new CommonObjectBuilder(new LightInjectObjectBuilder()))
            {
                builder.ConfigureComponent(typeof(InstancePerCallComponent), DependencyLifecycle.InstancePerCall);

                object instance1;
                using (var nestedContainer = builder.CreateChildBuilder())
                {
                    instance1 = nestedContainer.Build(typeof(InstancePerCallComponent));
                }

                object instance2;
                using (var anotherNestedContainer = builder.CreateChildBuilder())
                {
                    instance2 = anotherNestedContainer.Build(typeof(InstancePerCallComponent));
                }
                Assert.AreNotSame(instance1, instance2);
            }
        }

        [Test]
        public void UoW_components_in_the_parent_container_should_be_singletons_in_the_same_child_container()
        {
            using (var builder = new CommonObjectBuilder(new LightInjectObjectBuilder()))
            {
                builder.ConfigureComponent(typeof(InstancePerUoWComponent), DependencyLifecycle.InstancePerUnitOfWork);

                using (var nestedContainer = builder.CreateChildBuilder())
                {
                    var instance1 = nestedContainer.Build(typeof(InstancePerUoWComponent));
                    var instance2 = nestedContainer.Build(typeof(InstancePerUoWComponent));

                    Assert.AreSame(instance1, instance2, "UoW's should be singleton in child container");
                }
            }
        }

        [Test]
        public void UoW_component_factory_in_the_parent_container_should_be_singletons_in_the_same_child_container()
        {
            using (var builder = new CommonObjectBuilder(new LightInjectObjectBuilder()))
            {
                builder.ConfigureComponent(() => new InstancePerUoWComponent(), DependencyLifecycle.InstancePerUnitOfWork);

                using (var nestedContainer = builder.CreateChildBuilder())
                {
                    var instance1 = nestedContainer.Build(typeof(InstancePerUoWComponent));
                    var instance2 = nestedContainer.Build(typeof(InstancePerUoWComponent));

                    Assert.AreSame(instance1, instance2, "UoW's should be singleton in child container");
                }
            }
        }

        [Test]
        public void UoW_components_built_on_root_container_should_be_singletons_even_with_child_builder_present()
        {
            using (var builder = new CommonObjectBuilder(new LightInjectObjectBuilder()))
            {
                builder.ConfigureComponent(typeof(InstancePerUoWComponent), DependencyLifecycle.InstancePerUnitOfWork);

                using (builder.CreateChildBuilder())
                {
                    //no-op
                }

                var instance1 = builder.Build(typeof(InstancePerUoWComponent));
                var instance2 = builder.Build(typeof(InstancePerUoWComponent));
                Assert.AreSame(instance1, instance2, "UoW's should be singletons in the root container");
            }
        }

        [Test]
        public void UoW_component_factory_built_on_root_container_should_be_singletons_even_with_child_builder_present()
        {
            using (var builder = new CommonObjectBuilder(new LightInjectObjectBuilder()))
            {
                builder.ConfigureComponent(() => new InstancePerUoWComponent(), DependencyLifecycle.InstancePerUnitOfWork);

                using (builder.CreateChildBuilder())
                {
                    //no-op
                }

                var instance1 = builder.Build(typeof(InstancePerUoWComponent));
                var instance2 = builder.Build(typeof(InstancePerUoWComponent));
                Assert.AreSame(instance1, instance2, "UoW's should be singletons in the root container");
            }
        }

        [Test]
        public void Should_not_dispose_singletons_when_container_goes_out_of_scope()
        {
            using (var builder = new CommonObjectBuilder(new LightInjectObjectBuilder()))
            {
                var singletonInMainContainer = new SingletonComponent();

                builder.RegisterSingleton(singletonInMainContainer);
                builder.ConfigureComponent(typeof(ComponentThatDependsOfSingleton), DependencyLifecycle.InstancePerUnitOfWork);

                using (var nestedContainer = builder.CreateChildBuilder())
                {
                    nestedContainer.Build(typeof(ComponentThatDependsOfSingleton));
                }
                Assert.False(SingletonComponent.DisposeCalled);
            }
        }

        [Test]
        public void Should_dispose_all_non_percall_IDisposable_components_in_child_container()
        {
            using (var main = new CommonObjectBuilder(new LightInjectObjectBuilder()))
            {
                DisposableComponent.DisposeCalled = false;
                AnotherDisposableComponent.DisposeCalled = false;

                main.RegisterSingleton(new AnotherDisposableComponent());
                main.ConfigureComponent(typeof(DisposableComponent), DependencyLifecycle.InstancePerUnitOfWork);

                using (var builder = main.CreateChildBuilder())
                {
                    builder.Build(typeof(DisposableComponent));
                }
                Assert.False(AnotherDisposableComponent.DisposeCalled, "Dispose should not be called on AnotherSingletonComponent because it belongs to main container");
                Assert.True(DisposableComponent.DisposeCalled, "Dispose should be called on DisposableComponent");
            }
        }

        [Test]
        public void Should_dispose_all_non_percall_IDisposable_component_factory_in_child_container()
        {
            using (var main = new CommonObjectBuilder(new LightInjectObjectBuilder()))
            {
                DisposableComponent.DisposeCalled = false;
                AnotherDisposableComponent.DisposeCalled = false;

                main.RegisterSingleton(new AnotherDisposableComponent());
                main.ConfigureComponent(() => new DisposableComponent(), DependencyLifecycle.InstancePerUnitOfWork);

                using (var builder = main.CreateChildBuilder())
                {
                    builder.Build(typeof(DisposableComponent));
                }
                Assert.False(AnotherDisposableComponent.DisposeCalled, "Dispose should not be called on AnotherSingletonComponent because it belongs to main container");
                Assert.True(DisposableComponent.DisposeCalled, "Dispose should be called on DisposableComponent");
            }
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