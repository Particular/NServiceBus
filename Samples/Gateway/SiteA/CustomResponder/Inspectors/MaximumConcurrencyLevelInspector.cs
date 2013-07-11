namespace SiteA.CustomResponder.Inspectors
{
    using NServiceBus.Unicast.Transport;

    public class MaximumConcurrencyLevelInspector : IEndpointInspector
    {
        private readonly ITransport transport;

        public MaximumConcurrencyLevelInspector(ITransport transport)
        {
            this.transport = transport;
        }

        public string GetStatusAsHtml()
        {
            return string.Format("Maximum concurrency level is: " + transport.MaximumConcurrencyLevel);
        }
    }
}