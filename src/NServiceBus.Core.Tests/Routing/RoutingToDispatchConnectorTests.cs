namespace NServiceBus.Core.Tests.Routing
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.Routing;
    using Transport;
    using NUnit.Framework;
    using Testing;

    [TestFixture]
    public class RoutingToDispatchConnectorTests
    {
        [Test]
        public async Task Should_preserve_headers_generated_by_custom_routing_strategy()
        {
            var behavior = new RoutingToDispatchConnector();
            Dictionary<string, string> headers = null;
            await behavior.Invoke(new TestableRoutingContext { RoutingStrategies = new List<RoutingStrategy> { new CustomRoutingStrategy() } }, context =>
                {
                    headers = context.Operations.First().Message.Headers;
                    return TaskEx.CompletedTask;
                });

            Assert.IsTrue(headers.ContainsKey("CustomHeader"));
        }

        [Test]
        public async Task Should_dispatch_immediately_if_user_requested()
        {
            var behavior = new RoutingToDispatchConnector();
            var dispatched = false;

            var options = new SendOptions();
            options.RequireImmediateDispatch();

            var routingContext = new TestableRoutingContext()
            {
                RoutingStrategies = new RoutingStrategy[]
                {
                    new CustomRoutingStrategy()
                }
            };
            routingContext.Extensions.Set(new PendingTransportOperations()); // simular message handler batching behavior
            routingContext.Extensions.MergeScoped(options.Context, routingContext.Message.MessageId);

            await behavior.Invoke(routingContext, c =>
                {
                    dispatched = true;
                    return TaskEx.CompletedTask;
                });

            Assert.IsTrue(dispatched);
        }

        [Test]
        public async Task Should_dispatch_immediately_if_not_sending_from_a_handler()
        {
            var dispatched = false;
            var behavior = new RoutingToDispatchConnector();
            var message = new OutgoingMessage("ID", new Dictionary<string, string>(), new byte[0]);

            var routingContext = new TestableRoutingContext()
            {
                RoutingStrategies = new RoutingStrategy[]
                {
                    new CustomRoutingStrategy()
                }
            };
            await behavior.Invoke(routingContext, c =>
                {
                    dispatched = true;
                    return TaskEx.CompletedTask;
                });

            Assert.IsTrue(dispatched);
        }

        [Test]
        public async Task Should_not_dispatch_by_default()
        {
            var dispatched = false;
            var behavior = new RoutingToDispatchConnector();

            var routingContext = new TestableRoutingContext()
            {
                RoutingStrategies = new RoutingStrategy[]
                {
                    new CustomRoutingStrategy()
                }
            };
            routingContext.Extensions.Set(new PendingTransportOperations()); // simular message handler batching behavior
            await behavior.Invoke(routingContext, c =>
                {
                    dispatched = true;
                    return TaskEx.CompletedTask;
                });

            Assert.IsFalse(dispatched);
        }

        class CustomRoutingStrategy : RoutingStrategy
        {
            public override AddressTag Apply(Dictionary<string, string> headers)
            {
                headers["CustomHeader"] = "CustomValue";
                return new UnicastAddressTag("destination");
            }
        }

        class MyMessage : IMessage
        {
        }
    }
}