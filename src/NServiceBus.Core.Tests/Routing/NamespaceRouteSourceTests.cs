namespace NServiceBus.Core.Tests.Routing
{
    using System.Linq;
    using System.Reflection;
    using NamespaceRouteSourceTest;
    using NamespaceRouteSourceTest.OtherNamespace;
    using NServiceBus.Routing;
    using NUnit.Framework;

    [TestFixture]
    public class NamespaceRouteSourceTests
    {
        [Test]
        public void It_returns_only_message_types()
        {
            var source = new NamespaceRouteSource(Assembly.GetExecutingAssembly(), "NServiceBus.Core.Tests.Routing.NamespaceRouteSourceTest", UnicastRoute.CreateFromEndpointName("Destination"));
            var routes = source.GenerateRoutes(new Conventions()).ToArray();

            Assert.IsTrue(routes.Any(r => r.MessageType == typeof(Message)));
            Assert.IsFalse(routes.Any(r => r.MessageType == typeof(NonMessage)));
        }

        [Test]
        public void It_returns_only_types_from_specified_namespace()
        {
            var source = new NamespaceRouteSource(Assembly.GetExecutingAssembly(), "NServiceBus.Core.Tests.Routing.NamespaceRouteSourceTest", UnicastRoute.CreateFromEndpointName("Destination"));
            var routes = source.GenerateRoutes(new Conventions()).ToArray();

            Assert.IsTrue(routes.Any(r => r.MessageType == typeof(Message)));
            Assert.IsFalse(routes.Any(r => r.MessageType == typeof(ExcludedMessage)));
        }

        [Test]
        public void It_matches_namespace_in_case_insensitive_way()
        {
            var source = new NamespaceRouteSource(Assembly.GetExecutingAssembly(), "NServiceBus.Core.Tests.Routing.NAMESPACErouteSOURCEtest", UnicastRoute.CreateFromEndpointName("Destination"));
            var routes = source.GenerateRoutes(new Conventions()).ToArray();

            Assert.IsTrue(routes.Any(r => r.MessageType == typeof(Message)));
        }

        [Test]
        public void It_throws_if_specified_namespace_contains_no_message_types()
        {
            var source = new NamespaceRouteSource(Assembly.GetExecutingAssembly(), "NServiceBus.Core.Tests.Routing.NamespaceRouteSourceTest.NoMessages", UnicastRoute.CreateFromEndpointName("Destination"));

            Assert.That(() => source.GenerateRoutes(new Conventions()).ToArray(), Throws.Exception.Message.Contains("Cannot configure routing for namespace"));
        }
    }
}

namespace NServiceBus.Core.Tests.Routing.NamespaceRouteSourceTest.OtherNamespace
{
    class ExcludedMessage : IMessage
    {
    }
}

namespace NServiceBus.Core.Tests.Routing.NamespaceRouteSourceTest
{
    class Message : IMessage
    {
    }

    class NonMessage
    {
    }
}

namespace NServiceBus.Core.Tests.Routing.NamespaceRouteSourceTest.NoMessages
{
    class NonMessage
    {
    }
}