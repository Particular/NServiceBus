namespace NServiceBus
{
    using System;
    using NServiceBus.DelayedDelivery;
    using NServiceBus.DeliveryConstraints;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;

    class ApplyDelayedDeliveryConstraintBehavior:Behavior<OutgoingContext>
    {
        public override void Invoke(OutgoingContext context, Action next)
        {
            State state;

            if (context.TryGet(out state))
            {
                context.AddDeliveryConstraint(state.RequestedDelay);
            }

            next();
        }

        public class State
        {
            public State(DelayedDeliveryConstraint constraint)
            {
                RequestedDelay = constraint;
            }

            public DelayedDeliveryConstraint RequestedDelay { get; private set; }
        }
    }
}