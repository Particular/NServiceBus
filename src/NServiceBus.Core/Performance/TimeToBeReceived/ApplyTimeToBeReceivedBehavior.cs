namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using DeliveryConstraints;
    using NServiceBus.Pipeline.OutgoingPipeline;
    using Performance.TimeToBeReceived;
    using Pipeline;

    class ApplyTimeToBeReceivedBehavior : Behavior<OutgoingLogicalMessageContext>
    {
        public ApplyTimeToBeReceivedBehavior(TimeToBeReceivedMappings timeToBeReceivedMappings)
        {
            this.timeToBeReceivedMappings = timeToBeReceivedMappings;
        }
        
        public override Task Invoke(OutgoingLogicalMessageContext context, Func<Task> next)
        {
            TimeSpan timeToBeReceived;

            if (timeToBeReceivedMappings.TryGetTimeToBeReceived(context.Message.MessageType, out timeToBeReceived))
            {
                context.AddDeliveryConstraint(new DiscardIfNotReceivedBefore(timeToBeReceived));
                context.Headers[Headers.TimeToBeReceived] = timeToBeReceived.ToString();
            }

            return next();
        }

        TimeToBeReceivedMappings timeToBeReceivedMappings;
    }
}