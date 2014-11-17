namespace NServiceBus
{
    using System;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Routing;
    using NServiceBus.Unicast;

    class DynamicRoutingBehavior : IBehavior<OutgoingContext>
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

        public class RoutingDistributorRegistration : RegisterStep
        {
            public RoutingDistributorRegistration()
                : base("DynamicRouting", typeof(DynamicRoutingBehavior), "Changes destination address to be dynamic. The address is retrieved using the selected dynamic routing.")
            {
                InsertAfter(WellKnownStep.MutateOutgoingTransportMessage);
                InsertAfterIfExists("LogOutgoingMessage");
                InsertBefore(WellKnownStep.DispatchMessageToTransport);
            }
        }
    }
}

