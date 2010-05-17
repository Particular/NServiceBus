using System;
using NServiceBus.ObjectBuilder.StructureMap;
using NUnit.Framework;
using StructureMap;
using NBehave.Spec.NUnit;

namespace ObjectBuilder.Tests.StructureMapSpecific
{
    [TestFixture]
    public class When_using_the_nservicebus_specific_thread_static_lifecycle
    {
        [Test]
        public void The_cache_should_be_cleared_after_each_message_is_processed()
        {
            var lifecycle = new NServiceBusThreadLocalStorageLifestyle();

            var container = new Container(x => x.For<SomeClass>()
                                                   .LifecycleIs(lifecycle));

            var objectCachedAsThreadStatic = container.GetInstance<SomeClass>();

            objectCachedAsThreadStatic.ShouldBeTheSameAs(container.GetInstance<SomeClass>());

            lifecycle.HandleEndMessage();

            objectCachedAsThreadStatic.ShouldNotBeTheSameAs(container.GetInstance<SomeClass>());

        }
    }

    public class SomeClass
    {
        public int SomeProperty { get; set; }
    }
}