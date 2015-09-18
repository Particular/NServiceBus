namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using DeliveryConstraints;
    using Performance.TimeToBeReceived;
    using Pipeline;
    using Pipeline.Contexts;
    using TransportDispatch;

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
                context.SetHeader(Headers.TimeToBeReceived, timeToBeReceived.ToString());
            }

            return next();
        }

        TimeToBeReceivedMappings timeToBeReceivedMappings;
    }
}