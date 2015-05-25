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
        public void DeliveryOptions()
        {
            var options = new DeliveryMessageOptions
            {
                TimeToBeReceived = TimeSpan.FromMinutes(1),
                NonDurable = true
            };

            var converted = options.ToTransportOperationOptions().ToDeliveryOptions();

            Assert.AreEqual(converted.TimeToBeReceived, options.TimeToBeReceived); 
            Assert.AreEqual(converted.NonDurable, options.NonDurable);
      
        }

     
        class MyMessage
        { }
    }
}
