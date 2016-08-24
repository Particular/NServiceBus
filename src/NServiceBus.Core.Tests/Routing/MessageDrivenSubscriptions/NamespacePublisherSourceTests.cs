namespace NServiceBus.Core.Tests.Routing
{
    using System.Linq;
    using System.Reflection;
    using NamespacePublisherSourceTest;
    using NamespacePublisherSourceTest.OtherNamespace;
    using NServiceBus.Routing.MessageDrivenSubscriptions;
    using NUnit.Framework;

    [TestFixture]
    public class NamespacePublisherSourceTests
    {
        [Test]
        public void It_returns_only_event_types()
        {
            var source = new NamespacePublisherSource(Assembly.GetExecutingAssembly(), "NServiceBus.Core.Tests.Routing.NamespacePublisherSourceTest", PublisherAddress.CreateFromEndpointName("Destination"));
            var routes = source.Generate(new Conventions()).ToArray();

            Assert.IsTrue(routes.Any(r => r.EventType == typeof(Event)));
            Assert.IsFalse(routes.Any(r => r.EventType == typeof(NonMessage)));
            Assert.IsFalse(routes.Any(r => r.EventType == typeof(NonEvent)));
        }

        [Test]
        public void It_returns_only_types_from_specified_namespace()
        {
            var source = new NamespacePublisherSource(Assembly.GetExecutingAssembly(), "NServiceBus.Core.Tests.Routing.NamespacePublisherSourceTest", PublisherAddress.CreateFromEndpointName("Destination"));
            var routes = source.Generate(new Conventions()).ToArray();

            Assert.IsTrue(routes.Any(r => r.EventType == typeof(Event)));
            Assert.IsFalse(routes.Any(r => r.EventType == typeof(ExcludedEvent)));
        }

        [Test]
        public void It_throws_if_specified_namespace_contains_no_message_types()
        {
            var source = new NamespacePublisherSource(Assembly.GetExecutingAssembly(), "NServiceBus.Core.Tests.Routing.NamespacePublisherSourceTest.NoMessages", PublisherAddress.CreateFromEndpointName("Destination"));

            Assert.That(() => source.Generate(new Conventions()).ToArray(), Throws.Exception.Message.Contains("Cannot configure publisher for namespace"));
        }
    }
}

namespace NServiceBus.Core.Tests.Routing.NamespacePublisherSourceTest.OtherNamespace
{
    class ExcludedEvent : IEvent
    {
    }
}

namespace NServiceBus.Core.Tests.Routing.NamespacePublisherSourceTest
{
    class Event : IEvent
    {
    }

    class NonMessage
    {
    }

    class NonEvent : IMessage
    {
    }
}

namespace NServiceBus.Core.Tests.Routing.NamespacePublisherSourceTest.NoMessages
{
    class NonMessage
    {
    }
}