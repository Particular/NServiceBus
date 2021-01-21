namespace NServiceBus
{
    using Transport;
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Pipeline;

    class ThrowIfCannotDeferMessageBehavior : IBehavior<IRoutingContext, IRoutingContext>
    {
        public Task Invoke(IRoutingContext context, Func<IRoutingContext, CancellationToken, Task> next, CancellationToken token)
        {
            if (context.Extensions.TryGet<DispatchProperties>(out var properties))
            {

                if (properties.DelayDeliveryWith != null || properties.DoNotDeliverBefore != null)
                {
                    throw new InvalidOperationException(
                        "Cannot delay delivery of messages when there is no infrastructure support for delayed messages.");
                }
            }

            return next(context, token);
        }
    }
}