using System;
using NServiceBus.ObjectBuilder;
using NServiceBus.ObjectBuilder.Spring;
using NServiceBus.ObjectBuilder.Unity;
using NUnit.Framework;

namespace ObjectBuilder.Tests
{
    [TestFixture]
    public class When_using_nested_containers : BuilderFixture
    {
       
        [Test]
        public void Instance_per_uow__components_should_be_disposed_when_the_child_container_is_disposed()
        {
            VerifyForAllBuilders(builder =>
            {
                builder.Configure(typeof(InstancePerUoWComponent), DependencyLifecycle.InstancePerUnitOfWork);

                using (var nestedContainer = builder.BuildChildContainer())
                    nestedContainer.Build(typeof(InstancePerUoWComponent));

                Assert.True(InstancePerUoWComponent.DisposeCalled);
            },
            typeof(SpringObjectBuilder),
            typeof(UnityObjectBuilder));

        }


        [Test]
        public void Single_call_components_in_the_parent_container_should_be_singletons_in_the_child_container()
        {
            VerifyForAllBuilders(builder =>
            {
                builder.Configure(typeof(InstancePerUoWComponent), DependencyLifecycle.InstancePerUnitOfWork);

                var nestedContainer = builder.BuildChildContainer();

                Assert.AreEqual(nestedContainer.Build(typeof(InstancePerUoWComponent)), nestedContainer.Build(typeof(InstancePerUoWComponent)));
            },
             typeof(SpringObjectBuilder),
            typeof(UnityObjectBuilder));

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