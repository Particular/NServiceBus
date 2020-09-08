namespace NServiceBus
{
    using System.Threading;
    using System.Threading.Tasks;
    using Pipeline;
    using Transport;

    class NativeUnsubscribeTerminator : PipelineTerminator<IUnsubscribeContext>
    {
        public NativeUnsubscribeTerminator(IManageSubscriptions subscriptionManager)
        {
            this.subscriptionManager = subscriptionManager;
        }

        protected override Task Terminate(IUnsubscribeContext context, CancellationToken cancellationToken)
        {
            return subscriptionManager.Unsubscribe(context.EventType, context.Extensions);
        }

        readonly IManageSubscriptions subscriptionManager;
    }
}