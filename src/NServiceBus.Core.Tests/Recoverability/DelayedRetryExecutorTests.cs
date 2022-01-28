namespace NServiceBus.Core.Tests.Recoverability
{
    using System;
    using System.Collections.Generic;
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
            dispatchCollector = new DispatchCollector();
        }

        [Test]
        public async Task When_native_delayed_delivery_should_add_delivery_constraint()
        {
            var delayedRetryExecutor = CreateExecutor();
            var errorContext = CreateErrorContext();
            var delay = TimeSpan.FromSeconds(42);

            await delayedRetryExecutor.Retry(errorContext, delay, dispatchCollector.Collect);

            Assert.AreEqual(errorContext.ReceiveAddress, dispatchCollector.Destination);
            Assert.AreEqual(delay, dispatchCollector.Delay);
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
            await delayedRetryExecutor.Retry(errorContext, TimeSpan.Zero, dispatchCollector.Collect);

            var incomingMessage = errorContext.Message;

            var outgoingMessageHeaders = dispatchCollector.MessageHeaders;

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

            await delayedRetryExecutor.Retry(errorContext, TimeSpan.Zero, dispatchCollector.Collect);

            var outgoingMessageHeaders = dispatchCollector.MessageHeaders;

            Assert.AreEqual("1", outgoingMessageHeaders[Headers.DelayedRetries]);
            Assert.IsFalse(errorContext.Message.Headers.ContainsKey(Headers.DelayedRetries));
            Assert.IsTrue(outgoingMessageHeaders.ContainsKey(Headers.DelayedRetriesTimestamp));
            Assert.IsFalse(errorContext.Message.Headers.ContainsKey(Headers.DelayedRetriesTimestamp));
        }

        DelayedRetryExecutor CreateExecutor()
        {
            return new DelayedRetryExecutor();
        }

        ErrorContext CreateErrorContext(Dictionary<string, string> headers = null)
        {
            return new ErrorContext(new Exception(), headers ?? new Dictionary<string, string>(), "messageId", new byte[0], new TransportTransaction(), 0, "my-queue", new ContextBag());
        }

        DispatchCollector dispatchCollector;
    }
}