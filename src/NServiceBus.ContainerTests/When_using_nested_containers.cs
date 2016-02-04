namespace NServiceBus.ContainerTests
{
    using System;
    using System.Diagnostics;
    using NUnit.Framework;

    [TestFixture]
    public class When_using_nested_containers
    {
        [Test]
        public void Instance_per_uow__components_should_be_disposed_when_the_child_container_is_disposed()
        {
            using (var builder = TestContainerBuilder.ConstructBuilder())
            {
                builder.Configure(typeof(InstancePerUoWComponent), DependencyLifecycle.InstancePerUnitOfWork);

                using (var nestedContainer = builder.BuildChildContainer())
                {
                    nestedContainer.Build(typeof(InstancePerUoWComponent));
                }
                Assert.True(InstancePerUoWComponent.DisposeCalled);
            }
        }

        [Test]
        public void Instance_per_uow_components_should_not_be_shared_across_child_containers()
        {
            using (var builder = TestContainerBuilder.ConstructBuilder())
            {
                builder.Configure(typeof(InstancePerUoWComponent), DependencyLifecycle.InstancePerUnitOfWork);

                object instance1;
                using (var childContainer = builder.BuildChildContainer())
                {
                    instance1 = childContainer.Build(typeof(InstancePerUoWComponent));
                }

                object instance2;
                using (var childContainer = builder.BuildChildContainer())
                {
                    instance2 = childContainer.Build(typeof(InstancePerUoWComponent));
                }
                Assert.AreNotSame(instance1, instance2);
            }
        }

        [Test]
        public void Should_not_allow_reconfiguration_of_child_container()
        {
            using (var builder = TestContainerBuilder.ConstructBuilder())
            {
                builder.Configure(typeof(InstanceToReplaceInNested_Parent), DependencyLifecycle.SingleInstance);
                builder.Build(typeof(IInstanceToReplaceInNested));
                using (var nestedContainer = builder.BuildChildContainer())
                {
                    Assert.That(() => nestedContainer.Configure(typeof(InstanceToReplaceInNested_Child), DependencyLifecycle.SingleInstance), Throws.InvalidOperationException);
                }
            }
        }

        [Test]
        public void Instance_per_call_components_should_not_be_shared_across_child_containers()
        {
            using (var builder = TestContainerBuilder.ConstructBuilder())
            {
                builder.Configure(typeof(InstancePerCallComponent), DependencyLifecycle.InstancePerCall);

                object instance1;
                using (var nestedContainer = builder.BuildChildContainer())
                {
                    instance1 = nestedContainer.Build(typeof(InstancePerCallComponent));
                }

                object instance2;
                using (var anotherNestedContainer = builder.BuildChildContainer())
                {
                    instance2 = anotherNestedContainer.Build(typeof(InstancePerCallComponent));
                }
                Assert.AreNotSame(instance1, instance2);
            }
        }

        [Test]
        [Explicit]
        public void Instance_per_call_components_should_not_cause_memory_leaks()
        {
            const int iterations = 20000;

            using (var builder = TestContainerBuilder.ConstructBuilder())
            {
                builder.Configure(typeof(InstancePerCallComponent), DependencyLifecycle.InstancePerCall);

                GC.Collect();
                var before = GC.GetTotalMemory(true);
                var sw = Stopwatch.StartNew();

                for (var i = 0; i < iterations; i++)
                {
                    using (var nestedContainer = builder.BuildChildContainer())
                    {
                        nestedContainer.Build(typeof(InstancePerCallComponent));
                    }
                }

                sw.Stop();
                // Collect all generations of memory.
                GC.Collect();
                GC.WaitForPendingFinalizers();

                var after = GC.GetTotalMemory(true);
                Console.WriteLine("{0} Time: {1} MemDelta: {2} bytes", builder.GetType().Name, sw.Elapsed, after - before);

                var upperLimitBytes = 200*1024;
                Assert.That(after - before, Is.LessThan(upperLimitBytes), "Apparently {0} consumed more than {1} KB of memory", builder, upperLimitBytes/1024);
            }
        }

        [Test]
        [Explicit]
        public void Instance_per_uow_components_should_not_cause_memory_leaks()
        {
            const int iterations = 20000;

            using (var builder = TestContainerBuilder.ConstructBuilder())
            {
                builder.Configure(typeof(InstancePerUoWComponent), DependencyLifecycle.InstancePerUnitOfWork);

                GC.Collect();
                var before = GC.GetTotalMemory(true);
                var sw = Stopwatch.StartNew();

                for (var i = 0; i < iterations; i++)
                {
                    using (var nestedContainer = builder.BuildChildContainer())
                    {
                        nestedContainer.Build(typeof(InstancePerUoWComponent));
                    }
                }

                sw.Stop();
                // Collect all generations of memory.
                GC.Collect();
                GC.WaitForPendingFinalizers();

                var after = GC.GetTotalMemory(true);
                Console.WriteLine("{0} Time: {1} MemDelta: {2} bytes", builder.GetType().Name, sw.Elapsed, after - before);

                var upperLimitBytes = 200 * 1024;
                Assert.That(after - before, Is.LessThan(upperLimitBytes), "Apparently {0} consumed more than {1} KB of memory", builder, upperLimitBytes / 1024);
            }
        }

        [Test]
        public void UoW_components_in_the_parent_container_should_be_singletons_in_the_child_container()
        {
            using (var builder = TestContainerBuilder.ConstructBuilder())
            {
                builder.Configure(typeof(InstancePerUoWComponent), DependencyLifecycle.InstancePerUnitOfWork);

                using (var nestedContainer = builder.BuildChildContainer())
                {
                    var instance1 = nestedContainer.Build(typeof(InstancePerUoWComponent));
                    var instance2 = nestedContainer.Build(typeof(InstancePerUoWComponent));

                    Assert.AreSame(instance1, instance2, "UoW's should be singleton in child container");
                }
            }
        }

        [Test]
        public void UoW_components_should_be_singletons_in_root_container()
        {
            using (var builder = TestContainerBuilder.ConstructBuilder())
            {
                builder.Configure(typeof(InstancePerUoWComponent), DependencyLifecycle.InstancePerUnitOfWork);

                using (builder.BuildChildContainer())
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
            using (var builder = TestContainerBuilder.ConstructBuilder())
            {
                var singletonInMainContainer = new SingletonComponent();

                builder.RegisterSingleton(typeof(ISingletonComponent), singletonInMainContainer);
                builder.Configure(typeof(ComponentThatDependsOfSingleton), DependencyLifecycle.InstancePerUnitOfWork);

                using (var nestedContainer = builder.BuildChildContainer())
                {
                    nestedContainer.Build(typeof(ComponentThatDependsOfSingleton));
                }
                Assert.False(SingletonComponent.DisposeCalled);
            }
        }

        [Test]
        public void Should_dispose_all_IDisposable_components_in_child_container()
        {
            using (var main = TestContainerBuilder.ConstructBuilder())
            {
                DisposableComponent.DisposeCalled = false;
                AnotherDisposableComponent.DisposeCalled = false;

                main.RegisterSingleton(typeof(AnotherDisposableComponent), new AnotherDisposableComponent());
                main.Configure(typeof(DisposableComponent), DependencyLifecycle.InstancePerUnitOfWork);

                using (var builder = main.BuildChildContainer())
                {
                    builder.Build(typeof(DisposableComponent));
                }
                Assert.False(AnotherDisposableComponent.DisposeCalled, "Dispose should not be called on AnotherSingletonComponent because it belongs to main container");
                Assert.True(DisposableComponent.DisposeCalled, "Dispose should be called on DisposableComponent");
            }

            //Not supported by, typeof(SpringObjectBuilder));
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