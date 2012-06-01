namespace SiteA.CustomResponder.Inspectors
{
    using NServiceBus.Unicast;

    public class AuditInspector : IEndpointInspector
    {
        readonly UnicastBus bus;

        public AuditInspector(UnicastBus bus)
        {
            this.bus = bus;
        }


        public string GetStatusAsHtml()
        {
            if (bus.ForwardReceivedMessagesTo == null)
                return "Audit is turned off";

            return string.Format("Endpoint sending audit messages to: " + bus.ForwardReceivedMessagesTo);
        }
    }
}