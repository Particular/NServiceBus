namespace NServiceBus.Core.Tests.Outbox
{
    using System;
    using NServiceBus.Outbox;
    using NServiceBus.Unicast;
    using NUnit.Framework;

    [TestFixture]
    class TransportOperationConverterTests
    {
        [Test]
        public void SendMessageOptions()
        {
            var options = new SendMessageOptions("destination", DateTime.UtcNow.AddDays(1))
            {
                TimeToBeReceived = TimeSpan.FromMinutes(1),
                NonDurable = true,
                EnlistInReceiveTransaction = false,
                EnforceMessagingBestPractices = false
            };

            var converted = (SendMessageOptions)options.ToTransportOperationOptions().ToDeliveryOptions();

            Assert.AreEqual(converted.DeliverAt.ToString(), options.DeliverAt.ToString()); //the ticks will be off
            Assert.AreEqual(converted.Destination, options.Destination);
            Assert.AreEqual(converted.TimeToBeReceived, options.TimeToBeReceived); 
            Assert.AreEqual(converted.NonDurable, options.NonDurable);
            Assert.AreEqual(converted.EnforceMessagingBestPractices, options.EnforceMessagingBestPractices);
            Assert.AreEqual(converted.EnlistInReceiveTransaction, options.EnlistInReceiveTransaction);

            options = new SendMessageOptions("destination", delayDeliveryFor: TimeSpan.FromMinutes(1))
            {
                TimeToBeReceived = TimeSpan.FromMinutes(1),
                NonDurable = true,
                EnlistInReceiveTransaction = false,
                EnforceMessagingBestPractices = false
            };

            converted = (SendMessageOptions)options.ToTransportOperationOptions().ToDeliveryOptions();
            Assert.AreEqual(converted.DelayDeliveryFor, options.DelayDeliveryFor);
        }

        [Test]
        public void PublishMessageOptions()
        {

            var options = new PublishMessageOptions(typeof(MyMessage))
            {
                TimeToBeReceived = TimeSpan.FromMinutes(1),
                NonDurable = true,
                EnforceMessagingBestPractices = false,
            };

            var converted = (PublishMessageOptions)options.ToTransportOperationOptions().ToDeliveryOptions();


            Assert.AreEqual(typeof(MyMessage), options.EventType);
            Assert.AreEqual(converted.TimeToBeReceived, options.TimeToBeReceived);
            Assert.AreEqual(converted.NonDurable, options.NonDurable);
            Assert.AreEqual(converted.EnlistInReceiveTransaction, options.EnlistInReceiveTransaction);
            Assert.AreEqual(converted.EnforceMessagingBestPractices, options.EnforceMessagingBestPractices);
        }

        class MyMessage
        { }
    }
}
