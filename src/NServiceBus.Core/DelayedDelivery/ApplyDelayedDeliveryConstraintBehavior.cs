﻿namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.DelayedDelivery;
    using NServiceBus.DeliveryConstraints;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.OutgoingPipeline;

    class ApplyDelayedDeliveryConstraintBehavior:Behavior<OutgoingLogicalMessageContext>
    {
        public override Task Invoke(OutgoingLogicalMessageContext context, Func<Task> next)
        {
            State state;

            if (context.Extensions.TryGet(out state))
            {
                context.AddDeliveryConstraint(state.RequestedDelay);
            }

            return next();
        }

        public class State
        {
            public State(DelayedDeliveryConstraint constraint)
            {
                RequestedDelay = constraint;
            }

            public DelayedDeliveryConstraint RequestedDelay { get; }
        }
    }
}