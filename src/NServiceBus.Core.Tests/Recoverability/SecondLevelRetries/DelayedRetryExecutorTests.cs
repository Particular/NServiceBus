namespace NServiceBus.Core.Tests.Recoverability.SecondLevelRetries
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using DelayedDelivery;
    using Extensibility;
    using NServiceBus.Transports;
    using NUnit.Framework;

    [TestFixture]
    public class DelayedRetryExecutorTests
    {
        const string TimeoutManagerAddress = "timeout handling endpoint";
        const string EndpointInputQueue = "endpoint input queue";

        [Test]
        public async Task Should_dispatch_original_message_body()
        {
            var dispatcher = new FakeDispatcher();
            var delayedRetryExecutor = new DelayedRetryExecutor(EndpointInputQueue, dispatcher);
            var originalBody = Encoding.UTF8.GetBytes("original message body");
            var incomingMessage = new IncomingMessage("messageId", new Dictionary<string, string>(), new MemoryStream(originalBody));
            incomingMessage.Body = Encoding.UTF8.GetBytes("updated message body");

            await delayedRetryExecutor.Retry(incomingMessage, TimeSpan.Zero, new ContextBag());

            var outgoingMessage = dispatcher.TransportOperations.UnicastTransportOperations.Single();
            Assert.That(outgoingMessage.Message.Body, Is.EqualTo(originalBody));
        }

        [Test]
        public async Task Should_float_context_to_dispatcher()
        {
            var context = new ContextBag();
            var dispatcher = new FakeDispatcher();
            var delayedRetryExecutor = new DelayedRetryExecutor(EndpointInputQueue, dispatcher);
            var incomingMessage = new IncomingMessage("messageId", new Dictionary<string, string>(), Stream.Null);

            await delayedRetryExecutor.Retry(incomingMessage, TimeSpan.Zero, context);

            Assert.That(dispatcher.ContextBag, Is.SameAs(context));
        }

        [Test]
        public async Task When_native_delayed_delivery_should_add_delivery_constraint()
        {
            var dispatcher = new FakeDispatcher();
            var delayedRetryExecutor = new DelayedRetryExecutor(EndpointInputQueue, dispatcher);
            var delay = TimeSpan.FromSeconds(42);
            var incomingMessage = new IncomingMessage("messageId", new Dictionary<string, string>(), Stream.Null);

            await delayedRetryExecutor.Retry(incomingMessage, delay, new ContextBag());

            var outgoingMessage = dispatcher.TransportOperations.UnicastTransportOperations.Single();
            Assert.That(outgoingMessage.Destination, Is.EqualTo(EndpointInputQueue));
            var deliveryConstraint = outgoingMessage.DeliveryConstraints.OfType<DelayDeliveryWith>().SingleOrDefault();
            Assert.That(deliveryConstraint, Is.Not.Null);
            Assert.That(deliveryConstraint.Delay, Is.EqualTo(delay));
        }

        [Test]
        public async Task When_no_native_delayed_delivery_should_route_message_to_timeout_manager()
        {
            var dispatcher = new FakeDispatcher();
            var delayedRetryExecutor = new DelayedRetryExecutor(EndpointInputQueue, dispatcher, TimeoutManagerAddress);
            var delay = TimeSpan.FromSeconds(42);
            var incomingMessage = new IncomingMessage("messageId", new Dictionary<string, string>(), Stream.Null);

            await delayedRetryExecutor.Retry(incomingMessage, delay, new ContextBag());

            var outgoingMessage = dispatcher.TransportOperations.UnicastTransportOperations.Single();
            var deliveryConstraint = outgoingMessage.DeliveryConstraints.OfType<DelayDeliveryWith>().SingleOrDefault();
            Assert.That(deliveryConstraint, Is.Null);

            Assert.That(outgoingMessage.Message.Headers[TimeoutManagerHeaders.RouteExpiredTimeoutTo], Is.EqualTo(EndpointInputQueue));
            Assert.That(DateTimeExtensions.ToUtcDateTime(outgoingMessage.Message.Headers[TimeoutManagerHeaders.Expire]), Is.GreaterThan(DateTime.UtcNow).And.LessThanOrEqualTo(DateTime.UtcNow + delay));
            Assert.That(outgoingMessage.Destination, Is.EqualTo(TimeoutManagerAddress));
        }

        [Test]
        public async Task Should_update_retry_headers_when_present()
        {
            var dispatcher = new FakeDispatcher();
            var delayedRetryExecutor = new DelayedRetryExecutor(EndpointInputQueue, dispatcher);
            var dictionary = new Dictionary<string, string>()
            {
                {Headers.Retries, "2"}
            };
            var incomingMessage = new IncomingMessage("messageId", dictionary, Stream.Null);

            await delayedRetryExecutor.Retry(incomingMessage, TimeSpan.Zero, new ContextBag());

            var outgoingMessageHeaders = dispatcher.TransportOperations.UnicastTransportOperations.Single().Message.Headers;
            Assert.That(outgoingMessageHeaders[Headers.Retries], Is.EqualTo("3"));
            Assert.That(incomingMessage.Headers[Headers.Retries], Is.EqualTo("2"));
            Assert.That(outgoingMessageHeaders.ContainsKey(Headers.RetriesTimestamp), Is.True);
            Assert.That(incomingMessage.Headers.ContainsKey(Headers.RetriesTimestamp), Is.False);
        }

        [Test]
        public async Task Should_add_retry_headers_when_not_present()
        {
            var dispatcher = new FakeDispatcher();
            var delayedRetryExecutor = new DelayedRetryExecutor(EndpointInputQueue, dispatcher);
            var incomingMessage = new IncomingMessage("messageId", new Dictionary<string, string>(), Stream.Null);

            await delayedRetryExecutor.Retry(incomingMessage, TimeSpan.Zero, new ContextBag());

            var outgoingMessageHeaders = dispatcher.TransportOperations.UnicastTransportOperations.Single().Message.Headers;
            Assert.That(outgoingMessageHeaders[Headers.Retries], Is.EqualTo("1"));
            Assert.That(incomingMessage.Headers.ContainsKey(Headers.Retries), Is.False);
        }

        class FakeDispatcher : IDispatchMessages
        {
            public TransportOperations TransportOperations { get; set; }

            public ContextBag ContextBag { get; set; }

            public Task Dispatch(TransportOperations outgoingMessages, ContextBag context)
            {
                TransportOperations = outgoingMessages;
                ContextBag = context;
                return Task.FromResult(0);
            }
        }
    }
}