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
            var routes = source.GenerateWithBestPracticeEnforcement(new Conventions());
            var routedTypes = routes.Select(r => r.EventType).ToArray();

            CollectionAssert.Contains(routedTypes, typeof(Event));
            CollectionAssert.DoesNotContain(routedTypes, typeof(NonMessage));
            CollectionAssert.DoesNotContain(routedTypes, typeof(NonEvent));
        }

        [Test]
        public void It_returns_only_types_from_specified_namespace()
        {
            var source = new NamespacePublisherSource(Assembly.GetExecutingAssembly(), "NServiceBus.Core.Tests.Routing.NamespacePublisherSourceTest", PublisherAddress.CreateFromEndpointName("Destination"));
            var routes = source.GenerateWithBestPracticeEnforcement(new Conventions());
            var routedTypes = routes.Select(r => r.EventType).ToArray();

            CollectionAssert.Contains(routedTypes, typeof(Event));
            CollectionAssert.DoesNotContain(routedTypes, typeof(ExcludedEvent));
        }

        [Test]
        public void It_matches_namespace_in_case_insensitive_way()
        {
            var source = new NamespacePublisherSource(Assembly.GetExecutingAssembly(), "NServiceBus.Core.Tests.Routing.NAMESPACEpublisherSOURCEtest", PublisherAddress.CreateFromEndpointName("Destination"));
            var routes = source.GenerateWithBestPracticeEnforcement(new Conventions()).ToArray();
            var routedTypes = routes.Select(r => r.EventType);

            CollectionAssert.Contains(routedTypes, typeof(Event));
        }

        [Test]
        public void It_throws_if_specified_namespace_contains_no_message_types()
        {
            var source = new NamespacePublisherSource(Assembly.GetExecutingAssembly(), "NServiceBus.Core.Tests.Routing.NamespacePublisherSourceTest.NoMessages", PublisherAddress.CreateFromEndpointName("Destination"));

            Assert.That(() => source.GenerateWithBestPracticeEnforcement(new Conventions()).ToArray(), Throws.Exception.Message.Contains("Cannot configure publisher for namespace"));
        }

        [Test]
        public void Without_best_practice_enforcement_it_throws_if_specified_assembly_contains_only_commands()
        {
            var source = new NamespacePublisherSource(Assembly.GetExecutingAssembly(), "NServiceBus.Core.Tests.Routing.NamespacePublisherSourceTest.Commands", PublisherAddress.CreateFromEndpointName("Destination"));

            Assert.That(() => source.GenerateWithoutBestPracticeEnforcement(new Conventions()).ToArray(), Throws.Exception.Message.Contains("Cannot configure publisher for namespace"));
        }
    }
}

namespace NServiceBus.Core.Tests.Routing.NamespacePublisherSourceTest.Commands
{
    class Command : ICommand
    {
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