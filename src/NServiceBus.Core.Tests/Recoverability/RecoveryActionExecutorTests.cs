namespace NServiceBus.Core.Tests.Recoverability
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Extensibility;
    using NServiceBus.Transports;
    using NUnit.Framework;

    [TestFixture]
    public class RecoveryActionExecutorTests
    {
        const string ErrorQueueAddress = "errorQ";
        RecoveryActionExecutor recoveryActionExecutor;
        FakeDispatcher dispatcher;
        Dictionary<string, string> staticFaultMetadata;

        [SetUp]
        public void Setup()
        {
            dispatcher = new FakeDispatcher();
            staticFaultMetadata = new Dictionary<string, string>();
            recoveryActionExecutor = new RecoveryActionExecutor(dispatcher, ErrorQueueAddress, staticFaultMetadata);
        }

        [Test]
        public async Task MoveToErrorQueue_should_dispatch_message_to_error_queue()
        {
            var context = new ContextBag();
            var incomingMessage = new IncomingMessage("messageId", new Dictionary<string, string>(), Stream.Null);

            await recoveryActionExecutor.MoveToErrorQueue(incomingMessage, new Exception(), context);

            Assert.That(dispatcher.TransportOperations.MulticastTransportOperations.Count(), Is.EqualTo(0));
            Assert.That(dispatcher.TransportOperations.UnicastTransportOperations.Count(), Is.EqualTo(1));
            Assert.That(dispatcher.ContextBag, Is.EqualTo(context));

            var outgoingMessage = dispatcher.TransportOperations.UnicastTransportOperations.Single();
            Assert.That(outgoingMessage.Destination, Is.EqualTo(ErrorQueueAddress));
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

            var incomingMessage = new IncomingMessage("messageId", incomingMessageHeaders, Stream.Null);

            await recoveryActionExecutor.MoveToErrorQueue(incomingMessage, new Exception(), new ContextBag());

            var outgoingMessage = dispatcher.TransportOperations.UnicastTransportOperations.Single();
            Assert.That(incomingMessage.Headers, Is.SubsetOf(outgoingMessage.Message.Headers));
        }

        [Test]
        public async Task MoveToErrorQueue_should_dispatch_original_message_body()
        {
            var originalMessageBody = Encoding.UTF8.GetBytes("message body");
            var incomingMessage = new IncomingMessage("messageId", new Dictionary<string, string>(), new MemoryStream(originalMessageBody));
            incomingMessage.Body = Encoding.UTF8.GetBytes("new body");

            await recoveryActionExecutor.MoveToErrorQueue(incomingMessage, new Exception(), new ContextBag());

            var outgoingMessage = dispatcher.TransportOperations.UnicastTransportOperations.Single();
            Assert.That(outgoingMessage.Message.Body, Is.EqualTo(originalMessageBody));
        }

        [Test]
        public async Task MoveToErrorQueue_should_remove_known_retry_headers()
        {
            var retryHeaders = new Dictionary<string, string>()
            {
                { Headers.FLRetries, "42" },
                { Headers.Retries, "21" }
            };
            var incomingMessage = new IncomingMessage("messageId", retryHeaders, Stream.Null);

            await recoveryActionExecutor.MoveToErrorQueue(incomingMessage, new Exception(), new ContextBag());

            var outgoingMessage = dispatcher.TransportOperations.UnicastTransportOperations.Single();
            Assert.That(outgoingMessage.Message.Headers.Keys, Does.Not.Contain(Headers.FLRetries));
            Assert.That(outgoingMessage.Message.Headers.Keys, Does.Not.Contain(Headers.Retries));
        }

        [Test]
        public async Task MoveToErrorQueue_should_add_exception_headers()
        {
            var incomingMessage = new IncomingMessage("messageId", new Dictionary<string, string>(), Stream.Null);
            var exception = new InvalidOperationException("test exception");

            await recoveryActionExecutor.MoveToErrorQueue(incomingMessage, exception, new ContextBag());

            var outgoingMessageHeaders = dispatcher.TransportOperations.UnicastTransportOperations.Single().Message.Headers;
            // we only test presence of some exception headers set by ExceptionHeaderHelper
            Assert.That(outgoingMessageHeaders, Contains.Key("NServiceBus.ExceptionInfo.ExceptionType"));
            Assert.That(outgoingMessageHeaders, Contains.Key("NServiceBus.ExceptionInfo.Message"));
            Assert.That(outgoingMessageHeaders, Contains.Key("NServiceBus.ExceptionInfo.StackTrace"));
        }

        [Test]
        public async Task MoveToErrorQueue_should_add_static_fault_info_to_headers()
        {
            staticFaultMetadata.Add("staticFaultMetadataKey", "staticFaultMetadataValue");
            var incomingMessage = new IncomingMessage("messageId", new Dictionary<string, string>(), Stream.Null);

            await recoveryActionExecutor.MoveToErrorQueue(incomingMessage, new Exception(), new ContextBag());

            var outgoingMessageHeaders = dispatcher.TransportOperations.UnicastTransportOperations.Single().Message.Headers;
            Assert.That(outgoingMessageHeaders, Contains.Item(new KeyValuePair<string, string>("staticFaultMetadataKey", "staticFaultMetadataValue")));
        }

        class FakeDispatcher : IDispatchMessages
        {
            public TransportOperations TransportOperations { get; set; }

            public ContextBag ContextBag { get; set; }

            public Task Dispatch(TransportOperations outgoingMessages, ContextBag context)
            {
                TransportOperations = outgoingMessages;
                ContextBag = context;
                return Task.FromResult(0);
            }
        }
    }
}