namespace NServiceBus.Core.Tests.Recoverability
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Extensibility;
    using NServiceBus.Routing;
    using NServiceBus.Transport;
    using NUnit.Framework;
    using Testing;

    [TestFixture]
    public class DelayedRetryRecoverabilityActionTests
    {
        [Test]
        public void When_delay_message_retry()
        {
            var recoverabilityContext = CreateRecoverabilityContext();
            var delay = TimeSpan.FromSeconds(42);
            var delayedRetryAction = new DelayedRetry(delay);

            var routingContext = delayedRetryAction.GetRoutingContexts(recoverabilityContext)
                .Single();

            var routingStrategy = routingContext.RoutingStrategies.Single() as UnicastRoutingStrategy;

            Assert.AreEqual(recoverabilityContext.ErrorContext.ReceiveAddress, (routingStrategy.Apply(new Dictionary<string, string>()) as UnicastAddressTag).Destination);
            Assert.AreEqual(delay, routingContext.Extensions.Get<DispatchProperties>().DelayDeliveryWith.Delay);
            Assert.AreEqual(ErrorHandleResult.Handled, delayedRetryAction.ErrorHandleResult);
        }

        [Test]
        public void Should_update_retry_headers_when_present()
        {
            var delayedRetryAction = new DelayedRetry(TimeSpan.Zero);
            var originalHeadersTimestamp = DateTimeOffsetHelper.ToWireFormattedString(new DateTimeOffset(2012, 12, 12, 0, 0, 0, TimeSpan.Zero));

            var recoverabilityContext = CreateRecoverabilityContext(new Dictionary<string, string>
            {
                {Headers.DelayedRetries, "2"},
                {Headers.DelayedRetriesTimestamp, originalHeadersTimestamp}
            });

            var now = DateTimeOffset.UtcNow;
            var routingContexts = delayedRetryAction.GetRoutingContexts(recoverabilityContext);

            var incomingMessage = recoverabilityContext.ErrorContext.Message;

            var outgoingMessageHeaders = routingContexts.Single().Message.Headers;

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
            var recoverabilityContext = CreateRecoverabilityContext();

            var routingContexts = delayedRetryAction.GetRoutingContexts(recoverabilityContext);

            var outgoingMessageHeaders = routingContexts.Single().Message.Headers;

            Assert.AreEqual("1", outgoingMessageHeaders[Headers.DelayedRetries]);
            Assert.IsFalse(recoverabilityContext.ErrorContext.Message.Headers.ContainsKey(Headers.DelayedRetries));
            Assert.IsTrue(outgoingMessageHeaders.ContainsKey(Headers.DelayedRetriesTimestamp));
            Assert.IsFalse(recoverabilityContext.ErrorContext.Message.Headers.ContainsKey(Headers.DelayedRetriesTimestamp));
        }

        static TestableRecoverabilityContext CreateRecoverabilityContext(Dictionary<string, string> headers = null)
        {
            var errorContext = new ErrorContext(new Exception(), headers ?? new Dictionary<string, string>(),
                "messageId", Array.Empty<byte>(), new TransportTransaction(), 0, "my-queue", new ContextBag());
            return new TestableRecoverabilityContext { ErrorContext = errorContext };
        }
    }
}