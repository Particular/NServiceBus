namespace NServiceBus
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading;
    using System.Threading.Tasks;
    using DeliveryConstraints;
    using Pipeline;

    class DetermineMessageDurabilityBehavior : IBehavior<IOutgoingLogicalMessageContext, IOutgoingLogicalMessageContext>
    {
        public DetermineMessageDurabilityBehavior(Func<Type, bool> convention)
        {
            this.convention = convention;
            durabilityCache = new ConcurrentDictionary<Type, bool>();
        }

        public Task Invoke(IOutgoingLogicalMessageContext context, Func<IOutgoingLogicalMessageContext, CancellationToken, Task> next, CancellationToken cancellationToken)
        {
            if (durabilityCache.GetOrAdd(context.Message.MessageType, t => convention(t)))
            {
                context.Extensions.AddDeliveryConstraint(new NonDurableDelivery());

                context.Headers[Headers.NonDurableMessage] = true.ToString();
            }

            return next(context, cancellationToken);
        }

        readonly Func<Type, bool> convention;
        readonly ConcurrentDictionary<Type, bool> durabilityCache;
    }
}