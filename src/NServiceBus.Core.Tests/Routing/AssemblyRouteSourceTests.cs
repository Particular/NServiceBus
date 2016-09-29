namespace NServiceBus.Core.Tests.Routing
{
    using System.Linq;
    using System.Reflection;
    using NServiceBus.Routing;
    using NUnit.Framework;

    [TestFixture]
    public class AssemblyRouteSourceTests
    {
        [Test]
        public void It_returns_only_message_types()
        {
            var source = new AssemblyRouteSource(Assembly.GetExecutingAssembly(), UnicastRoute.CreateFromEndpointName("Destination"));
            var routes = source.GenerateRoutes(new Conventions()).ToArray();
            var routeTypes = routes.Select(r => r.MessageType);

            CollectionAssert.DoesNotContain(routeTypes, typeof(NonMessage));
        }


        [Test]
        public void It_throws_if_specified_assembly_contains_no_message_types()
        {
            var source = new AssemblyRouteSource(typeof(string).Assembly, UnicastRoute.CreateFromEndpointName("Destination"));

            Assert.That(() => source.GenerateRoutes(new Conventions()).ToArray(), Throws.Exception.Message.Contains("Cannot configure routing for assembly"));
        }

        class NonMessage
        {
        }
    }
}
