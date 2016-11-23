namespace NServiceBus
{
    using System.Threading.Tasks;
    using Features;
    using ObjectBuilder;
    using Pipeline;

    class SubscribeTerminator : PipelineTerminator<ISubscribeContext>
    {
        public SubscribeTerminator(RoutingComponent routing, IBuilder builder)
        {
            this.routing = routing;
            this.builder = builder;
        }

        protected override async Task Terminate(ISubscribeContext context)
        {
            foreach (var handler in routing.BuildSubscriptionHandlers(builder))
            {
                await handler.Subscribe(context.EventType, context.Extensions).ConfigureAwait(false);
            }
        }

        RoutingComponent routing;
        IBuilder builder;
    }
}