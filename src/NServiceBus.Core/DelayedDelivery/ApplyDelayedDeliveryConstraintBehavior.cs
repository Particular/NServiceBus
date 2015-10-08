namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using DelayedDelivery;
    using DeliveryConstraints;
    using Pipeline;
    using Pipeline.Contexts;

    class ApplyDelayedDeliveryConstraintBehavior : Behavior<OutgoingLogicalMessageContext>
    {
        public override Task Invoke(OutgoingLogicalMessageContext context, Func<Task> next)
        {
            State state;

            if (context.TryGet(out state))
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