namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using DeliveryConstraints;
    using Performance.TimeToBeReceived;
    using Pipeline;

    class ApplyTimeToBeReceivedBehavior : Behavior<IOutgoingLogicalMessageContext>
    {
        public ApplyTimeToBeReceivedBehavior(TimeToBeReceivedMappings timeToBeReceivedMappings)
        {
            this.timeToBeReceivedMappings = timeToBeReceivedMappings;
        }
        
        public override Task Invoke(IOutgoingLogicalMessageContext context, Func<Task> next)
        {
            TimeSpan timeToBeReceived;

            if (timeToBeReceivedMappings.TryGetTimeToBeReceived(context.Message.MessageType, out timeToBeReceived))
            {
                context.Extensions.AddDeliveryConstraint(new DiscardIfNotReceivedBefore(timeToBeReceived));
                context.Headers[Headers.TimeToBeReceived] = timeToBeReceived.ToString();
            }

            return next();
        }

        TimeToBeReceivedMappings timeToBeReceivedMappings;
    }
}