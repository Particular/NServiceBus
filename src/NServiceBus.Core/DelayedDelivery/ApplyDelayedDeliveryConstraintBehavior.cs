namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.DelayedDelivery;
    using NServiceBus.DeliveryConstraints;
    using NServiceBus.Extensibility;
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
                state.MarkAsHandled();
            }

            return next();
        }

        public class State : OutgoingPipelineExtensionState
        {
            public State(DelayedDeliveryConstraint constraint)
            {
                RequestedDelay = constraint;
            }

            public DelayedDeliveryConstraint RequestedDelay { get; }
            protected override string GenerateErrorMessageWhenNotHandled()
            {
                return "Cannot delay delivery of messages when DelayedDelivery feature is disabled.";
            }
        }
    }
}