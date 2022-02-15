namespace NServiceBus.Core.Tests.Recoverability
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Routing;
    using NUnit.Framework;
    using Testing;
    using Transport;

    [TestFixture]
    public class MoveToErrorsExecutorTests
    {
        [Test]
        public void MoveToErrorQueue_should_route_message_to_error_queue()
        {
            var customErrorQueue = "random_error_queue";

            var recoverabilityContext = CreateRecoverabilityContext();
            var moveToErrorAction = new MoveToError(customErrorQueue);
            var routingContext = moveToErrorAction.GetRoutingContexts(recoverabilityContext)
                .Single();

            var addressTag = (UnicastAddressTag)((UnicastRoutingStrategy)routingContext.RoutingStrategies.Single())
                .Apply(new Dictionary<string, string>());

            Assert.AreEqual(customErrorQueue, addressTag.Destination);
            Assert.AreEqual(ErrorHandleResult.Handled, moveToErrorAction.ErrorHandleResult);
        }

        [Test]
        public void MoveToErrorQueue_should_preserve_incoming_message_headers()
        {
            var incomingMessageHeaders = new Dictionary<string, string>
            {
                {"key1", "value1"},
                {"key2", "value2"}
            };

            var recoverabilityContext = CreateRecoverabilityContext(messageHeaders: incomingMessageHeaders);

            var moveToErrorAction = new MoveToError(ErrorQueueAddress);
            var routingContext = moveToErrorAction.GetRoutingContexts(recoverabilityContext)
                .Single();

            var outgoingMessageHeaders = routingContext.Message.Headers;

            Assert.That(recoverabilityContext.FailedMessage.Headers, Is.SubsetOf(outgoingMessageHeaders));
        }

        [Test]
        public void MoveToErrorQueue_should_remove_known_retry_headers()
        {
            var retryHeaders = new Dictionary<string, string>
            {
                {Headers.ImmediateRetries, "42"},
                {Headers.DelayedRetries, "21"}
            };

            var recoverabilityContext = CreateRecoverabilityContext(messageHeaders: retryHeaders);

            var moveToErrorAction = new MoveToError(ErrorQueueAddress);
            var transportOperation = moveToErrorAction.GetRoutingContexts(recoverabilityContext)
                .Single();
            var outgoingMessageHeaders = transportOperation.Message.Headers;

            Assert.That(outgoingMessageHeaders.Keys, Does.Not.Contain(Headers.ImmediateRetries));
            Assert.That(outgoingMessageHeaders.Keys, Does.Not.Contain(Headers.DelayedRetries));
        }

        [Test]
        public void MoveToErrorQueue_should_add_metadata_to_headers()
        {
            var recoverabilityContext = CreateRecoverabilityContext(metadata: new Dictionary<string, string> { { "staticFaultMetadataKey", "staticFaultMetadataValue" } });

            var moveToErrorAction = new MoveToError(ErrorQueueAddress);
            var transportOperation = moveToErrorAction.GetRoutingContexts(recoverabilityContext)
                .Single();
            var outgoingMessageHeaders = transportOperation.Message.Headers;

            Assert.That(outgoingMessageHeaders, Contains.Item(new KeyValuePair<string, string>("staticFaultMetadataKey", "staticFaultMetadataValue")));
            // check for leaking headers
            Assert.That(recoverabilityContext.FailedMessage.Headers.ContainsKey("staticFaultMetadataKey"), Is.False);
        }

        static TestableRecoverabilityContext CreateRecoverabilityContext(Exception raisedException = null, string exceptionMessage = "default-message", string messageId = "default-id", Dictionary<string, string> messageHeaders = default, Dictionary<string, string> metadata = default)
        {
            var recoverabilityContext = new TestableRecoverabilityContext
            {
                FailedMessage = new IncomingMessage(messageId, messageHeaders ?? new Dictionary<string, string>(), ReadOnlyMemory<byte>.Empty),
                Exception = raisedException ?? new Exception(exceptionMessage)
            };

            if (metadata != default)
            {
                recoverabilityContext.Metadata = metadata;
            }
            return recoverabilityContext;
        }

        const string ErrorQueueAddress = "errorQ";
    }
}