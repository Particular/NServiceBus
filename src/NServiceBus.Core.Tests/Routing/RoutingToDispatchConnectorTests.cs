namespace NServiceBus.Core.Tests.Routing
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Extensibility;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NServiceBus.Transports;
    using NUnit.Framework;
    using ObjectBuilder;

    [TestFixture]
    public class RoutingToDispatchConnectorTests
    {
        [Test]
        public async Task It_preserves_headers_generated_by_custom_routing_strategy()
        {
            var behavior = new RoutingToDispatchConnector();
            var message = new OutgoingMessage("ID", new Dictionary<string, string>(), new byte[0]);
            Dictionary<string, string> headers = null;
            await behavior.Invoke(new RoutingContext(message,
                new CustomRoutingStrategy(), new FakeContext()), context =>
                {
                    headers = context.Operations.First().Message.Headers;
                    return TaskEx.CompletedTask;
                });

            Assert.IsTrue(headers.ContainsKey("CustomHeader"));
        }

        class FakeContext : ContextBag, IBehaviorContext
        {
            public ContextBag Extensions => this;
            public IBuilder Builder => null;
        }

        class CustomRoutingStrategy : RoutingStrategy
        {
            public override AddressTag Apply(Dictionary<string, string> headers)
            {
                headers["CustomHeader"] = "CustomValue";
                return new UnicastAddressTag("destination");
            }
        }
    }
}