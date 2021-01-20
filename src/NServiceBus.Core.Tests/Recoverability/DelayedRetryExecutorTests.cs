using System.Threading;
using NServiceBus.Transport;

namespace NServiceBus.Core.Tests.Recoverability
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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
            var delayedRetryExecutor = CreateExecutor();
            var incomingMessage = CreateMessage();

            await delayedRetryExecutor.Retry(incomingMessage, TimeSpan.Zero, transportTransaction);

            Assert.AreEqual(dispatcher.Transaction, transportTransaction);
        }

        [Test]
        public async Task When_native_delayed_delivery_should_add_delivery_constraint()
        {
            var delayedRetryExecutor = CreateExecutor();
            var incomingMessage = CreateMessage();
            var delay = TimeSpan.FromSeconds(42);

            await delayedRetryExecutor.Retry(incomingMessage, delay, new TransportTransaction());

            var transportOperation = dispatcher.UnicastTransportOperations.Single();
            var deliveryConstraint = transportOperation.Properties.DelayDeliveryWith;

            Assert.AreEqual(transportOperation.Destination, EndpointInputQueue);
            Assert.IsNotNull(deliveryConstraint);
            Assert.AreEqual(delay, deliveryConstraint.Delay);
        }

        [Test]
        public async Task Should_update_retry_headers_when_present()
        {
            var delayedRetryExecutor = CreateExecutor();
            var originalHeadersTimestamp = DateTimeOffsetHelper.ToWireFormattedString(new DateTime(2012, 12, 12, 0, 0, 0, DateTimeKind.Utc));

            var incomingMessage = CreateMessage(new Dictionary<string, string>
            {
                {Headers.DelayedRetries, "2"},
                {Headers.DelayedRetriesTimestamp, originalHeadersTimestamp}
            });

            var now = DateTime.UtcNow;
            await delayedRetryExecutor.Retry(incomingMessage, TimeSpan.Zero, new TransportTransaction());

            var outgoingMessageHeaders = dispatcher.UnicastTransportOperations.Single().Message.Headers;

            Assert.AreEqual("3", outgoingMessageHeaders[Headers.DelayedRetries]);
            Assert.AreEqual("2", incomingMessage.Headers[Headers.DelayedRetries]);

            var utcDateTime = DateTimeOffsetHelper.ToDateTimeOffset(outgoingMessageHeaders[Headers.DelayedRetriesTimestamp]);
            // the serialization removes precision which may lead to now being greater than the deserialized header value
            var adjustedNow = DateTimeOffsetHelper.ToDateTimeOffset(DateTimeOffsetHelper.ToWireFormattedString(now));
            Assert.That(utcDateTime, Is.GreaterThanOrEqualTo(adjustedNow));
            Assert.AreEqual(originalHeadersTimestamp, incomingMessage.Headers[Headers.DelayedRetriesTimestamp]);
        }

        [Test]
        public async Task Should_add_retry_headers_when_not_present()
        {
            var delayedRetryExecutor = CreateExecutor();
            var incomingMessage = CreateMessage();

            await delayedRetryExecutor.Retry(incomingMessage, TimeSpan.Zero, new TransportTransaction());

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

        DelayedRetryExecutor CreateExecutor()
        {
            return new DelayedRetryExecutor(EndpointInputQueue, dispatcher);
        }

        FakeDispatcher dispatcher;
        const string EndpointInputQueue = "endpoint input queue";

        class FakeDispatcher : IMessageDispatcher
        {
            public TransportOperations TransportOperations { get; private set; }

            public List<UnicastTransportOperation> UnicastTransportOperations => TransportOperations.UnicastTransportOperations;

            public TransportTransaction Transaction { get; private set; }

            public Task Dispatch(TransportOperations outgoingMessages, TransportTransaction transaction)
            {
                TransportOperations = outgoingMessages;
                Transaction = transaction;
                return Task.FromResult(0);
            }
        }
    }
}