using System;
using NServiceBus.ObjectBuilder;
using NServiceBus.ObjectBuilder.Spring;
using NUnit.Framework;

namespace ObjectBuilder.Tests
{
    using NServiceBus.ObjectBuilder.Ninject;

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
            typeof(SpringObjectBuilder),
            typeof(NServiceBus.ObjectBuilder.Unity.UnityObjectBuilder),
            typeof(NinjectObjectBuilder));

        }

        [Test]
        public void UoW_components_in_the_parent_container_should_be_singletons_in_the_child_container()
        {
            ForAllBuilders(builder =>
            {
                builder.Configure(typeof(InstancePerUoWComponent), DependencyLifecycle.InstancePerUnitOfWork);

                var nestedContainer = builder.BuildChildContainer();

                Assert.AreEqual(nestedContainer.Build(typeof(InstancePerUoWComponent)), nestedContainer.Build(typeof(InstancePerUoWComponent)));
            },
            typeof(SpringObjectBuilder),
            typeof(NServiceBus.ObjectBuilder.Unity.UnityObjectBuilder),
            //typeof(NServiceBus.ObjectBuilder.Unity2.UnityObjectBuilder),
            typeof(NinjectObjectBuilder));
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

    public class SingletonComponent : ISingletonComponent, IDisposable
    {
        public static bool DisposeCalled;

        public void Dispose()
        {
            DisposeCalled = true;
        }
    }

    public interface ISingletonComponent
    {
    }
}