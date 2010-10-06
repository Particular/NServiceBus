using NServiceBus.ObjectBuilder.StructureMap;
using NUnit.Framework;
using StructureMap;

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

            Assert.AreEqual(objectCachedAsThreadStatic,container.GetInstance<SomeClass>());

            lifecycle.HandleEndMessage();

            Assert.AreNotEqual(objectCachedAsThreadStatic,container.GetInstance<SomeClass>());

        }
    }

    public class SomeClass
    {
        public int SomeProperty { get; set; }
    }
}