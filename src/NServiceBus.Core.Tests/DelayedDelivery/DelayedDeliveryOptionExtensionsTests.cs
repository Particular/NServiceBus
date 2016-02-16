namespace NServiceBus.Core.Tests.Timeout
{
    using System;
    using NUnit.Framework;

    [TestFixture]
    public class DelayedDeliveryOptionExtensionsTests
    {
        [Test]
        public void GetDeliveryDelay_Should_Return_The_Configured_Delay_TimeSpan()
        {
            var options = new SendOptions();
            var delay = TimeSpan.FromMinutes(42);
            options.DelayDeliveryWith(delay);

            Assert.AreEqual(delay, options.GetDeliveryDelay());
        }

        [Test]
        public void GetDeliveryDelay_Should_Return_Null_When_No_Delay_Configured()
        {
            var options = new SendOptions();

            Assert.IsNull(options.GetDeliveryDelay());
        }

        [Test]
        public void GetDeliveryDate_Should_Return_The_Configured_Delivery_Date()
        {
            var options = new SendOptions();
            DateTimeOffset deliveryDate = new DateTime(2012, 12, 12, 12, 12, 12);
            options.DoNotDeliverBefore(deliveryDate);

            Assert.AreEqual(deliveryDate, options.GetDeliveryDate());
        }

        [Test]
        public void GetDeliveryDate_Should_Return_Null_When_No_Date_Configured()
        {
            var options = new SendOptions();

            Assert.IsNull(options.GetDeliveryDate());
        }
    }
}