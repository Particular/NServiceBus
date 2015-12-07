namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;
    using NServiceBus.TransportDispatch;

    class ThrowIfCannotDeferMessageBehavior : Behavior<RoutingContext>
    {
        public override Task Invoke(RoutingContext context, Func<Task> next)
        {
            ApplyDelayedDeliveryConstraintBehavior.State delayState;
            if (context.Extensions.TryGet(out delayState))
            {
                throw new InvalidOperationException("Cannot delay delivery of messages when TimeoutManager is disabled or there is no infrastructure support for delayed messages.");
            }
            return next();
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
