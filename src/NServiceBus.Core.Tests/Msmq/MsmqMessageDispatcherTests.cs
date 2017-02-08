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

    [TestFixture]
    public class MsmqMessageDispatcherTests
    {
        [Test]
        public void Should_set_label_when_convention_configured()
        {
            var dispatchedMessage = DispatchMessage("labelTest", new MsmqSettings(), messageLabelGenerator: _ => "mylabel");

            Assert.AreEqual("mylabel", dispatchedMessage.Label);
        }

        [Test]
        public void Should_default_dlq_to_off_for_messages_with_ttbr()
        {
            var dispatchedMessage = DispatchMessage("dlqOffForTTBR", deliveryConstraint: new DiscardIfNotReceivedBefore(TimeSpan.FromMinutes(10)));

            Assert.False(dispatchedMessage.UseDeadLetterQueue);
        }

        [Test]
        public void Should_allow_optin_for_dlq_on_ttbr_messages()
        {
            var settings = new MsmqSettings
            {
                UseDeadLetterQueueForMessagesWithTimeToReachQueue = true
            };

            var dispatchedMessage = DispatchMessage("dlqOnForTTBR", settings, new DiscardIfNotReceivedBefore(TimeSpan.FromMinutes(10)));

            Assert.True(dispatchedMessage.UseDeadLetterQueue);
        }

        [Test]
        public void Should_set_dlq_by_default_for_non_ttbr_messages()
        {
            var dispatchedMessage = DispatchMessage("dlqOnByDefault");

            Assert.True(dispatchedMessage.UseDeadLetterQueue);
        }

        static Message DispatchMessage(string queueName, MsmqSettings settings = null, DeliveryConstraint deliveryConstraint = null, Func<IReadOnlyDictionary<string, string>, string> messageLabelGenerator = null)
        {
            if (settings == null)
            {
                settings = new MsmqSettings();
            }

            if (messageLabelGenerator == null)
            {
                messageLabelGenerator = _ => string.Empty;
            }

            var path = $@".\private$\{queueName}";

            try
            {
                MsmqHelpers.DeleteQueue(path);
                MsmqHelpers.CreateQueue(path);

                var messageSender = new MsmqMessageDispatcher(settings, messageLabelGenerator);

                var bytes = new byte[]
                {
                    1
                };
                var headers = new Dictionary<string, string>();
                var outgoingMessage = new OutgoingMessage("1", headers, bytes);
                var deliveryConstraints = new List<DeliveryConstraint>();

                if (deliveryConstraint != null)
                {
                    deliveryConstraints.Add(deliveryConstraint);
                }

                var transportOperation = new TransportOperation(outgoingMessage, new UnicastAddressTag(queueName), DispatchConsistency.Default, deliveryConstraints);

                messageSender.Dispatch(new TransportOperations(transportOperation), new TransportTransaction(), new ContextBag());

                using (var queue = new MessageQueue(path))
                using (var message = queue.Receive(TimeSpan.FromSeconds(5)))
                {
                    return message;
                }
            }
            finally
            {
                MsmqHelpers.DeleteQueue(path);
            }
        }
    }
}