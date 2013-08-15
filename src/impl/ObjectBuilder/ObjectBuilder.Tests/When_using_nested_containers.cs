using System;
using NServiceBus;
using NServiceBus.ObjectBuilder.Spring;
using NUnit.Framework;
using System.Threading.Tasks;

namespace ObjectBuilder.Tests
{
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
                                    return childContainer.Build(typeof (InstancePerUoWComponent));
                                }
                            });
                var task2 =
                    Task<object>.Factory.StartNew(
                        () =>
                            {
                                using (var childContainer = builder.BuildChildContainer())
                                {
                                    return childContainer.Build(typeof (InstancePerUoWComponent));
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
                    instance1 = nestedContainer.Build(typeof (InstancePerCallComponent));
                }

                using (var anotherNestedContainer = builder.BuildChildContainer())
                {
                    instance2 = anotherNestedContainer.Build(typeof (InstancePerCallComponent));
                }

                Assert.AreNotSame(instance1, instance2);
            });
        }

        [Test]
        public void UoW_components_in_the_parent_container_should_be_singletons_in_the_child_container()
        {
            ForAllBuilders(builder =>
            {
                builder.Configure(typeof(InstancePerUoWComponent), DependencyLifecycle.InstancePerUnitOfWork);

                using (var nestedContainer = builder.BuildChildContainer())
                {
                    Assert.AreEqual(nestedContainer.Build(typeof(InstancePerUoWComponent)), nestedContainer.Build(typeof(InstancePerUoWComponent)));
                }
            },
            typeof(SpringObjectBuilder));
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

    public class InstancePerCallComponent
    {
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
