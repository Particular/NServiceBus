namespace NServiceBus.Core.Tests.Recoverability
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
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
            var transportTransaction = new TransportTransaction();
            var incomingMessage = new IncomingMessage("messageId", new Dictionary<string, string>(), new byte[0]);

            await moveToErrorsExecutor.MoveToErrorQueue(customErrorQueue, incomingMessage, new Exception(), transportTransaction);

            Assert.That(dispatcher.TransportOperations.MulticastTransportOperations.Count(), Is.EqualTo(0));
            Assert.That(dispatcher.TransportOperations.UnicastTransportOperations.Count(), Is.EqualTo(1));
            Assert.That(dispatcher.Transaction, Is.EqualTo(transportTransaction));

            var outgoingMessage = dispatcher.TransportOperations.UnicastTransportOperations.Single();
            Assert.That(outgoingMessage.Destination, Is.EqualTo(customErrorQueue));
            Assert.That(outgoingMessage.Message.MessageId, Is.EqualTo(incomingMessage.MessageId));
        }

        [Test]
        public async Task MoveToErrorQueue_should_preserve_incoming_message_headers()
        {
            var incomingMessageHeaders = new Dictionary<string, string>
            {
                {"key1", "value1"},
                {"key2", "value2"}
            };

            var incomingMessage = new IncomingMessage("messageId", incomingMessageHeaders, new byte[0]);

            await moveToErrorsExecutor.MoveToErrorQueue(ErrorQueueAddress, incomingMessage, new Exception(), new TransportTransaction());

            var outgoingMessage = dispatcher.TransportOperations.UnicastTransportOperations.Single();
            Assert.That(incomingMessage.Headers, Is.SubsetOf(outgoingMessage.Message.Headers));
        }

        [Test]
        public async Task MoveToErrorQueue_should_dispatch_original_message_body()
        {
            var originalMessageBody = Encoding.UTF8.GetBytes("message body");
            var incomingMessage = new IncomingMessage("messageId", new Dictionary<string, string>(), originalMessageBody);
            incomingMessage.UpdateBody(Encoding.UTF8.GetBytes("new body"));

            await moveToErrorsExecutor.MoveToErrorQueue(ErrorQueueAddress, incomingMessage, new Exception(), new TransportTransaction());

            var outgoingMessage = dispatcher.TransportOperations.UnicastTransportOperations.Single();
            Assert.That(outgoingMessage.Message.Body, Is.EqualTo(originalMessageBody));
        }

        [Test]
        public async Task MoveToErrorQueue_should_remove_known_retry_headers()
        {
            var retryHeaders = new Dictionary<string, string>
            {
                {Headers.ImmediateRetries, "42"},
                {Headers.DelayedRetries, "21"}
            };
            var incomingMessage = new IncomingMessage("messageId", retryHeaders, new byte[0]);

            await moveToErrorsExecutor.MoveToErrorQueue(ErrorQueueAddress, incomingMessage, new Exception(), new TransportTransaction());

            var outgoingMessage = dispatcher.TransportOperations.UnicastTransportOperations.Single();
            Assert.That(outgoingMessage.Message.Headers.Keys, Does.Not.Contain(Headers.ImmediateRetries));
            Assert.That(outgoingMessage.Message.Headers.Keys, Does.Not.Contain(Headers.DelayedRetries));
        }

        [Test]
        public async Task MoveToErrorQueue_should_add_exception_headers()
        {
            var incomingMessage = new IncomingMessage("messageId", new Dictionary<string, string>(), new byte[0]);
            var exception = new InvalidOperationException("test exception");

            await moveToErrorsExecutor.MoveToErrorQueue(ErrorQueueAddress, incomingMessage, exception, new TransportTransaction());

            var outgoingMessageHeaders = dispatcher.TransportOperations.UnicastTransportOperations.Single().Message.Headers;
            // we only test presence of some exception headers set by ExceptionHeaderHelper
            Assert.That(outgoingMessageHeaders, Contains.Key("NServiceBus.ExceptionInfo.ExceptionType"));
            Assert.That(outgoingMessageHeaders, Contains.Key("NServiceBus.ExceptionInfo.Message"));
            Assert.That(outgoingMessageHeaders, Contains.Key("NServiceBus.ExceptionInfo.StackTrace"));
            // check for leaking headers
            Assert.That(incomingMessage.Headers.ContainsKey("NServiceBus.ExceptionInfo.ExceptionType"), Is.False);
        }

        [Test]
        public async Task MoveToErrorQueue_should_add_static_fault_info_to_headers()
        {
            staticFaultMetadata.Add("staticFaultMetadataKey", "staticFaultMetadataValue");
            var incomingMessage = new IncomingMessage("messageId", new Dictionary<string, string>(), new byte[0]);

            await moveToErrorsExecutor.MoveToErrorQueue(ErrorQueueAddress, incomingMessage, new Exception(), new TransportTransaction());

            var outgoingMessageHeaders = dispatcher.TransportOperations.UnicastTransportOperations.Single().Message.Headers;
            Assert.That(outgoingMessageHeaders, Contains.Item(new KeyValuePair<string, string>("staticFaultMetadataKey", "staticFaultMetadataValue")));
            // check for leaking headers
            Assert.That(incomingMessage.Headers.ContainsKey("staticFaultMetadataKey"), Is.False);
        }

        [Test]
        public async Task MoveToErrorQueue_should_apply_header_customizations_before_dispatch()
        {
            staticFaultMetadata.Add("staticFaultMetadataKey", "staticFaultMetadataValue");
            var incomingMessage = new IncomingMessage("messageId", new Dictionary<string, string>(), new byte[0]);
            var exception = new InvalidOperationException("test exception");

            Dictionary<string, string> passedInHeaders = null;
            moveToErrorsExecutor = new MoveToErrorsExecutor(dispatcher, staticFaultMetadata, headers => { passedInHeaders = headers; });

            await moveToErrorsExecutor.MoveToErrorQueue(ErrorQueueAddress, incomingMessage, exception, new TransportTransaction());

            Assert.NotNull(passedInHeaders);
            Assert.That(passedInHeaders, Contains.Key("staticFaultMetadataKey"));
            Assert.That(passedInHeaders, Contains.Key("NServiceBus.ExceptionInfo.Message"));
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