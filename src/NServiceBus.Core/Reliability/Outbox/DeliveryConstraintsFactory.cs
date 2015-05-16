namespace NServiceBus.Reliability.Outbox
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.DelayedDelivery;
    using NServiceBus.DeliveryConstraints;
    using NServiceBus.Performance.TimeToBeReceived;

    class DeliveryConstraintsFactory
    {
        public IEnumerable<DeliveryConstraint> DeserializeConstraints(Dictionary<string, string> options)
        {
            if (options.ContainsKey("NonDurable"))
            {
                yield return new NonDurableDelivery();
            }

            string deliverAt;
            if (options.TryGetValue("DeliverAt", out deliverAt))
            {
                yield return new DoNotDeliverBefore(DateTimeExtensions.ToUtcDateTime(deliverAt));
            }


            string delay;
            if (options.TryGetValue("DelayDeliveryFor", out delay))
            {
                yield return new DelayDeliveryWith(TimeSpan.Parse(delay));
            }

            string ttbr;

            if (options.TryGetValue("TimeToBeReceived", out ttbr))
            {
                yield return new DiscardIfNotReceivedBefore(TimeSpan.Parse(ttbr));
            }
        }
    }
}