namespace NServiceBus.Core.Tests.Recoverability
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Transport;
    using NUnit.Framework;

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
            var delayedRetryExecutor = CreateExecutor();
            var errorContext = CreateErrorContext();

            await delayedRetryExecutor.Retry(errorContext, TimeSpan.Zero);

            Assert.AreEqual(dispatcher.Transaction, errorContext.TransportTransaction);
        }

        [Test]
        public async Task When_native_delayed_delivery_should_add_delivery_constraint()
        {
            var delayedRetryExecutor = CreateExecutor();
            var errorContext = CreateErrorContext();
            var delay = TimeSpan.FromSeconds(42);

            await delayedRetryExecutor.Retry(errorContext, delay);

            var transportOperation = dispatcher.UnicastTransportOperations.Single();
            var deliveryConstraint = transportOperation.Properties.DelayDeliveryWith;

            Assert.AreEqual(transportOperation.Destination, errorContext.ReceiveAddress);
            Assert.IsNotNull(deliveryConstraint);
            Assert.AreEqual(delay, deliveryConstraint.Delay);
        }

        [Test]
        public async Task Should_update_retry_headers_when_present()
        {
            var delayedRetryExecutor = CreateExecutor();
            var originalHeadersTimestamp = DateTimeOffsetHelper.ToWireFormattedString(new DateTimeOffset(2012, 12, 12, 0, 0, 0, TimeSpan.Zero));

            var errorContext = CreateErrorContext(new Dictionary<string, string>
            {
                {Headers.DelayedRetries, "2"},
                {Headers.DelayedRetriesTimestamp, originalHeadersTimestamp}
            });

            var now = DateTimeOffset.UtcNow;
            await delayedRetryExecutor.Retry(errorContext, TimeSpan.Zero);

            var incomingMessage = errorContext.Message;

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
            var errorContext = CreateErrorContext();

            await delayedRetryExecutor.Retry(errorContext, TimeSpan.Zero);

            var outgoingMessageHeaders = dispatcher.TransportOperations.UnicastTransportOperations.Single().Message.Headers;

            Assert.AreEqual("1", outgoingMessageHeaders[Headers.DelayedRetries]);
            Assert.IsFalse(errorContext.Message.Headers.ContainsKey(Headers.DelayedRetries));
            Assert.IsTrue(outgoingMessageHeaders.ContainsKey(Headers.DelayedRetriesTimestamp));
            Assert.IsFalse(errorContext.Message.Headers.ContainsKey(Headers.DelayedRetriesTimestamp));
        }

        DelayedRetryExecutor CreateExecutor()
        {
            return new DelayedRetryExecutor(dispatcher);
        }

        ErrorContext CreateErrorContext(Dictionary<string, string> headers = null)
        {
            return new ErrorContext(new Exception(), headers ?? new Dictionary<string, string>(), "messageId", new byte[0], new TransportTransaction(), 0, "my-queue", new ContextBag());
        }

        FakeDispatcher dispatcher;

        class FakeDispatcher : IMessageDispatcher
        {
            public TransportOperations TransportOperations { get; private set; }

            public List<UnicastTransportOperation> UnicastTransportOperations => TransportOperations.UnicastTransportOperations;

            public TransportTransaction Transaction { get; private set; }

            public Task Dispatch(TransportOperations outgoingMessages, TransportTransaction transaction, CancellationToken cancellationToken = default)
            {
                TransportOperations = outgoingMessages;
                Transaction = transaction;
                return Task.FromResult(0);
            }
        }
    }
}