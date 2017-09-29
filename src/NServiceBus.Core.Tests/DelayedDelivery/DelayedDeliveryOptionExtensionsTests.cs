namespace NServiceBus.Core.Tests.Timeout
{
    using System;
    using NUnit.Framework;

    [TestFixture]
    public class DelayedDeliveryOptionExtensionsTests
    {
        [Test]
        public void GetDeliveryDelayShouldReturnTheConfiguredDelayTimeSpan()
        {
            var options = new SendOptions();
            var delay = TimeSpan.FromMinutes(42);
            options.DelayDeliveryWith(delay);

            Assert.AreEqual(delay, options.GetDeliveryDelay());
        }

        [Test]
        public void GetDeliveryDelayShouldReturnNullWhenNoDelayConfigured()
        {
            var options = new SendOptions();

            Assert.IsNull(options.GetDeliveryDelay());
        }

        [Test]
        public void GetDeliveryDateShouldReturnTheConfiguredDeliveryDate()
        {
            var options = new SendOptions();
            DateTimeOffset deliveryDate = new DateTime(2012, 12, 12, 12, 12, 12);
            options.DoNotDeliverBefore(deliveryDate);

            Assert.AreEqual(deliveryDate, options.GetDeliveryDate());
        }

        [Test]
        public void GetDeliveryDateShouldReturnNullWhenNoDateConfigured()
        {
            var options = new SendOptions();

            Assert.IsNull(options.GetDeliveryDate());
        }

        [Test]
        public void DelayDeliveryWithShouldThrowExceptionWhenDoNotDeliverBeforeAlreadyExists()
        {
            var options = new SendOptions();
            options.DoNotDeliverBefore(DateTimeOffset.Now.AddDays(1));

            Assert.Throws<InvalidOperationException>(() => options.DelayDeliveryWith(TimeSpan.FromDays(1)));
        }

        [Test]
        public void DoNotDeliverBeforeShouldThrowExceptionWhenDelayDeliveryWithAlreadyExists()
        {
            var options = new SendOptions();
            options.DelayDeliveryWith(TimeSpan.FromDays(1));

            Assert.Throws<InvalidOperationException>(() => options.DoNotDeliverBefore(DateTimeOffset.Now.AddDays(1)));
        }
    }
}