namespace NServiceBus.Core.Tests.Routing
{
    using System.Linq;
    using NServiceBus.Routing;
    using NUnit.Framework;

    [TestFixture]
    public class TypeRouteSourceTests
    {
        [Test]
        public void It_throws_if_specified_type_is_not_a_message()
        {
            var source = new TypeRouteSource(typeof(NonMessage), UnicastRoute.CreateFromEndpointName("Destination"));
            Assert.That(() => source.GenerateRoutes(new Conventions()).ToArray(), Throws.Exception.Message.Contains("it is not considered a message"));
        }

        class NonMessage
        {
        }
    }
}