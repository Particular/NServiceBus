using NServiceBus.Transports;

namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;

    class ThrowIfCannotDeferMessageBehavior : IBehavior<IRoutingContext, IRoutingContext>
    {
        public Task Invoke(IRoutingContext context, Func<IRoutingContext, Task> next)
        {
            if (context.Extensions.TryGet<OperationProperties>(out var properties))
            {

                if (properties.DelayDeliveryWith != null || properties.DoNotDeliverBefore != null)
                {
                    throw new InvalidOperationException(
                        "Cannot delay delivery of messages when there is no infrastructure support for delayed messages.");
                }
            }

            return next(context);
        }
    }
}