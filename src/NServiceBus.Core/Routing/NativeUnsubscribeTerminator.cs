using System;
using System.Threading;

namespace NServiceBus
{
    using System.Threading.Tasks;
    using Pipeline;
    using Transport;

    class NativeUnsubscribeTerminator : PipelineTerminator<IUnsubscribeContext>
    {
        public NativeUnsubscribeTerminator(Func<IManageSubscriptions> subscriptionManager)
        {
            this.subscriptionManager = subscriptionManager;
        }

        protected override Task Terminate(IUnsubscribeContext context)
        {
            return subscriptionManager().Unsubscribe(context.EventType, context.Extensions, CancellationToken.None);
        }

        readonly Func<IManageSubscriptions> subscriptionManager;
    }
}