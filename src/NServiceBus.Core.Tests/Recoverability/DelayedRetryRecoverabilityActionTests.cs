namespace NServiceBus.Core.Tests.Recoverability
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Extensibility;
    using NServiceBus.Routing;
    using NServiceBus.Transport;
    using NUnit.Framework;

    [TestFixture]
    public class DelayedRetryRecoverabilityActionTests
    {
        [Test]
        public void When_delay_message_retry()
        {
            var errorContext = CreateErrorContext();
            var delay = TimeSpan.FromSeconds(42);
            var delayedRetryAction = new DelayedRetry(delay);

            var transportOperation = delayedRetryAction.Execute(errorContext, new Dictionary<string, string>())
                .Single();

            var addressTag = transportOperation.AddressTag as UnicastAddressTag;

            Assert.AreEqual(errorContext.ReceiveAddress, addressTag.Destination);
            Assert.AreEqual(delay, transportOperation.Properties.DelayDeliveryWith.Delay);
            Assert.AreEqual(ErrorHandleResult.Handled, delayedRetryAction.ErrorHandleResult);
        }

        [Test]
        public void Should_update_retry_headers_when_present()
        {
            var delayedRetryAction = new DelayedRetry(TimeSpan.Zero);
            var originalHeadersTimestamp = DateTimeOffsetHelper.ToWireFormattedString(new DateTimeOffset(2012, 12, 12, 0, 0, 0, TimeSpan.Zero));

            var errorContext = CreateErrorContext(new Dictionary<string, string>
            {
                {Headers.DelayedRetries, "2"},
                {Headers.DelayedRetriesTimestamp, originalHeadersTimestamp}
            });

            var now = DateTimeOffset.UtcNow;
            var transportOperations = delayedRetryAction.Execute(errorContext, new Dictionary<string, string>());

            var incomingMessage = errorContext.Message;

            var outgoingMessageHeaders = transportOperations.Single().Message.Headers;

            Assert.AreEqual("3", outgoingMessageHeaders[Headers.DelayedRetries]);
            Assert.AreEqual("2", incomingMessage.Headers[Headers.DelayedRetries]);

            var utcDateTime = DateTimeOffsetHelper.ToDateTimeOffset(outgoingMessageHeaders[Headers.DelayedRetriesTimestamp]);
            // the serialization removes precision which may lead to now being greater than the deserialized header value
            var adjustedNow = DateTimeOffsetHelper.ToDateTimeOffset(DateTimeOffsetHelper.ToWireFormattedString(now));
            Assert.That(utcDateTime, Is.GreaterThanOrEqualTo(adjustedNow));
            Assert.AreEqual(originalHeadersTimestamp, incomingMessage.Headers[Headers.DelayedRetriesTimestamp]);
        }

        [Test]
        public void Should_add_retry_headers_when_not_present()
        {
            var delayedRetryAction = new DelayedRetry(TimeSpan.Zero);
            var errorContext = CreateErrorContext();

            var transportOperations = delayedRetryAction.Execute(errorContext, new Dictionary<string, string>());

            var outgoingMessageHeaders = transportOperations.Single().Message.Headers;

            Assert.AreEqual("1", outgoingMessageHeaders[Headers.DelayedRetries]);
            Assert.IsFalse(errorContext.Message.Headers.ContainsKey(Headers.DelayedRetries));
            Assert.IsTrue(outgoingMessageHeaders.ContainsKey(Headers.DelayedRetriesTimestamp));
            Assert.IsFalse(errorContext.Message.Headers.ContainsKey(Headers.DelayedRetriesTimestamp));
        }

        ErrorContext CreateErrorContext(Dictionary<string, string> headers = null)
        {
            return new ErrorContext(new Exception(), headers ?? new Dictionary<string, string>(), "messageId", new byte[0], new TransportTransaction(), 0, "my-queue", new ContextBag());
        }
    }
}