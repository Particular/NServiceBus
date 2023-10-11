namespace NServiceBus.Core.Tests.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NUnit.Framework;
    using Testing;
    using Transport;

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
                    return Task.CompletedTask;
                });

            Assert.IsTrue(headers.ContainsKey("CustomHeader"));
        }

        [Test]
        public async Task Should_dispatch_immediately_if_user_requested()
        {
            var options = new SendOptions();
            options.RequireImmediateDispatch();

            var dispatched = false;
            var behavior = new RoutingToDispatchConnector();
            var message = new OutgoingMessage("ID", [], new byte[0]);

            await behavior.Invoke(new RoutingContext(message,
                new UnicastRoutingStrategy("Destination"), CreateContext(options, true)), c =>
                {
                    dispatched = true;
                    return Task.CompletedTask;
                });

            Assert.IsTrue(dispatched);
        }

        [Test]
        public async Task Should_dispatch_immediately_if_not_sending_from_a_handler()
        {
            var dispatched = false;
            var behavior = new RoutingToDispatchConnector();
            var message = new OutgoingMessage("ID", [], new byte[0]);

            await behavior.Invoke(new RoutingContext(message,
                new UnicastRoutingStrategy("Destination"), CreateContext(new SendOptions(), false)), c =>
                {
                    dispatched = true;
                    return Task.CompletedTask;
                });

            Assert.IsTrue(dispatched);
        }

        [Test]
        public async Task Should_not_dispatch_by_default()
        {
            var dispatched = false;
            var behavior = new RoutingToDispatchConnector();
            var message = new OutgoingMessage("ID", [], new byte[0]);

            await behavior.Invoke(new RoutingContext(message,
                new UnicastRoutingStrategy("Destination"), CreateContext(new SendOptions(), true)), c =>
                {
                    dispatched = true;
                    return Task.CompletedTask;
                });

            Assert.IsFalse(dispatched);
        }

        [Test]
        public async Task Should_promote_message_headers_to_pipeline_activity()
        {
            var behavior = new RoutingToDispatchConnector();
            var routingContext = new TestableRoutingContext();
            routingContext.Message.Headers[Headers.ContentType] = "test content type"; // one of the headers that will be mapped to tags

            using var pipelineActivity = new Activity("pipeline activity");
            routingContext.Extensions.SetOutgoingPipelineActitvity(pipelineActivity);
            using var ambientActivity = new Activity("ambient activity");
            ambientActivity.Start();

            await behavior.Invoke(routingContext, _ => Task.CompletedTask);

            var contentTypeTag = pipelineActivity.GetTagItem(ActivityTags.ContentType);
            Assert.AreEqual("test content type", contentTypeTag, "should set tags on activity from pipeline context");

            Assert.IsNull(ambientActivity.GetTagItem(ActivityTags.ContentType), "should not set tags on Activity.Current");
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