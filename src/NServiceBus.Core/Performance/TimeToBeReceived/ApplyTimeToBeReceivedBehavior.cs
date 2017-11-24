﻿namespace NServiceBus
{
    using System;
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

        public Task Invoke(IOutgoingLogicalMessageContext context, Func<IOutgoingLogicalMessageContext, Task> next)
        {
            if (timeToBeReceivedMappings.TryGetTimeToBeReceived(context.Message.MessageType, out var timeToBeReceived))
            {
                context.Extensions.AddDeliveryConstraint(new DiscardIfNotReceivedBefore(timeToBeReceived));
                context.Headers[Headers.TimeToBeReceived] = timeToBeReceived.ToString();
            }

            return next(context);
        }

        readonly TimeToBeReceivedMappings timeToBeReceivedMappings;
    }
}