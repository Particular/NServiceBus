namespace NServiceBus.Core.Tests.Recoverability
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NUnit.Framework;
    using Transport;

    [TestFixture]
    public class MoveToErrorsExecutorTests
    {
        [SetUp]
        public void Setup()
        {
            dispatcher = new FakeDispatcher();
            staticFaultMetadata = new Dictionary<string, string>();
            moveToErrorsExecutor = new MoveToErrorsExecutor(dispatcher, staticFaultMetadata, headers => { });
        }

        [Test]
        public async Task MoveToErrorQueue_should_dispatch_message_to_error_queue()
        {
            var customErrorQueue = "random_error_queue";

            var errorContext = CreateErrorContext();

            await moveToErrorsExecutor.MoveToErrorQueue(customErrorQueue, errorContext);

            Assert.That(dispatcher.TransportOperations.MulticastTransportOperations.Count(), Is.EqualTo(0));
            Assert.That(dispatcher.TransportOperations.UnicastTransportOperations.Count(), Is.EqualTo(1));
            Assert.That(dispatcher.Transaction, Is.EqualTo(errorContext.TransportTransaction));

            var outgoingMessage = dispatcher.TransportOperations.UnicastTransportOperations.Single();
            Assert.That(outgoingMessage.Destination, Is.EqualTo(customErrorQueue));
            Assert.That(outgoingMessage.Message.MessageId, Is.EqualTo(errorContext.Message.MessageId));
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

            await moveToErrorsExecutor.MoveToErrorQueue(ErrorQueueAddress, errorContext);

            var outgoingMessage = dispatcher.TransportOperations.UnicastTransportOperations.Single();
            Assert.That(errorContext.Message.Headers, Is.SubsetOf(outgoingMessage.Message.Headers));
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

            await moveToErrorsExecutor.MoveToErrorQueue(ErrorQueueAddress, errorContext);

            var outgoingMessage = dispatcher.TransportOperations.UnicastTransportOperations.Single();
            Assert.That(outgoingMessage.Message.Headers.Keys, Does.Not.Contain(Headers.ImmediateRetries));
            Assert.That(outgoingMessage.Message.Headers.Keys, Does.Not.Contain(Headers.DelayedRetries));
        }

        [Test]
        public async Task MoveToErrorQueue_should_add_exception_headers()
        {
            var exception = new InvalidOperationException("test exception");
            var errorContext = CreateErrorContext(raisedException: exception);

            await moveToErrorsExecutor.MoveToErrorQueue(ErrorQueueAddress, errorContext);

            var outgoingMessageHeaders = dispatcher.TransportOperations.UnicastTransportOperations.Single().Message.Headers;
            // we only test presence of some exception headers set by ExceptionHeaderHelper
            Assert.That(outgoingMessageHeaders, Contains.Key("NServiceBus.ExceptionInfo.ExceptionType"));
            Assert.That(outgoingMessageHeaders, Contains.Key("NServiceBus.ExceptionInfo.Message"));
            Assert.That(outgoingMessageHeaders, Contains.Key("NServiceBus.ExceptionInfo.StackTrace"));
            // check for leaking headers
            Assert.That(errorContext.Message.Headers.ContainsKey("NServiceBus.ExceptionInfo.ExceptionType"), Is.False);
        }

        [Test]
        public async Task MoveToErrorQueue_should_add_static_fault_info_to_headers()
        {
            staticFaultMetadata.Add("staticFaultMetadataKey", "staticFaultMetadataValue");

            var errorContext = CreateErrorContext();

            await moveToErrorsExecutor.MoveToErrorQueue(ErrorQueueAddress, errorContext);

            var outgoingMessageHeaders = dispatcher.TransportOperations.UnicastTransportOperations.Single().Message.Headers;
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
            moveToErrorsExecutor = new MoveToErrorsExecutor(dispatcher, staticFaultMetadata, headers => { passedInHeaders = headers; });

            await moveToErrorsExecutor.MoveToErrorQueue(ErrorQueueAddress, CreateErrorContext(raisedException: exception));

            Assert.NotNull(passedInHeaders);
            Assert.That(passedInHeaders, Contains.Key("staticFaultMetadataKey"));
            Assert.That(passedInHeaders, Contains.Key("NServiceBus.ExceptionInfo.Message"));
        }

        static ErrorContext CreateErrorContext(Exception raisedException = null, string exceptionMessage = "default-message", string messageId = "default-id", int numberOfDeliveryAttempts = 1, Dictionary<string, string> messageHeaders = default)
        {
            return new ErrorContext(raisedException ?? new Exception(exceptionMessage), messageHeaders ?? new Dictionary<string, string>(), messageId, new byte[0], new TransportTransaction(), numberOfDeliveryAttempts, "my-endpoint", new ContextBag());
        }


        MoveToErrorsExecutor moveToErrorsExecutor;
        FakeDispatcher dispatcher;
        Dictionary<string, string> staticFaultMetadata;
        const string ErrorQueueAddress = "errorQ";

        class FakeDispatcher : IMessageDispatcher
        {
            public TransportOperations TransportOperations { get; private set; }

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