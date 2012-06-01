namespace SiteA.CustomResponder.Inspectors
{
    using NServiceBus.Unicast.Transport;

    public class NumberOfThreadsInspector:IEndpointInspector
    {
        readonly ITransport transport;

        public NumberOfThreadsInspector(ITransport transport)
        {
            this.transport = transport;
        }

        public string GetStatusAsHtml()
        {
            return string.Format("Current number of worker threads is: " + transport.NumberOfWorkerThreads);
        }
    }
}