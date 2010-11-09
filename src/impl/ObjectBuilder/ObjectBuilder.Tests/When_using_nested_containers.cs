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
        public void Singleton_components_should_have_their_interfaces_registered_to_avoid_beeing_disposed()
        {
            VerifyForAllBuilders(builder =>
            {
                builder.Configure(typeof(SingletonComponent), ComponentCallModelEnum.Singleton);

                using (var nestedContainer = builder.BuildChildContainer())
                    nestedContainer.Build(typeof(ISingletonComponent));

                Assert.False(SingletonComponent.DisposeCalled);
            },
            typeof(SpringObjectBuilder),
            typeof(UnityObjectBuilder));


        }

        [Test]
        public void Single_call_components_should_be_disposed_when_the_child_container_is_disposed()
        {
            VerifyForAllBuilders(builder =>
            {
                builder.Configure(typeof(SinglecallComponent), ComponentCallModelEnum.Singlecall);

                using (var nestedContainer = builder.BuildChildContainer())
                    nestedContainer.Build(typeof(SinglecallComponent));

                Assert.True(SinglecallComponent.DisposeCalled);
            },
            typeof(SpringObjectBuilder),
            typeof(UnityObjectBuilder));

        }


        [Test,Ignore("Hmm, this is only vorking for StructureMap, I thought the general idea of childcontainers would be to make all singlecall components singleton for the lifetime of the container??")]
        public void Single_call_components_in_the_parent_container_should_be_singletons_in_the_child_container()
        {
            VerifyForAllBuilders(builder =>
            {
                builder.Configure(typeof(SinglecallComponent), ComponentCallModelEnum.Singlecall);

                var singleCallComponentInMainContainer = builder.Build(typeof (SinglecallComponent));
                var nestedContainer = builder.BuildChildContainer();

                Assert.AreEqual(nestedContainer.Build(typeof(SinglecallComponent)),nestedContainer.Build(typeof(SinglecallComponent)));
            },
             typeof(SpringObjectBuilder),
            typeof(UnityObjectBuilder));

        }
    }
    public class SinglecallComponent : IDisposable
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