namespace ObjectBuilder.Tests
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using NServiceBus;
    using NServiceBus.ObjectBuilder.Autofac;
    using NServiceBus.ObjectBuilder.CastleWindsor;
    using NServiceBus.ObjectBuilder.Common;
    using NServiceBus.ObjectBuilder.Ninject;
    using NServiceBus.ObjectBuilder.Spring;
    using NServiceBus.ObjectBuilder.Unity;
    using NUnit.Framework;

    [TestFixture]
    public class When_using_nested_containers : BuilderFixture
    {

        [Test]
        public void Instance_per_uow__components_should_be_disposed_when_the_child_container_is_disposed()
        {
            ForAllBuilders(builder =>
            {
                builder.Configure(typeof(InstancePerUoWComponent), DependencyLifecycle.InstancePerUnitOfWork);

                using (var nestedContainer = builder.BuildChildContainer())
                    nestedContainer.Build(typeof(InstancePerUoWComponent));

                Assert.True(InstancePerUoWComponent.DisposeCalled);
            },
            typeof(SpringObjectBuilder));
        }

        [Test]
        public void Instance_per_uow__components_should_not_be_shared_across_child_containers()
        {
            ForAllBuilders(builder =>
            {
                builder.Configure(typeof(InstancePerUoWComponent),
                                  DependencyLifecycle.InstancePerUnitOfWork);

                var task1 =
                    Task<object>.Factory.StartNew(
                        () =>
                        {
                            using (var childContainer = builder.BuildChildContainer())
                            {
                                return childContainer.Build(typeof(InstancePerUoWComponent));
                            }
                        });
                var task2 =
                    Task<object>.Factory.StartNew(
                        () =>
                        {
                            using (var childContainer = builder.BuildChildContainer())
                            {
                                return childContainer.Build(typeof(InstancePerUoWComponent));
                            }
                        });

                Assert.AreNotSame(task1.Result, task2.Result);

            }, typeof(SpringObjectBuilder));
        }

        [Test]
        public void Instance_per_call_components_should_not_be_shared_across_child_containers()
        {
            ForAllBuilders(builder =>
            {
                builder.Configure(typeof(InstancePerCallComponent), DependencyLifecycle.InstancePerCall);

                object instance1, instance2;
                using (var nestedContainer = builder.BuildChildContainer())
                {
                    instance1 = nestedContainer.Build(typeof(InstancePerCallComponent));
                }

                using (var anotherNestedContainer = builder.BuildChildContainer())
                {
                    instance2 = anotherNestedContainer.Build(typeof(InstancePerCallComponent));
                }

                Assert.AreNotSame(instance1, instance2);
            });
        }

        [Test, Explicit("Time consuming")]
        public void Instance_per_call_components_should_not_cause_memory_leaks()
        {
            //const int iterations = 1000000;
            const int iterations = 20000;

            ForAllBuilders(builder =>
            {
                builder.Configure(typeof(InstancePerCallComponent), DependencyLifecycle.InstancePerUnitOfWork);

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

                var after = GC.GetTotalMemory(true);
                Console.WriteLine("{0} Time: {1} MemDelta: {2} bytes", builder.GetType().Name, sw.Elapsed, after - before);

                var upperLimitBytes = 200 * 1024;
                Assert.That(after-before, Is.LessThan(upperLimitBytes), "Apparently {0} consumed more than {1} KB of memory", builder, upperLimitBytes/1024);
            }, typeof(NinjectObjectBuilder));
        }

        [TestCase(10000, Ignore=true)]
        [TestCase(20000, Ignore=true)]
        [Description("Left in for convenience - MHG will remove soon")]
        public void It_works_with_specific_object_builder(int iterations)
        {
            IContainer builder = new NinjectObjectBuilder();

            builder.Configure(typeof(InstancePerCallComponent), DependencyLifecycle.SingleInstance);

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

            var after = GC.GetTotalMemory(true);
            Console.WriteLine("{0} reps: {1} Time: {2} MemDelta: {3} bytes", iterations, builder.GetType().Name, sw.Elapsed, after - before);
        }

        [Test]
        public void UoW_components_in_the_parent_container_should_be_singletons_in_the_child_container()
        {
            ForAllBuilders(builder =>
            {
                builder.Configure(typeof(InstancePerUoWComponent), DependencyLifecycle.InstancePerUnitOfWork);

                using (var nestedContainer = builder.BuildChildContainer())
                {
                    Assert.AreSame(nestedContainer.Build(typeof(InstancePerUoWComponent)), nestedContainer.Build(typeof(InstancePerUoWComponent)),"UoW's should be singleton in child container");
                }
            },
            typeof(SpringObjectBuilder));
        }

        [Test]
        public void UoW_components_should_by_instance_per_call_in_root_container()
        {
            ForAllBuilders(builder =>
            {
                builder.Configure(typeof(InstancePerUoWComponent), DependencyLifecycle.InstancePerUnitOfWork);

                using (builder.BuildChildContainer())
                {
                   //no-op
                }

                Assert.AreNotSame(builder.Build(typeof(InstancePerUoWComponent)), builder.Build(typeof(InstancePerUoWComponent)), "UoW's should be instance per call in the root container");
            },
            typeof(AutofacObjectBuilder), typeof(WindsorObjectBuilder), typeof(UnityObjectBuilder));
        }
        [Test]
        public void Should_not_dispose_singletons_when_container_goes_out_of_scope()
        {
            ForAllBuilders(builder =>
            {
                var singletonInMainContainer = new SingletonComponent();

                builder.RegisterSingleton(typeof(ISingletonComponent), singletonInMainContainer);
                builder.Configure(typeof(ComponentThatDependsOfSingleton), DependencyLifecycle.InstancePerUnitOfWork);

                using (var nestedContainer = builder.BuildChildContainer())
                {
                    nestedContainer.Build(typeof(ComponentThatDependsOfSingleton));
                }

                Assert.False(SingletonComponent.DisposeCalled);
            },
            typeof(SpringObjectBuilder));
        }

        class SingletonComponent : ISingletonComponent, IDisposable
        {
            public static bool DisposeCalled;

            public void Dispose()
            {
                DisposeCalled = true;
            }
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
        public static bool DisposeCalled;

        public void Dispose()
        {
            DisposeCalled = true;
        }

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
}
