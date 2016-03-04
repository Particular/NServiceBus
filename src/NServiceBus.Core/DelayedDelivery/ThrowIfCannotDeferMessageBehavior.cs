namespace NServiceBus
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using DelayedDelivery;
    using DeliveryConstraints;
    using Pipeline;

    class ThrowIfCannotDeferMessageBehavior : Behavior<IRoutingContext>
    {
        public override Task Invoke(IRoutingContext context, Func<Task> next)
        {
            var deliveryConstraints = context.Extensions.GetDeliveryConstraints();
            if (deliveryConstraints.Any(constraint => constraint is DelayedDeliveryConstraint))
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