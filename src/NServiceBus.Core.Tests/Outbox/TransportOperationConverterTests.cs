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
        public void SendOptions()
        {

            var options = new SendOptions("destination")
            {
                CorrelationId = "cid",
                DelayDeliveryWith = TimeSpan.FromMinutes(1),
                DeliverAt = DateTime.UtcNow.AddDays(1),
                TimeToBeReceived = TimeSpan.FromMinutes(1),
                NonDurable = true,
                ReplyToAddress = "reply to",
                EnlistInReceiveTransaction = false,
                EnforceMessagingBestPractices = false
            };

            var converted = (SendOptions)options.ToTransportOperationOptions().ToDeliveryOptions();

            Assert.AreEqual(converted.CorrelationId,options.CorrelationId);
            Assert.AreEqual(converted.DelayDeliveryWith, options.DelayDeliveryWith);
            Assert.AreEqual(converted.DeliverAt.ToString(), options.DeliverAt.ToString()); //the ticks will be off
            Assert.AreEqual(converted.Destination, options.Destination);
            Assert.AreEqual(converted.TimeToBeReceived, options.TimeToBeReceived); 
            Assert.AreEqual(converted.NonDurable, options.NonDurable);
            Assert.AreEqual(converted.ReplyToAddress, options.ReplyToAddress);
            Assert.AreEqual(converted.EnforceMessagingBestPractices, options.EnforceMessagingBestPractices);
            Assert.AreEqual(converted.EnlistInReceiveTransaction, options.EnlistInReceiveTransaction);
        }

        [Test]
        public void PublishOptions()
        {

            var options = new PublishOptions(typeof(MyMessage))
            {
                TimeToBeReceived = TimeSpan.FromMinutes(1),
                NonDurable = true,
                EnforceMessagingBestPractices = false,
                ReplyToAddress = "reply to"
            };

            var converted = (PublishOptions)options.ToTransportOperationOptions().ToDeliveryOptions();


            Assert.AreEqual(typeof(MyMessage), options.EventType);
            Assert.AreEqual(converted.TimeToBeReceived, options.TimeToBeReceived);
            Assert.AreEqual(converted.NonDurable, options.NonDurable);
            Assert.AreEqual(converted.ReplyToAddress, options.ReplyToAddress);
            Assert.AreEqual(converted.EnlistInReceiveTransaction, options.EnlistInReceiveTransaction);
            Assert.AreEqual(converted.EnforceMessagingBestPractices, options.EnforceMessagingBestPractices);
        }

        class MyMessage
        { }
    }
}
