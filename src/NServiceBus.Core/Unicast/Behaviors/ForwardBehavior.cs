namespace NServiceBus
{
    using System;
    using NServiceBus.Routing;
    using NServiceBus.Unicast;
    using Pipeline;
    using Pipeline.Contexts;
    using Transports;

    class ForwardBehavior : IBehavior<IncomingContext>
    {
        public IAuditMessages MessageAuditer { get; set; }

        public Address ForwardReceivedMessagesTo { get; set; }

        public TimeSpan? TimeToBeReceivedOnForwardedMessages { get; set; }

        public DynamicRoutingProvider RoutingProvider { get; set; }        

        public void Invoke(IncomingContext context, Action next)
        {
            next();

            var address = RoutingProvider.GetRouteAddress(ForwardReceivedMessagesTo);

            MessageAuditer.Audit(new SendOptions(address)
            {
                TimeToBeReceived = TimeToBeReceivedOnForwardedMessages
            }, context.PhysicalMessage);

        }

        public class Registration : RegisterStep
        {
            public Registration()
                : base("ForwardMessageTo", typeof(ForwardBehavior), "Forwards message to the specified queue in the UnicastBus config section.")
            {
                InsertBefore(WellKnownStep.ExecuteUnitOfWork);
            }
        }
    }
}