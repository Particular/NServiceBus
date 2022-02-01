namespace NServiceBus.Core.Tests.Recoverability
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Extensibility;
    using NServiceBus.Routing;
    using NUnit.Framework;
    using Transport;

    [TestFixture]
    public class MoveToErrorsExecutorTests
    {
        [Test]
        public void MoveToErrorQueue_should_dispatch_message_to_error_queue()
        {
            var customErrorQueue = "random_error_queue";

            var errorContext = CreateErrorContext();
            var moveToErrorAction = new MoveToError(customErrorQueue);
            var transportOperation = moveToErrorAction.Execute(errorContext, new Dictionary<string, string>())
                .Single();

            var addressTag = transportOperation.AddressTag as UnicastAddressTag;

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

            var errorContext = CreateErrorContext(messageHeaders: incomingMessageHeaders);

            var moveToErrorAction = new MoveToError(ErrorQueueAddress);
            var transportOperation = moveToErrorAction.Execute(errorContext, new Dictionary<string, string>())
                .Single();

            var outgoingMessageHeaders = transportOperation.Message.Headers;

            Assert.That(errorContext.Message.Headers, Is.SubsetOf(outgoingMessageHeaders));
        }

        [Test]
        public void MoveToErrorQueue_should_remove_known_retry_headers()
        {
            var retryHeaders = new Dictionary<string, string>
            {
                {Headers.ImmediateRetries, "42"},
                {Headers.DelayedRetries, "21"}
            };

            var errorContext = CreateErrorContext(messageHeaders: retryHeaders);

            var moveToErrorAction = new MoveToError(ErrorQueueAddress);
            var transportOperation = moveToErrorAction.Execute(errorContext, new Dictionary<string, string>())
                .Single();
            var outgoingMessageHeaders = transportOperation.Message.Headers;

            Assert.That(outgoingMessageHeaders.Keys, Does.Not.Contain(Headers.ImmediateRetries));
            Assert.That(outgoingMessageHeaders.Keys, Does.Not.Contain(Headers.DelayedRetries));
        }

        [Test]
        public void MoveToErrorQueue_should_add_metadata_to_headers()
        {
            var errorContext = CreateErrorContext();

            var moveToErrorAction = new MoveToError(ErrorQueueAddress);
            var transportOperation = moveToErrorAction.Execute(errorContext, new Dictionary<string, string> { { "staticFaultMetadataKey", "staticFaultMetadataValue" } })
                .Single();
            var outgoingMessageHeaders = transportOperation.Message.Headers;

            Assert.That(outgoingMessageHeaders, Contains.Item(new KeyValuePair<string, string>("staticFaultMetadataKey", "staticFaultMetadataValue")));
            // check for leaking headers
            Assert.That(errorContext.Message.Headers.ContainsKey("staticFaultMetadataKey"), Is.False);
        }

        static ErrorContext CreateErrorContext(Exception raisedException = null, string exceptionMessage = "default-message", string messageId = "default-id", int numberOfDeliveryAttempts = 1, Dictionary<string, string> messageHeaders = default)
        {
            return new ErrorContext(raisedException ?? new Exception(exceptionMessage), messageHeaders ?? new Dictionary<string, string>(), messageId, new byte[0], new TransportTransaction(), numberOfDeliveryAttempts, ReceiveAddress, new ContextBag());
        }

        const string ErrorQueueAddress = "errorQ";
        const string ReceiveAddress = "my-endpoint";
    }
}