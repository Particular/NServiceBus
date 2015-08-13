namespace NServiceBus.Core.Tests.Msmq
{
    using System;
    using System.Collections.Generic;
    using System.Messaging;
    using NServiceBus.ConsistencyGuarantees;
    using NServiceBus.DeliveryConstraints;
    using NServiceBus.Extensibility;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NServiceBus.Transports;
    using NServiceBus.Transports.Msmq;
    using NServiceBus.Transports.Msmq.Config;
    using NUnit.Framework;

    [TestFixture]
    public class MsmqMessageSenderTests
    {
        class FakeContext : BehaviorContext
        {
            public FakeContext()
                : base(null)
            {
            }
        }

        [Test]
        public void Should_set_label_when_convention_configured()
        {
            var queueName = "labelTest";
            var path = string.Format(@"{0}\private$\{1}", Environment.MachineName, queueName);
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
                var dispatchOptions = new DispatchOptions(new DirectToTargetDestination(queueName),new AtomicWithReceiveOperation(), new List<DeliveryConstraint>(), new ContextBag());
                messageSender.Dispatch(outgoingMessage, dispatchOptions);
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
            var path = string.Format(@".\private$\{0}", queueName);
            try
            {
                MsmqHelpers.DeleteQueue(path);
                MsmqHelpers.CreateQueue(path);
                var messageSender = new MsmqMessageSender(new MsmqSettings(),null);

                var bytes = new byte[]
                {
                    1
                };
                var headers = new Dictionary<string, string>();
                var outgoingMessage = new OutgoingMessage("1", headers, bytes);
                var dispatchOptions = new DispatchOptions(new DirectToTargetDestination(queueName), new AtomicWithReceiveOperation(), new List<DeliveryConstraint>(), new ContextBag());
                messageSender.Dispatch(outgoingMessage, dispatchOptions);
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
                if (message != null)
                {
                    return message.Label;
                }
            }
            return null;
        }
    }
}