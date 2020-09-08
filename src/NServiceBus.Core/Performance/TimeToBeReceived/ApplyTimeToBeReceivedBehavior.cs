namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using DeliveryConstraints;
    using Performance.TimeToBeReceived;
    using Pipeline;

    class ApplyTimeToBeReceivedBehavior : IBehavior<IOutgoingLogicalMessageContext, IOutgoingLogicalMessageContext>
    {
        public ApplyTimeToBeReceivedBehavior(TimeToBeReceivedMappings timeToBeReceivedMappings)
        {
            this.timeToBeReceivedMappings = timeToBeReceivedMappings;
        }

        public Task Invoke(IOutgoingLogicalMessageContext context, Func<IOutgoingLogicalMessageContext, CancellationToken, Task> next, CancellationToken cancellationToken)
        {
            if (timeToBeReceivedMappings.TryGetTimeToBeReceived(context.Message.MessageType, out var timeToBeReceived))
            {
                context.Extensions.AddDeliveryConstraint(new DiscardIfNotReceivedBefore(timeToBeReceived));
                context.Headers[Headers.TimeToBeReceived] = timeToBeReceived.ToString();
            }

            return next(context, cancellationToken);
        }

        readonly TimeToBeReceivedMappings timeToBeReceivedMappings;
    }
}