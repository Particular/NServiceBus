using System;
using NServiceBus.ObjectBuilder;
using NServiceBus.ObjectBuilder.Common;
using NServiceBus.ObjectBuilder.StructureMap;
using NUnit.Framework;

namespace ObjectBuilder.Tests.StructureMapSpecific
{
    [TestFixture]
    public class When_using_nested_containers
    {
        [Test]
        public void Singleton_components_should_have_their_interfaces_registered_to_avoid_beeing_disposed()
        {
            IContainer builder = new StructureMapObjectBuilder();

            builder.Configure(typeof(SingletonComponent), ComponentCallModelEnum.Singleton);

            using (var nestedContainer = builder.BuildChildContainer())
                nestedContainer.Build(typeof(ISingletonComponent));

            Assert.False(SingletonComponent.DisposeCalled);

        }

        [Test]
        public void Single_call_components_should_be_disposed_when_the_child_container_is_disposed()
        {
            IContainer builder = new StructureMapObjectBuilder();

            builder.Configure(typeof(SinglecallComponent), ComponentCallModelEnum.Singlecall);

            using (var nestedContainer = builder.BuildChildContainer())
                nestedContainer.Build(typeof(SinglecallComponent));

            Assert.True(SinglecallComponent.DisposeCalled);
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