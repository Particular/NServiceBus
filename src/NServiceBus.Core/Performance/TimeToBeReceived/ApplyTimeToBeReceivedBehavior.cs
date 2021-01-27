﻿namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Performance.TimeToBeReceived;
    using Pipeline;
    using Transport;

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
                context.Extensions.Get<DispatchProperties>().DiscardIfNotReceivedBefore = new DiscardIfNotReceivedBefore(timeToBeReceived);
                context.Headers[Headers.TimeToBeReceived] = timeToBeReceived.ToString();
            }

            return next(context);
        }

        readonly TimeToBeReceivedMappings timeToBeReceivedMappings;
    }
}