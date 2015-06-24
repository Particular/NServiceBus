namespace NServiceBus.Core.Tests.Reliability.Outbox
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.DelayedDelivery;
    using NServiceBus.DeliveryConstraints;
    using NServiceBus.Performance.TimeToBeReceived;
    using NServiceBus.Reliability.Outbox;
    using NUnit.Framework;

    public class DeliveryConstraintsFactoryTests
    {
        [Test]
        public void DeserializeNonDurable()
        {
            var options = new Dictionary<string, string>();

            new NonDurableDelivery().Serialize(options);


            Assert.IsInstanceOf<NonDurableDelivery>(new DeliveryConstraintsFactory().DeserializeConstraints(options).Single());
        }

        [Test]
        public void DeserializeDoNotDeliverBefore()
        {
            var options = new Dictionary<string, string>();

            var at = DateTime.UtcNow.AddDays(1);

            new DoNotDeliverBefore(at).Serialize(options);

            DoNotDeliverBefore constraint;

            new DeliveryConstraintsFactory().DeserializeConstraints(options).TryGet(out constraint);

            Assert.AreEqual(at.ToLongTimeString(),constraint.At.ToLongTimeString());
        }

        
        [Test]
        public void DeserializeDelayDeliveryWith()
        {
            var options = new Dictionary<string, string>();

            var delay = TimeSpan.Parse("10:00:00");

            new DelayDeliveryWith(delay).Serialize(options);

            DelayDeliveryWith constraint;

            new DeliveryConstraintsFactory().DeserializeConstraints(options).TryGet(out constraint);

            Assert.AreEqual(delay,constraint.Delay);
        }

        
        [Test]
        public void DeserializeDiscardIfNotReceivedBefore()
        {
            var options = new Dictionary<string, string>();

            var delay = TimeSpan.Parse("00:10:00");

            new DiscardIfNotReceivedBefore(delay).Serialize(options);

            DiscardIfNotReceivedBefore constraint;

            new DeliveryConstraintsFactory().DeserializeConstraints(options).TryGet(out constraint);

            Assert.AreEqual(delay,constraint.MaxTime);
        }
        
        
    }
}