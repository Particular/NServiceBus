namespace NServiceBus.Features
{
    using System;
    using System.Linq;
    using NServiceBus.Routing;
    using NServiceBus.Unicast.Routing;

    class RoutingAdapter : MessageRouter
    {
        readonly StaticMessageRouter router;

        public RoutingAdapter(StaticMessageRouter router)
        {
            this.router = router;
        }

        public override bool TryGetRoute(Type messageType, out string destination)
        {
            destination = router.GetDestinationFor(messageType).FirstOrDefault();

            return !string.IsNullOrEmpty(destination);
        }


    }
}