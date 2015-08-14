namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.DelayedDelivery;
    using NServiceBus.DeliveryConstraints;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;

    class ApplyDelayedDeliveryConstraintBehavior:Behavior<OutgoingContext>
    {
        public override Task Invoke(OutgoingContext context, Func<Task> next)
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

            public DelayedDeliveryConstraint RequestedDelay { get; private set; }
        }
    }
}