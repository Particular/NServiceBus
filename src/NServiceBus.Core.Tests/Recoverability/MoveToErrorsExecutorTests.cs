namespace NServiceBus.Core.Tests.Recoverability
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Faults;
    using NUnit.Framework;
    using Transport;

    [TestFixture]
    public class MoveToErrorsExecutorTests
    {
        [SetUp]
        public void Setup()
        {
            dispatchCollector = new DispatchCollector();
            staticFaultMetadata = new Dictionary<string, string>();
            moveToErrorsExecutor = new MoveToErrorsExecutor(staticFaultMetadata, headers => { });
        }

        [Test]
        public async Task MoveToErrorQueue_should_dispatch_message_to_error_queue()
        {
            var customErrorQueue = "random_error_queue";

            var errorContext = CreateErrorContext();

            await moveToErrorsExecutor.MoveToErrorQueue(customErrorQueue, errorContext, dispatchCollector.Collect);

            Assert.AreEqual(customErrorQueue, dispatchCollector.Destination);
        }

        [Test]
        public async Task MoveToErrorQueue_should_preserve_incoming_message_headers()
        {
            var incomingMessageHeaders = new Dictionary<string, string>
            {
                {"key1", "value1"},
                {"key2", "value2"}
            };

            var errorContext = CreateErrorContext(messageHeaders: incomingMessageHeaders);

            await moveToErrorsExecutor.MoveToErrorQueue(ErrorQueueAddress, errorContext, dispatchCollector.Collect);

            Assert.That(errorContext.Message.Headers, Is.SubsetOf(dispatchCollector.MessageHeaders));
        }

        [Test]
        public async Task MoveToErrorQueue_should_remove_known_retry_headers()
        {
            var retryHeaders = new Dictionary<string, string>
            {
                {Headers.ImmediateRetries, "42"},
                {Headers.DelayedRetries, "21"}
            };

            var errorContext = CreateErrorContext(messageHeaders: retryHeaders);

            await moveToErrorsExecutor.MoveToErrorQueue(ErrorQueueAddress, errorContext, dispatchCollector.Collect);

            Assert.That(dispatchCollector.MessageHeaders.Keys, Does.Not.Contain(Headers.ImmediateRetries));
            Assert.That(dispatchCollector.MessageHeaders.Keys, Does.Not.Contain(Headers.DelayedRetries));
        }

        [Test]
        public async Task MoveToErrorQueue_should_add_exception_headers()
        {
            var exception = new InvalidOperationException("test exception");
            var errorContext = CreateErrorContext(raisedException: exception);

            await moveToErrorsExecutor.MoveToErrorQueue(ErrorQueueAddress, errorContext, dispatchCollector.Collect);

            var outgoingMessageHeaders = dispatchCollector.MessageHeaders;
            // we only test presence of some exception headers set by ExceptionHeaderHelper
            Assert.That(outgoingMessageHeaders, Contains.Key("NServiceBus.ExceptionInfo.ExceptionType"));
            Assert.That(outgoingMessageHeaders, Contains.Key("NServiceBus.ExceptionInfo.Message"));
            Assert.That(outgoingMessageHeaders, Contains.Key("NServiceBus.ExceptionInfo.StackTrace"));
            // check for leaking headers
            Assert.That(errorContext.Message.Headers.ContainsKey("NServiceBus.ExceptionInfo.ExceptionType"), Is.False);
        }


        [Test]
        public async Task MoveToErrorQueue_should_add_failed_queue_header()
        {
            var errorContext = CreateErrorContext();

            await moveToErrorsExecutor.MoveToErrorQueue(ErrorQueueAddress, errorContext, dispatchCollector.Collect);

            var outgoingMessageHeaders = dispatchCollector.MessageHeaders;

            Assert.That(outgoingMessageHeaders, Contains.Key(FaultsHeaderKeys.FailedQ));
            Assert.AreEqual(outgoingMessageHeaders[FaultsHeaderKeys.FailedQ], ReceiveAddress);
        }

        [Test]
        public async Task MoveToErrorQueue_should_add_static_fault_info_to_headers()
        {
            staticFaultMetadata.Add("staticFaultMetadataKey", "staticFaultMetadataValue");

            var errorContext = CreateErrorContext();

            await moveToErrorsExecutor.MoveToErrorQueue(ErrorQueueAddress, errorContext, dispatchCollector.Collect);

            var outgoingMessageHeaders = dispatchCollector.MessageHeaders;
            Assert.That(outgoingMessageHeaders, Contains.Item(new KeyValuePair<string, string>("staticFaultMetadataKey", "staticFaultMetadataValue")));
            // check for leaking headers
            Assert.That(errorContext.Message.Headers.ContainsKey("staticFaultMetadataKey"), Is.False);
        }

        [Test]
        public async Task MoveToErrorQueue_should_apply_header_customizations_before_dispatch()
        {
            staticFaultMetadata.Add("staticFaultMetadataKey", "staticFaultMetadataValue");
            var exception = new InvalidOperationException("test exception");

            Dictionary<string, string> passedInHeaders = null;
            moveToErrorsExecutor = new MoveToErrorsExecutor(staticFaultMetadata, headers => { passedInHeaders = headers; });

            await moveToErrorsExecutor.MoveToErrorQueue(ErrorQueueAddress, CreateErrorContext(raisedException: exception), dispatchCollector.Collect);

            Assert.NotNull(passedInHeaders);
            Assert.That(passedInHeaders, Contains.Key("staticFaultMetadataKey"));
            Assert.That(passedInHeaders, Contains.Key("NServiceBus.ExceptionInfo.Message"));
        }

        static ErrorContext CreateErrorContext(Exception raisedException = null, string exceptionMessage = "default-message", string messageId = "default-id", int numberOfDeliveryAttempts = 1, Dictionary<string, string> messageHeaders = default)
        {
            return new ErrorContext(raisedException ?? new Exception(exceptionMessage), messageHeaders ?? new Dictionary<string, string>(), messageId, new byte[0], new TransportTransaction(), numberOfDeliveryAttempts, ReceiveAddress, new ContextBag());
        }


        MoveToErrorsExecutor moveToErrorsExecutor;
        DispatchCollector dispatchCollector;
        Dictionary<string, string> staticFaultMetadata;
        const string ErrorQueueAddress = "errorQ";
        const string ReceiveAddress = "my-endpoint";
    }
}