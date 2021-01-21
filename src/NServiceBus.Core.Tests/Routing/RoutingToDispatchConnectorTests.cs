namespace NServiceBus.Core.Tests.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;
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
            await behavior.Invoke(new TestableRoutingContext { RoutingStrategies = new List<RoutingStrategy> { new CustomRoutingStrategy() } }, (context, t) =>
                {
                    headers = context.Operations.First().Message.Headers;
                    return Task.CompletedTask;
                }, default);

            Assert.IsTrue(headers.ContainsKey("CustomHeader"));
        }

        [Test]
        public async Task Should_dispatch_immediately_if_user_requested()
        {
            var options = new SendOptions();
            options.RequireImmediateDispatch();

            var dispatched = false;
            var behavior = new RoutingToDispatchConnector();
            var message = new OutgoingMessage("ID", new Dictionary<string, string>(), new byte[0]);

            await behavior.Invoke(new RoutingContext(message,
                new UnicastRoutingStrategy("Destination"), CreateContext(options, true)), (_, __) =>
                {
                    dispatched = true;
                    return Task.CompletedTask;
                }, default);

            Assert.IsTrue(dispatched);
        }

        [Test]
        public async Task Should_dispatch_immediately_if_not_sending_from_a_handler()
        {
            var dispatched = false;
            var behavior = new RoutingToDispatchConnector();
            var message = new OutgoingMessage("ID", new Dictionary<string, string>(), new byte[0]);

            await behavior.Invoke(new RoutingContext(message,
                new UnicastRoutingStrategy("Destination"), CreateContext(new SendOptions(), false)), (_, __) =>
                {
                    dispatched = true;
                    return Task.CompletedTask;
                }, default);

            Assert.IsTrue(dispatched);
        }

        [Test]
        public async Task Should_not_dispatch_by_default()
        {
            var dispatched = false;
            var behavior = new RoutingToDispatchConnector();
            var message = new OutgoingMessage("ID", new Dictionary<string, string>(), new byte[0]);

            await behavior.Invoke(new RoutingContext(message,
                new UnicastRoutingStrategy("Destination"), CreateContext(new SendOptions(), true)), (_, __) =>
                {
                    dispatched = true;
                    return Task.CompletedTask;
                }, default);

            Assert.IsFalse(dispatched);
        }

        static IOutgoingSendContext CreateContext(SendOptions options, bool fromHandler)
        {
            var message = new MyMessage();
            var context = new OutgoingSendContext(new OutgoingLogicalMessage(message.GetType(), message), options.UserDefinedMessageId ?? Guid.NewGuid().ToString(), options.OutgoingHeaders, options.Context, new FakeRootContext());
            if (fromHandler)
            {
                context.Extensions.Set(new PendingTransportOperations());
            }
            return context;
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