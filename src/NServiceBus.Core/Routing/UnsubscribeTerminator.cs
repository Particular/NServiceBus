namespace NServiceBus
{
    using System.Threading.Tasks;
    using Features;
    using ObjectBuilder;
    using Pipeline;

    class UnsubscribeTerminator : PipelineTerminator<IUnsubscribeContext>
    {
        public UnsubscribeTerminator(RoutingComponent routing, IBuilder builder)
        {
            this.routing = routing;
            this.builder = builder;
        }

        protected override async Task Terminate(IUnsubscribeContext context)
        {
            foreach (var handler in routing.BuildSubscriptionHandlers(builder))
            {
                await handler.Unsubscribe(context.EventType, context.Extensions).ConfigureAwait(false);
            }
        }

        RoutingComponent routing;
        IBuilder builder;
    }
}