namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.DeliveryConstraints;
    using NServiceBus.OutgoingPipeline;
    using NServiceBus.Performance.TimeToBeReceived;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.TransportDispatch;

    class ApplyTimeToBeReceivedBehavior : Behavior<OutgoingContext>
    {
        public ApplyTimeToBeReceivedBehavior(TimeToBeReceivedMappings timeToBeReceivedMappings)
        {
            this.timeToBeReceivedMappings = timeToBeReceivedMappings;
        }
        
        public override Task Invoke(OutgoingContext context, Func<Task> next)
        {
            TimeSpan timeToBeReceived;

            if (timeToBeReceivedMappings.TryGetTimeToBeReceived(context.GetMessageType(), out timeToBeReceived))
            {
                context.AddDeliveryConstraint(new DiscardIfNotReceivedBefore(timeToBeReceived));
                context.SetHeader(Headers.TimeToBeReceived, timeToBeReceived.ToString());
            }

            return next();
        }

        TimeToBeReceivedMappings timeToBeReceivedMappings;
    }
}