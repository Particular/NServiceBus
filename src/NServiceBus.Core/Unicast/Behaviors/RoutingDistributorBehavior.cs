namespace NServiceBus
{
    using System;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Unicast;
    using NServiceBus.Unicast.Routing;

    class RoutingDistributorBehavior : IBehavior<OutgoingContext>
    {
        public IRouterDistributor Routing { get; set; }

        public Func<Address, string> Translator { get; set; }

        public void Invoke(OutgoingContext context, Action next)
        {
            var sendOptions = context.DeliveryOptions as SendOptions;

            if (sendOptions == null)
            {
                next();
                return;
            }

            sendOptions.Destination = GetNextAddress(sendOptions.Destination);
            next();
        }

        Address GetNextAddress(Address destination)
        {
            string address;
            if (!Routing.TryGetRouteAddress(Translator(destination), out address))
            {
                return destination;
            }

            return Address.Parse(address);
        }
    }
}

