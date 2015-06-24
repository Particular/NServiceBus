namespace NServiceBus
{
    using System;
    using NServiceBus.DeliveryConstraints;
    using NServiceBus.Performance.TimeToBeReceived;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.TransportDispatch;

    class ApplyTimeToBeReceivedBehavior:Behavior<OutgoingContext>
    {
        public ApplyTimeToBeReceivedBehavior(TimeToBeReceivedMappings timeToBeReceivedMappings)
        {
            this.timeToBeReceivedMappings = timeToBeReceivedMappings;
        }
        
        public override void Invoke(OutgoingContext context, Action next)
        {
            TimeSpan timeToBeReceived;

            if (timeToBeReceivedMappings.TryGetTimeToBeReceived(context.MessageType, out timeToBeReceived))
            {
                context.AddDeliveryConstraint(new DiscardIfNotReceivedBefore(timeToBeReceived));
                context.SetHeader(Headers.TimeToBeReceived, timeToBeReceived.ToString());
            }

            next();
        }

        readonly TimeToBeReceivedMappings timeToBeReceivedMappings;
    }
}