namespace NServiceBus
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using DelayedDelivery;
    using DeliveryConstraints;
    using Pipeline;

    class ThrowIfCannotDeferMessageBehavior : IBehavior<IRoutingContext, IRoutingContext>
    {
        public Task Invoke(IRoutingContext context, Func<IRoutingContext, Task> next)
        {
            var deliveryConstraints = context.Extensions.GetDeliveryConstraints();
            if (deliveryConstraints.Any(constraint => constraint is DelayedDeliveryConstraint))
            {
                throw new InvalidOperationException("Cannot delay delivery of messages when TimeoutManager is disabled or there is no infrastructure support for delayed messages.");
            }

            return next(context);
        }
    }
}