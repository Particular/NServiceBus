namespace NServiceBus.MSMQ.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Messaging;
    using NServiceBus.Extensibility;
    using NServiceBus.Transports;
    using NServiceBus.Transports.Msmq;
    using NServiceBus.Transports.Msmq.Config;
    using NUnit.Framework;

    [TestFixture]
    public class MsmqMessageSenderTests
    {
        [Test]
        public void Should_set_label_when_convention_configured()
        {
            var queueName = "labelTest";
            var path = $@"{Environment.MachineName}\private$\{queueName}";
            try
            {
                MsmqHelpers.DeleteQueue(path);
                MsmqHelpers.CreateQueue(path);
                var messageSender = new MsmqMessageSender(new MsmqSettings(), _ => "mylabel");

                var bytes = new byte[]
                {
                    1
                };
                var headers = new Dictionary<string, string>();
                var outgoingMessage = new OutgoingMessage("1", headers, bytes);
                var transportOperation = new UnicastTransportOperation(outgoingMessage, queueName);
                messageSender.Dispatch(new TransportOperations(Enumerable.Empty<MulticastTransportOperation>(), new [] { transportOperation}), new ContextBag());
                var messageLabel = ReadMessageLabel(path);
                Assert.AreEqual("mylabel", messageLabel);

            }
            finally
            {
                MsmqHelpers.DeleteQueue(path);
            }
        }
        [Test]
        public void Should_use_string_empty_label_when_no_convention_configured()
        {
            var queueName = "emptyLabelTest";
            var path = $@".\private$\{queueName}";
            try
            {
                MsmqHelpers.DeleteQueue(path);
                MsmqHelpers.CreateQueue(path);
                var messageSender = new MsmqMessageSender(new MsmqSettings(), pairs => string.Empty);

                var bytes = new byte[]
                {
                    1
                };
                var headers = new Dictionary<string, string>();
                var outgoingMessage = new OutgoingMessage("1", headers, bytes);
                var transportOperation = new UnicastTransportOperation(outgoingMessage, queueName);
                messageSender.Dispatch(new TransportOperations(Enumerable.Empty<MulticastTransportOperation>(), new[] { transportOperation }), new ContextBag());
                var messageLabel = ReadMessageLabel(path);
                Assert.IsEmpty(messageLabel);

            }
            finally
            {
                MsmqHelpers.DeleteQueue(path);
            }
        }

        static string ReadMessageLabel(string path)
        {
            using (var queue = new MessageQueue(path))
            using (var message = queue.Receive(TimeSpan.FromSeconds(5)))
            {
                return message?.Label;
            }
        }
    }
}