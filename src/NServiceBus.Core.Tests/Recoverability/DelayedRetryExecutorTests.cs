namespace NServiceBus.Core.Tests.Recoverability
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using DelayedDelivery;
    using Extensibility;
    using NUnit.Framework;
    using Transport;

    [TestFixture]
    public class DelayedRetryExecutorTests
    {
        [SetUp]
        public void Setup()
        {
            dispatcher = new FakeDispatcher();
        }

        [Test]
        public async Task Should_float_transport_transaction_to_dispatcher()
        {
            var transportTransaction = new TransportTransaction();
            var delayedRetryExecutor = CreateExecutor(nativeDeferralsOn: true);
            var incomingMessage = CreateMessage();

            await delayedRetryExecutor.Retry(incomingMessage, TimeSpan.Zero, transportTransaction, CancellationToken.None);

            Assert.AreEqual(dispatcher.Transaction, transportTransaction);
        }

        [Test]
        public async Task When_native_delayed_delivery_should_add_delivery_constraint()
        {
            var delayedRetryExecutor = CreateExecutor(nativeDeferralsOn: true);
            var incomingMessage = CreateMessage();
            var delay = TimeSpan.FromSeconds(42);

            await delayedRetryExecutor.Retry(incomingMessage, delay, new TransportTransaction(), CancellationToken.None);

            var transportOperation = dispatcher.UnicastTransportOperations.Single();
            var deliveryConstraint = transportOperation.DeliveryConstraints.OfType<DelayDeliveryWith>().SingleOrDefault();

            Assert.AreEqual(transportOperation.Destination, EndpointInputQueue);
            Assert.IsNotNull(deliveryConstraint);
            Assert.AreEqual(delay, deliveryConstraint.Delay);
        }

        [Test]
        public async Task When_no_native_delayed_delivery_should_route_message_to_timeout_manager()
        {
            var delayedRetryExecutor = CreateExecutor(nativeDeferralsOn: false);
            var incomingMessage = CreateMessage();
            var delay = TimeSpan.FromSeconds(42);

            await delayedRetryExecutor.Retry(incomingMessage, delay, new TransportTransaction(), CancellationToken.None);

            var transportOperation = dispatcher.UnicastTransportOperations.Single();
            var deliveryConstraint = transportOperation.DeliveryConstraints.OfType<DelayDeliveryWith>().SingleOrDefault();

            Assert.IsNull(deliveryConstraint);
            Assert.AreEqual(EndpointInputQueue, transportOperation.Message.Headers[TimeoutManagerHeaders.RouteExpiredTimeoutTo]);
            Assert.That(DateTimeExtensions.ToUtcDateTime(transportOperation.Message.Headers[TimeoutManagerHeaders.Expire]), Is.GreaterThan(DateTime.UtcNow).And.LessThanOrEqualTo(DateTime.UtcNow + delay));
            Assert.AreEqual(TimeoutManagerAddress, transportOperation.Destination);
        }

        [Test]
        public async Task Should_update_retry_headers_when_present()
        {
            var delayedRetryExecutor = CreateExecutor(nativeDeferralsOn: true);
            var originalHeadersTimestamp = DateTimeExtensions.ToWireFormattedString(new DateTime(2012, 12, 12, 0, 0, 0, DateTimeKind.Utc));

            var incomingMessage = CreateMessage(new Dictionary<string, string>
            {
                {Headers.DelayedRetries, "2"},
                {Headers.DelayedRetriesTimestamp, originalHeadersTimestamp}
            });

            var now = DateTime.UtcNow;
            await delayedRetryExecutor.Retry(incomingMessage, TimeSpan.Zero, new TransportTransaction(), CancellationToken.None);

            var outgoingMessageHeaders = dispatcher.UnicastTransportOperations.Single().Message.Headers;

            Assert.AreEqual("3", outgoingMessageHeaders[Headers.DelayedRetries]);
            Assert.AreEqual("2", incomingMessage.Headers[Headers.DelayedRetries]);

            var utcDateTime = DateTimeExtensions.ToUtcDateTime(outgoingMessageHeaders[Headers.DelayedRetriesTimestamp]);
            // the serialization removes precision which may lead to now being greater than the deserialized header value
            var adjustedNow = DateTimeExtensions.ToUtcDateTime(DateTimeExtensions.ToWireFormattedString(now));
            Assert.That(utcDateTime, Is.GreaterThanOrEqualTo(adjustedNow));
            Assert.AreEqual(originalHeadersTimestamp, incomingMessage.Headers[Headers.DelayedRetriesTimestamp]);
        }

        [Test]
        public async Task Should_add_retry_headers_when_not_present()
        {
            var delayedRetryExecutor = CreateExecutor(nativeDeferralsOn: false);
            var incomingMessage = CreateMessage();

            await delayedRetryExecutor.Retry(incomingMessage, TimeSpan.Zero, new TransportTransaction(), CancellationToken.None);

            var outgoingMessageHeaders = dispatcher.TransportOperations.UnicastTransportOperations.Single().Message.Headers;

            Assert.AreEqual("1", outgoingMessageHeaders[Headers.DelayedRetries]);
            Assert.IsFalse(incomingMessage.Headers.ContainsKey(Headers.DelayedRetries));
            Assert.IsTrue(outgoingMessageHeaders.ContainsKey(Headers.DelayedRetriesTimestamp));
            Assert.IsFalse(incomingMessage.Headers.ContainsKey(Headers.DelayedRetriesTimestamp));
        }

        IncomingMessage CreateMessage(Dictionary<string, string> headers = null)
        {
            return new IncomingMessage("messageId", headers ?? new Dictionary<string, string>(), new byte[0]);
        }

        DelayedRetryExecutor CreateExecutor(bool nativeDeferralsOn = true)
        {
            return new DelayedRetryExecutor(EndpointInputQueue, dispatcher, nativeDeferralsOn ? null : TimeoutManagerAddress);
        }

        FakeDispatcher dispatcher;

        const string TimeoutManagerAddress = "timeout handling endpoint";
        const string EndpointInputQueue = "endpoint input queue";

        class FakeDispatcher : IDispatchMessages
        {
            public TransportOperations TransportOperations { get; private set; }

            public List<UnicastTransportOperation> UnicastTransportOperations => TransportOperations.UnicastTransportOperations;

            public ContextBag ContextBag { get; private set; }

            public TransportTransaction Transaction { get; private set; }

            public Task Dispatch(TransportOperations outgoingMessages, TransportTransaction transaction, ContextBag context, CancellationToken cancellationToken)
            {
                TransportOperations = outgoingMessages;
                ContextBag = context;
                Transaction = transaction;
                return Task.FromResult(0);
            }
        }
    }
}