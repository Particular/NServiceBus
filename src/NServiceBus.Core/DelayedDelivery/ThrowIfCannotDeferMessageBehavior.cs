namespace NServiceBus
{
    using System;
    using NServiceBus.Pipeline;
    using NServiceBus.TransportDispatch;

    class ThrowIfCannotDeferMessageBehavior : Behavior<DispatchContext>
    {
        public override void Invoke(DispatchContext context, Action next)
        {
            ApplyDelayedDeliveryConstraintBehavior.State delayState;
            if (context.TryGet(out delayState))
            {
                throw new InvalidOperationException("Cannot delay delivery of messages when TimeoutManager is disabled or there is no infrastructure support for delayed messages.");
            }
            next();
        }

        public class Registration : RegisterStep
        {
            public Registration()
                : base("ThrowIfCannotDeferMessage", typeof(ThrowIfCannotDeferMessageBehavior), "Throws an exception if an attempt is made to defer a message without infrastructure support.")
            {
            }
        }
    }
}
