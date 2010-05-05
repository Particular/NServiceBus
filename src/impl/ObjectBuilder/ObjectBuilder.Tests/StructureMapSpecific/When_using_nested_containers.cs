using System;
using NServiceBus.ObjectBuilder;
using NServiceBus.ObjectBuilder.StructureMap;
using NUnit.Framework;
using StructureMap;
using NBehave.Spec.NUnit;

namespace ObjectBuilder.Tests.StructureMapSpecific
{
    [TestFixture]
    public class When_using_nested_containers
    {
        [Test]
        public void Singleton_components_should_have_their_interfaces_registered_to_avoid_beeing_disposed()
        {
            var container = new Container();

            NServiceBus.ObjectBuilder.Common.IContainer builder = new StructureMapObjectBuilder(container);

            builder.Configure(typeof(SingletonComponent), ComponentCallModelEnum.Singleton);

            using (var nestedContainer = container.GetNestedContainer())
                nestedContainer.GetInstance<ISingletonComponent>();

            SingletonComponent.DisposeCalled.ShouldBeFalse();

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