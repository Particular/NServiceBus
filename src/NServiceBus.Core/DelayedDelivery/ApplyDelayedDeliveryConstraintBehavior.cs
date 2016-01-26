namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.DelayedDelivery;
    using NServiceBus.DeliveryConstraints;
    using NServiceBus.Pipeline;

    class ApplyDelayedDeliveryConstraintBehavior : Behavior<IOutgoingLogicalMessageContext>
    {
        public override Task Invoke(IOutgoingLogicalMessageContext context, Func<Task> next)
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