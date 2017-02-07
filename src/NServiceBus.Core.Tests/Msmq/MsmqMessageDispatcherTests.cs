namespace NServiceBus.Core.Tests.Msmq
{
    using System;
    using System.Collections.Generic;
    using System.Messaging;
    using DeliveryConstraints;
    using Extensibility;
    using NServiceBus.Performance.TimeToBeReceived;
    using NServiceBus.Routing;
    using Transport;
    using NUnit.Framework;
    using Support;

    [TestFixture]
    public class MsmqMessageDispatcherTests
    {
        [Test]
        public void Should_set_label_when_convention_configured()
        {
            var queueName = "labelTest";
            var path = $@"{RuntimeEnvironment.MachineName}\private$\{queueName}";
            try
            {
                MsmqHelpers.DeleteQueue(path);
                MsmqHelpers.CreateQueue(path);
                var messageSender = new MsmqMessageDispatcher(new MsmqSettings(), _ => "mylabel");

                var bytes = new byte[]
                {
                    1
                };
                var headers = new Dictionary<string, string>();
                var outgoingMessage = new OutgoingMessage("1", headers, bytes);
                var transportOperation = new TransportOperation(outgoingMessage, new UnicastAddressTag(queueName), DispatchConsistency.Default);
                messageSender.Dispatch(new TransportOperations(transportOperation), new TransportTransaction(), new ContextBag());
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
                var messageSender = new MsmqMessageDispatcher(new MsmqSettings(), pairs => string.Empty);

                var bytes = new byte[]
                {
                    1
                };
                var headers = new Dictionary<string, string>();
                var outgoingMessage = new OutgoingMessage("1", headers, bytes);
                var transportOperation = new TransportOperation(outgoingMessage, new UnicastAddressTag(queueName), DispatchConsistency.Default);
                messageSender.Dispatch(new TransportOperations(transportOperation), new TransportTransaction(), new ContextBag());
                var messageLabel = ReadMessageLabel(path);
                Assert.IsEmpty(messageLabel);

            }
            finally
            {
                MsmqHelpers.DeleteQueue(path);
            }
        }

        [Test]
        public void Should_default_dlq_to_off_for_messages_with_ttbr()
        {
            var queueName = "dlqWithTTBRDefaultTest";
            var path = $@".\private$\{queueName}";
            try
            {
                MsmqHelpers.DeleteQueue(path);
                MsmqHelpers.CreateQueue(path);
                var messageSender = new MsmqMessageDispatcher(new MsmqSettings(), pairs => string.Empty);

                var bytes = new byte[] { 1 };
                var headers = new Dictionary<string, string>();
                var outgoingMessage = new OutgoingMessage("1", headers, bytes);
                var transportOperation = new TransportOperation(outgoingMessage, new UnicastAddressTag(queueName), DispatchConsistency.Default, new List<DeliveryConstraint>
                {
                    new DiscardIfNotReceivedBefore(TimeSpan.FromMinutes(10))
                });

                messageSender.Dispatch(new TransportOperations(transportOperation), new TransportTransaction(), new ContextBag());
                var message = ReadMessage(path);

                Assert.False(message.UseDeadLetterQueue);

            }
            finally
            {
                MsmqHelpers.DeleteQueue(path);
            }
        }

        [Test]
        public void Should_allow_optin_for_dlq_on_ttbr_messages()
        {
            var queueName = "dlqOptInForTTBRTest";
            var path = $@".\private$\{queueName}";
            try
            {
                MsmqHelpers.DeleteQueue(path);
                MsmqHelpers.CreateQueue(path);
                var messageSender = new MsmqMessageDispatcher(new MsmqSettings
                {
                    UseDeadLetterQueueForMessagesWithTimeToReachQueue = true
                }, pairs => string.Empty);

                var bytes = new byte[] { 1 };
                var headers = new Dictionary<string, string>();
                var outgoingMessage = new OutgoingMessage("1", headers, bytes);
                var transportOperation = new TransportOperation(outgoingMessage, new UnicastAddressTag(queueName), DispatchConsistency.Default, new List<DeliveryConstraint>
                {
                    new DiscardIfNotReceivedBefore(TimeSpan.FromMinutes(10))
                });

                messageSender.Dispatch(new TransportOperations(transportOperation), new TransportTransaction(), new ContextBag());
                var message = ReadMessage(path);

                Assert.True(message.UseDeadLetterQueue);

            }
            finally
            {
                MsmqHelpers.DeleteQueue(path);
            }
        }

        [Test]
        public void Should_default_honor_dlq_setting()
        {
            var queueName = "dlqDefaultTest";
            var path = $@".\private$\{queueName}";
            try
            {
                MsmqHelpers.DeleteQueue(path);
                MsmqHelpers.CreateQueue(path);
                var messageSender = new MsmqMessageDispatcher(new MsmqSettings
                {
                    UseDeadLetterQueue = true
                }, pairs => string.Empty);

                var bytes = new byte[]
                {
                    1
                };
                var headers = new Dictionary<string, string>();
                var outgoingMessage = new OutgoingMessage("1", headers, bytes);
                var transportOperation = new TransportOperation(outgoingMessage, new UnicastAddressTag(queueName), DispatchConsistency.Default);

                messageSender.Dispatch(new TransportOperations(transportOperation), new TransportTransaction(), new ContextBag());
                var message = ReadMessage(path);

                Assert.True(message.UseDeadLetterQueue);

            }
            finally
            {
                MsmqHelpers.DeleteQueue(path);
            }
        }


        static string ReadMessageLabel(string path)
        {
            return ReadMessage(path)?.Label;
        }

        static Message ReadMessage(string path)
        {
            using (var queue = new MessageQueue(path))
            using (var message = queue.Receive(TimeSpan.FromSeconds(5)))
            {
                return message;
            }
        }
    }
}